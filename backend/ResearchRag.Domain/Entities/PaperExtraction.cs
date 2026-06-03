using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class PaperExtraction : Entity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public string PaperTitle { get; set; } = "";
    public string AuthorsJson { get; set; } = "[]";
    public string Dataset { get; set; } = "";
    public string Model { get; set; } = "";
    public string MetricsJson { get; set; } = "[]";
    public string Accuracy { get; set; } = "";
    public string LimitationsJson { get; set; } = "[]";
    public string FutureWorkJson { get; set; } = "[]";
}

