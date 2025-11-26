using k8s;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using SqlServerOperator.Builders;
using SqlServerOperator.Configuration;
using SqlServerOperator.Entities;
using SqlServerOperator.Finalizers;
using System.Security.Cryptography;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServer), Verbs = RbacVerb.All)]
public class SQLServerController(ILogger<SQLServerController> logger, IKubernetesClient kubernetesClient, DefaultMssqlConfig config, SqlServerImages sqlServerImages) : IEntityController<V1SQLServer>
{
    public async Task<ReconciliationResult<V1SQLServer>> ReconcileAsync(V1SQLServer entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling SQLServer: {Name}", entity.Metadata.Name);

        await EnsureSaPasswordSecretAsync(entity);
        await EnsureConfigMapAsync(entity);
        await EnsureStatefulSetAsync(entity);
        await EnsureServiceAsync(entity);

        return ReconciliationResult<V1SQLServer>.Success(entity);
    }

    public Task<ReconciliationResult<V1SQLServer>> DeletedAsync(V1SQLServer entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted SQLServer: {Name}", entity.Metadata.Name);
        return Task.FromResult(ReconciliationResult<V1SQLServer>.Success(entity));
    }

    private async Task EnsureConfigMapAsync(V1SQLServer entity)
    {
        var configMapName = $"{entity.Metadata.Name}-config";

        var configMap = new ConfigMapBuilder()
            .WithMetadata(configMapName, entity.Metadata.NamespaceProperty, new Dictionary<string, string> { { "app", entity.Metadata.Name } })
            .WithData("mssql.conf", config.DefaultConfigMapData)
            .Build();

        try
        {
            await kubernetesClient.ApiClient.CoreV1.CreateNamespacedConfigMapAsync(configMap, entity.Metadata.NamespaceProperty);
            logger.LogInformation("Created ConfigMap for SQLServer: {Name}", entity.Metadata.Name);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("ConfigMap for SQLServer {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task EnsureStatefulSetAsync(V1SQLServer entity)
    {
        var statefulSetName = $"{entity.Metadata.Name}-statefulset";


        var sqlServerImage = sqlServerImages.UbuntuBasedImage(entity.Spec.Version, entity.Spec.EnableFullTextSearch);

        var statefulSet = new StatefulSetBuilder()
            .WithMetadata(statefulSetName, entity.Metadata.NamespaceProperty, new Dictionary<string, string> { { "app", entity.Metadata.Name } })
            .WithSpec(new V1StatefulSetSpec
            {
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string> { { "app", entity.Metadata.Name } }
                },
                ServiceName = entity.Metadata.Name,
                Replicas = 1,
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string> { { "app", entity.Metadata.Name } },
                        Annotations = new Dictionary<string, string>
                        {
                            { "sidecar.istio.io/inject", "false" }
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        SecurityContext = new V1PodSecurityContext
                        {
                            FsGroup = 0,
                            RunAsGroup = 0,
                            RunAsUser = 10001
                        },
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "sqlserver",
                                Image = sqlServerImage,
                                Env =
                            [
                                new V1EnvVar
                                {
                                    Name = "ACCEPT_EULA",
                                    Value = "Y"
                                },
                                new V1EnvVar
                                {
                                    Name = "SA_PASSWORD",
                                    ValueFrom = new V1EnvVarSource
                                    {
                                        SecretKeyRef = new V1SecretKeySelector
                                        {
                                            Name = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret",
                                            Key = "password"
                                        }
                                    }
                                },
                                new V1EnvVar
                                {
                                    Name = "MSSQL_PID",
                                    Value = "Developer"
                                }
                            ],
                                Ports = [new V1ContainerPort { ContainerPort = 1433 }],
                                VolumeMounts =
                            [
                                new V1VolumeMount
                                {
                                    Name = "mssql-config-volume",
                                    MountPath = "/var/opt/config",
                                },
                                new V1VolumeMount
                                {
                                    Name = "mssql-config-volume",
                                    MountPath = "/var/opt/mssql-config",
                                }

                            ],
                                Command = ["/opt/mssql/bin/sqlservr"],
                            }
                        ],
                        Volumes =
                        [
                            new V1Volume
                            {
                                Name = "mssql-config-volume",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = $"{entity.Metadata.Name}-config"
                                }
                            },
                            new V1Volume
                            {
                                Name = "mssql-secret-volume",
                                Secret = new V1SecretVolumeSource
                                {
                                    SecretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret"
                                }
                            }
                        ]
                    }
                }
            })
            .Build();

        try
        {
            await kubernetesClient.ApiClient.AppsV1.CreateNamespacedStatefulSetAsync(statefulSet, entity.Metadata.NamespaceProperty);
            logger.LogInformation("Created StatefulSet for SQLServer: {Name}", entity.Metadata.Name);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("StatefulSet for SQLServer {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task EnsureServiceAsync(V1SQLServer entity)
    {
        var appLabel = entity.Metadata.Name;
        var namespaceName = entity.Metadata.NamespaceProperty;
        var serviceType = entity.Spec.ServiceType?.ToLower() ?? "none";

        // Always create the headless service
        var headlessServiceName = $"{appLabel}-headless";
        var headlessService = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = headlessServiceName,
                NamespaceProperty = namespaceName,
                Labels = new Dictionary<string, string> { { "app", appLabel } }
            },
            Spec = new V1ServiceSpec
            {
                ClusterIP = "None",
                Selector = new Dictionary<string, string> { { "app", appLabel } },
                Ports = new List<V1ServicePort>
            {
                new() { Name = "sql", Port = 1433, TargetPort = 1433 }
            }
            }
        };

        await CreateOrUpdateServiceAsync(headlessService, namespaceName);
        logger.LogInformation("Ensured headless service '{ServiceName}' for SQLServer: {Name}", headlessServiceName, appLabel);

        // If the type is "none", we're done after creating the headless service
        if (serviceType == "none")
        {
            return;
        }

        // Determine and create the appropriate additional service based on service type
        var serviceName = $"{appLabel}-service";

        var additionalService = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceName,
                NamespaceProperty = namespaceName,
                Labels = new Dictionary<string, string> { { "app", appLabel } }
            },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string> { { "app", appLabel } },
                Ports = new List<V1ServicePort>
            {
                new()
                {
                    Name = "sql",
                    Port = 1433,
                    TargetPort = 1433,
                    NodePort = serviceType == "nodeport" ? 30080 : null
                }
            },
                Type = serviceType switch
                {
                    "loadbalancer" => "LoadBalancer",
                    "nodeport" => "NodePort",
                    _ => throw new InvalidOperationException($"Unsupported service type: {serviceType}")
                }
            }
        };

        await CreateOrUpdateServiceAsync(additionalService, namespaceName);
        logger.LogInformation("Ensured service '{ServiceName}' of type '{ServiceType}' for SQLServer: {Name}", serviceName, additionalService.Spec.Type, appLabel);
    }

    private async Task CreateOrUpdateServiceAsync(V1Service service, string namespaceName)
    {
        try
        {
            var existingService = await kubernetesClient.ApiClient.CoreV1.ReadNamespacedServiceAsync(service.Metadata.Name, namespaceName);
            await kubernetesClient.ApiClient.CoreV1.ReplaceNamespacedServiceAsync(service, service.Metadata.Name, namespaceName);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await kubernetesClient.ApiClient.CoreV1.CreateNamespacedServiceAsync(service, namespaceName);
        }
    }

    private async Task EnsureSaPasswordSecretAsync(V1SQLServer entity)
    {
        var secretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";
        var namespaceName = entity.Metadata.NamespaceProperty;

        try
        {
            var existingSecret = await kubernetesClient.ApiClient.CoreV1.ReadNamespacedSecretAsync(secretName, namespaceName);

            if ((existingSecret.Data is not null) && existingSecret.Data.ContainsKey("password"))
            {
                return;
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
                { "password", password }
            },
            Type = "Opaque"
        };

        await kubernetesClient.ApiClient.CoreV1.CreateNamespacedSecretAsync(secret, namespaceName);
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