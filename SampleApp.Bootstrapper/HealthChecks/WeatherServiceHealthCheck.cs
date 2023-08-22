﻿using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SampleApp.Bootstrapper.HealthChecks;

public class WeatherServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<WeatherServiceHealthCheck> _logger;
    private readonly IWeatherService _weatherService;

    public WeatherServiceHealthCheck(ILogger<WeatherServiceHealthCheck> logger, IWeatherService weatherService)
    {
        _logger = logger;
        _weatherService = weatherService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WeatherServiceHealthCheck - Start");

        var status = HealthStatus.Healthy;
        try
        {
            var response = _weatherService.GetCurrentAsync("San Diego, CA");
            if (response == null) status = HealthStatus.Unhealthy;
            _logger.LogInformation("WeatherServiceHealthCheck - Complete");
        }
        catch (Exception ex)
        {
            status = HealthStatus.Unhealthy;
            _logger.LogError(ex, "WeatherServiceHealthCheck - Error");
        }

        return Task.FromResult(new HealthCheckResult(status,
            description: $"WeatherService is {status}.",
            exception: null,
            data: null));
    }
}
