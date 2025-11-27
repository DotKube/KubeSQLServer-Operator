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

[EntityRbac(typeof(V1Alpha1SQLServerLogin), Verbs = RbacVerb.All)]
public class SQLServerLoginController(
    ILogger<SQLServerLoginController> logger,
    IKubernetesClient kubernetesClient,
    SqlServerEndpointService sqlServerEndpointService
) : IEntityController<V1Alpha1SQLServerLogin>
{
    public async Task<ReconciliationResult<V1Alpha1SQLServerLogin>> ReconcileAsync(V1Alpha1SQLServerLogin entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServerLogin: {Name}", entity.Metadata.Name);

        try
        {
            var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(entity.Spec.SqlServerName, entity.Metadata.NamespaceProperty);
            if (sqlServer is null)
            {
                throw new Exception($"SQLServer instance '{entity.Spec.SqlServerName}' not found.");
            }

            var server = await sqlServerEndpointService.GetSqlServerEndpointAsync(sqlServer.Metadata.Name, sqlServer.Metadata.NamespaceProperty);
            var secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
            var (username, password) = await GetSqlServerCredentialsAsync(secretName, entity.Metadata.NamespaceProperty);
            await EnsureLoginExistsAsync(entity.Spec.LoginName, entity.Spec.AuthenticationType, server, username, password);

            entity.Status ??= new();
            entity.Status.State = "Ready";
            entity.Status.Message = "Login ensured.";
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerLogin>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerLogin: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1SQLServerLogin>.Failure(entity, ex.Message, ex);
        }
    }

    public Task<ReconciliationResult<V1Alpha1SQLServerLogin>> DeletedAsync(V1Alpha1SQLServerLogin entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted SQLServerLogin: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1SQLServerLogin>.Success(entity));
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


    private async Task EnsureLoginExistsAsync(string loginName, string authenticationType, string server, string username, string password)
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

        var commandText = $@"
        IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'{loginName}')
        BEGIN
            CREATE LOGIN [{loginName}] WITH PASSWORD = '{password}';
        END";

        using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync();
    }



}
