var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.OperatorTemplate_ApiService>("apiservice");

builder.Build().Run();