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

[EntityRbac(typeof(V1Alpha1DatabaseUser), Verbs = RbacVerb.All)]
public class SQLServerUserController(
    ILogger<SQLServerUserController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor,
    IDatabaseReferenceResolver databaseReferenceResolver
) : IEntityController<V1Alpha1DatabaseUser>
{
    public async Task<ReconciliationResult<V1Alpha1DatabaseUser>> ReconcileAsync(V1Alpha1DatabaseUser entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServerUser: {Name}", entity.Metadata.Name);

        try
        {
            var resolvedDb = await databaseReferenceResolver.ResolveAsync(
                entity.Spec.DatabaseRef,
                entity.Spec.SqlServerName,
                entity.Spec.DatabaseName,
                entity.Metadata.NamespaceProperty);

            var (username, password) = await GetSqlServerCredentialsAsync(resolvedDb.SecretName, entity.Metadata.NamespaceProperty);
            await EnsureUserExistsAsync(resolvedDb.DatabaseName!, entity.Spec.LoginName, entity.Spec.Roles, resolvedDb.Host, username, password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Database user ensured.";
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
        logger.LogInformation("Deleted SQLServerUser: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1DatabaseUser>.Success(entity));
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(string secretName, string namespaceName)
    {
        var secret = await kubernetesClient.GetAsync<V1Secret>(secretName, namespaceName);
        if (secret is null)
        {
            throw new Exception($"Secret '{secretName}' not found in namespace '{namespaceName}'.");
        }

        if (secret.Data is null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{secretName}' does not contain the expected 'password' key.");
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

        var parameters = new Dictionary<string, object>
        {
            ["@LoginName"] = loginName
        };

        await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, commandText, parameters);

        foreach (var role in roles)
        {
            var roleCommandText = "EXEC sp_addrolemember @rolename, @membername";
            var roleParameters = new Dictionary<string, object>
            {
                ["@rolename"] = role,
                ["@membername"] = loginName
            };
            await sqlExecutor.ExecuteNonQueryAsync(builder.ConnectionString, roleCommandText, roleParameters);
        }
    }

}