using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Data.SqlClient;
using SqlServerOperator.Entities;
using System.Text;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1Alpha1ExternalSQLServer), Verbs = RbacVerb.All)]
public class ExternalSQLServerController(
    ILogger<ExternalSQLServerController> logger,
    IKubernetesClient kubernetesClient) 
    : IEntityController<V1Alpha1ExternalSQLServer>
{
    public async Task<ReconciliationResult<V1Alpha1ExternalSQLServer>> ReconcileAsync(V1Alpha1ExternalSQLServer entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling ExternalSQLServer: {Name}", entity.Metadata.Name);

        try
        {
            var (username, password) = await GetCredentialsAsync(entity);
            var connectionString = BuildConnectionString(entity, username, password);
            
            // Verify connection
            await VerifyConnectionAsync(connectionString);
            
            await UpdateStatusAsync(entity, "Ready", "Connection verified successfully.", DateTime.UtcNow, true);
            return ReconciliationResult<V1Alpha1ExternalSQLServer>.Success(entity, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of ExternalSQLServer: {Name}", entity.Metadata.Name);
            await UpdateStatusAsync(entity, "Error", ex.Message, DateTime.UtcNow, false);
            return ReconciliationResult<V1Alpha1ExternalSQLServer>.Failure(entity, ex.Message, ex, TimeSpan.FromMinutes(1));
        }
    }

    public Task<ReconciliationResult<V1Alpha1ExternalSQLServer>> DeletedAsync(V1Alpha1ExternalSQLServer entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted ExternalSQLServer: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1ExternalSQLServer>.Success(entity));
    }

    private async Task<(string username, string password)> GetCredentialsAsync(V1Alpha1ExternalSQLServer entity)
    {
        var namespaceName = entity.Metadata.NamespaceProperty;
        var secret = await kubernetesClient.GetAsync<V1Secret>(entity.Spec.SecretName, namespaceName);

        if (secret?.Data is null || !secret.Data.ContainsKey("password"))
        {
            throw new Exception($"Secret '{entity.Spec.SecretName}' does not contain required 'username' and 'password' keys.");
        }

        var username = secret.Data.ContainsKey("username") ? Encoding.UTF8.GetString(secret.Data["username"]) : "sa";
        var password = Encoding.UTF8.GetString(secret.Data["password"]);

        return (username, password);
    }

    private string BuildConnectionString(V1Alpha1ExternalSQLServer entity, string username, string password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{entity.Spec.Host},{entity.Spec.Port}",
            UserID = username,
            Password = password,
            InitialCatalog = "master",
            Encrypt = entity.Spec.UseEncryption,
            TrustServerCertificate = entity.Spec.TrustServerCertificate,
            ConnectTimeout = 15
        };

        // Add any additional connection properties
        if (entity.Spec.AdditionalConnectionProperties != null)
        {
            foreach (var prop in entity.Spec.AdditionalConnectionProperties)
            {
                builder[prop.Key] = prop.Value;
            }
        }

        return builder.ConnectionString;
    }

    private async Task VerifyConnectionAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Run a simple query to verify connection
        using var command = new SqlCommand("SELECT @@VERSION", connection);
        var version = await command.ExecuteScalarAsync();
        
        logger.LogInformation("Successfully connected to SQL Server. Version: {Version}", version);
    }

    private async Task UpdateStatusAsync(V1Alpha1ExternalSQLServer entity, string state, string message, DateTime? lastChecked, bool isConnected)
    {
        entity.Status ??= new V1Alpha1ExternalSQLServer.V1Alpha1ExternalSQLServerStatus();
        entity.Status.State = state;
        entity.Status.Message = message;
        entity.Status.LastChecked = lastChecked;
        entity.Status.IsConnected = isConnected;

        await kubernetesClient.UpdateStatusAsync(entity);
        logger.LogInformation("Updated status for ExternalSQLServer: {Name} to State: {State}, Message: {Message}", 
            entity.Metadata.Name, state, message);
    }
}
