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
    /// Currently SIMULATED — returns a mock success response.
    /// To use real provider APIs, configure URLs in appsettings.json ProviderApi section
    /// and add API key to user-secrets.
    /// </summary>
    public async Task<(bool success, int httpStatusCode, string? responsePayload, string? error)> CallProviderAsync(
        CardProvider provider, string cardNumberMasked, CardStatus newStatus)
    {
        var providerKey = provider.ToString();
        var url = _config[$"ProviderApi:{providerKey}:Url"] ?? "https://api.placeholder.com/v1/cards/status";
        var action = newStatus == CardStatus.Enabled ? "unblock" : "block";

        var requestBody = new
        {
            cardNumber = CardMaskingUtility.MaskCardNumber(cardNumberMasked),
            action,
            status = newStatus.ToString()
        };

        try
        {
            // --- SIMULATED PROVIDER CALL ---
            // In production, uncomment the real HTTP call below:
            //
            // var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            // var responseBody = await response.Content.ReadAsStringAsync();
            // return (response.IsSuccessStatusCode, (int)response.StatusCode, responseBody, null);
            //
            // For now, simulate:
            await Task.Delay(300); // Simulate network latency

            var responsePayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                provider = providerKey,
                action,
                acknowledged = true,
                referenceId = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow.ToString("o")
            });

            _logger.LogInformation("Provider API SIMULATED call to {Url} for {Provider}", url, providerKey);

            return (true, 200, responsePayload, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider API call FAILED for {Provider} at {Url}", providerKey, url);
            return (false, 502, null, ex.Message);
        }
    }
}