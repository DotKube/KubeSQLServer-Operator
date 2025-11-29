using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1Alpha1SQLServerDatabase), Verbs = RbacVerb.All)]
public class SQLServerDatabaseController(
    ILogger<SQLServerDatabaseController> logger,
    IKubernetesClient kubernetesClient,
    DefaultMssqlConfig config,
    SqlServerEndpointService sqlServerEndpointService) 
    : IEntityController<V1Alpha1SQLServerDatabase>
{
    public async Task<ReconciliationResult<V1Alpha1SQLServerDatabase>> ReconcileAsync(V1Alpha1SQLServerDatabase entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServerDatabase: {Name}", entity.Metadata.Name);

        try
        {
            var secretName = await DetermineSecretNameAsync(entity);
            var (server, username, password) = await GetSqlServerCredentialsAsync(entity, secretName);
            await EnsureDatabaseExistsAsync(entity.Spec.DatabaseName, server, username, password);
            await UpdateStatusAsync(entity, "Ready", "Database ensured.", DateTime.UtcNow);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerDatabase: {Name}", entity.Metadata.Name);
            await UpdateStatusAsync(entity, "Error", ex.Message, DateTime.UtcNow);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Failure(entity, ex.Message, ex);
        }
    }

    public async Task<ReconciliationResult<V1Alpha1SQLServerDatabase>> DeletedAsync(V1Alpha1SQLServerDatabase entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted SQLServerDatabase: {Name}", entity.Metadata.Name);

        try
        {
            // Respect reclaim policy (default: Retain)
            var reclaimPolicy = entity.Spec.DatabaseReclaimPolicy ?? "Retain";
            if (!string.Equals(reclaimPolicy, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Database reclaim policy is '{Policy}'; skipping physical database deletion for: {Name}", reclaimPolicy, entity.Metadata.Name);
                return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
            }

            var secretName = await DetermineSecretNameAsync(entity);
            var (server, username, password) = await GetSqlServerCredentialsAsync(entity, secretName);

            await EnsureDatabaseDeletedAsync(entity.Spec.DatabaseName, server, username, password);

            logger.LogInformation("Database '{DatabaseName}' deletion attempted for SQLServerDatabase: {Name}", entity.Spec.DatabaseName, entity.Metadata.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting database for SQLServerDatabase: {Name}", entity.Metadata.Name);
        }

        return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
    }

    private async Task<string> DetermineSecretNameAsync(V1Alpha1SQLServerDatabase entity)
    {
        var instanceName = entity.Spec.InstanceName;
        var namespaceName = entity.Metadata.NamespaceProperty;
        var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(instanceName, namespaceName);

        if (sqlServer is null)
        {
            throw new Exception($"SQLServer instance '{instanceName}' not found in namespace '{namespaceName}'.");
        }

        return sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
    }

    private async Task<(string server, string username, string password)> GetSqlServerCredentialsAsync(V1Alpha1SQLServerDatabase entity, string secretName)
    {
        var namespaceName = entity.Metadata.NamespaceProperty;
        var secret = await kubernetesClient.GetAsync<V1Secret>(secretName, namespaceName);

        if (secret?.Data == null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{secretName}' does not contain the expected 'password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(entity.Spec.InstanceName, namespaceName);
        var username = "sa";

        return (server, username, password);
    }

    private async Task EnsureDatabaseExistsAsync(string databaseName, string server, string username, string password)
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

        var connectionString = builder.ConnectionString;

        logger.LogInformation("Ensuring database '{DatabaseName}' on server '{Server}'.", databaseName, server);
        logger.LogInformation("Connection string: {ConnectionString}", connectionString);

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var commandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName) CREATE DATABASE [{databaseName}]";
            using var command = new SqlCommand(commandText, connection);
            command.Parameters.AddWithValue("@DatabaseName", databaseName);

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{DatabaseName}' ensured on server '{Server}'.", databaseName, server);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to ensure database '{databaseName}': {ex.Message}", ex);
        }
    }

    private async Task EnsureDatabaseDeletedAsync(string databaseName, string server, string username, string password)
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

        var connectionString = builder.ConnectionString;

        logger.LogInformation("Ensuring database '{DatabaseName}' is deleted on server '{Server}'.", databaseName, server);

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var commandText = $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName) DROP DATABASE [{databaseName}]";
            using var command = new SqlCommand(commandText, connection);
            command.Parameters.AddWithValue("@DatabaseName", databaseName);

            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{DatabaseName}' dropped on server '{Server}'.", databaseName, server);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete database '{databaseName}': {ex.Message}", ex);
        }
    }

    private async Task UpdateStatusAsync(V1Alpha1SQLServerDatabase entity, string state, string message, DateTime? lastChecked)
    {
        entity.Status ??= new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseStatus();
        entity.Status.State = state;
        entity.Status.Message = message;
        entity.Status.LastChecked = lastChecked;

        await kubernetesClient.UpdateStatusAsync(entity);
        logger.LogInformation("Updated status for SQLServerDatabase: {Name} to State: {State}, Message: {Message}", entity.Metadata.Name, state, message);
    }
}
