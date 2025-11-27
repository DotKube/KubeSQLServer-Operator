using k8s;
using KubeOps.Operator;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<SqlServerImages>();
builder.Services.AddSingleton<DefaultMssqlConfig>();
builder.Services.AddSingleton<SqlServerEndpointService>();



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