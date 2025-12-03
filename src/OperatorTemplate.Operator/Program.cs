using k8s;
using KubeOps.Operator;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DefaultMssqlConfig>();
builder.Services.AddSingleton<ISqlServerEndpointService, SqlServerEndpointService>();
builder.Services.AddSingleton<ISqlExecutor, SqlExecutor>();



if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKubernetesOperator()
        .AddCrdInstaller()
        .RegisterComponents();
}
else
{
    builder.Services.AddKubernetesOperator()
        .RegisterComponents();
}

var app = builder.Build();

await app.RunAsync();