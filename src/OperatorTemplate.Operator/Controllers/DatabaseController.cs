using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using SqlServerOperator.Entities;

namespace SqlServerOperator.Controllers;

[EntityRbac(typeof(V1SQLServerDatabase), Verbs = RbacVerb.All)]
public class SQLServerDatabaseController(ILogger<SQLServerDatabaseController> logger, IKubernetesClient kubernetesClient) : IResourceController<V1SQLServerDatabase>
{
    public async Task<ResourceControllerResult?> ReconcileAsync(V1SQLServerDatabase entity)
    {
        logger.LogInformation("Reconciling SQLServerDatabase: {Name}", entity.Metadata.Name);

        try
        {
            // Determine the SecretName to use
            var secretName = await DetermineSecretNameAsync(entity);

            // Ensure the CronJob exists
            await EnsureCronJobAsync(entity, secretName);

            // Update the status to reflect success
            await UpdateStatusAsync(entity, "Ready", "CronJob created and reconciliation succeeded.", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reconciliation of SQLServerDatabase: {Name}", entity.Metadata.Name);

            // Update the status to reflect the error
            await UpdateStatusAsync(entity, "Error", ex.Message, DateTime.UtcNow);
        }

        return null; // No need to requeue; the CronJob handles periodic checks
    }

    private async Task<string> DetermineSecretNameAsync(V1SQLServerDatabase entity)
    {
        var instanceName = entity.Spec.InstanceName;
        var namespaceName = entity.Metadata.NamespaceProperty;

        // Fetch the SQLServer instance
        var sqlServer = await kubernetesClient.Get<V1SQLServer>(instanceName, namespaceName);

        if (sqlServer == null)
        {
            throw new Exception($"SQLServer instance '{instanceName}' not found in namespace '{namespaceName}'.");
        }

        // Use the SecretName from the SQLServer instance if not explicitly defined
        var secretName = sqlServer.Spec.SecretName ?? $"{sqlServer.Metadata.Name}-secret";

        logger.LogInformation("Determined SecretName for SQLServerDatabase {Name}: {SecretName}", entity.Metadata.Name, secretName);

        return secretName;
    }

    private async Task EnsureCronJobAsync(V1SQLServerDatabase entity, string secretName)
    {
        var cronJobName = $"{entity.Metadata.Name}-cronjob";
        var namespaceName = entity.Metadata.NamespaceProperty;
        var instanceName = entity.Spec.InstanceName;
        var databaseName = entity.Spec.DatabaseName;

        var cronJob = new V1CronJob
        {
            Metadata = new V1ObjectMeta
            {
                Name = cronJobName,
                NamespaceProperty = namespaceName,
                Labels = new Dictionary<string, string>
                {
                    { "app", "sqlserver-database" },
                    { "database-name", databaseName }
                }
            },
            Spec = new V1CronJobSpec
            {
                Schedule = "*/5 * * * *", // Run every 5 minutes
                JobTemplate = new V1JobTemplateSpec
                {
                    Spec = new V1JobSpec
                    {
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>
                                {
                                    { "app", "sqlserver-database" }
                                }
                            },
                            Spec = new V1PodSpec
                            {
                                Containers = new List<V1Container>
                                {
                                    new V1Container
                                    {
                                        Name = "sqlcmd-check",
                                        Image = "sqlcmd-tools-container:latest",
                                        Command = new List<string>
                                        {
                                            "/bin/bash", "-c",
                                            @$"
                                            if ! echo 'SELECT name FROM sys.databases WHERE name = ''{databaseName}'';' | sqlcmd -S tcp:{instanceName}-headless,1433 -U sa -P $(cat /var/run/secrets/sql/sa-password) | grep -q '{databaseName}'; then
                                                echo 'Database {databaseName} does not exist. Creating...';
                                                echo 'CREATE DATABASE [{databaseName}];' | sqlcmd -S tcp:{instanceName}-headless,1433 -U sa -P $(cat /var/run/secrets/sql/sa-password);
                                            else
                                                echo 'Database {databaseName} already exists.';
                                            fi"
                                        },
                                        VolumeMounts = new List<V1VolumeMount>
                                        {
                                            new V1VolumeMount
                                            {
                                                Name = "sql-secret",
                                                MountPath = "/var/run/secrets/sql",
                                            }
                                        }
                                    }
                                },
                                RestartPolicy = "OnFailure",
                                Volumes = new List<V1Volume>
                                {
                                    new V1Volume
                                    {
                                        Name = "sql-secret",
                                        Secret = new V1SecretVolumeSource
                                        {
                                            SecretName = secretName
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        try
        {
            await kubernetesClient.ApiClient.BatchV1.CreateNamespacedCronJobAsync(cronJob, namespaceName);
            logger.LogInformation("Created CronJob for SQLServerDatabase: {Name}", entity.Metadata.Name);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogInformation("CronJob for SQLServerDatabase {Name} already exists. Skipping creation.", entity.Metadata.Name);
        }
    }

    private async Task UpdateStatusAsync(V1SQLServerDatabase entity, string state, string message, DateTime? lastChecked)
    {
        entity.Status ??= new V1SQLServerDatabase.V1SQLServerDatabaseStatus();
        entity.Status.State = state;
        entity.Status.Message = message;
        entity.Status.LastChecked = lastChecked;

        await kubernetesClient.UpdateStatus(entity);
        logger.LogInformation("Updated status for SQLServerDatabase: {Name} to State: {State}, Message: {Message}", entity.Metadata.Name, state, message);
    }
}
