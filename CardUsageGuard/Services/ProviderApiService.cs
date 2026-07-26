using CardUsageGuard.Models.Enums;
using CardUsageGuard.Utilities;
using System.Net.Http.Json;

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
    /// Calls the card provider's API to block/unblock a card.
    /// Returns the full request and response for audit logging.
    /// Currently SIMULATED — returns a mock success response.
    /// </summary>
    public async Task<(bool success, int httpStatusCode, string? requestPayload, string? responsePayload, string? httpUrl, string? error)> CallProviderAsync(
        CardProvider provider, string cardNumberMasked, CardStatus newStatus)
    {
        var providerKey = provider.ToString();
        var url = _config[$"ProviderApi:{providerKey}:Url"] ?? "https://api.placeholder-provider.com/v1/cards/status";
        var action = newStatus == CardStatus.Enabled ? "unblock" : "block";

        // Build the actual request body that would be sent to the provider
        var requestBody = new
        {
            cardNumber = CardMaskingUtility.MaskCardNumber(cardNumberMasked),
            action,
            status = newStatus.ToString(),
            provider = providerKey
        };

        var requestJson = System.Text.Json.JsonSerializer.Serialize(requestBody, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            // --- SIMULATED PROVIDER CALL ---
            // In production, uncomment the real HTTP call below:
            //
            // _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config[$"ProviderApi:{providerKey}:ApiKey"] ?? "");
            // var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            // var responseBody = await response.Content.ReadAsStringAsync();
            // return (response.IsSuccessStatusCode, (int)response.StatusCode, requestJson, responseBody, url, null);
            //
            // For now, simulate:
            await Task.Delay(300); // Simulate network latency

            var simulatedResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = true,
                provider = providerKey,
                action,
                acknowledged = true,
                referenceId = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow.ToString("o"),
                cardStatus = newStatus.ToString()
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Provider API SIMULATED call to {Url} for {Provider}", url, providerKey);

            return (true, 200, requestJson, simulatedResponse, url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider API call FAILED for {Provider} at {Url}", providerKey, url);
            return (false, 502, requestJson, null, url, ex.Message);
        }
    }
}
