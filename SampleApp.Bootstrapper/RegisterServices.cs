﻿using Application.Contracts.Services;
using Application.Services;
using CorrelationId.HttpClient;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundService;
using Polly;
using Polly.Extensions.Http;
using SampleApp.BackgroundServices.Scheduler;
using SampleApp.Bootstrapper.Automapper;
using SampleApp.Bootstrapper.HealthChecks;
using SampleApp.Bootstrapper.StartupTasks;
using System;
using System.Net.Http;

namespace SampleApp.Bootstrapper;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Register/configure domain services in the container
    /// </summary>
    public static IServiceCollection RegisterDomainServices(this IServiceCollection services, IConfiguration config)
    {
        _ = services.GetHashCode();
        _ = config.GetHashCode();
        return services;
    }

    /// <summary>
    /// Register/configure services in the container
    /// </summary>
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(config.GetSection(TodoServiceSettings.ConfigSectionName));

        return services;
    }

    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        //LazyCache.AspNetCore, lightweight wrapper around memorycache; prevent race conditions when multiple threads attempt to refresh empty cache item
        services.AddLazyCache();

        //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-6.0
        string? connectionString = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "redis1";
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); //local server only, not distributed. Helps with tests
        }

        //AutoMapper Configuration - map domain <-> application 
        ConfigureAutomapper.Configure(services);

        //Infrastructure Services
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

        services.Configure<WeatherServiceSettings>(config.GetSection(WeatherServiceSettings.ConfigSectionName));

        //external weather service
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.BaseAddress = new Uri(config.GetValue<string>("WeatherSettings:BaseUrl")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherSettings:Key")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherSettings:Host")!);
        })
        //integration testing breaks since there is no header to propagate (just a sample, doesn't apply to RapidAPI)
        //.AddHeaderPropagation() 
        //.AddCorrelationIdForwarding()

        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        //Database 
        connectionString = config.GetConnectionString("TodoDbContextTrxn");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //multiple in memory DbContexts use the same DB
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            var inMemoryDatabaseRoot = new InMemoryDatabaseRoot();

            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextTrxn>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot));

            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextQuery>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot));
        }
        else
        {
            services.AddDbContextPool<TodoDbContextTrxn>(options =>
                options.UseSqlServer(connectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    })
                );

            services.AddDbContextPool<TodoDbContextQuery>(options =>
                options.UseSqlServer(connectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    })
                );
        }

        //background services
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        services.AddHostedService<CronService>();
        services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(config.GetSection(CronServiceSettings.ConfigSectionName));


        return services;
    }

    /// <summary>
    /// Used at runtime for http services; not used for Workers/Functions/Tests
    /// </summary>
    public static IServiceCollection RegisterRuntimeServices(this IServiceCollection services, IConfiguration config)
    {
        //HealthChecks - having infrastructure references
        //tag full will run when hitting health/full
        services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: new[] { "full", "memory" }, thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: new[] { "full", "db" })
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: new[] { "full", "db" })
            .AddCheck<ExternalServiceHealthCheck>("External Service", tags: new[] { "full", "extservice" });

        //background services - infrastructure
        services.AddHostedService<BackgroundTaskService>();

        //StartupTasks - executes once at startup
        services.AddTransient<IStartupTask, LoadCache>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int numRetries = 5, int secDelay = 2) //, HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        Random jitterer = new();
        return HttpPolicyExtensions
            .HandleTransientHttpError() //known transient errors
                                        //.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // other errors to consider transient (retry-able)
            .WaitAndRetryAsync(numRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(secDelay, retryAttempt))
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))
            );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int numConsecutiveFaults = 10, int secondsToWait = 30)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(numConsecutiveFaults, TimeSpan.FromSeconds(secondsToWait));
    }
}
