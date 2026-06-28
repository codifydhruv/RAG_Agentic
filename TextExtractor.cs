using Azure.Storage.Blobs;

public class ExtractedDocument
{
    public string SourceFile { get; set; } = "";
    public string Department { get; set; } = "";
    public string Content { get; set; } = "";
}

public static class TextExtractor
{
    public static async Task<List<ExtractedDocument>> ExtractAllAsync(string connectionString)
    {
        var results = new List<ExtractedDocument>();
        var containerClient = new BlobContainerClient(connectionString, "raw-documents");

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var downloadResult = await blobClient.DownloadContentAsync();
            string content = downloadResult.Value.Content.ToString();

            string department = DepartmentResolver.Resolve(blobItem.Name);

            results.Add(new ExtractedDocument
            {
                SourceFile = blobItem.Name,
                Department = department,
                Content = content
            });

            Console.WriteLine($"Extracted: {blobItem.Name} ({department}) — {content.Length} chars");
        }

        return results;
    }
}
