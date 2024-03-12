﻿using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Common;
using Test.Support;

//https://github.com/dotnet/BenchmarkDotNet

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ValidatorBenchmarks : DbIntegrationTestBase
{
    private IValidationHelper _validationHelper = null!;

    //[Params(5, 10)]
    //public int NameLength { get; set; }

    [Benchmark]
    public async Task<FluentValidation.Results.ValidationResult?> TodoItemValidateAsync()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            return await _validationHelper.ValidateAsync(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
            return null;
        }
    }

    [Benchmark]
    public async Task TodoItemValidateAndThrowAsync()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            await _validationHelper.ValidateAndThrowAsync(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
        }
    }

    //BenchmarkDotNet does not support async setup/teardown
    //https://github.com/dotnet/BenchmarkDotNet/issues/1738#issuecomment-1687832731

    [IterationSetup]
    public static void IterationSetup()
    {
        ResetDatabaseAsync().GetAwaiter().GetResult();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        StartContainerAsync().GetAwaiter().GetResult();
        ConfigureTestInstanceAsync().GetAwaiter().GetResult();
        _validationHelper = ServiceScope.ServiceProvider.GetRequiredService<IValidationHelper>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        StopContainerAsync().GetAwaiter().GetResult();
    }

    //set up infrastructure not using DbIntegrationTestBase/TestContainer
    //[GlobalSetup]
    //public void GlobalSetup()
    //{
    //    ServiceCollection services = [];
    //    //bootstrapper service registrations - infrastructure, domain, application 
    //    services
    //        .RegisterInfrastructureServices(Config)
    //        .RegisterBackgroundServices(Config)
    //        .RegisterDomainServices(Config)
    //        .RegisterApplicationServices(Config);
    //    services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
    //    _serviceScope = Services.CreateScope(); //needed for injecting scoped services
    //    _validationHelper = _serviceScope.ServiceProvider.GetRequiredService<IValidationHelper>();
    //}


}
