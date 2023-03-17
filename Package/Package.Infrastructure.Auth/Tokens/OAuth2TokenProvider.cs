﻿using LazyCache;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Http.Tokens;
public class OAuth2TokenProvider : IOAuth2TokenProvider
{
    private readonly OAuth2Options _oauth2Options;
    private readonly IAppCache _memoryCache;

    public OAuth2TokenProvider(IOptions<OAuth2Options> oauth2Options, IAppCache memoryCache)
    {
        _oauth2Options = oauth2Options.Value;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var accessToken = await _memoryCache.GetOrAddAsync("access_token", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            //todo: inject typed client
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_oauth2Options.ClientId}:{_oauth2Options.ClientSecret}")));

            var content = new FormUrlEncodedContent(GetTokenRequestBody());

            var response = await httpClient.PostAsync(_oauth2Options.TokenEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<OAuth2TokenResponse>(responseContent);

            return tokenResponse.AccessToken;
        });

        return accessToken;
    }

    private IEnumerable<KeyValuePair<string, string>> GetTokenRequestBody()
    {
        var requestBody = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", _oauth2Options.GrantType),
            new KeyValuePair<string, string>("client_id", _oauth2Options.ClientId),
            new KeyValuePair<string, string>("client_secret", _oauth2Options.ClientSecret)
        };

        if (!string.IsNullOrEmpty(_oauth2Options.Scope))
        {
            requestBody.Add(new KeyValuePair<string, string>("scope", _oauth2Options.Scope));
        }

        if (!string.IsNullOrEmpty(_oauth2Options.Username) && !string.IsNullOrEmpty(_oauth2Options.Password))
        {
            requestBody.Add(new KeyValuePair<string, string>("username", _oauth2Options.Username));
            requestBody.Add(new KeyValuePair<string, string>("password", _oauth2Options.Password));
        }

        return requestBody;
    }

    private sealed class OAuth2TokenResponse
    {
        public string? AccessToken { get; set; }
    }
}

public class OAuth2Options
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TokenEndpoint { get; set; }
    public string GrantType { get; set; }
    public string Scope { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
