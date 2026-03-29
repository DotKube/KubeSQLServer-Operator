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

[EntityRbac(typeof(V1Alpha1SQLServerSchema), Verbs = RbacVerb.All)]
public class SQLServerSchemaController(
    ILogger<SQLServerSchemaController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1SQLServerSchema>
{
    private readonly string? _targetName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAME");
    private readonly string? _targetNamespace = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAMESPACE");
    private readonly string? _targetKind = Environment.GetEnvironmentVariable("TARGET_RESOURCE_KIND");
    private readonly string? _sqlServerUrl = Environment.GetEnvironmentVariable("SQL_SERVER_URL");
    private readonly string? _sqlServerPort = Environment.GetEnvironmentVariable("SQL_SERVER_PORT");
    private readonly string? _sqlSecretName = Environment.GetEnvironmentVariable("SQL_SECRET_NAME");

    public async Task<ReconciliationResult<V1Alpha1SQLServerSchema>> ReconcileAsync(V1Alpha1SQLServerSchema entity, CancellationToken cancellationToken)
    {
        bool isTarget = false;
        string? databaseName = null;

        if (_targetKind == "ExternalDatabase" && entity.Spec.DatabaseRef == _targetName && !string.IsNullOrEmpty(_targetName))
        {
            isTarget = true;
            var extDb = await kubernetesClient.GetAsync<V1Alpha1ExternalDatabase>(_targetName, _targetNamespace ?? entity.Metadata.NamespaceProperty);
            databaseName = extDb?.Spec.DatabaseName;
        }
        else if (_targetKind == "ExternalSQLServer" && entity.Spec.InstanceName == _targetName)
        {
            isTarget = true;
            databaseName = entity.Spec.DatabaseName;
        }

        if (!isTarget || string.IsNullOrEmpty(databaseName))
        {
            return ReconciliationResult<V1Alpha1SQLServerSchema>.Success(entity);
        }

        logger.LogInformation("Worker reconciling assigned SQLServerSchema: {Name} for Database: {Db}", entity.Metadata.Name, databaseName);

        try
        {
            if (string.IsNullOrEmpty(_sqlServerUrl) || string.IsNullOrEmpty(_sqlSecretName))
            {
                throw new Exception("Worker is missing SQL connection configuration.");
            }

            var (username, password) = await GetSqlServerCredentialsAsync(_sqlSecretName, _targetNamespace ?? entity.Metadata.NamespaceProperty);
            var server = $"{_sqlServerUrl},{_sqlServerPort ?? "1433"}";

            await EnsureSchemaExistsAsync(databaseName, entity.Spec.SchemaName, entity.Spec.SchemaOwner, server, username, password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Schema ensured by worker.";
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
        return Task.FromResult(ReconciliationResult<V1Alpha1SQLServerSchema>.Success(entity));
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(string secretName, string namespaceName)
    {
        var secret = await kubernetesClient.GetAsync<V1Secret>(secretName, namespaceName);
        if (secret?.Data == null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{secretName}' not found or missing 'password'.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var username = secret.Data.ContainsKey("username") ? Encoding.UTF8.GetString(secret.Data["username"]) : "sa";

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

        var commandText = @"
            IF @SchemaOwner <> 'dbo' AND NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @SchemaOwner)
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @SchemaOwner)
                BEGIN
                    DECLARE @createUserSql NVARCHAR(MAX) = N'CREATE USER [' + @SchemaOwner + '] FOR LOGIN [' + @SchemaOwner + ']';
                    EXEC sp_executesql @createUserSql;
                END
            END

            IF NOT EXISTS (
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name = @SchemaName
            )
            BEGIN
                DECLARE @sql NVARCHAR(MAX) = N'CREATE SCHEMA [' + @SchemaName + '] AUTHORIZATION [' + @SchemaOwner + ']';
                EXEC sp_executesql @sql;
            END";

        await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, commandText, new Dictionary<string, object>
        {
            ["@SchemaName"] = schemaName,
            ["@SchemaOwner"] = schemaOwner
        });
    }
}