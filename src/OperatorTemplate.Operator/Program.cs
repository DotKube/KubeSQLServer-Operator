using k8s;
using KubeOps.Operator;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<SqlServerImages>();
builder.Services.AddSingleton<DefaultMssqlConfig>();
builder.Services.AddSingleton<SqlServerEndpointService>();

// Add the Kubernetes client
Kubernetes kubernetesClient;

if (builder.Environment.IsProduction())
{
    // Use in-cluster config when running in Kubernetes
    var kubernetesConfig = KubernetesClientConfiguration.InClusterConfig();
    kubernetesClient = new Kubernetes(kubernetesConfig);
}
else
{
    // Use default kubeconfig for local development
    var kubernetesConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
    kubernetesClient = new Kubernetes(kubernetesConfig);
}

builder.Services.AddSingleton(kubernetesClient);
builder.Services.AddKubernetesOperator();

// Explicitly add Kubernetes client config
builder.Services.AddKubernetesOperator();

var app = builder.Build();

app.UseKubernetesOperator();

await app.RunOperatorAsync(args);