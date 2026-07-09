using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchRag.Application.Abstractions;
using ResearchRag.Infrastructure.Ai;
using ResearchRag.Infrastructure.Auth;
using ResearchRag.Infrastructure.Persistence;
using ResearchRag.Infrastructure.Processing;
using ResearchRag.Infrastructure.Research;
using ResearchRag.Infrastructure.Retrieval;
using ResearchRag.Infrastructure.Vector;

namespace ResearchRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ??
            "Server=localhost;Port=3306;Database=researchrag;User=researchrag;Password=researchrag;";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(connectionString, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("ResearchRagDev");
                return;
            }

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddHttpClient();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IDocumentProcessorClient, DatabaseDocumentProcessorClient>();
        services.AddScoped<IEmbeddingProvider>(sp => CreateEmbeddingProvider(sp, configuration));
        services.AddScoped<IRerankerProvider>(sp => CreateRerankerProvider(sp, configuration));
        services.AddScoped<ILLMProvider>(sp => CreateLlmProvider(sp, configuration));
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddScoped<IRagAnswerService, RagAnswerService>();
        services.AddScoped<IResearchAnalysisService, ResearchAnalysisService>();

        services.AddHttpClient<IVectorStore, QdrantVectorStore>(client =>
        {
            client.BaseAddress = new Uri(configuration["Qdrant:Url"] ?? "http://localhost:6333");
        });

        return services;
    }

    private static ILLMProvider CreateLlmProvider(IServiceProvider services, IConfiguration configuration)
    {
        var provider = configuration["Ai:ChatProvider"] ?? "echo";
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        return provider.ToLowerInvariant() switch
        {
            "openai" => new OpenAiChatProvider(CreateClient(httpClientFactory, configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com"), configuration),
            "ollama" => new OllamaChatProvider(CreateClient(httpClientFactory, configuration["Ollama:BaseUrl"] ?? "http://localhost:11434"), configuration),
            _ => new EchoLlmProvider()
        };
    }

    private static IEmbeddingProvider CreateEmbeddingProvider(IServiceProvider services, IConfiguration configuration)
    {
        var provider = configuration["Ai:EmbeddingProvider"] ?? "hash";
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        return provider.ToLowerInvariant() switch
        {
            "openai" => new OpenAiEmbeddingProvider(CreateClient(httpClientFactory, configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com"), configuration),
            "ollama" => new OllamaEmbeddingProvider(CreateClient(httpClientFactory, configuration["Ollama:BaseUrl"] ?? "http://localhost:11434"), configuration),
            _ => new HashEmbeddingProvider()
        };
    }

    private static IRerankerProvider CreateRerankerProvider(IServiceProvider services, IConfiguration configuration)
    {
        var provider = configuration["Ai:RerankerProvider"] ?? "passthrough";
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        return provider.ToLowerInvariant() switch
        {
            "cohere" => new CohereRerankerProvider(CreateClient(httpClientFactory, configuration["Cohere:BaseUrl"] ?? "https://api.cohere.com"), configuration),
            _ => new PassThroughRerankerProvider()
        };
    }

    private static HttpClient CreateClient(IHttpClientFactory factory, string baseUrl)
    {
        var client = factory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }
}
