using KubeOps.Operator;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
builder.Services.AddKubernetesOperator();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseKubernetesOperator();

await app.RunOperatorAsync(args);