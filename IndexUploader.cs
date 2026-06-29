using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

using System.Text.Json.Serialization;

public class IndexedChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("contentVector")]
    public float[] ContentVector { get; set; } = Array.Empty<float>();

    [JsonPropertyName("sourceFile")]
    public string SourceFile { get; set; } = "";

    [JsonPropertyName("department")]
    public string Department { get; set; } = "";

    [JsonPropertyName("chunkIndex")]
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
