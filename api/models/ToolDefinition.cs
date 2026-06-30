public class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";   // what the LLM reads to decide when to use it
    public Type RequestType { get; set; } = null!;
    public bool RequiresApproval { get; set; }       // Step 6 hook — set now, enforced later
}
