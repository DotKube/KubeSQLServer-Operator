var builder = DistributedApplication.CreateBuilder(args);

var k8Operator = builder.AddProject<Projects.OperatorTemplate_Operator>("k8Operator");

var docsSite = builder.AddNpmApp("documentation", "../../docs-site", "start")
    .WithEnvironment("BROWSER", "none");

builder.Build().Run();