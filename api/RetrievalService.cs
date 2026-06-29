using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAI.Embeddings;

public class RetrievalService : IRetrievalService
{
    private readonly SearchClient _searchClient;
    private readonly EmbeddingClient _embeddingClient;
    private const double RelevanceThreshold = 0.55; // tune based on Step 2g data
    private const int TopK = 3;

    public RetrievalService(SearchClient searchClient, EmbeddingClient embeddingClient)
    {
        _searchClient = searchClient;
        _embeddingClient = embeddingClient;
    }

    public async Task<RetrievalResult> RetrieveAsync(string question, string? departmentFilter = null)
    {
        var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(question);
        float[] queryVector = embeddingResponse.Value.ToFloats().ToArray();

        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(queryVector) { KNearestNeighborsCount = TopK, Fields = { "contentVector" } } }
            },
            Size = TopK
        };

        if (!string.IsNullOrEmpty(departmentFilter))
        {
            searchOptions.Filter = $"department eq '{departmentFilter}'";
        }

        var response = await _searchClient.SearchAsync<IndexedChunk>(null, searchOptions);

        var allResults = new List<RetrievedChunk>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            allResults.Add(new RetrievedChunk
            {
                Content = result.Document.Content,
                SourceFile = result.Document.SourceFile,
                Department = result.Document.Department,
                Score = result.Score ?? 0
            });
        }

        if (allResults.Count == 0 || allResults[0].Score < RelevanceThreshold)
        {
            return new RetrievalResult { HasRelevantContent = false, Chunks = new() };
        }

        return new RetrievalResult
        {
            HasRelevantContent = true,
            Chunks = allResults.Where(c => c.Score >= RelevanceThreshold * 0.8).ToList()
            // 0.8 multiplier: lets reasonably-strong supporting chunks through even if
            // slightly below the hard cutoff, without re-opening the door to weak/irrelevant matches
        };
    }
}
