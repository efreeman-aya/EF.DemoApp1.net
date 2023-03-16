﻿namespace Infrastructure.RapidApi.WeatherApi;
public class WeatherServiceSettings
{
    public const string ConfigSectionName = "WeatherSettings";
    public string BaseUrl { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Host { get; set; } = null!;
}
