using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using SqlServerOperator.Entities;
using SqlServerOperator.Finalizers;
using System.Security.Cryptography;
using System.Text;
using k8s;
using KubeOps.KubernetesClient;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServer), Verbs = RbacVerb.All)]
public class SQLServerController : IResourceController<V1SQLServer>
{
    private readonly ILogger<SQLServerController> _logger;
    private readonly IFinalizerManager<V1SQLServer> _finalizerManager;
    private readonly IKubernetesClient _kubernetesClient;

    public SQLServerController(ILogger<SQLServerController> logger, IFinalizerManager<V1SQLServer> finalizerManager, IKubernetesClient kubernetesClient)
    {
        _logger = logger;
        _finalizerManager = finalizerManager;
        _kubernetesClient = kubernetesClient;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1SQLServer entity)
    {
        _logger.LogInformation("Reconciling SQLServer: {Name}", entity.Metadata.Name);

        // Register the finalizer
        await _finalizerManager.RegisterFinalizerAsync<SQLServerFinalizer>(entity);

        // Handle SA password secret
        var saPasswordSecretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";
        var saPassword = await GetOrCreateSaPasswordAsync(saPasswordSecretName, entity.Metadata.NamespaceProperty);

        // Define StatefulSet metadata
        var statefulSetName = $"{entity.Metadata.Name}-statefulset";
        var statefulSet = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta
            {
                Name = statefulSetName,
                NamespaceProperty = entity.Metadata.NamespaceProperty,
                Labels = new Dictionary<string, string>
                {
                    { "app", entity.Metadata.Name }
                }
            },
            Spec = new V1StatefulSetSpec
            {
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { "app", entity.Metadata.Name }
                    }
                },
                ServiceName = entity.Metadata.Name,
                Replicas = 1,
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            { "app", entity.Metadata.Name }
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "sqlserver",
                                Image = $"mcr.microsoft.com/mssql/server:{entity.Spec.Version}-latest",
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar { Name = "ACCEPT_EULA", Value = "Y" },
                                    new V1EnvVar
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
                                },
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort { ContainerPort = 1433 }
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "data",
                                        MountPath = "/var/opt/mssql"
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>()
                    }
                },
                VolumeClaimTemplates = new List<V1PersistentVolumeClaim>
                {
                    new V1PersistentVolumeClaim
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Name = "data",
                            Labels = new Dictionary<string, string>
                            {
                                { "app", entity.Metadata.Name }
                            }
                        },
                        Spec = new V1PersistentVolumeClaimSpec
                        {
                            AccessModes = new List<string> { "ReadWriteOnce" },
                            Resources = new V1ResourceRequirements
                            {
                                Requests = new Dictionary<string, ResourceQuantity>
                                {
                                    { "storage", new ResourceQuantity(entity.Spec.StorageSize) }
                                }
                            },
                            StorageClassName = entity.Spec.StorageClass
                        }
                    }
                }
            }
        };

        // Create or update the StatefulSet
        try
        {
            var existingStatefulSet = await _kubernetesClient.ApiClient.AppsV1.ReadNamespacedStatefulSetAsync(statefulSetName, entity.Metadata.NamespaceProperty);
            _logger.LogInformation("Updating StatefulSet for SQLServer: {Name}", entity.Metadata.Name);
            await _kubernetesClient.ApiClient.AppsV1.ReplaceNamespacedStatefulSetAsync(statefulSet, statefulSetName, entity.Metadata.NamespaceProperty);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Creating StatefulSet for SQLServer: {Name}", entity.Metadata.Name);
            await _kubernetesClient.ApiClient.AppsV1.CreateNamespacedStatefulSetAsync(statefulSet, entity.Metadata.NamespaceProperty);
        }

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromMinutes(5));
    }

    public Task StatusModifiedAsync(V1SQLServer entity)
    {
        _logger.LogInformation("Status modified for SQLServer: {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1SQLServer entity)
    {
        _logger.LogInformation("Deleted SQLServer: {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    private async Task<string> GetOrCreateSaPasswordAsync(string secretName, string namespaceName)
    {
        try
        {
            var existingSecret = await _kubernetesClient.ApiClient.CoreV1.ReadNamespacedSecretAsync(secretName, namespaceName);

            if (existingSecret.Data != null && existingSecret.Data.ContainsKey("sa-password"))
            {
                return Encoding.UTF8.GetString(existingSecret.Data["sa-password"]);
            }
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Secret {SecretName} not found. Creating a new one.", secretName);
        }

        var password = GenerateRandomPassword();
        var secret = new V1Secret
        {
            Metadata = new V1ObjectMeta
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

        await _kubernetesClient.ApiClient.CoreV1.CreateNamespacedSecretAsync(secret, namespaceName);
        return password;
    }

    private string GenerateRandomPassword()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
        var random = new RNGCryptoServiceProvider();
        var result = new char[16];
        var buffer = new byte[1];

        for (int i = 0; i < result.Length; i++)
        {
            random.GetBytes(buffer);
            result[i] = validChars[buffer[0] % validChars.Length];
        }

        return new string(result);
    }
}
