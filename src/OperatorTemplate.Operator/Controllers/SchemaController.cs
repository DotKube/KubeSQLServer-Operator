using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServerSchema), Verbs = RbacVerb.All)]
public class SQLServerSchemaController(
    ILogger<SQLServerSchemaController> logger,
    IKubernetesClient kubernetesClient,
    SqlServerEndpointService sqlServerEndpointService
) : IEntityController<V1SQLServerSchema>
{
    public async Task<ReconciliationResult<V1SQLServerSchema>> ReconcileAsync(V1SQLServerSchema entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServerSchema: {Name}", entity.Metadata.Name);

        try
        {
            var sqlServer = await kubernetesClient.GetAsync<V1SQLServer>(entity.Spec.InstanceName, entity.Metadata.NamespaceProperty);
            if (sqlServer is null)
            {
                throw new Exception($"SQLServer instance '{entity.Spec.InstanceName}' not found.");
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(sqlServer.Metadata.Name, sqlServer.Metadata.NamespaceProperty);
            var secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
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
            return ReconciliationResult<V1SQLServerSchema>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerSchema: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1SQLServerSchema>.Failure(entity, ex.Message, ex);
        }
    }

    public Task<ReconciliationResult<V1SQLServerSchema>> DeletedAsync(V1SQLServerSchema entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted SQLServerSchema: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1SQLServerSchema>.Success(entity));
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

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var schemaExistsCommandText = $@"
            IF NOT EXISTS (
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name = @schemaName
            )
            BEGIN
                EXEC('CREATE SCHEMA [{schemaName}] AUTHORIZATION [{schemaOwner}]');
            END";

        using var command = new SqlCommand(schemaExistsCommandText, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        await command.ExecuteNonQueryAsync();
    }
}
