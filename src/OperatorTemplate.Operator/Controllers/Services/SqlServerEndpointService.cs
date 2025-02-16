using k8s.Models;
using KubeOps.KubernetesClient;

namespace SqlServerOperator.Controllers.Services;

public class SqlServerEndpointService(ILogger<SqlServerEndpointService> logger, IKubernetesClient kubernetesClient)
{
    public async Task<string> GetSqlServerEndpointAsync(string instanceName, string namespaceName)
    {
        var serviceName = $"{instanceName}-service";
        var service = await kubernetesClient.Get<V1Service>(serviceName, namespaceName);

        if (service is null)
        {
            throw new Exception($"Service '{serviceName}' not found in namespace '{namespaceName}'.");
        }

        if (service.Spec.Type == "LoadBalancer")
        {
            var externalIP = await GetLoadBalancerIPAsync(serviceName, namespaceName);
            if (!string.IsNullOrEmpty(externalIP))
            {
                return externalIP;
            }
        }

        return $"{serviceName}.{namespaceName}";
    }

    public async Task<string?> GetLoadBalancerIPAsync(string serviceName, string namespaceName)
    {
        var service = await kubernetesClient.Get<V1Service>(serviceName, namespaceName);
        if (service?.Status?.LoadBalancer?.Ingress != null && service.Status.LoadBalancer.Ingress.Count > 0)
        {
            return service.Status.LoadBalancer.Ingress[0].Ip ?? service.Status.LoadBalancer.Ingress[0].Hostname;
        }

        return null;
    }
}
