using System.Text.Json.Serialization;

namespace IA.WebApi.Controllers.Chat;

public class OllamaResponse
{
    [JsonPropertyName("response")] public string? Response { get; set; }

    [JsonPropertyName("done")] public bool Done { get; set; }

    [JsonPropertyName("context")] public int[]? Context { get; set; }

    [JsonPropertyName("total_duration")] public long TotalDuration { get; set; }

    [JsonPropertyName("load_duration")] public long LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvalDuration { get; set; }

    [JsonPropertyName("eval_count")] public int EvalCount { get; set; }

    [JsonPropertyName("eval_duration")] public long EvalDuration { get; set; }

    // ✅ PROPRIEDADES CALCULADAS
    public double TotalDurationMs => TotalDuration / 1_000_000.0; // Nanosegundos para milissegundos
    public double LoadDurationMs => LoadDuration / 1_000_000.0;
    public double PromptEvalDurationMs => PromptEvalDuration / 1_000_000.0;
    public double EvalDurationMs => EvalDuration / 1_000_000.0;

    public double TokensPerSecond => EvalDuration > 0 ? EvalCount * 1_000_000_000.0 / EvalDuration : 0;

    public bool IsValid => !string.IsNullOrWhiteSpace(Response);
}