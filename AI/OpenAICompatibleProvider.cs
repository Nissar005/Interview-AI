using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PrivateAIOverlay.Security;

namespace PrivateAIOverlay.AI;

public sealed class OpenAICompatibleProvider : IAIProvider, IVisionProvider
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKeyOverride;

    public OpenAICompatibleProvider(HttpClient? httpClient = null, string? apiKeyOverride = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiKeyOverride = apiKeyOverride;
    }

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";

    public async Task<string> GenerateTextAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _apiKeyOverride ?? CredentialManager.LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "No API key is configured. Add your provider key in Settings to enable text AI.";
        }

        try
        {
            var prompt = BuildPrompt(request);
            var payload = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = "You are a highly intelligent personal assistant. Keep responses concise, natural, and technical where helpful." },
                    new { role = "user", content = prompt }
                },
                stream = false
            };

            using var response = await SendWithRetryAsync(() =>
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/chat/completions");
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                return requestMessage;
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return $"AI service is still busy after automatic retries. Try again later or choose another model. Details: {body}";
                }

                return $"AI request failed: {response.StatusCode}. {body}";
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var output = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return output ?? "No response was returned by the provider.";
        }
        catch (Exception ex)
        {
            return $"AI request failed: {ex.Message}";
        }
    }

    public async Task<string> AnalyzeImageAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _apiKeyOverride ?? CredentialManager.LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "No API key is configured. Add your provider key in Settings to enable vision analysis.";
        }

        var image = request.Image;
        if (string.IsNullOrWhiteSpace(image))
        {
            return "No image was provided for vision analysis.";
        }

        try
        {
            byte[] imageBytes;
            var imageUrl = image;

            if (File.Exists(image))
            {
                imageBytes = await File.ReadAllBytesAsync(image, cancellationToken);
                imageUrl = "data:image/png;base64," + Convert.ToBase64String(imageBytes);
            }
            else if (image.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                imageUrl = image;
            }
            else
            {
                return "The image path is invalid or unavailable for vision analysis.";
            }

            var messages = new object[]
            {
                new Dictionary<string, object>
                {
                    ["role"] = "system",
                    ["content"] = "You are a private desktop assistant. Analyze the screen context concisely and avoid revealing private data or credentials."
                },
                new Dictionary<string, object>
                {
                    ["role"] = "user",
                    ["content"] = new object[]
                    {
                        new Dictionary<string, object> { ["type"] = "text", ["text"] = request.Text },
                        new Dictionary<string, object> { ["type"] = "image_url", ["image_url"] = new Dictionary<string, string> { ["url"] = imageUrl } }
                    }
                }
            };

            var payload = new
            {
                model = Model,
                messages,
                max_tokens = 400
            };

            using var response = await SendWithRetryAsync(() =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                return httpRequest;
            }, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return $"Vision service is still busy after automatic retries. Try again later or choose another model. Details: {responseBody}";
                }

                return $"Vision analysis failed: {response.StatusCode}. {responseBody}";
            }

            using var document = JsonDocument.Parse(responseBody);
            var output = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return output ?? "No response was returned for the screen analysis.";
        }
        catch (Exception ex)
        {
            return $"Vision analysis failed: {ex.Message}";
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 2;

        for (var attempt = 0; ; attempt++)
        {
            using var request = createRequest();
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable || attempt >= maxRetries)
            {
                return response;
            }

            var retryDelay = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
                ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
            if (retryDelay <= TimeSpan.Zero)
            {
                retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds, 15)), cancellationToken);
        }
    }

    private static string BuildPrompt(AIRequest request)
    {
        var chunks = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            chunks.Add(request.Text);
        }

        if (!string.IsNullOrWhiteSpace(request.ConversationContext))
        {
            chunks.Add($"Conversation context: {request.ConversationContext}");
        }

        if (!string.IsNullOrWhiteSpace(request.RelevantMemory))
        {
            chunks.Add($"Relevant memory: {request.RelevantMemory}");
        }

        if (!string.IsNullOrWhiteSpace(request.Image))
        {
            chunks.Add($"Visual context was provided at: {request.Image}");
        }

        if (!string.IsNullOrWhiteSpace(request.AudioContext))
        {
            chunks.Add($"Audio context: {request.AudioContext}");
        }

        return string.Join("\n", chunks);
    }
}
