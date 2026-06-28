using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

public class IndexedChunk
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public float[] ContentVector { get; set; } = Array.Empty<float>();
    public string SourceFile { get; set; } = "";
    public string Department { get; set; } = "";
    public int ChunkIndex { get; set; }
}

public static class IndexUploader
{
    public static async Task UploadChunksAsync(SearchClient searchClient, List<IndexedChunk> chunks)
    {
        // Sanitize id: Search keys can't contain certain characters
        foreach (var chunk in chunks)
        {
            string safeFile = chunk.SourceFile.Replace(".", "_").Replace(" ", "_");
            chunk.Id = $"{safeFile}_chunk_{chunk.ChunkIndex}";
        }

        var batch = IndexDocumentsBatch.Upload(chunks);
        var response = await searchClient.IndexDocumentsAsync(batch);

        int successCount = 0, failCount = 0;
        foreach (var result in response.Value.Results)
        {
            if (result.Succeeded)
            {
                successCount++;
            }
            else
            {
                failCount++;
                Console.WriteLine($"FAILED to index '{result.Key}': {result.ErrorMessage}");
            }
        }

        Console.WriteLine($"Indexing complete: {successCount} succeeded, {failCount} failed.");
    }
}
