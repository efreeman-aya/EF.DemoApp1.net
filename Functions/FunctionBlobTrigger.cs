using Functions.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionBlobTrigger
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionBlobTrigger> _logger;
    private readonly Settings1 _settings;
    private readonly IDatabaseService _dbService;

    public FunctionBlobTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings, IDatabaseService dbService)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionBlobTrigger>();
        _settings = settings.Value;
        _dbService = dbService;
    }

    /// <summary>
    /// large blobs - dont want the fileContent as string
    /// If all (default) 5 tries fail, Azure Functions adds a message to a Storage queue named webjobs-blobtrigger-poison
    /// </summary>
    /// <param name="fileContent"></param>
    /// <param name="fileName"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [Function("BlobTrigger")]
    public async Task Run([BlobTrigger("%BlobContainer%/{fileName}", Connection = "StorageBlob1")] string fileContent, string fileName)
    {
        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();
        _ = _dbService.GetHashCode();
        _ = fileContent.GetHashCode();

        _logger.Log(LogLevel.Information, "BlobTrigger - Start {FileName}", fileName);

        //await some service call
        await Task.CompletedTask;

        _logger.Log(LogLevel.Information, "BlobTrigger - Finish {FileName}", fileName);
    }

}
