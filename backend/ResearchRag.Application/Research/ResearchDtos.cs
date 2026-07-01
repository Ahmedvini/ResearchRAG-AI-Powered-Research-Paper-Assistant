namespace ResearchRag.Application.Research;

public sealed record LiteratureReviewRequest(Guid WorkspaceId, IReadOnlyList<Guid>? DocumentIds);
public sealed record LiteratureReviewDto(string Background, string ExistingMethods, string Trends, string ResearchGaps, string FutureWork, string Markdown);

public sealed record PaperComparisonRequest(Guid WorkspaceId, IReadOnlyList<Guid> DocumentIds);
public sealed record PaperComparisonRowDto(Guid DocumentId, string Paper, string Dataset, string Model, string Methodology, string Metrics, string Results, string Strengths, string Weaknesses);
public sealed record PaperComparisonDto(IReadOnlyList<PaperComparisonRowDto> Rows);

public sealed record ResearchGapRequest(Guid WorkspaceId);
public sealed record ResearchGapDto(IReadOnlyList<string> CommonLimitations, IReadOnlyList<string> UnderexploredAreas, IReadOnlyList<string> MissingDatasets, IReadOnlyList<string> MissingEvaluations, string Markdown);

public sealed record KnowledgeGraphRequest(Guid WorkspaceId);
public sealed record KnowledgeGraphNodeDto(string Id, string Label, string Type);
public sealed record KnowledgeGraphEdgeDto(string Source, string Target, string Relation);
public sealed record KnowledgeGraphDto(IReadOnlyList<KnowledgeGraphNodeDto> Nodes, IReadOnlyList<KnowledgeGraphEdgeDto> Edges);

public sealed record StudyToolsRequest(Guid WorkspaceId, Guid? DocumentId, int Count);
public sealed record FlashcardDto(string Front, string Back);
public sealed record QuizQuestionDto(string Type, string Question, IReadOnlyList<string> Options, string Answer);
public sealed record StudyToolsDto(IReadOnlyList<FlashcardDto> Flashcards, IReadOnlyList<QuizQuestionDto> Quiz);

