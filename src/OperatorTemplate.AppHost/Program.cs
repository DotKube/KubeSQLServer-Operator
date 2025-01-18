var builder = DistributedApplication.CreateBuilder(args);

var k8Operator = builder.AddProject<Projects.OperatorTemplate_Operator>("k8Operator");

builder.Build().Run();