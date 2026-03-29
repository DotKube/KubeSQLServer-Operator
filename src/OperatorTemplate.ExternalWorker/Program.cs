using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKubernetesOperator();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseKubernetesOperator();
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

app.Run();