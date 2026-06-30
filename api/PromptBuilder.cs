public static class PromptBuilder
{
    public const string SystemPrompt = @"
You are an internal enterprise assistant. You answer questions using ONLY the provided document excerpts below.

Rules you must follow strictly:
1. Use only the information in the excerpts. Do not use outside knowledge, even if you know the answer.
2. Every factual claim must be attributable to a specific excerpt. Cite the source file in parentheses after each claim, e.g. (Source: HR_Leave_Policy.md).
3. If the excerpts only partially answer the question, answer what you can and explicitly state what is not covered.
4. If the excerpts do not contain enough information to answer at all, respond exactly with: 'I could not find relevant information in the available documents to answer this question.'
5. Do not speculate, infer company policy beyond what is written, or fill gaps with general knowledge.
";

    public static string BuildUserPrompt(string question, List<RetrievedChunk> chunks)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Document excerpts:");
        sb.AppendLine();

        foreach (var chunk in chunks)
        {
            sb.AppendLine($"[Source: {chunk.SourceFile}]");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        sb.AppendLine($"Question: {question}");
        return sb.ToString();
    }
}
