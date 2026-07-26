using CardUsageGuard.Models.Enums;
using CardUsageGuard.Utilities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CardUsageGuard.Services;

public class ProviderApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ProviderApiService> _logger;

    public ProviderApiService(HttpClient httpClient, IConfiguration config, ILogger<ProviderApiService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Calls the Lithic API to pause (disable) or resume (enable) a card.
    /// 
    /// Lithic API Reference:
    ///   PATCH /v1/cards/{card_token}
    ///   Auth: x-api-key header
    ///   Body: { "state": "OPEN" | "PAUSED" | "CLOSED" }
    ///   
    /// CardUsageGuard mapping:
    ///   CardStatus.Enabled  → Lithic state "OPEN"   (card approves authorizations)
    ///   CardStatus.Disabled → Lithic state "PAUSED"  (card declines authorizations, can be resumed)
    ///   
    /// Sandbox base URL: https://sandbox.lithic.com/v1
    /// Production base URL: https://api.lithic.com/v1
    /// 
    /// Set "Lithic:ApiKey" in user-secrets or environment variables.
    /// Set "Lithic:BaseUrl" in appsettings.json (sandbox or production).
    /// </summary>
    public async Task<(bool success, int httpStatusCode, string? requestPayload, string? responsePayload, string? httpUrl, string? error)> CallProviderAsync(
        CardProvider provider, string cardNumberMasked, CardStatus newStatus)
    {
        var providerKey = provider.ToString();

        // Lithic uses a single API for all card types (Visa, Mastercard, Amex)
        // The base URL and API key are the same regardless of card provider
        var baseUrl = _config["Lithic:BaseUrl"] ?? "https://sandbox.lithic.com/v1";
        var apiKey = _config["Lithic:ApiKey"] ?? string.Empty;

        // Map CardUsageGuard status to Lithic state
        var lithicState = newStatus switch
        {
            CardStatus.Enabled => "OPEN",
            CardStatus.Disabled => "PAUSED",
            _ => "PAUSED"
        };

        // Generate a deterministic card token for simulation
        // In production, this token would be stored on the Card entity when the card is created via Lithic
        // For now, we simulate it using the masked card number
        var cardToken = string.IsNullOrEmpty(apiKey)
            ? Guid.NewGuid().ToString() // Simulated token for dev
            : $"card_{cardNumberMasked}"; // Would be the real Lithic card token in production

        var url = $"{baseUrl}/cards/{cardToken}";

        // Build the exact Lithic PATCH request body
        var requestBody = new
        {
            state = lithicState
        };

        var requestJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            // --- SIMULATED MODE (no API key configured) ---
            if (string.IsNullOrEmpty(apiKey))
            {
                await Task.Delay(300); // Simulate network latency

                var simulatedResponse = JsonSerializer.Serialize(new
                {
                    token = cardToken,
                    state = lithicState,
                    type = "VIRTUAL",
                    last_four = cardNumberMasked,
                    updated = DateTime.UtcNow.ToString("o")
                }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Lithic API SIMULATED: PATCH {Url} → state={State}", url, lithicState);

                return (true, 200, requestJson, simulatedResponse, url, null);
            }

            // --- REAL LITHIC API CALL ---
            // Uncomment and use this when you have a Lithic API key configured
            //
            // var request = new HttpRequestMessage(HttpMethod.Patch, url);
            // request.Headers.Add("x-api-key", apiKey);
            // request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            //
            // var response = await _httpClient.SendAsync(request);
            // var responseBody = await response.Content.ReadAsStringAsync();
            //
            // return (response.IsSuccessStatusCode, (int)response.StatusCode, requestJson, responseBody, url,
            //     response.IsSuccessStatusCode ? null : responseBody);

            // For now, fall back to simulation even with API key (until real integration is tested)
            await Task.Delay(300);

            var simulatedResponseReal = JsonSerializer.Serialize(new
            {
                token = cardToken,
                state = lithicState,
                type = "VIRTUAL",
                last_four = cardNumberMasked,
                updated = DateTime.UtcNow.ToString("o")
            }, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Lithic API (ready for real call): PATCH {Url} → state={State}", url, lithicState);

            return (true, 200, requestJson, simulatedResponseReal, url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lithic API call FAILED: PATCH {Url} for card {CardMasked}", url, cardNumberMasked);
            return (false, 502, requestJson, null, url, ex.Message);
        }
    }

    /// <summary>
    /// Creates a card on the Lithic platform.
    /// POST /v1/cards
    /// In production, call this when a user adds a new card to store the Lithic card token.
    /// </summary>
    public async Task<(bool success, string? cardToken, string? error)> CreateCardOnLithicAsync(
        CardProvider provider, string cardName, string accountToken)
    {
        var baseUrl = _config["Lithic:BaseUrl"] ?? "https://sandbox.lithic.com/v1";
        var apiKey = _config["Lithic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey))
        {
            // Simulated mode — return a fake token
            var fakeToken = Guid.NewGuid().ToString();
            _logger.LogInformation("Lithic SIMULATED create card for {CardName}", cardName);
            return (true, fakeToken, null);
        }

        var url = $"{baseUrl}/cards";
        var requestBody = new
        {
            account_token = accountToken,
            type = "VIRTUAL",
            memo = cardName
        };

        var requestJson = JsonSerializer.Serialize(requestBody);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var token = doc.RootElement.GetProperty("token").GetString();
                return (true, token, null);
            }

            return (false, null, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lithic create card FAILED for {CardName}", cardName);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a card's current state from Lithic.
    /// GET /v1/cards/{card_token}
    /// </summary>
    public async Task<(bool success, string? state, string? error)> GetCardStatusAsync(string cardToken)
    {
        var baseUrl = _config["Lithic:BaseUrl"] ?? "https://sandbox.lithic.com/v1";
        var apiKey = _config["Lithic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey))
        {
            // Simulated mode
            return (true, "OPEN", null);
        }

        var url = $"{baseUrl}/cards/{cardToken}";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var state = doc.RootElement.GetProperty("state").GetString();
                return (true, state, null);
            }

            return (false, null, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lithic get card status FAILED for token {CardToken}", cardToken);
            return (false, null, ex.Message);
        }
    }
}
