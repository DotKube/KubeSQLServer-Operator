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

[EntityRbac(typeof(V1Alpha1DatabaseUser), Verbs = RbacVerb.All)]
public class SQLServerUserController(
    ILogger<SQLServerUserController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1DatabaseUser>
{
    private readonly string? _targetName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAME");
    private readonly string? _targetNamespace = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAMESPACE");
    private readonly string? _targetKind = Environment.GetEnvironmentVariable("TARGET_RESOURCE_KIND");
    private readonly string? _sqlServerUrl = Environment.GetEnvironmentVariable("SQL_SERVER_URL");
    private readonly string? _sqlServerPort = Environment.GetEnvironmentVariable("SQL_SERVER_PORT");
    private readonly string? _sqlSecretName = Environment.GetEnvironmentVariable("SQL_SECRET_NAME");

    public async Task<ReconciliationResult<V1Alpha1DatabaseUser>> ReconcileAsync(V1Alpha1DatabaseUser entity, CancellationToken cancellationToken)
    {
        bool isTarget = false;
        string? databaseName = null;

        if (_targetKind == "ExternalDatabase" && entity.Spec.DatabaseRef == _targetName && !string.IsNullOrEmpty(_targetName))
        {
            isTarget = true;
            // For ExternalDatabase, the target itself IS the database
            var extDb = await kubernetesClient.GetAsync<V1Alpha1ExternalDatabase>(_targetName, _targetNamespace ?? entity.Metadata.NamespaceProperty);
            databaseName = extDb?.Spec.DatabaseName;
        }
        else if (_targetKind == "ExternalSQLServer" && entity.Spec.SqlServerName == _targetName)
        {
            isTarget = true;
            databaseName = entity.Spec.DatabaseName;
        }

        if (!isTarget || string.IsNullOrEmpty(databaseName))
        {
            return ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity);
        }

        logger.LogInformation("Worker reconciling assigned SQLServerUser: {Name} for Database: {Db}", entity.Metadata.Name, databaseName);

        try
        {
            if (string.IsNullOrEmpty(_sqlServerUrl) || string.IsNullOrEmpty(_sqlSecretName))
            {
                throw new Exception("Worker is missing SQL connection configuration.");
            }

            var (username, password) = await GetSqlServerCredentialsAsync(_sqlSecretName, _targetNamespace ?? entity.Metadata.NamespaceProperty);
            var server = $"{_sqlServerUrl},{_sqlServerPort ?? "1433"}";

            await EnsureUserExistsAsync(databaseName, entity.Spec.LoginName, entity.Spec.Roles, server, username, password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Database user ensured by worker.";
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerUser: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1DatabaseUser>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1DatabaseUser>> DeletedAsync(V1Alpha1DatabaseUser entity, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity));
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

    private async Task EnsureUserExistsAsync(string databaseName, string loginName, List<string> roles, string server, string username, string password)
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
        IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = @LoginName)
        BEGIN
            DECLARE @sql NVARCHAR(MAX) = N'CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + ']';
            EXEC sp_executesql @sql;
        END";

        await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, commandText, new Dictionary<string, object> { ["@LoginName"] = loginName });

        foreach (var role in roles)
        {
            await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, "EXEC sp_addrolemember @rolename, @membername", new Dictionary<string, object>
            {
                ["@rolename"] = role,
                ["@membername"] = loginName
            });
        }
    }
}