using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities.V1Alpha1;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "KubeSqlWorker")]
[EntityScope(EntityScope.Cluster)]
public class V1Alpha1KubeSqlWorker : CustomKubernetesEntity<V1Alpha1KubeSqlWorker.V1Alpha1KubeSqlWorkerSpec, V1Alpha1KubeSqlWorker.V1Alpha1KubeSqlWorkerStatus>
{
    [Description("Spec of the KubeSqlWorker configuration.")]
    public class V1Alpha1KubeSqlWorkerSpec
    {
        [Description("The kind of resource to manage (ExternalSQLServer or ExternalDatabase).")]
        public string TargetResourceKind { get; set; } = string.Empty;

        [Description("The name of the resource to manage.")]
        public string TargetResourceName { get; set; } = string.Empty;

        [Description("The namespace of the resource to manage.")]
        public string TargetResourceNamespace { get; set; } = string.Empty;

        [Description("The URL or hostname of the SQL Server to manage.")]
        public string ServerUrl { get; set; } = string.Empty;

        [Description("The port of the SQL Server to manage.")]
        public int Port { get; set; } = 1433;

        [Description("The authentication method to use (SqlLogin, WorkloadIdentity, AppRegistration).")]
        public string AuthType { get; set; } = "SqlLogin";

        [Description("The name of the secret containing credentials (required for SqlLogin).")]
        public string? SecretName { get; set; }

        [Description("The name of the ServiceAccount to use for the worker (required for WorkloadIdentity).")]
        public string? ServiceAccountName { get; set; }

        [Description("The Client ID for managed identity mapping (required for WorkloadIdentity or AppRegistration).")]
        public string? ClientId { get; set; }
    }

    [Description("Status of the KubeSqlWorker configuration.")]
    public class V1Alpha1KubeSqlWorkerStatus
    {
        [Description("The current state of the worker configuration.")]
        public string State { get; set; } = "Pending";
    }
}
