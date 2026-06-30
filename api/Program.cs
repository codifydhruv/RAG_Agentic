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

//step 3
builder.Services.AddSingleton(sp =>
{
    var azureClient = new AzureOpenAIClient(
        new Uri(builder.Configuration["Foundry:Endpoint"]!),
        new AzureKeyCredential(builder.Configuration["Foundry:ApiKey"]!));
    return azureClient.GetChatClient(builder.Configuration["Foundry:ChatDeployment"]!);
});

builder.Services.AddSingleton<IAnswerService, AnswerService>();


//step 4
builder.Services.AddSingleton<ITicketingTool, MockTicketingTool>();
builder.Services.AddSingleton(sp =>
{
    var registry = new ToolRegistry();
    registry.Register(new ToolDefinition
    {
        Name = "create_ticket",
        Description = "Creates a support ticket for IT, HR, or Finance issues that require human follow-up. Use this when the user reports a problem needing action, not just information.",
        RequestType = typeof(CreateTicketRequest),
        RequiresApproval = true
    });
    return registry;
});
builder.Services.AddSingleton<ITicketingTool, MockTicketingTool>();

