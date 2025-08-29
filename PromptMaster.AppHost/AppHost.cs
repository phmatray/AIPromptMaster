var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgWeb();

var bgProcessorDb = postgres
    .AddDatabase("bg-processor-db");

var promptManagerDb = postgres
    .AddDatabase("prompt-manager-db");

builder.AddProject<Projects.PromptMaster_BackgroundProcessor>("bg-processor")
    .WithReference(bgProcessorDb)
    .WaitFor(bgProcessorDb);
    
builder.AddProject<Projects.AIPromptManager>("aipromptmanager")
    .WithReference(promptManagerDb)
    .WaitFor(promptManagerDb);

builder.Build().Run();
