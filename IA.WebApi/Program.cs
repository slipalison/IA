using System.Net.Sockets;
using System.Text.Json;
using ChromaDb;
using IA.WebApi.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IA.WebApi;

// 🏗️ Classes de configuração PRIMEIRO
public class DevOpsAISettings
{
    public FeaturesSettings Features { get; set; } = new();
    public LimitsSettings Limits { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
}

public class FeaturesSettings
{
    public bool LogAnalysis { get; set; }
    public bool CodeGeneration { get; set; }
    public bool DocumentationRAG { get; set; }
    public bool MetricsAnalysis { get; set; }
}

public class LimitsSettings
{
    public int MaxTokensPerRequest { get; set; }
    public int MaxConcurrentRequests { get; set; }
    public int CacheExpirationMinutes { get; set; }
}

public class SecuritySettings
{
    public bool EnableRateLimiting { get; set; }
    public int MaxRequestsPerMinute { get; set; }
    public bool EnableApiKey { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        // ⚡ Carregar variáveis do .env PRIMEIRO
        LoadEnvironmentVariables();

        // Configurar Serilog a partir do appsettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                true)
            .AddEnvironmentVariables() // 🔑 Variáveis de ambiente têm prioridade
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(new JsonFormatter())
            .CreateLogger();

        try
        {
            Log.Information("🚀 Iniciando DevOpsAI API...");
            Log.Information("🔧 Carregadas variáveis de ambiente do .env");

            var builder = WebApplication.CreateBuilder(args);

            // Configurar Serilog
            builder.Host.UseSerilog();

            // Adicionar serviços
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configurar pipeline
            Configure(app);

            Log.Information("✅ DevOpsAI API iniciada com sucesso em {Environment}",
                builder.Environment.EnvironmentName);
            Log.Information("📡 Swagger disponível em: {SwaggerUrl}", "http://localhost:5000");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "❌ Erro crítico ao iniciar DevOpsAI API");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void LoadEnvironmentVariables()
    {
        try
        {
            // Tentar carregar .env da raiz do projeto
            var envFile = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(envFile))
            {
                Env.Load(envFile);
                Console.WriteLine("✅ Arquivo .env carregado da raiz do projeto");
            }
            // Tentar carregar .env do diretório atual
            else if (File.Exists(".env"))
            {
                Env.Load();
                Console.WriteLine("✅ Arquivo .env carregado do diretório atual");
            }
            else
            {
                Console.WriteLine("⚠️ Arquivo .env não encontrado - usando variáveis do sistema");
            }

            // Log das variáveis carregadas (SEM mostrar valores sensíveis)
            var envVars = new[] { "OLLAMA_URL", "CHROMA_URL", "REDIS_URL", "POSTGRES_CONNECTION" };
            foreach (var envVar in envVars)
            {
                var value = Environment.GetEnvironmentVariable(envVar);
                Console.WriteLine($"🔧 {envVar}: {(string.IsNullOrEmpty(value) ? "❌ NÃO DEFINIDA" : "✅ DEFINIDA")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao carregar .env: {ex.Message}");
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IChromaService, ChromaService>();
        services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
        services.AddHttpClient<IChromaApiClient, ChromaApiClient>(x =>
            x.BaseAddress = new Uri("http://localhost:8000"));


        // Obter URLs dos serviços (das variáveis de ambiente)
        var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434";
        var chromaUrl = Environment.GetEnvironmentVariable("CHROMA_URL") ?? "http://localhost:8000";

        Log.Information("🔧 Configurando serviços...");
        Log.Information("  🤖 Ollama: {OllamaUrl}", ollamaUrl);
        Log.Information("  📊 Chroma: {ChromaUrl}", chromaUrl);

        // API Controllers
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DevOpsAI API",
                Version = "v1.0.0",
                Description = "🤖 API de IA para DevOps - Análise de logs, documentação e automação",
                Contact = new OpenApiContact { Name = "DevOpsAI Team" }
            });
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // HTTP Clients com URLs das variáveis de ambiente
        services.AddHttpClient("OllamaClient", client =>
        {
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient("ChromaClient", client =>
        {
            client.BaseAddress = new Uri(chromaUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 🏥 Health Checks DETALHADOS com assinatura correta
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API DevOpsAI está funcionando"))
            .AddAsyncCheck("ollama", async cancellationToken =>
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = await client.GetAsync($"{ollamaUrl}/api/tags", cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        return HealthCheckResult.Healthy($"✅ Ollama OK - {ollamaUrl}");
                    }

                    return HealthCheckResult.Unhealthy($"❌ Ollama resposta inválida: {response.StatusCode}");
                }
                catch (HttpRequestException ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ Ollama conexão falhou: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    return HealthCheckResult.Unhealthy($"❌ Ollama timeout - {ollamaUrl}");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ Ollama erro: {ex.Message}");
                }
            })
            .AddAsyncCheck("chroma", async cancellationToken =>
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = await client.GetAsync($"{chromaUrl}/api/v2/heartbeat", cancellationToken);

                    if (response.IsSuccessStatusCode) return HealthCheckResult.Healthy($"✅ Chroma OK - {chromaUrl}");

                    return HealthCheckResult.Unhealthy($"❌ Chroma resposta inválida: {response.StatusCode}");
                }
                catch (HttpRequestException ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ Chroma conexão falhou: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    return HealthCheckResult.Unhealthy($"❌ Chroma timeout - {chromaUrl}");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ Chroma erro: {ex.Message}");
                }
            })
            .AddAsyncCheck("redis", async cancellationToken =>
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync("localhost", 6379);
                    return HealthCheckResult.Healthy("✅ Redis OK - localhost:6379");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ Redis erro: {ex.Message}");
                }
            })
            .AddAsyncCheck("postgres", async cancellationToken =>
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync("localhost", 5432);
                    return HealthCheckResult.Healthy("✅ PostgreSQL OK - localhost:5432");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"❌ PostgreSQL erro: {ex.Message}");
                }
            });

        // Configurações específicas
        services.Configure<DevOpsAISettings>(configuration.GetSection("DevOpsAI"));
    }

    private static void Configure(WebApplication app)
    {
        // Swagger sempre disponível para desenvolvimento
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevOpsAI API v1.0.0");
            c.RoutePrefix = string.Empty; // Swagger na raiz
            c.DocumentTitle = "🤖 DevOpsAI API";
        });

        // Pipeline
        app.UseCors();
        app.UseRouting();
        app.MapControllers();

        // 🏥 Health check BÁSICO
        app.MapHealthChecks("/health");

        // 🔍 Health check DETALHADO
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.TotalMilliseconds,
                        description = entry.Value.Description,
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        });

        // Endpoint de status que mostra variáveis de ambiente
        app.MapGet("/status", (IConfiguration config) => new
        {
            message = "🤖 DevOpsAI API",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            services = new
            {
                ollama = Environment.GetEnvironmentVariable("OLLAMA_URL"),
                chroma = Environment.GetEnvironmentVariable("CHROMA_URL"),
                redis = Environment.GetEnvironmentVariable("REDIS_URL"),
                postgres = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") != null
                    ? "CONFIGURADO"
                    : "NÃO CONFIGURADO"
            },
            envLoaded = new
            {
                ollamaUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OLLAMA_URL")),
                chromaUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CHROMA_URL")),
                redisUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_URL")),
                postgresConnection = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
            }
        });

        // Endpoint raiz simples
        app.MapGet("/", () => "🚀 DevOpsAI API - Acesse /swagger para documentação");
    }
}