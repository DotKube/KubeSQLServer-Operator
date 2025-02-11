using k8s.Models;

using KubeOps.Operator.Entities;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "database.example.com", ApiVersion = "v1alpha1", Kind = "SQLServer")]
public class V1SQLServer : CustomKubernetesEntity<V1SQLServer.V1SQLServerSpec, V1SQLServer.V1SQLServerStatus>
{
    public class V1SQLServerSpec
    {
        public string Version { get; set; } = "2022";
        public string StorageClass { get; set; } = "standard";
        public string StorageSize { get; set; } = "20Gi";
        public string? SecretName { get; set; }
        public string? ServiceType { get; set; } = "None";
        public bool EnableFullTextSearch { get; set; } = false;
    }

    public class V1SQLServerStatus
    {
        public string State { get; set; } = "Pending";
        public string Message { get; set; } = "Awaiting deployment.";
    }
}