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

[EntityRbac(typeof(V1Alpha1SQLServerDatabase), Verbs = RbacVerb.All)]
public class SQLServerDatabaseController(
    ILogger<SQLServerDatabaseController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1SQLServerDatabase>
{
    private readonly string? _targetName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAME");
    private readonly string? _targetNamespace = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAMESPACE");
    private readonly string? _targetKind = Environment.GetEnvironmentVariable("TARGET_RESOURCE_KIND");
    private readonly string? _sqlServerUrl = Environment.GetEnvironmentVariable("SQL_SERVER_URL");
    private readonly string? _sqlServerPort = Environment.GetEnvironmentVariable("SQL_SERVER_PORT");
    private readonly string? _sqlSecretName = Environment.GetEnvironmentVariable("SQL_SECRET_NAME");

    public async Task<ReconciliationResult<V1Alpha1SQLServerDatabase>> ReconcileAsync(V1Alpha1SQLServerDatabase entity, CancellationToken cancellationToken)
    {
        if (_targetKind != "ExternalSQLServer" || entity.Spec.InstanceName != _targetName)
        {
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity);
        }

        logger.LogInformation("Worker reconciling assigned SQLServerDatabase: {Name}", entity.Metadata.Name);

        try
        {
            if (string.IsNullOrEmpty(_sqlServerUrl) || string.IsNullOrEmpty(_sqlSecretName))
            {
                throw new Exception("Worker is missing SQL connection configuration.");
            }

            var (username, password) = await GetSqlServerCredentialsAsync(_sqlSecretName, _targetNamespace ?? entity.Metadata.NamespaceProperty);
            var server = $"{_sqlServerUrl},{_sqlServerPort ?? "1433"}";

            await EnsureDatabaseExistsAsync(entity.Spec.DatabaseName, server, username, password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Database ensured by worker.";
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerDatabase: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerDatabase>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1SQLServerDatabase>> DeletedAsync(V1Alpha1SQLServerDatabase entity, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReconciliationResult<V1Alpha1SQLServerDatabase>.Success(entity));
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

        var commandText = @"
        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
        BEGIN
            DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE [' + @DatabaseName + ']';
            EXEC sp_executesql @sql;
        END";

        await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, commandText, new Dictionary<string, object> { ["@DatabaseName"] = databaseName });
    }
}
