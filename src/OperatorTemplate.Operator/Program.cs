using KubeOps.Operator;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<SqlServerImages>();
builder.Services.AddSingleton<DefaultMssqlConfig>();
builder.Services.AddSingleton<SqlServerEndpointService>();

builder.Services.AddKubernetesOperator();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseKubernetesOperator();

await app.RunOperatorAsync(args);