using k8s.Models;
using KubeOps.KubernetesClient;
using SqlServerOperator.Entities;

namespace SqlServerOperator.Controllers.Services;

public class SqlServerEndpointService(
    ILogger<SqlServerEndpointService> logger,
    IKubernetesClient kubernetesClient,
    IWebHostEnvironment environment) : ISqlServerEndpointService
{
    public async Task<string> GetSqlServerEndpointAsync(string instanceName, string namespaceName)
    {
        // First, try to find an ExternalSQLServer
        var externalServer = await kubernetesClient.GetAsync<V1Alpha1ExternalSQLServer>(instanceName, namespaceName);
        if (externalServer != null)
        {
            return $"{externalServer.Spec.Host},{externalServer.Spec.Port}";
        }

        // Fall back to internal SQLServer
        return await GetInternalSqlServerEndpointAsync(instanceName, namespaceName);
    }

    private async Task<string> GetInternalSqlServerEndpointAsync(string instanceName, string namespaceName)
    {
        var serviceType = await GetServiceTypeAsync(instanceName, namespaceName);

        if (environment.IsProduction())
        {
            // Production environment: Always use the headless service FQDN
            var headlessServiceName = $"{instanceName}-headless";
            return $"{headlessServiceName}.{namespaceName}.svc.cluster.local";
        }

        // Non-production (dev) environment logic:
        switch (serviceType)
        {
            case "LoadBalancer":
                var externalIP = await GetLoadBalancerIPAsync($"{instanceName}-service", namespaceName);
                if (!string.IsNullOrEmpty(externalIP))
                {
                    return externalIP;
                }
                logger.LogWarning("LoadBalancer IP for service '{Service}' not yet available.", $"{instanceName}-service");
                break;

            case "NodePort":
                return "localhost,1434";

            case "None":
                logger.LogInformation("ServiceType is 'None', using headless FQDN.");
                return $"{instanceName}-headless.{namespaceName}.svc.cluster.local";
        }

        throw new Exception($"Unable to determine a valid SQL Server endpoint for instance '{instanceName}' in namespace '{namespaceName}'.");
    }

    private async Task<string> GetServiceTypeAsync(string instanceName, string namespaceName)
    {
        var service = await kubernetesClient.GetAsync<V1Service>($"{instanceName}-service", namespaceName);
        return service?.Spec?.Type ?? "None";
    }

    private async Task<string?> GetLoadBalancerIPAsync(string serviceName, string namespaceName)
    {
        var service = await kubernetesClient.GetAsync<V1Service>(serviceName, namespaceName);

        var ingress = service?.Status?.LoadBalancer?.Ingress?.FirstOrDefault();
        return ingress?.Ip ?? ingress?.Hostname;
    }
}