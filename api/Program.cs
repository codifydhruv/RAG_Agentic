builder.Services.AddSingleton(new SearchClient(
    new Uri(builder.Configuration["Search:Endpoint"]!),
    "enterprise-docs-index",
    new AzureKeyCredential(builder.Configuration["Search:AdminKey"]!)));

builder.Services.AddSingleton(sp =>
{
    var azureClient = new AzureOpenAIClient(
        new Uri(builder.Configuration["Foundry:Endpoint"]!),
        new AzureKeyCredential(builder.Configuration["Foundry:ApiKey"]!));
    return azureClient.GetEmbeddingClient(builder.Configuration["Foundry:EmbeddingDeployment"]!);
});

builder.Services.AddSingleton<IRetrievalService, RetrievalService>();
