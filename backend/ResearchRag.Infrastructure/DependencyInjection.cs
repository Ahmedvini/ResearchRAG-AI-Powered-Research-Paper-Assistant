using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchRag.Application.Abstractions;
using ResearchRag.Infrastructure.Ai;
using ResearchRag.Infrastructure.Auth;
using ResearchRag.Infrastructure.Persistence;
using ResearchRag.Infrastructure.Processing;
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

        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IDocumentProcessorClient, DatabaseDocumentProcessorClient>();
        services.AddScoped<IEmbeddingProvider, HashEmbeddingProvider>();
        services.AddScoped<IRerankerProvider, PassThroughRerankerProvider>();
        services.AddScoped<ILLMProvider, EchoLlmProvider>();
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddScoped<IRagAnswerService, RagAnswerService>();

        services.AddHttpClient<IVectorStore, QdrantVectorStore>(client =>
        {
            client.BaseAddress = new Uri(configuration["Qdrant:Url"] ?? "http://localhost:6333");
        });

        return services;
    }
}
