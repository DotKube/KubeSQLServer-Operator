
using k8s.Models;
using KubeOps.Operator.Entities;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "database.example.com", ApiVersion = "v1alpha1", Kind = "Database")]
public class V1SQLServerDatabase : CustomKubernetesEntity<V1SQLServerDatabase.V1SQLServerDatabaseSpec, V1SQLServerDatabase.V1SQLServerDatabaseStatus>
{
    public class V1SQLServerDatabaseSpec
    {
        public string InstanceName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }

    public class V1SQLServerDatabaseStatus
    {
        public string State { get; set; } = "Pending";
        public string Message { get; set; } = "Awaiting reconciliation.";
        public DateTime? LastChecked { get; set; }
    }
}