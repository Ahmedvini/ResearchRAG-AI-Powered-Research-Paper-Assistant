using ResearchRag.Application.Research;

namespace ResearchRag.Application.Abstractions;

public interface IResearchAnalysisService
{
    Task<LiteratureReviewDto> GenerateLiteratureReviewAsync(Guid userId, LiteratureReviewRequest request, CancellationToken cancellationToken);
    Task<PaperComparisonDto> ComparePapersAsync(Guid userId, PaperComparisonRequest request, CancellationToken cancellationToken);
    Task<ResearchGapDto> AnalyzeResearchGapsAsync(Guid userId, ResearchGapRequest request, CancellationToken cancellationToken);
    Task<KnowledgeGraphDto> GenerateKnowledgeGraphAsync(Guid userId, KnowledgeGraphRequest request, CancellationToken cancellationToken);
    Task<StudyToolsDto> GenerateStudyToolsAsync(Guid userId, StudyToolsRequest request, CancellationToken cancellationToken);
}

