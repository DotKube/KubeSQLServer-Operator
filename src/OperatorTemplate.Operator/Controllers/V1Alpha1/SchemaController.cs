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

[EntityRbac(typeof(V1Alpha1SQLServerSchema), Verbs = RbacVerb.All)]
public class SQLServerSchemaController(
    ILogger<SQLServerSchemaController> logger,
    IKubernetesClient kubernetesClient,
    ISqlServerEndpointService sqlServerEndpointService,
    ISqlExecutor sqlExecutor
) : IEntityController<V1Alpha1SQLServerSchema>
{
    public async Task<ReconciliationResult<V1Alpha1SQLServerSchema>> ReconcileAsync(V1Alpha1SQLServerSchema entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServerSchema: {Name}", entity.Metadata.Name);

        try
        {
            // Try ExternalSQLServer first
            var externalServer = await kubernetesClient.GetAsync<V1Alpha1ExternalSQLServer>(entity.Spec.InstanceName, entity.Metadata.NamespaceProperty);
            string secretName;

            if (externalServer is not null)
            {
                secretName = externalServer.Spec.SecretName;
            }
            else
            {
                // Fall back to internal SQLServer
                var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(entity.Spec.InstanceName, entity.Metadata.NamespaceProperty);
                if (sqlServer is null)
                {
                    throw new Exception($"SQLServer or ExternalSQLServer instance '{entity.Spec.InstanceName}' not found.");
                }
                secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(entity.Spec.InstanceName, entity.Metadata.NamespaceProperty);
            var (username, password) = await GetSqlServerCredentialsAsync(secretName, entity.Metadata.NamespaceProperty);

            await EnsureSchemaExistsAsync(
                entity.Spec.DatabaseName,
                entity.Spec.SchemaName,
                entity.Spec.SchemaOwner,
                server,
                username,
                password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Schema ensured.";
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerSchema>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerSchema: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerSchema>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1SQLServerSchema>> DeletedAsync(V1Alpha1SQLServerSchema entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted SQLServerSchema: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1SQLServerSchema>.Success(entity));
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

    private async Task EnsureSchemaExistsAsync(string databaseName, string schemaName, string schemaOwner, string server, string username, string password)
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

        var schemaExistsCommandText = @"
            IF NOT EXISTS (
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name = @SchemaName
            )
            BEGIN
                DECLARE @sql NVARCHAR(MAX) = N'CREATE SCHEMA [' + @SchemaName + '] AUTHORIZATION [' + @SchemaOwner + ']';
                EXEC sp_executesql @sql;
            END";

        var parameters = new Dictionary<string, object>
        {
            ["@SchemaName"] = schemaName,
            ["@SchemaOwner"] = schemaOwner
        };

        await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, schemaExistsCommandText, parameters);
    }
}