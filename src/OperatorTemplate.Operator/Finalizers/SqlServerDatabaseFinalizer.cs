using k8s.Models;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Finalizers;

public class SqlServerDatabaseFinalizer(
    ILogger<SqlServerDatabaseFinalizer> logger,
    IKubernetesClient kubernetesClient,
    SqlServerEndpointService sqlServerEndpointService
) : IEntityFinalizer<V1Alpha1SQLServerDatabase>
{
    public async Task<ReconciliationResult<V1Alpha1SQLServerDatabase>> FinalizeAsync(V1Alpha1SQLServerDatabase entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing SQLServerDatabase: {Name}", entity.Metadata.Name);

        try
        {
            // Respect reclaim policy (default: Retain)
            var reclaimPolicy = entity.Spec.DatabaseReclaimPolicy ?? "Retain";
            if (!string.Equals(reclaimPolicy, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Database reclaim policy is '{Policy}'; skipping physical database deletion for: {Name}", reclaimPolicy, entity.Metadata.Name);
                return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
            }

            var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(entity.Spec.InstanceName, entity.Metadata.NamespaceProperty);
            if (sqlServer is null)
            {
                logger.LogWarning("SQLServer instance '{SqlServerName}' not found. Skipping finalization.", entity.Spec.InstanceName);
                return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(sqlServer.Metadata.Name, sqlServer.Metadata.NamespaceProperty);
            var secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            var (username, password) = await GetSqlServerCredentialsAsync(secretName, entity.Metadata.NamespaceProperty);
            await DeleteDatabaseAsync(entity.Spec.DatabaseName, server, username, password);

            logger.LogInformation("Finalization complete for SQLServerDatabase: {Name}", entity.Metadata.Name);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during finalization of SQLServerDatabase: {Name}", entity.Metadata.Name);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Failure(entity, ex.Message, ex);
        }
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(string secretName, string namespaceName)
    {
        var secret = await kubernetesClient.GetAsync<V1Secret>(secretName, namespaceName);
        if (secret?.Data is null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{secretName}' does not contain the expected 'password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var username = "sa";

        return (username, password);
    }

    private async Task DeleteDatabaseAsync(string databaseName, string server, string username, string password)
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

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var commandText = $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName) DROP DATABASE [{databaseName}]";

        using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        await command.ExecuteNonQueryAsync();
    }
}
