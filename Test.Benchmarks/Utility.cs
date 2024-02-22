﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Bootstrapper;
using SampleApp.Bootstrapper.Automapper;

namespace Test.Benchmarks;

public static class Utility
{
    public static readonly IConfigurationRoot Config = Config ?? Support.Utility.BuildConfiguration().Build();
    private static readonly IServiceCollection _services = new ServiceCollection();
    private static IServiceProvider? ServiceProvider;

    static Utility()
    {
        //bootstrapper service registrations - infrastructure, domain, application 
        _services
            .RegisterInfrastructureServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        //configure & register Automapper, application and infrastructure mapping profiles
        ConfigureAutomapper.Configure(_services);
    }

    public static IServiceCollection GetServiceCollection()
    {
        return _services;
    }

    public static IServiceProvider GetServiceProvider()
    {
        if (ServiceProvider != null) return ServiceProvider;
        //build IServiceProvider for subsequent use finding/injecting services
        ServiceProvider = _services.BuildServiceProvider(validateScopes: true);
        return ServiceProvider;
    }

    private static readonly Random random = new();
    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
