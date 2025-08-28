var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AIPromptManager>("aipromptmanager");

builder.Build().Run();
