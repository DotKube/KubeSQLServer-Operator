using k8s;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;
using SqlServerOperator.Entities;

namespace SqlServerOperator.Finalizers;

public class SQLServerFinalizer(ILogger<SQLServerFinalizer> logger, IKubernetesClient kubernetesClient) : IResourceFinalizer<V1SQLServer>
{
    public async Task FinalizeAsync(V1SQLServer entity)
    {
        logger.LogInformation("Finalizing SQLServer: {Name}", entity.Metadata.Name);

        var namespaceName = entity.Metadata.NamespaceProperty;
        var statefulSetName = $"{entity.Metadata.Name}-statefulset";
        var serviceName = $"{entity.Metadata.Name}-service";
        var secretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";
        var configMapName = $"{entity.Metadata.Name}-config";

        try
        {
            // Delete the StatefulSet
            await DeleteStatefulSetAsync(statefulSetName, namespaceName);

            // Delete the headless service
            await DeleteServiceAsync(serviceName, namespaceName);

            // Delete the Secret
            await DeleteSecretAsync(secretName, namespaceName);

            // Delete the ConfigMap
            await DeleteConfigMapAsync(configMapName, namespaceName);

            logger.LogInformation("Finalization complete for SQLServer: {Name}", entity.Metadata.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while finalizing SQLServer: {Name}", entity.Metadata.Name);
        }
    }

    private async Task DeleteStatefulSetAsync(string statefulSetName, string namespaceName)
    {
        try
        {
            logger.LogInformation("Deleting StatefulSet: {StatefulSetName} in namespace {Namespace}", statefulSetName, namespaceName);
            await kubernetesClient.ApiClient.AppsV1.DeleteNamespacedStatefulSetAsync(statefulSetName, namespaceName);
            logger.LogInformation("StatefulSet {StatefulSetName} deleted successfully.", statefulSetName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete StatefulSet: {StatefulSetName} in namespace {Namespace}", statefulSetName, namespaceName);
        }
    }

    private async Task DeleteServiceAsync(string serviceName, string namespaceName)
    {
        try
        {
            logger.LogInformation("Deleting Service: {ServiceName} in namespace {Namespace}", serviceName, namespaceName);
            await kubernetesClient.ApiClient.CoreV1.DeleteNamespacedServiceAsync(serviceName, namespaceName);
            logger.LogInformation("Service {ServiceName} deleted successfully.", serviceName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete Service: {ServiceName} in namespace {Namespace}", serviceName, namespaceName);
        }
    }

    private async Task DeleteSecretAsync(string secretName, string namespaceName)
    {
        try
        {
            logger.LogInformation("Deleting Secret: {SecretName} in namespace {Namespace}", secretName, namespaceName);
            await kubernetesClient.ApiClient.CoreV1.DeleteNamespacedSecretAsync(secretName, namespaceName);
            logger.LogInformation("Secret {SecretName} deleted successfully.", secretName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete Secret: {SecretName} in namespace {Namespace}", secretName, namespaceName);
        }
    }

    private async Task DeleteConfigMapAsync(string configMapName, string namespaceName)
    {
        try
        {
            logger.LogInformation("Deleting ConfigMap: {ConfigMapName} in namespace {Namespace}", configMapName, namespaceName);
            await kubernetesClient.ApiClient.CoreV1.DeleteNamespacedConfigMapAsync(configMapName, namespaceName);
            logger.LogInformation("ConfigMap {ConfigMapName} deleted successfully.", configMapName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete ConfigMap: {ConfigMapName} in namespace {Namespace}", configMapName, namespaceName);
        }
    }
}