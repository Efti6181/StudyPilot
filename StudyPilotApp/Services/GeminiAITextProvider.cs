using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
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
        ModelNamePattern().IsMatch(_options.Model.Trim());

    public async Task<AIProviderResult> GenerateAsync(
        string systemInstruction,
        string prompt,
        CancellationToken cancellationToken = default,
        int? maxOutputTokens = null)
    {
        if (!IsConfigured)
            return new AIProviderResult(false, null, "Deterministic fallback", "not_configured");

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
                MaxOutputTokens = Math.Clamp(
                    maxOutputTokens ?? _options.MaxOutputTokens,
                    256,
                    8192)
            },
            Store = false
        };

        var models = GetModelCandidates();
        var lastErrorCode = "provider_error";
        for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
        {
            var model = models[modelIndex];
            for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
            {
                try
                {
                    using var request = CreateRequest(model, payload);
                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        var statusCode = (int)response.StatusCode;
                        var error = await ReadErrorAsync(response, cancellationToken);
                        lastErrorCode = ClassifyError(response.StatusCode);
                        _logger.LogWarning(
                            "Gemini model {Model} returned {StatusCode} ({ApiStatus}): {ApiMessage}",
                            model,
                            statusCode,
                            error.Status ?? "unknown",
                            SanitizeLogText(error.Message));

                        if (lastErrorCode == "model_unavailable" && modelIndex < models.Count - 1)
                        {
                            _logger.LogInformation(
                                "Gemini model {Model} is unavailable; trying compatibility model {FallbackModel}.",
                                model,
                                models[modelIndex + 1]);
                            break;
                        }

                        if (IsTransient(response.StatusCode) && attempt < MaximumAttempts)
                        {
                            await DelayBeforeRetryAsync(attempt, cancellationToken);
                            continue;
                        }

                        return new AIProviderResult(false, null, "Gemini", lastErrorCode);
                    }

                    var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(
                        cancellationToken: cancellationToken);
                    var textParts = result?.Candidates?
                        .SelectMany(candidate => candidate.Content?.Parts ?? [])
                        .Select(part => part.Text)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .ToList() ?? [];
                    var text = string.Join(Environment.NewLine, textParts).Trim();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        var candidate = result?.Candidates?.FirstOrDefault();
                        var errorCode = ClassifyEmptyResponse(result);
                        _logger.LogWarning(
                            "Gemini model {Model} returned no text. ErrorCode={ErrorCode}, FinishReason={FinishReason}, FinishMessage={FinishMessage}, BlockReason={BlockReason}, MaxOutputTokens={MaxOutputTokens}.",
                            model,
                            errorCode,
                            candidate?.FinishReason ?? "none",
                            SanitizeLogText(candidate?.FinishMessage),
                            result?.PromptFeedback?.BlockReason ?? "none",
                            payload.GenerationConfig.MaxOutputTokens);
                        return new AIProviderResult(false, null, "Gemini", errorCode);
                    }

                    return new AIProviderResult(true, text, $"Gemini · {model}");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException) when (attempt < MaximumAttempts)
                {
                    lastErrorCode = "timeout";
                    _logger.LogWarning(
                        "Gemini model {Model} timed out; retrying ({Attempt}/{MaximumAttempts}).",
                        model,
                        attempt,
                        MaximumAttempts);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "Gemini model {Model} timed out after {Attempts} attempts.",
                        model,
                        MaximumAttempts);
                    return new AIProviderResult(false, null, "Gemini", "timeout");
                }
                catch (HttpRequestException exception) when (attempt < MaximumAttempts)
                {
                    lastErrorCode = "network_error";
                    _logger.LogWarning(
                        exception,
                        "Gemini network request for {Model} failed; retrying ({Attempt}/{MaximumAttempts}).",
                        model,
                        attempt,
                        MaximumAttempts);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogWarning(exception, "Gemini request could not be completed after retries.");
                    return new AIProviderResult(false, null, "Gemini", "network_error");
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(exception, "Gemini returned an unreadable JSON response.");
                    return new AIProviderResult(false, null, "Gemini", "invalid_response");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unexpected Gemini provider failure.");
                    return new AIProviderResult(false, null, "Gemini", "provider_error");
                }
            }
        }

        return new AIProviderResult(false, null, "Gemini", lastErrorCode);
    }

    private IReadOnlyList<string> GetModelCandidates()
    {
        var candidates = new[]
        {
            _options.Model.Trim(),
            _options.FallbackModel?.Trim(),
            "gemini-3.8-flash"
        };
        return candidates
            .Where(model => !string.IsNullOrWhiteSpace(model) && ModelNamePattern().IsMatch(model))
            .Select(model => model!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static string ClassifyError(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "request_rejected",
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "api_key_rejected",
        HttpStatusCode.NotFound => "model_unavailable",
        HttpStatusCode.RequestTimeout => "timeout",
        HttpStatusCode.TooManyRequests => "quota_exceeded",
        _ when (int)statusCode >= 500 => "provider_busy",
        _ => $"http_{(int)statusCode}"
    };

    private static async Task<GeminiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body)) return new GeminiError(null, null);
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("error", out var error))
                return new GeminiError(null, body);
            var status = error.TryGetProperty("status", out var statusValue)
                ? statusValue.GetString()
                : null;
            var message = error.TryGetProperty("message", out var messageValue)
                ? messageValue.GetString()
                : null;
            return new GeminiError(status, message);
        }
        catch (JsonException)
        {
            return new GeminiError(null, "Gemini returned a non-JSON error response.");
        }
    }

    private static string SanitizeLogText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "No error message supplied.";
        var safe = string.Join(' ', value.Split(
            ['\r', '\n', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return safe[..Math.Min(safe.Length, 500)];
    }

    private static string ClassifyEmptyResponse(GeminiGenerateResponse? response)
    {
        if (!string.IsNullOrWhiteSpace(response?.PromptFeedback?.BlockReason))
            return "safety_block";

        var finishReason = response?.Candidates?
            .Select(candidate => candidate.FinishReason)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return finishReason?.ToUpperInvariant() switch
        {
            "MAX_TOKENS" => "output_limit",
            "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT" => "safety_block",
            _ => "empty_response"
        };
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
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
        public string? FinishMessage { get; set; }
    }

    private sealed class GeminiPromptFeedback
    {
        public string? BlockReason { get; set; }
        public string? BlockReasonMessage { get; set; }
    }

    private sealed record GeminiError(string? Status, string? Message);
}
