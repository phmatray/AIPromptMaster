var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgWeb();

var postgresdb = postgres
    .AddDatabase("postgresdb");



builder.AddProject<Projects.PromptMaster_BackgroundProcessor>("bg-processor")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);
    
builder.AddProject<Projects.AIPromptManager>("aipromptmanager");

builder.Build().Run();
