using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using SqlServerOperator.Entities;
using SqlServerOperator.Finalizers;
using System.Security.Cryptography;
using System.Text;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServer), Verbs = RbacVerb.All)]
public class SQLServerController(ILogger<SQLServerController> logger, IFinalizerManager<V1SQLServer> finalizerManager, IKubernetesClient kubernetesClient) : IResourceController<V1SQLServer>
{
    public async Task<ResourceControllerResult?> ReconcileAsync(V1SQLServer entity)
    {
        logger.LogInformation("Reconciling SQLServer: {Name}", entity.Metadata.Name);

        // Register the finalizer
        await finalizerManager.RegisterFinalizerAsync<SQLServerFinalizer>(entity);

        // Handle SA password secret
        var saPasswordSecretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";
        _ = await GetOrCreateSaPasswordAsync(saPasswordSecretName, entity.Metadata.NamespaceProperty);

        // Define StatefulSet metadata
        var statefulSetName = $"{entity.Metadata.Name}-statefulset";
        V1StatefulSet statefulSet = new()
        {
            Metadata = new()
            {
                Name = statefulSetName,
                NamespaceProperty = entity.Metadata.NamespaceProperty,
                Labels = new Dictionary<string, string>
                {
                    { "app", entity.Metadata.Name }
                }
            },
            Spec = new()
            {
                Selector = new()
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { "app", entity.Metadata.Name }
                    }
                },
                ServiceName = entity.Metadata.Name,
                Replicas = 1,
                Template = new()
                {
                    Metadata = new()
                    {
                        Labels = new Dictionary<string, string>
                        {
                            { "app", entity.Metadata.Name }
                        }
                    },
                    Spec = new()
                    {
                        Containers =
                        [
                            new()
                            {
                                Name = "sqlserver",
                                Image = $"mcr.microsoft.com/mssql/server:{entity.Spec.Version}-latest",
                                Env =
                                [
                                    new() { Name = "ACCEPT_EULA", Value = "Y" },
                                    new()
                                    {
                                        Name = "SA_PASSWORD",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            SecretKeyRef = new V1SecretKeySelector
                                            {
                                                Name = saPasswordSecretName,
                                                Key = "sa-password"
                                            }
                                        }
                                    }
                                ],
                                Ports =
                                [
                                    new V1ContainerPort { ContainerPort = 1433 }
                                ],
                                VolumeMounts =
                                [
                                    new V1VolumeMount
                                    {
                                        Name = "data",
                                        MountPath = "/var/opt/mssql"
                                    }
                                ]
                            }
                        ],
                        Volumes = []
                    }
                },
                VolumeClaimTemplates =
                [
                    new()
                    {
                        Metadata = new()
                        {
                            Name = "data",
                            Labels = new Dictionary<string, string>
                            {
                                { "app", entity.Metadata.Name }
                            }
                        },
                        Spec = new()
                        {
                            AccessModes = ["ReadWriteOnce"],
                            Resources = new()
                            {
                                Requests = new Dictionary<string, ResourceQuantity>
                                {
                                    { "storage", new ResourceQuantity(entity.Spec.StorageSize) }
                                }
                            },
                            StorageClassName = entity.Spec.StorageClass
                        }
                    }
                ]
            }
        };

        // Create or update the StatefulSet
        try
        {
            V1StatefulSet? existingStatefulSet = await kubernetesClient.ApiClient
                .AppsV1
                .ReadNamespacedStatefulSetAsync(statefulSetName, entity.Metadata.NamespaceProperty);

            logger.LogInformation("Updating StatefulSet for SQLServer: {Name}", entity.Metadata.Name);

            await kubernetesClient.ApiClient
            .AppsV1
            .ReplaceNamespacedStatefulSetAsync(statefulSet, statefulSetName, entity.Metadata.NamespaceProperty);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Creating StatefulSet for SQLServer: {Name}", entity.Metadata.Name);
            await kubernetesClient.ApiClient.AppsV1.CreateNamespacedStatefulSetAsync(statefulSet, entity.Metadata.NamespaceProperty);
        }

        // Define headless service
        var serviceName = $"{entity.Metadata.Name}-headless";
        V1Service service = new()
        {
            Metadata = new()
            {
                Name = serviceName,
                NamespaceProperty = entity.Metadata.NamespaceProperty,
                Labels = new Dictionary<string, string>
            {
                { "app", entity.Metadata.Name }
            }
            },
            Spec = new()
            {
                Selector = new Dictionary<string, string>
            {
                { "app", entity.Metadata.Name }
            },
                ClusterIP = "None",
                Ports = new List<V1ServicePort>
            {
                new() { Name = "sql", Port = 1433, TargetPort = 1433 }
            }
            }
        };

        // Create or update the headless service
        try
        {
            V1Service? existingService = await kubernetesClient.ApiClient
                .CoreV1
                .ReadNamespacedServiceAsync(serviceName, entity.Metadata.NamespaceProperty);

            logger.LogInformation("Updating headless service for SQLServer: {Name}", entity.Metadata.Name);

            await kubernetesClient.ApiClient
            .CoreV1
            .ReplaceNamespacedServiceAsync(service, serviceName, entity.Metadata.NamespaceProperty);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Creating headless service for SQLServer: {Name}", entity.Metadata.Name);
            await kubernetesClient.ApiClient.CoreV1.CreateNamespacedServiceAsync(service, entity.Metadata.NamespaceProperty);
        }

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromMinutes(5));
    }

    public Task StatusModifiedAsync(V1SQLServer entity)
    {
        logger.LogInformation("Status modified for SQLServer: {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1SQLServer entity)
    {
        logger.LogInformation("Deleted SQLServer: {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    private async Task<string> GetOrCreateSaPasswordAsync(string secretName, string namespaceName)
    {
        try
        {
            var existingSecret = await kubernetesClient.ApiClient.CoreV1.ReadNamespacedSecretAsync(secretName, namespaceName);

            if (existingSecret.Data != null && existingSecret.Data.ContainsKey("sa-password"))
            {
                return Encoding.UTF8.GetString(existingSecret.Data["sa-password"]);
            }
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Secret {SecretName} not found. Creating a new one.", secretName);
        }

        var password = GenerateRandomPassword();
        V1Secret secret = new()
        {
            Metadata = new()
            {
                Name = secretName,
                NamespaceProperty = namespaceName
            },
            StringData = new Dictionary<string, string>
            {
                { "sa-password", password }
            },
            Type = "Opaque"
        };

        await kubernetesClient.ApiClient.CoreV1.CreateNamespacedSecretAsync(secret, namespaceName);
        return password;
    }

    private string GenerateRandomPassword()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
        var result = new char[16];
        var buffer = new byte[1];

        for (int i = 0; i < result.Length; i++)
        {
            RandomNumberGenerator.Fill(buffer);
            result[i] = validChars[buffer[0] % validChars.Length];
        }

        return new string(result);
    }
}