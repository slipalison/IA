using System.Text;
using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public class PromptBuilder : IPromptBuilder
{
    private const int MAX_CONTEXT_LENGTH = 800;
    private const int MAX_CONTEXT_DOCUMENTS = 3;

    public string BuildPrompt(string userMessage, List<DocumentChunk> context)
    {
        var systemPrompt = CreateSystemPrompt();
        var contextSection = BuildContextSection(context);

        return $"{systemPrompt}\n\n{contextSection}\n\nCONVERSA ATUAL:\nUsuário: {userMessage}\n\nDevOpsGPT: ";
    }

    private static string CreateSystemPrompt()
    {
        return @"Você é DevOpsGPT, um assistente especializado em DevOps que SEMPRE responde em português brasileiro.

SUAS ESPECIALIDADES:
🚀 CI/CD: Pipelines, automação, deploy
🐳 Containers: Docker, Kubernetes, orquestração  
☁️ Cloud: AWS, Azure, GCP, arquitetura
📊 Monitoramento: Logs, métricas, alertas
🛠️ IaC: Terraform, Ansible, CloudFormation
🔒 Segurança: DevSecOps, compliance
🔧 Troubleshooting: Análise de problemas

COMO RESPONDER:
✅ Sempre em português brasileiro
✅ Prático e direto ao ponto
✅ Use emojis para clareza
✅ Forneça exemplos de código quando relevante
✅ Mencione boas práticas
✅ Se não souber algo, seja honesto";
    }

    private string BuildContextSection(List<DocumentChunk> context)
    {
        if (!context.Any())
            return "CONTEXTO: Nenhuma informação específica encontrada na base de conhecimento.";

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("CONTEXTO DA BASE DE CONHECIMENTO:");
        contextBuilder.AppendLine();

        var relevantDocuments = context.Take(MAX_CONTEXT_DOCUMENTS);

        foreach (var doc in relevantDocuments) AppendDocumentToContext(contextBuilder, doc);

        return contextBuilder.ToString();
    }

    private void AppendDocumentToContext(StringBuilder contextBuilder, DocumentChunk doc)
    {
        var title = ExtractTitle(doc);
        var category = ExtractCategory(doc);
        var truncatedContent = TruncateContent(doc.Content);

        contextBuilder.AppendLine($"📄 {title} ({category}):");
        contextBuilder.AppendLine(truncatedContent);
        contextBuilder.AppendLine();
    }

    private static string ExtractTitle(DocumentChunk doc)
    {
        return doc.Metadata.GetValueOrDefault("title", "Documento")?.ToString() ?? "Documento";
    }

    private static string ExtractCategory(DocumentChunk doc)
    {
        return doc.Metadata.GetValueOrDefault("category", "geral")?.ToString() ?? "geral";
    }

    private string TruncateContent(string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= MAX_CONTEXT_LENGTH)
            return content;

        return content.Substring(0, MAX_CONTEXT_LENGTH) + "...";
    }
}