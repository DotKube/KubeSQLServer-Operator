using KubeOps.KubernetesClient;
using SqlServerOperator.Entities.V1Alpha1;

namespace SqlServerOperator.Controllers.Services;

public class DatabaseReferenceResolver(IKubernetesClient kubernetesClient) : IDatabaseReferenceResolver
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
                return new ResolvedDatabase(db.Spec.InstanceName, db.Spec.DatabaseName);
            }

            // Try ExternalDatabase
            var externalDb = await kubernetesClient.GetAsync<V1Alpha1ExternalDatabase>(databaseRef, namespaceName);
            if (externalDb != null)
            {
                if (externalDb.Status?.State != "Ready")
                {
                    throw new Exception($"Referenced ExternalDatabase '{databaseRef}' is not in Ready state (Current state: {externalDb.Status?.State ?? "Pending"}).");
                }
                return new ResolvedDatabase(externalDb.Spec.InstanceName, externalDb.Spec.DatabaseName);
            }

            throw new Exception($"Referenced Database or ExternalDatabase '{databaseRef}' not found in namespace '{namespaceName}'.");
        }

        if (string.IsNullOrEmpty(instanceName))
        {
            throw new Exception("Either DatabaseRef or InstanceName must be provided.");
        }

        return new ResolvedDatabase(instanceName, databaseName);
    }
}