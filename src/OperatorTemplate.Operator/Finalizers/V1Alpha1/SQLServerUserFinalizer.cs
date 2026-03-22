using k8s.Models;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using System.Text;

namespace SqlServerOperator.Finalizers.V1Alpha1;

public class SQLServerUserFinalizer(
    ILogger<SQLServerUserFinalizer> logger,
    IKubernetesClient kubernetesClient,
    ISqlServerEndpointService sqlServerEndpointService,
    IDatabaseReferenceResolver databaseReferenceResolver
) : IEntityFinalizer<V1Alpha1DatabaseUser>
{
    public async Task<ReconciliationResult<V1Alpha1DatabaseUser>> FinalizeAsync(V1Alpha1DatabaseUser entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing SQLServerUser: {Name}", entity.Metadata.Name);

        try
        {
            var resolvedDb = await databaseReferenceResolver.ResolveAsync(
                entity.Spec.DatabaseRef,
                entity.Spec.SqlServerName,
                entity.Spec.DatabaseName,
                entity.Metadata.NamespaceProperty);

            // Try ExternalSQLServer first
            var externalServer = await kubernetesClient.GetAsync<V1Alpha1ExternalSQLServer>(resolvedDb.InstanceName, entity.Metadata.NamespaceProperty);
            string secretName;

            if (externalServer is not null)
            {
                secretName = externalServer.Spec.SecretName;
            }
            else
            {
                // Fall back to internal SQLServer
                var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(resolvedDb.InstanceName, entity.Metadata.NamespaceProperty);
                if (sqlServer is null)
                {
                    logger.LogWarning("SQLServer instance '{SqlServerName}' not found. Skipping finalization.", resolvedDb.InstanceName);
                    return ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity);
                }
                secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(resolvedDb.InstanceName, entity.Metadata.NamespaceProperty);
            var (username, password) = await GetSqlServerCredentialsAsync(secretName, entity.Metadata.NamespaceProperty);
            await DeleteUserAsync(resolvedDb.DatabaseName!, entity.Spec.LoginName, server, username, password);

            logger.LogInformation("Finalization complete for SQLServerUser: {Name}", entity.Metadata.Name);
            return ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during finalization of SQLServerUser: {Name}", entity.Metadata.Name);
            // If the database or instance is gone, we can't do much, so we allow deletion to proceed
            return ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity);
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

    private async Task DeleteUserAsync(string databaseName, string loginName, string server, string username, string password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            UserID = username,
            Password = password,
            InitialCatalog = databaseName,
            TrustServerCertificate = true,
            Encrypt = false,
        };

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var commandText = $@"
            IF EXISTS (SELECT name FROM sys.database_principals WHERE name = @LoginName)
            BEGIN
                DROP USER [{loginName}];
            END";

        using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@LoginName", loginName);
        await command.ExecuteNonQueryAsync();
    }
}