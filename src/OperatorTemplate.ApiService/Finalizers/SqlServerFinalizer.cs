using k8s;

using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;

using SqlServerOperator.Entities;

namespace SqlServerOperator.Finalizers;

public class SQLServerFinalizer : IResourceFinalizer<V1SQLServer>
{
    private readonly ILogger<SQLServerFinalizer> _logger;
    private readonly IKubernetesClient _kubernetesClient;

    public SQLServerFinalizer(ILogger<SQLServerFinalizer> logger, IKubernetesClient kubernetesClient)
    {
        _logger = logger;
        _kubernetesClient = kubernetesClient;
    }

    public async Task FinalizeAsync(V1SQLServer entity)
    {
        _logger.LogInformation("Finalizing SQLServer: {Name}", entity.Metadata.Name);

        var namespaceName = entity.Metadata.NamespaceProperty;
        var statefulSetName = $"{entity.Metadata.Name}-statefulset";
        var secretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";

        try
        {
            // Delete the StatefulSet
            await DeleteStatefulSetAsync(statefulSetName, namespaceName);

            // Delete the Secret
            await DeleteSecretAsync(secretName, namespaceName);

            _logger.LogInformation("Finalization complete for SQLServer: {Name}", entity.Metadata.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while finalizing SQLServer: {Name}", entity.Metadata.Name);
        }
    }

    private async Task DeleteStatefulSetAsync(string statefulSetName, string namespaceName)
    {
        try
        {
            _logger.LogInformation("Deleting StatefulSet: {StatefulSetName} in namespace {Namespace}", statefulSetName, namespaceName);
            await _kubernetesClient.ApiClient.AppsV1.DeleteNamespacedStatefulSetAsync(statefulSetName, namespaceName);
            _logger.LogInformation("StatefulSet {StatefulSetName} deleted successfully.", statefulSetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete StatefulSet: {StatefulSetName} in namespace {Namespace}", statefulSetName, namespaceName);
        }
    }

    private async Task DeleteSecretAsync(string secretName, string namespaceName)
    {
        try
        {
            _logger.LogInformation("Deleting Secret: {SecretName} in namespace {Namespace}", secretName, namespaceName);
            await _kubernetesClient.ApiClient.CoreV1.DeleteNamespacedSecretAsync(secretName, namespaceName);
            _logger.LogInformation("Secret {SecretName} deleted successfully.", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Secret: {SecretName} in namespace {Namespace}", secretName, namespaceName);
        }
    }
}