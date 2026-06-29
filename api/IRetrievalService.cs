public interface IRetrievalService
{
    Task<RetrievalResult> RetrieveAsync(string question, string? departmentFilter = null);
}

public class RetrievalResult
{
    public bool HasRelevantContent { get; set; }
    public List<RetrievedChunk> Chunks { get; set; } = new();
}

public class RetrievedChunk
{
    public string Content { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string Department { get; set; } = "";
    public double Score { get; set; }
}
