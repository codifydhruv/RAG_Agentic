using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string searchEndpoint = config["SearchEndpoint"]!;
string searchAdminKey = config["SearchAdminKey"]!;
string storageConnectionString = config["StorageConnectionString"]!;
string foundryEndpoint = config["FoundryEndpoint"]!;
string foundryApiKey = config["FoundryApiKey"]!;
string embeddingDeployment = config["EmbeddingDeploymentName"]!;

var indexClient = new SearchIndexClient(new Uri(searchEndpoint), new AzureKeyCredential(searchAdminKey));
var searchClient = new SearchClient(new Uri(searchEndpoint), "enterprise-docs-index", new AzureKeyCredential(searchAdminKey));

// 2a - idempotent index creation
try
{
    await indexClient.GetIndexAsync("enterprise-docs-index");
    Console.WriteLine("Index already exists — skipping creation.");
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    await IndexSetup.CreateIndexAsync(indexClient);
}
/*
// 2c - extract
var extractedDocs = await TextExtractor.ExtractAllAsync(storageConnectionString);
Console.WriteLine($"Extracted {extractedDocs.Count} documents\n");

// 2d - chunk
var allChunks = new List<Chunk>();
foreach (var doc in extractedDocs)
{
    allChunks.AddRange(Chunker.ChunkDocument(doc));
}
Console.WriteLine($"\nTotal chunks: {allChunks.Count}\n");

// 2e - embed
var indexedChunks = await EmbeddingGenerator.EmbedChunksAsync(allChunks, foundryEndpoint, foundryApiKey, embeddingDeployment);
Console.WriteLine($"\nEmbedded {indexedChunks.Count} chunks\n");

// 2f - index
await IndexUploader.UploadChunksAsync(searchClient, indexedChunks);
*/

var azureClient = new AzureOpenAIClient(new Uri(foundryEndpoint), new AzureKeyCredential(foundryApiKey));
EmbeddingClient embeddingClient = azureClient.GetEmbeddingClient(embeddingDeployment);
await RetrievalTester.TestQueryAsync(searchClient, embeddingClient, "What is the remote work policy?");
await RetrievalTester.TestQueryAsync(searchClient, embeddingClient, "How do I submit an expense reimbursement?");
await RetrievalTester.TestQueryAsync(searchClient, embeddingClient, "What should I do during a security incident?");
