
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "Database")]
public class V1Alpha1SQLServerDatabase : CustomKubernetesEntity<V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseSpec, V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseStatus>
{

    [Description("Spec of the SQL Server database.")]
    public class V1Alpha1SQLServerDatabaseSpec
    {
        [Description("The name of the SQL Server instance where the database will be created.")]
        public string InstanceName { get; set; } = string.Empty;

        [Description("The name of the database to be created.")]
        public string DatabaseName { get; set; } = string.Empty;
        
        [Description("Policy that controls what happens to the physical database when the CR is deleted. Either 'Retain' or 'Delete'. Default: 'Retain'.")]
        public string DatabaseReclaimPolicy { get; set; } = "Retain";
    }

    [Description("Status of the SQL Server database.")]
    public class V1Alpha1SQLServerDatabaseStatus
    {
        [Description("The current state of the database.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the database.")]
        public string Message { get; set; } = "Awaiting reconciliation.";

        [Description("The last time the database status was checked.")]
        public DateTime? LastChecked { get; set; }
    }
}