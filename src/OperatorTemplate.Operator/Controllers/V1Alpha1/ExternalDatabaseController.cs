using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using System.Text;

namespace SqlServerOperator.Controllers.V1Alpha1;

[EntityRbac(typeof(V1Alpha1ExternalDatabase), Verbs = RbacVerb.All)]
public class ExternalDatabaseController(
    ILogger<ExternalDatabaseController> logger,
    IKubernetesClient kubernetesClient,
    ISqlServerEndpointService sqlServerEndpointService,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1ExternalDatabase>
{
    public async Task<ReconciliationResult<V1Alpha1ExternalDatabase>> ReconcileAsync(V1Alpha1ExternalDatabase entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling ExternalDatabase: {Name}", entity.Metadata.Name);

        try
        {
            var (server, username, password) = await GetSqlServerCredentialsAsync(entity);
            var isAvailable = await VerifyDatabaseExistsAsync(entity.Spec.DatabaseName, server, username, password);

            if (isAvailable)
            {
                await UpdateStatusAsync(entity, "Ready", "External database verified.", DateTime.UtcNow, true);
            }
            else
            {
                await UpdateStatusAsync(entity, "NotAvailable", $"Database '{entity.Spec.DatabaseName}' not found on instance '{entity.Spec.InstanceName}'.", DateTime.UtcNow, false);
            }

            return ReconciliationResult<V1Alpha1ExternalDatabase>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of ExternalDatabase: {Name}", entity.Metadata.Name);
            await UpdateStatusAsync(entity, "Error", ex.Message, DateTime.UtcNow, false);
            return ReconciliationResult<V1Alpha1ExternalDatabase>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1ExternalDatabase>> DeletedAsync(V1Alpha1ExternalDatabase entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted ExternalDatabase: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1ExternalDatabase>.Success(entity));
    }

    private async Task<(string server, string username, string password)> GetSqlServerCredentialsAsync(V1Alpha1ExternalDatabase entity)
    {
        var instanceName = entity.Spec.InstanceName;
        var namespaceName = entity.Metadata.NamespaceProperty;

        string secretName;
        // Try ExternalSQLServer first
        var externalServer = await kubernetesClient.GetAsync<V1Alpha1ExternalSQLServer>(instanceName, namespaceName);
        if (externalServer is not null)
        {
            secretName = externalServer.Spec.SecretName;
        }
        else
        {
            // Fall back to internal SQLServer
            var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(instanceName, namespaceName);
            if (sqlServer is not null)
            {
                secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            }
            else
            {
                throw new Exception($"SQLServer or ExternalSQLServer instance '{instanceName}' not found in namespace '{namespaceName}'.");
            }
        }

        var secret = await kubernetesClient.GetAsync<V1Secret>(secretName, namespaceName);
        if (secret?.Data == null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{secretName}' does not contain the expected 'password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(instanceName, namespaceName);
        var username = secret.Data.ContainsKey("username") ? Encoding.UTF8.GetString(secret.Data["username"]) : "sa";

        return (server, username, password);
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

        var commandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
        var parameters = new Dictionary<string, object>
        {
            ["@DatabaseName"] = databaseName
        };

        var count = await sqlExecutor.ExecuteScalarAsync<int>(builder.ConnectionString, commandText, parameters);
        return count > 0;
    }

    private async Task UpdateStatusAsync(V1Alpha1ExternalDatabase entity, string state, string message, DateTime? lastChecked, bool isAvailable)
    {
        entity.Status ??= new V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseStatus();
        entity.Status.State = state;
        entity.Status.Message = message;
        entity.Status.LastChecked = lastChecked;
        entity.Status.IsAvailable = isAvailable;

        await kubernetesClient.UpdateStatusAsync(entity);
        logger.LogInformation("Updated status for ExternalDatabase: {Name} to State: {State}, Message: {Message}", entity.Metadata.Name, state, message);
    }
}