using KubeOps.Operator;
using SqlServerOperator.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<DefaultMssqlConfig>();

builder.Services.AddKubernetesOperator();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseKubernetesOperator();

await app.RunOperatorAsync(args);