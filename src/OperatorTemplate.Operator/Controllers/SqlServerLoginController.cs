using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServerLogin), Verbs = RbacVerb.All)]
public class SQLServerLoginController(
    ILogger<SQLServerLoginController> logger,
    IKubernetesClient kubernetesClient,
    SqlServerEndpointService sqlServerEndpointService
) : IResourceController<V1SQLServerLogin>
{
    public async Task<ResourceControllerResult?> ReconcileAsync(V1SQLServerLogin entity)
    {
        logger.LogInformation("Reconciling SQLServerLogin: {Name}", entity.Metadata.Name);

        try
        {
            var sqlServer = await kubernetesClient.Get<V1SQLServer>(entity.Spec.SqlServerName, entity.Metadata.NamespaceProperty);
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

            await kubernetesClient.UpdateStatus(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerLogin: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;

            await kubernetesClient.UpdateStatus(entity);
        }

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromMinutes(0.5));
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(string secretName, string namespaceName)
    {
        var secret = await kubernetesClient.Get<V1Secret>(secretName, namespaceName);
        if (secret?.Data is null || !secret.Data.ContainsKey("sa-password"))
        {
            throw new Exception($"Secret '{secretName}' does not contain the expected 'sa-password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["sa-password"]);
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
