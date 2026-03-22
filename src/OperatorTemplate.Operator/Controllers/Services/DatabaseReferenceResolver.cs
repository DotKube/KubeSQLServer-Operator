using KubeOps.KubernetesClient;
using SqlServerOperator.Entities.V1Alpha1;

namespace SqlServerOperator.Controllers.Services;

public class DatabaseReferenceResolver(
    IKubernetesClient kubernetesClient,
    ISqlServerEndpointService sqlServerEndpointService
) : IDatabaseReferenceResolver
{
    public async Task<ResolvedDatabase> ResolveAsync(string? databaseRef, string? instanceName, string? databaseName, string namespaceName)
    {
        if (!string.IsNullOrEmpty(databaseRef))
        {
            // Try SQLServerDatabase first
            var db = await kubernetesClient.GetAsync<V1Alpha1SQLServerDatabase>(databaseRef, namespaceName);
            if (db != null)
            {
                if (db.Status?.State != "Ready")
                {
                    throw new Exception($"Referenced Database '{databaseRef}' is not in Ready state (Current state: {db.Status?.State ?? "Pending"}).");
                }
                var host = await sqlServerEndpointService.GetSqlServerEndpointAsync(db.Spec.InstanceName, namespaceName);
                var secretName = await DetermineSecretNameAsync(db.Spec.InstanceName, namespaceName);
                return new ResolvedDatabase(host, db.Spec.DatabaseName, secretName);
            }

            // Try ExternalDatabase
            var externalDb = await kubernetesClient.GetAsync<V1Alpha1ExternalDatabase>(databaseRef, namespaceName);
            if (externalDb != null)
            {
                if (externalDb.Status?.State != "Ready")
                {
                    throw new Exception($"Referenced ExternalDatabase '{databaseRef}' is not in Ready state (Current state: {externalDb.Status?.State ?? "Pending"}).");
                }
                return new ResolvedDatabase(externalDb.Spec.ServerUrl, externalDb.Spec.DatabaseName, externalDb.Spec.SecretName);
            }

            throw new Exception($"Referenced Database or ExternalDatabase '{databaseRef}' not found in namespace '{namespaceName}'.");
        }

        if (string.IsNullOrEmpty(instanceName))
        {
            throw new Exception("Either DatabaseRef or InstanceName must be provided.");
        }

        var directHost = await sqlServerEndpointService.GetSqlServerEndpointAsync(instanceName, namespaceName);
        var directSecretName = await DetermineSecretNameAsync(instanceName, namespaceName);
        return new ResolvedDatabase(directHost, databaseName, directSecretName);
    }

    private async Task<string> DetermineSecretNameAsync(string instanceName, string namespaceName)
    {
        // Try ExternalSQLServer first
        var externalServer = await kubernetesClient.GetAsync<V1Alpha1ExternalSQLServer>(instanceName, namespaceName);
        if (externalServer is not null)
        {
            return externalServer.Spec.SecretName;
        }

        // Fall back to internal SQLServer
        var sqlServer = await kubernetesClient.GetAsync<V1Alpha1SQLServer>(instanceName, namespaceName);
        if (sqlServer is not null)
        {
            return sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";
        }

        throw new Exception($"SQLServer or ExternalSQLServer instance '{instanceName}' not found in namespace '{namespaceName}'.");
    }
}