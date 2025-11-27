using k8s.Models;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Finalizers;

public class SQLServerUserFinalizer(
    ILogger<SQLServerUserFinalizer> logger,
    IKubernetesClient kubernetesClient,
    SqlServerEndpointService sqlServerEndpointService
) : IEntityFinalizer<V1DatabaseUser>
{
    public async Task<ReconciliationResult<V1DatabaseUser>> FinalizeAsync(V1DatabaseUser entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing SQLServerUser: {Name}", entity.Metadata.Name);

        try
        {
            var sqlServer = await kubernetesClient.GetAsync<V1SQLServer>(entity.Spec.SqlServerName, entity.Metadata.NamespaceProperty);
            if (sqlServer is null)
            {
                logger.LogWarning("SQLServer instance '{SqlServerName}' not found. Skipping finalization.", entity.Spec.SqlServerName);
                return ReconciliationResult<V1DatabaseUser>.Success(entity);
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(sqlServer.Metadata.Name, sqlServer.Metadata.NamespaceProperty);
            var secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            var (username, password) = await GetSqlServerCredentialsAsync(secretName, entity.Metadata.NamespaceProperty);
            await DeleteUserAsync(entity.Spec.DatabaseName, entity.Spec.LoginName, server, username, password);

            logger.LogInformation("Finalization complete for SQLServerUser: {Name}", entity.Metadata.Name);
            return ReconciliationResult<V1DatabaseUser>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during finalization of SQLServerUser: {Name}", entity.Metadata.Name);
            return ReconciliationResult<V1DatabaseUser>.Failure(entity, ex.Message, ex);
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
