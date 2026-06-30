public class AgentOrchestrator
{
    private readonly ChatClient _chatClient;
    private readonly IAnswerService _answerService;

    public AgentOrchestrator(ChatClient chatClient, IAnswerService answerService)
    {
        _chatClient = chatClient;
        _answerService = answerService;
    }

    public async Task<object> HandleAsync(string question)
    {
        var options = new ChatCompletionOptions();
        options.Tools.Add(ToolSchemaBuilder.BuildCreateTicketTool());

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(RouterSystemPrompt),
            new UserChatMessage(question)
        };

        ChatCompletion routerResult = await _chatClient.CompleteChatAsync(messages, options);

        if (routerResult.ToolCalls.Count > 0)
        {
            // Step 5b handles this branch next
            throw new NotImplementedException("Tool execution path — built in 5b");
        }

        // No tool needed — fall through to the existing grounded RAG pipeline (Step 3)
        return await _answerService.AnswerAsync(question);
    }
}
