using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PrivateAIOverlay.Security;

namespace PrivateAIOverlay.AI;

public sealed class WindowsSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly HttpClient _httpClient = new();

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "whisper-1";

    public async Task<string> TranscribeAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        if (audioData == null || audioData.Length == 0)
        {
            return string.Empty;
        }

        var apiKey = CredentialManager.LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "No API key is configured. Add your provider key to enable microphone transcription.";
        }

        try
        {
            var wavAudio = PcmToWave(audioData);
            if (IsGeminiEndpoint(BaseUrl))
            {
                return await TranscribeWithGeminiAsync(wavAudio, apiKey, cancellationToken);
            }

            return await TranscribeWithWhisperAsync(wavAudio, apiKey, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Transcription failed: {ex.Message}";
        }
    }

    private async Task<string> TranscribeWithGeminiAsync(byte[] wavAudio, string apiKey, CancellationToken cancellationToken)
    {
        var baseUri = new Uri(BaseUrl);
        var basePath = baseUri.AbsolutePath.TrimEnd('/');
        if (basePath.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
        {
            basePath = basePath[..^"/openai".Length];
        }

        var endpoint = new UriBuilder(baseUri)
        {
            Path = $"{basePath}/models/{Uri.EscapeDataString(Model)}:generateContent",
            Query = string.Empty
        }.Uri;
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Transcribe the speech in this audio. Return only the spoken words." },
                        new { inlineData = new { mimeType = "audio/wav", data = Convert.ToBase64String(wavAudio) } }
                    }
                }
            },
            generationConfig = new { temperature = 0 }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return $"Transcription failed: {response.StatusCode}. {responseBody}";
        }

        using var document = JsonDocument.Parse(responseBody);
        var transcript = string.Join("", document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")
            .EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString()));

        return string.IsNullOrWhiteSpace(transcript) ? "No speech was detected." : transcript.Trim();
    }

    private async Task<string> TranscribeWithWhisperAsync(byte[] wavAudio, string apiKey, CancellationToken cancellationToken)
    {
            using var form = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(wavAudio);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            form.Add(audioContent, "file", "recording.wav");
            form.Add(new StringContent(Model), "model");
            form.Add(new StringContent("false"), "response_format");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/audio/transcriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = form;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return $"Transcription failed: {response.StatusCode}. {payload}";
            }

            using var document = JsonDocument.Parse(payload);
            var text = document.RootElement.GetProperty("text").GetString();
            return string.IsNullOrWhiteSpace(text) ? "No speech was detected." : text.Trim();
    }

    private static bool IsGeminiEndpoint(string baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) &&
               string.Equals(uri.Host, "generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] PcmToWave(byte[] pcmAudio)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + pcmAudio.Length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(16000);
        writer.Write(32000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(pcmAudio.Length);
        writer.Write(pcmAudio);
        return stream.ToArray();
    }
}
