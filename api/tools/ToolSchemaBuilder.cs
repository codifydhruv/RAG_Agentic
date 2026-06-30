using OpenAI.Chat;

public static class ToolSchemaBuilder
{
    public static ChatTool BuildCreateTicketTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "create_ticket",
            functionDescription: "Creates a support ticket for IT, HR, or Finance issues that require human follow-up. Use this when the user reports a problem needing action, not just information.",
            functionParameters: BinaryData.FromString(@"
            {
                ""type"": ""object"",
                ""properties"": {
                    ""title"": { ""type"": ""string"", ""description"": ""Short summary of the issue"" },
                    ""description"": { ""type"": ""string"", ""description"": ""Full details of the issue"" },
                    ""priority"": { ""type"": ""string"", ""enum"": [""Low"", ""Medium"", ""High"", ""Critical""] },
                    ""department"": { ""type"": ""string"", ""enum"": [""IT"", ""HR"", ""Finance""] }
                },
                ""required"": [""title"", ""description"", ""priority"", ""department""]
            }")
        );
    }
}
