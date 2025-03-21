var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GD_Api>("gd-api");
builder.AddProject<Projects.GD>("gd-pwa");

builder.Build().Run();
