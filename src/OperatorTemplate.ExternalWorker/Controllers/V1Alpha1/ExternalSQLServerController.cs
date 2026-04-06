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

[EntityRbac(typeof(V1Alpha1ExternalSQLServer), Verbs = RbacVerb.All)]
public class ExternalSQLServerController(
    ILogger<ExternalSQLServerController> logger,
    IKubernetesClient kubernetesClient,
    ISqlExecutor sqlExecutor)
    : IEntityController<V1Alpha1ExternalSQLServer>
{
    private readonly string? _targetName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAME");
    private readonly string? _targetNamespace = Environment.GetEnvironmentVariable("TARGET_RESOURCE_NAMESPACE");
    private readonly string? _targetKind = Environment.GetEnvironmentVariable("TARGET_RESOURCE_KIND");

    public async Task<ReconciliationResult<V1Alpha1ExternalSQLServer>> ReconcileAsync(V1Alpha1ExternalSQLServer entity, CancellationToken cancellationToken)
    {
        if (_targetKind != "ExternalSQLServer" || entity.Metadata.Name != _targetName || entity.Metadata.NamespaceProperty != _targetNamespace)
        {
            return ReconciliationResult<V1Alpha1ExternalSQLServer>.Success(entity);
        }

        logger.LogInformation("Worker reconciling assigned ExternalSQLServer: {Name}", entity.Metadata.Name);

        try
        {
            var (username, password) = await GetSqlServerCredentialsAsync(entity);
            var isConnected = await VerifyConnectionAsync(entity.Spec.Host, entity.Spec.Port, username, password, entity.Spec.TrustServerCertificate);

            entity.Status ??= new();
            entity.Status.LastChecked = DateTime.UtcNow;
            entity.Status.IsConnected = isConnected;

            if (isConnected)
            {
                entity.Status.State = "Ready";
                entity.Status.Message = "External SQL Server connection verified.";
            }
            else
            {
                entity.Status.State = "Error";
                entity.Status.Message = "Failed to connect to External SQL Server.";
            }

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1ExternalSQLServer>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of ExternalSQLServer: {Name}", entity.Metadata.Name);
            entity.Status ??= new();
            entity.Status.State = "Error";
            entity.Status.Message = ex.Message;
            entity.Status.LastChecked = DateTime.UtcNow;
            entity.Status.IsConnected = false;

            await kubernetesClient.UpdateStatusAsync(entity);
            return ReconciliationResult<V1Alpha1ExternalSQLServer>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1ExternalSQLServer>> DeletedAsync(V1Alpha1ExternalSQLServer entity, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReconciliationResult<V1Alpha1ExternalSQLServer>.Success(entity));
    }

    private async Task<(string username, string password)> GetSqlServerCredentialsAsync(V1Alpha1ExternalSQLServer entity)
    {
        var secret = await kubernetesClient.GetAsync<V1Secret>(entity.Spec.SecretName, entity.Metadata.NamespaceProperty);
        if (secret?.Data == null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{entity.Spec.SecretName}' does not contain the expected 'password' key.");
        }

        var password = Encoding.UTF8.GetString(secret.Data["password"]);
        var username = secret.Data.ContainsKey("username") ? Encoding.UTF8.GetString(secret.Data["username"]) : "sa";

        return (username, password);
    }

    private async Task<bool> VerifyConnectionAsync(string host, int port, string username, string password, bool trustCertificate)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{host},{port}",
            UserID = username,
            Password = password,
            InitialCatalog = "master",
            TrustServerCertificate = trustCertificate,
            Encrypt = true,
            ConnectTimeout = 15
        };

        try
        {
            await sqlExecutor.ExecuteScalarAsync<int>(builder.ConnectionString, "SELECT 1");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to {Host}:{Port}", host, port);
            return false;
        }
    }
}