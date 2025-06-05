using IA.WebApi.Controllers.Chat;
using Microsoft.AspNetCore.Mvc;

namespace IA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            _logger.LogInformation("Nova mensagem recebida: {Message}", request.Message);

            var response = await _chatService.ProcessMessageAsync(request);

            _logger.LogInformation("Resposta gerada com sucesso");
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Requisição inválida: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("stream")]
    public async Task<IActionResult> StreamMessage([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            ConfigureStreamingResponse();
            await _chatService.StreamMessageAsync(request, Response);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no streaming");
            return StatusCode(500, "Erro no streaming");
        }
    }

    private void ConfigureStreamingResponse()
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
    }
}