using System.ComponentModel.DataAnnotations;

namespace IA.WebApi.Controllers.Chat;

public class ChatRequest
{
    [Required(ErrorMessage = "Mensagem é obrigatória")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Mensagem deve ter entre 1 e 2000 caracteres")]
    public string Message { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ID da conversa deve ter no máximo 100 caracteres")]
    public string? ConversationId { get; set; }

    public bool UseRAG { get; set; } = true;

    [StringLength(50, ErrorMessage = "Nome do modelo deve ter no máximo 50 caracteres")]
    public string? Model { get; set; } = "llama2:7b";

    [Range(1, 10, ErrorMessage = "Máximo de resultados deve estar entre 1 e 10")]
    public int MaxResults { get; set; } = 5;

    [Range(0.0, 2.0, ErrorMessage = "Temperatura deve estar entre 0.0 e 2.0")]
    public double Temperature { get; set; } = 0.7;

    public bool Stream { get; set; } = false;
}