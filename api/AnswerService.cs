using OpenAI.Chat;

public interface IAnswerService
{
    Task<AnswerResult> AnswerAsync(string question);
}

public class AnswerService : IAnswerService
{
    private readonly IRetrievalService _retrievalService;
    private readonly ChatClient _chatClient;

    private const string RefusalPhrase =
        "I could not find relevant information in the available documents to answer this question.";

    public AnswerService(IRetrievalService retrievalService, ChatClient chatClient)
    {
        _retrievalService = retrievalService;
        _chatClient = chatClient;
    }

    public async Task<AnswerResult> AnswerAsync(string question)
    {
        var retrieval = await _retrievalService.RetrieveAsync(question);

        if (!retrieval.HasRelevantContent)
        {
            return new AnswerResult
            {
                Answer = RefusalPhrase,
                Sources = new(),
                WasRefusal = true
            };
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(PromptBuilder.SystemPrompt),
            new UserChatMessage(PromptBuilder.BuildUserPrompt(question, retrieval.Chunks))
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
        string answerText = completion.Content[0].Text;

        bool wasRefusal = answerText.Trim() == RefusalPhrase;

        return new AnswerResult
        {
            Answer = answerText,
            Sources = wasRefusal ? new() : retrieval.Chunks.Select(c => c.SourceFile).Distinct().ToList(),
            WasRefusal = wasRefusal
        };
    }
}
