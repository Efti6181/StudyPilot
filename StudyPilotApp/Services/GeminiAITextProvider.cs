using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using StudyPilotApp.Options;

namespace StudyPilotApp.Services;

public sealed partial class GeminiAITextProvider : IAITextProvider
{
    private readonly HttpClient _httpClient;
    private readonly AcademicAIOptions _options;
    private readonly ILogger<GeminiAITextProvider> _logger;

    public GeminiAITextProvider(
        HttpClient httpClient,
        IOptions<AcademicAIOptions> options,
        ILogger<GeminiAITextProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.Model) &&
        ModelNamePattern().IsMatch(_options.Model);

    public async Task<AIProviderResult> GenerateAsync(
        string systemInstruction,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new AIProviderResult(false, null, "Deterministic fallback", "not_configured");

        var model = _options.Model.Trim();
        var payload = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = systemInstruction }]
            },
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.35m,
                MaxOutputTokens = _options.MaxOutputTokens
            },
            Store = false
        };

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("x-goog-api-key", _options.ApiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini request failed with status {StatusCode}.",
                    (int)response.StatusCode);
                return new AIProviderResult(false, null, "Gemini", $"http_{(int)response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(
                cancellationToken: cancellationToken);
            var textParts = result?.Candidates?
                .SelectMany(candidate => candidate.Content?.Parts ?? [])
                .Select(part => part.Text)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList() ?? [];
            var text = string.Join(Environment.NewLine, textParts).Trim();

            return string.IsNullOrWhiteSpace(text)
                ? new AIProviderResult(false, null, "Gemini", "empty_response")
                : new AIProviderResult(true, text, $"Gemini · {model}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Gemini request timed out.");
            return new AIProviderResult(false, null, "Gemini", "timeout");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Gemini request could not be completed.");
            return new AIProviderResult(false, null, "Gemini", "network_error");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected Gemini provider failure.");
            return new AIProviderResult(false, null, "Gemini", "provider_error");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex ModelNamePattern();

    private sealed class GeminiGenerateRequest
    {
        public GeminiContent SystemInstruction { get; set; } = new();
        public IReadOnlyList<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
        public bool Store { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; }
        public IReadOnlyList<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        public decimal Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
    }

    private sealed class GeminiGenerateResponse
    {
        public IReadOnlyList<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }
}
