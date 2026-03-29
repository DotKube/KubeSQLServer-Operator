using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using OperatorTemplate.ExternalWorker.Services;
using SqlServerOperator.Entities.V1Alpha1;
using System.Text;

namespace OperatorTemplate.ExternalWorker.Controllers.V1Alpha1;

[EntityRbac(typeof(V1Alpha1ExternalDatabase), Verbs = RbacVerb.All)]
public class ExternalDatabaseController(
    ILogger<ExternalDatabaseController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1ExternalDatabase>
{
    private readonly string? _targetName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAME");
    private readonly string? _targetNamespace = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAMESPACE");
    private readonly string? _targetKind = Environment.GetEnvironmentVariable("TARGET_RESOURCE_KIND");

    public async Task<ReconciliationResult<V1Alpha1ExternalDatabase>> ReconcileAsync(V1Alpha1ExternalDatabase entity, CancellationToken cancellationToken)
    {
        if (_targetKind != "ExternalDatabase" || entity.Metadata.Name != _targetName || entity.Metadata.NamespaceProperty != _targetNamespace)
        {
            return ReconciliationResult<V1Alpha1ExternalDatabase>.Success(entity);
        }

        logger.LogInformation("Worker reconciling assigned ExternalDatabase: {Name}", entity.Metadata.Name);

        try
        {
            var (username, password) = await GetSqlServerCredentialsAsync(entity);
            var isAvailable = await VerifyDatabaseExistsAsync(entity.Spec.DatabaseName, entity.Spec.ServerUrl, username, password);

            entity.Status ??= new();
            entity.Status.LastChecked = DateTime.UtcNow;
            entity.Status.IsAvailable = isAvailable;

            if (isAvailable)
            {
                entity.Status.State = "Ready";
                entity.Status.Message = "External database verified.";
            }
            else
            {
                entity.Status.State = "NotAvailable";
                entity.Status.Message = $"Database '{entity.Spec.DatabaseName}' not found on server '{entity.Spec.ServerUrl}'.";
            }

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1ExternalDatabase>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of ExternalDatabase: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;
            entity.Status.IsAvailable = false;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1ExternalDatabase>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1ExternalDatabase>> DeletedAsync(V1Alpha1ExternalDatabase entity, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReconciliationResult<V1Alpha1ExternalDatabase>.Success(entity));
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(V1Alpha1ExternalDatabase entity)
    {
        var secret = await kubernetesClient.GetAsync<V1Secret>(entity.Spec.SecretName, entity.Metadata.NamespaceProperty);
        if (secret?.Data == null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{entity.Spec.SecretName}' does not contain the expected 'password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var username = secret.Data.ContainsKey("username") ? Encoding.UTF8.GetString(secret.Data["username"]) : "sa";

        return (username, password);
    }

    private async Task<bool> VerifyDatabaseExistsAsync(string databaseName, string server, string username, string password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            UserID = username,
            Password = password,
            InitialCatalog = "master",
            TrustServerCertificate = true,
            Encrypt = false,
        };

        var count = await sqlExecutor.ExecuteScalarAsync<int>(builder.ConnectionString, "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName", new Dictionary<string, object> { ["@DatabaseName"] = databaseName });
        return count > 0;
    }
}