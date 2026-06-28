using Azure;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

public static class EmbeddingGenerator
{
    private const int BatchSize = 16;
    private const int MaxRetries = 3;

    public static async Task<List<IndexedChunk>> EmbedChunksAsync(
        List<Chunk> chunks,
        string foundryEndpoint,
        string foundryApiKey,
        string deploymentName)
    {
        var azureClient = new AzureOpenAIClient(
            new Uri(foundryEndpoint),
            new AzureKeyCredential(foundryApiKey));

        EmbeddingClient embeddingClient = azureClient.GetEmbeddingClient(deploymentName);

        var results = new List<IndexedChunk>();

        for (int i = 0; i < chunks.Count; i += BatchSize)
        {
            var batch = chunks.Skip(i).Take(BatchSize).ToList();
            var texts = batch.Select(c => c.Content).ToList();

            OpenAIEmbeddingCollection embeddings = await GetEmbeddingsWithRetryAsync(embeddingClient, texts);

            for (int j = 0; j < batch.Count; j++)
            {
                results.Add(new IndexedChunk
                {
                    Content = batch[j].Content,
                    ContentVector = embeddings[j].ToFloats().ToArray(),
                    SourceFile = batch[j].SourceFile,
                    Department = batch[j].Department,
                    ChunkIndex = batch[j].ChunkIndex
                });
            }

            Console.WriteLine($"Embedded batch {i / BatchSize + 1}: {batch.Count} chunks");
        }

        return results;
    }

    private static async Task<OpenAIEmbeddingCollection> GetEmbeddingsWithRetryAsync(
        EmbeddingClient client, List<string> texts)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                var response = await client.GenerateEmbeddingsAsync(texts);
                return response.Value;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                attempt++;
                int delaySeconds = (int)Math.Pow(2, attempt); // 2s, 4s, 8s
                Console.WriteLine($"Embedding call failed (attempt {attempt}): {ex.Message}. Retrying in {delaySeconds}s...");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}
