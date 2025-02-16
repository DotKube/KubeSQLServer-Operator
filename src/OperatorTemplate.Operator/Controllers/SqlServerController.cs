using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using SqlServerOperator.Builders;
using SqlServerOperator.Configuration;
using SqlServerOperator.Entities;
using SqlServerOperator.Finalizers;
using System.Security.Cryptography;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServer), Verbs = RbacVerb.All)]
public class SQLServerController(ILogger<SQLServerController> logger, IFinalizerManager<V1SQLServer> finalizerManager, IKubernetesClient kubernetesClient, DefaultMssqlConfig config, SqlServerImages sqlServerImages) : IResourceController<V1SQLServer>
{
    public async Task<ResourceControllerResult?> ReconcileAsync(V1SQLServer entity)
    {
        logger.LogInformation("Reconciling SQLServer: {Name}", entity.Metadata.Name);

        await RegisterFinalizerAsync(entity);
        await EnsureSaPasswordSecretAsync(entity);
        await EnsureConfigMapAsync(entity);
        await EnsureStatefulSetAsync(entity);
        await EnsureServiceAsync(entity);

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromMinutes(config.DefaultRequeueTimeMinutes));
    }

    private async Task RegisterFinalizerAsync(V1SQLServer entity)
    {
        await finalizerManager.RegisterFinalizerAsync<SQLServerFinalizer>(entity);
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
                                            Key = "sa-password"
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
        var serviceName = $"{entity.Metadata.Name}-service";
        string serviceType = entity.Spec.ServiceType.ToLower();

        // Initialize serviceSpec
        var serviceSpec = new V1ServiceSpec
        {
            Selector = new Dictionary<string, string> { { "app", entity.Metadata.Name } },
            Ports = new List<V1ServicePort> { new V1ServicePort { Name = "sql", Port = 1433, TargetPort = 1433 } }
        };

        // Set service type (LoadBalancer or None for headless)
        serviceSpec.Type = serviceType switch
        {
            "loadbalancer" => "LoadBalancer",
            _ => "None", // Default to headless service
        };

        // Create the service
        var service = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceName,
                NamespaceProperty = entity.Metadata.NamespaceProperty
            },
            Spec = serviceSpec
        };

        try
        {
            await kubernetesClient.ApiClient.CoreV1.CreateNamespacedServiceAsync(service, entity.Metadata.NamespaceProperty);
            logger.LogInformation("Created service for SQLServer: {Name} with type {ServiceType}", entity.Metadata.Name, entity.Spec.ServiceType);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("Service for SQLServer {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task EnsureSaPasswordSecretAsync(V1SQLServer entity)
    {
        var secretName = entity.Spec.SecretName ?? $"{entity.Metadata.Name}-secret";
        var namespaceName = entity.Metadata.NamespaceProperty;

        try
        {
            var existingSecret = await kubernetesClient.ApiClient.CoreV1.ReadNamespacedSecretAsync(secretName, namespaceName);

            if ((existingSecret.Data is not null) && existingSecret.Data.ContainsKey("sa-password"))
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
                { "sa-password", password }
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