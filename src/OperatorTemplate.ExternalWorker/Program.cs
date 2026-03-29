using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OperatorTemplate.ExternalWorker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddKubernetesOperator()
    .RegisterComponents();

builder.Services.AddScoped<ISqlExecutor, SqlExecutor>();

var app = builder.Build();

app.MapDefaultEndpoints();
// In KubeOps 10, UseKubernetesOperator is not used on WebApplication in the same way or has changed.
// Actually, KubeOps 10 usually handles it via the SDK's internal wiring or specific Map/Use calls.
// Let's check Operator project's Program.cs again.

await app.RunAsync();