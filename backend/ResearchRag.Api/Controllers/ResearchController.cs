using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Research;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class ResearchController(IResearchAnalysisService research) : ApiControllerBase
{
    [HttpPost("literature-review")]
    public Task<LiteratureReviewDto> LiteratureReview(LiteratureReviewRequest request, CancellationToken cancellationToken)
    {
        return research.GenerateLiteratureReviewAsync(CurrentUserId, request, cancellationToken);
    }

    [HttpPost("paper-comparison")]
    public Task<PaperComparisonDto> PaperComparison(PaperComparisonRequest request, CancellationToken cancellationToken)
    {
        return research.ComparePapersAsync(CurrentUserId, request, cancellationToken);
    }

    [HttpPost("research-gaps")]
    public Task<ResearchGapDto> ResearchGaps(ResearchGapRequest request, CancellationToken cancellationToken)
    {
        return research.AnalyzeResearchGapsAsync(CurrentUserId, request, cancellationToken);
    }

    [HttpPost("knowledge-graph")]
    public Task<KnowledgeGraphDto> KnowledgeGraph(KnowledgeGraphRequest request, CancellationToken cancellationToken)
    {
        return research.GenerateKnowledgeGraphAsync(CurrentUserId, request, cancellationToken);
    }

    [HttpPost("study-tools")]
    public Task<StudyToolsDto> StudyTools(StudyToolsRequest request, CancellationToken cancellationToken)
    {
        return research.GenerateStudyToolsAsync(CurrentUserId, request, cancellationToken);
    }
}

