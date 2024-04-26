using Functions;
using Functions.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleApp.Bootstrapper;

/// <summary>
/// https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
/// retries - https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-error-pages?tabs=fixed-delay%2Cin-process&pivots=programming-language-csharp#retries
/// </summary>
/// 

const string SERVICE_NAME = "Functions v4/net8";

ILogger<Program> loggerStartup;
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
    //builder.AddApplicationInsights();
});
loggerStartup = loggerFactory.CreateLogger<Program>();

try
{
    loggerStartup.LogInformation("{ServiceName} - Startup.", SERVICE_NAME);

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            // NOTE: It's important to add json config sources before the call to ConfigureFunctionsWorkerDefaults as this
            // adds environment variables into configuration enabling overrides by azure configuration settings.
            config.AddJsonFile("appsettings.json", optional: true);
        })
        .ConfigureServices((hostContext, services) =>
        {
            var config = hostContext.Configuration;

            services
                //app insights telemetry logging for non-http service
                //https://github.com/devops-circle/Azure-Functions-Logging-Tests/blob/master/Func.Isolated.Net7.With.AI/Program.cs
                .AddApplicationInsightsTelemetryWorkerService(config)
                .ConfigureFunctionsApplicationInsights()
                .Configure<LoggerFilterOptions>(options =>
                {
                    var toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

                    if (toRemove is not null)
                    {
                        options.Rules.Remove(toRemove);
                    }
                })
                //needed for logging to app insights?
                //.AddLogging(builder =>
                //{
                //    builder.AddApplicationInsights(configTelem =>
                //    {
                //        configTelem.ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString");
                //    },
                //    options => { });
                //})

                //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
                .RegisterInfrastructureServices(config)
                //domain services
                .RegisterDomainServices(config)
                //app servives
                .RegisterApplicationServices(config)
                //BackgroundTaskQueue needed by other services
                .RegisterBackgroundServices(config)
                //Function app specific registrations
                .AddTransient<IDatabaseService, DatabaseService>()
                //Configuration, enables injecting IOptions<>
                .Configure<Settings1>(config.GetSection("Settings1"));
        })
        .ConfigureFunctionsWorkerDefaults(builder =>
        {
            builder
                .UseMiddleware<GlobalExceptionHandler>();
        })
        .ConfigureLogging((hostingContext, logging) =>
        {
            // Make sure the configuration of the appsettings.json file is picked up.
            //https://github.com/devops-circle/Azure-Functions-Logging-Tests/blob/master/Func.Isolated.Net7.With.AI/Program.cs
            logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
        })
        .Build();

    await host.RunStartupTasks();
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