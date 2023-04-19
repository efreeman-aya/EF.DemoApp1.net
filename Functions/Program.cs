using Functions;
using Functions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleApp.Bootstrapper;

//namespace Functions;

/// <summary>
/// https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
/// net7 now supported in isolated mode - https://devblogs.microsoft.com/dotnet/dotnet-7-comes-to-azure-functions/
/// </summary>
/// 

const string SERVICE_NAME = "Functions net7/v4";

ILogger<Program> loggerStartup;
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
    builder.AddApplicationInsights();
});
loggerStartup = loggerFactory.CreateLogger<Program>();

try
{
    loggerStartup.LogInformation("{ServiceName} - Startup.", SERVICE_NAME);

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            var config = hostContext.Configuration;

            services
                //app insights telemetry logging for non-http service
                .AddApplicationInsightsTelemetryWorkerService(config)
                //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
                .RegisterInfrastructureServices(config)
                //domain services
                .RegisterDomainServices(config)
                //app servives
                .RegisterApplicationServices(config)
                //function app specific registrations
                .AddTransient<IDatabaseService, DatabaseService>()
                .Configure<Settings1>(config.GetSection("Settings1"));
        })
        .ConfigureFunctionsWorkerDefaults(workerApplication =>
        {
            workerApplication.UseMiddleware<GlobalExceptionHandler>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{ServiceName} - Host terminated unexpectedly.", SERVICE_NAME);
}
finally
{
    loggerStartup.LogInformation("{ServiceName} - Ending application.", SERVICE_NAME);
}