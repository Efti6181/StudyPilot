using System.Net.Http.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using StudyPilotApp.Options;

namespace StudyPilotApp.Services;

public sealed partial class GeminiAITextProvider : IAITextProvider
{
    private const int MaximumAttempts = 3;
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

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                using var request = CreateRequest(model, payload);
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    if (IsTransient(response.StatusCode) && attempt < MaximumAttempts)
                    {
                        _logger.LogWarning(
                            "Gemini request returned {StatusCode}; retrying ({Attempt}/{MaximumAttempts}).",
                            statusCode,
                            attempt,
                            MaximumAttempts);
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning(
                        "Gemini request failed with status {StatusCode} after {Attempt} attempt(s).",
                        statusCode,
                        attempt);
                    return new AIProviderResult(false, null, "Gemini", $"http_{statusCode}");
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (attempt < MaximumAttempts)
            {
                _logger.LogWarning(
                    "Gemini request timed out; retrying ({Attempt}/{MaximumAttempts}).",
                    attempt,
                    MaximumAttempts);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini request timed out after {Attempts} attempts.", MaximumAttempts);
                return new AIProviderResult(false, null, "Gemini", "timeout");
            }
            catch (HttpRequestException exception) when (attempt < MaximumAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Gemini network request failed; retrying ({Attempt}/{MaximumAttempts}).",
                    attempt,
                    MaximumAttempts);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Gemini request could not be completed after retries.");
                return new AIProviderResult(false, null, "Gemini", "network_error");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected Gemini provider failure.");
                return new AIProviderResult(false, null, "Gemini", "provider_error");
            }
        }

        return new AIProviderResult(false, null, "Gemini", "provider_error");
    }

    private HttpRequestMessage CreateRequest(string model, GeminiGenerateRequest payload)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", _options.ApiKey.Trim());
        return request;
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 450));
        return Task.Delay(exponentialDelay + jitter, cancellationToken);
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
