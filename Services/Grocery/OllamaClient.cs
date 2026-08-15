using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ValensFit.Models;

namespace ValensFit.Services.Grocery
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OllamaClient> _logger;

        public class OllamaBudgetResponse
        {
            [JsonPropertyName("estimated_weekly_cost")]
            public decimal EstimatedWeeklyCost { get; set; }

            [JsonPropertyName("estimated_monthly_cost")]
            public decimal EstimatedMonthlyCost { get; set; }

            [JsonPropertyName("verdict")]
            public string Verdict { get; set; } = "fits"; // fits, tight, over_budget

            [JsonPropertyName("notes")]
            public string Notes { get; set; } = string.Empty;

            [JsonPropertyName("swap_suggestions")]
            public List<string> SwapSuggestions { get; set; } = new();
        }

        public OllamaClient(HttpClient httpClient, IConfiguration config, ILogger<OllamaClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<OllamaBudgetResponse?> EvaluateBudgetWithAiAsync(
            UserInputModel input,
            GroceryBudgetVerdictModel deterministicVerdict,
            CancellationToken ct = default)
        {
            bool enableAi = _config.GetValue<bool>("Ollama:EnableAI", true);
            if (!enableAi)
            {
                _logger.LogInformation("Ollama AI evaluation is disabled via configuration.");
                return null;
            }

            string primaryEndpoint = _config.GetValue<string>("Ollama:DockerEndpoint") ?? "http://ollama:11434";
            string fallbackEndpoint = _config.GetValue<string>("Ollama:Endpoint") ?? "http://localhost:11434";
            string model = _config.GetValue<string>("Ollama:Model") ?? "llama3.2:1b";
            int timeoutSeconds = _config.GetValue<int>("Ollama:TimeoutSeconds", 10);

            // Prepare prompt context
            var foodListSnippet = new StringBuilder();
            var priceSnippet = new StringBuilder();

            foreach (var item in deterministicVerdict.Items.Take(12))
            {
                foodListSnippet.AppendLine($"- {item.FoodName}: {item.DisplayWeeklyQuantity}");
                priceSnippet.AppendLine($"- {item.FoodName}: ~{item.Currency} {item.UnitPrice} per {item.PriceUnit}");
            }

            string prompt = $@"Country: {input.Country}
Currency: {deterministicVerdict.Currency}
Monthly budget: {input.MonthlyBudget ?? deterministicVerdict.EstimatedMonthlyCost}
Weekly food list (item: quantity):
{foodListSnippet}
Reference prices:
{priceSnippet}
Deterministic Weekly Cost: {deterministicVerdict.EstimatedWeeklyCost}
Deterministic Monthly Cost: {deterministicVerdict.EstimatedMonthlyCost}";

            string systemPrompt = @"You are an elite grocery budgeting assistant. Only use the price data provided. Never invent prices. Always answer in strict JSON matching the schema:
{
  ""estimated_weekly_cost"": number,
  ""estimated_monthly_cost"": number,
  ""verdict"": ""fits"" | ""tight"" | ""over_budget"",
  ""notes"": ""1-2 short punchy sentences"",
  ""swap_suggestions"": [""suggestion 1"", ""suggestion 2""]
}";

            var payload = new
            {
                model = model,
                system = systemPrompt,
                prompt = prompt,
                stream = false,
                format = "json",
                options = new
                {
                    temperature = 0.2,
                    num_predict = 250
                }
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            // Try primary endpoint first, then fallback endpoint
            string[] endpointsToTry = { primaryEndpoint, fallbackEndpoint };

            foreach (var baseUrl in endpointsToTry.Distinct())
            {
                try
                {
                    var requestUri = $"{baseUrl.TrimEnd('/')}/api/generate";
                    _logger.LogInformation("Calling Ollama AI endpoint at {Uri}...", requestUri);

                    using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(requestUri, content, linkedCts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync(linkedCts.Token);
                        using var doc = JsonDocument.Parse(responseString);
                        
                        if (doc.RootElement.TryGetProperty("response", out var rawResponseJson))
                        {
                            var parsedJsonText = rawResponseJson.GetString();
                            if (!string.IsNullOrWhiteSpace(parsedJsonText))
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var parsed = JsonSerializer.Deserialize<OllamaBudgetResponse>(parsedJsonText, options);
                                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Verdict))
                                {
                                    _logger.LogInformation("Ollama successfully generated budget response: {Verdict}", parsed.Verdict);
                                    return parsed;
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Ollama call to {Endpoint} timed out after {Seconds}s. Falling back gracefully to deterministic market index.", baseUrl, timeoutSeconds);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ollama call to {Endpoint} failed or is not reachable. Falling back to deterministic market index.", baseUrl);
                }
            }

            return null; // Graceful fallback
        }
    }
}
