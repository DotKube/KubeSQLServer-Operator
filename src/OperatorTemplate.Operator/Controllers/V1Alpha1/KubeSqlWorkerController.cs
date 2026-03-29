using k8s;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using SqlServerOperator.Entities.V1Alpha1;

namespace SqlServerOperator.Controllers.V1Alpha1;

[EntityRbac(typeof(V1Alpha1KubeSqlWorker), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1ServiceAccount), Verbs = RbacVerb.All)]
public class KubeSqlWorkerController(ILogger<KubeSqlWorkerController> logger, IKubernetesClient kubernetesClient) : IEntityController<V1Alpha1KubeSqlWorker>
{
    public async Task<ReconciliationResult<V1Alpha1KubeSqlWorker>> ReconcileAsync(V1Alpha1KubeSqlWorker entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling KubeSqlWorker: {Name}", entity.Metadata.Name);

        await EnsureServiceAccountAsync(entity);
        await EnsureDeploymentAsync(entity);

        await UpdateStatusAsync(entity, "Ready", "Worker Resources Created.");
        return ReconciliationResult<V1Alpha1KubeSqlWorker>.Success(entity, TimeSpan.FromMinutes(5));
    }

    public Task<ReconciliationResult<V1Alpha1KubeSqlWorker>> DeletedAsync(V1Alpha1KubeSqlWorker entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted KubeSqlWorker: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1Alpha1KubeSqlWorker>.Success(entity, TimeSpan.FromMinutes(5)));
    }

    private async Task EnsureServiceAccountAsync(V1Alpha1KubeSqlWorker entity)
    {
        var saName = $"{entity.Metadata.Name}-sa";
        var namespaceName = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";

        var sa = new V1ServiceAccount
        {
            Metadata = new V1ObjectMeta
            {
                Name = saName,
                NamespaceProperty = namespaceName,
                Labels = new Dictionary<string, string> { { "app", entity.Metadata.Name } }
            }
        };

        try
        {
            await kubernetesClient.ApiClient.CoreV1.CreateNamespacedServiceAccountAsync(sa, namespaceName);
            logger.LogInformation("Created ServiceAccount for KubeSqlWorker: {Name}", entity.Metadata.Name);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("ServiceAccount for KubeSqlWorker {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task EnsureDeploymentAsync(V1Alpha1KubeSqlWorker entity)
    {
        var deploymentName = $"{entity.Metadata.Name}-worker";
        var namespaceName = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";
        var saName = $"{entity.Metadata.Name}-sa";

        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = deploymentName,
                NamespaceProperty = namespaceName,
                Labels = new Dictionary<string, string> { { "app", entity.Metadata.Name } }
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string> { { "app", entity.Metadata.Name } }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string> { { "app", entity.Metadata.Name } }
                    },
                    Spec = new V1PodSpec
                    {
                        ServiceAccountName = saName,
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "worker",
                                Image = "ghcr.io/dotkube/kubesqlworker:latest"
                            }
                        }
                    }
                }
            }
        };

        try
        {
            await kubernetesClient.ApiClient.AppsV1.CreateNamespacedDeploymentAsync(deployment, namespaceName);
            logger.LogInformation("Created Deployment for KubeSqlWorker: {Name}", entity.Metadata.Name);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("Deployment for KubeSqlWorker {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task UpdateStatusAsync(V1Alpha1KubeSqlWorker entity, string state, string message)
    {
        entity.Status ??= new V1Alpha1KubeSqlWorker.V1Alpha1KubeSqlWorkerStatus();
        entity.Status.State = state;
        await kubernetesClient.UpdateStatusAsync(entity);
    }
}