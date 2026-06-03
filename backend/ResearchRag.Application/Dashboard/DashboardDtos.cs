namespace ResearchRag.Application.Dashboard;

public sealed record DashboardDto(
    int TotalPapers,
    int TotalChats,
    int TotalQueries,
    IReadOnlyList<TopicCountDto> MostStudiedTopics,
    IReadOnlyList<RecentDocumentDto> RecentlyUploadedPapers,
    IReadOnlyList<YearCountDto> PapersPerYear);

public sealed record TopicCountDto(string Topic, int Count);
public sealed record RecentDocumentDto(Guid Id, string Name, string Status, DateTimeOffset UploadedAt);
public sealed record YearCountDto(int Year, int Count);

