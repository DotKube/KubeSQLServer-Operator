using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServer")]
public class V1SQLServer : CustomKubernetesEntity<V1SQLServer.V1SQLServerSpec, V1SQLServer.V1SQLServerStatus>
{
    [Description("Spec of the SQL Server deployment.")]
    public class V1SQLServerSpec
    {
        [Description("The version of SQL Server to deploy.")]
        public string Version { get; set; } = "2022";

        [Description("The name of the storage class to use for SQL Server storage.")]
        public string StorageClass { get; set; } = "standard";

        [Description("The size of the persistent storage volume.")]
        public string StorageSize { get; set; } = "20Gi";

        [Description("The name of the Kubernetes secret containing SQL Server credentials.")]
        public string? SecretName { get; set; }

        [Description("The type of Kubernetes service to expose SQL Server (e.g., ClusterIP, NodePort, LoadBalancer).")]
        public string? ServiceType { get; set; } = "None";

        [Description("Specifies whether full-text search is enabled in SQL Server.")]
        public bool EnableFullTextSearch { get; set; } = false;
    }

    [Description("Status of the SQL Server deployment.")]
    public class V1SQLServerStatus
    {
        [Description("The current state of the SQL Server deployment.")]
        public string State { get; set; } = "Pending";

        [Description("A message providing details on the current status of SQL Server.")]
        public string Message { get; set; } = "Awaiting deployment.";
    }
}