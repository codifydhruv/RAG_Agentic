public class PendingAction
{
    public string ToolName { get; set; } = "";
    public object Request { get; set; } = null!;
    public string Status { get; set; } = "AwaitingApproval"; // only status that exists until Step 6
}

public class AgentResponse
{
    public bool RequiresApproval { get; set; }
    public PendingAction? PendingAction { get; set; }
    public string? Message { get; set; }
    public object? AnswerResult { get; set; } // populated only on the non-tool path
}
