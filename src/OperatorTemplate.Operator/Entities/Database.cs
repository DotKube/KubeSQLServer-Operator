
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "Database")]
public class V1SQLServerDatabase : CustomKubernetesEntity<V1SQLServerDatabase.V1SQLServerDatabaseSpec, V1SQLServerDatabase.V1SQLServerDatabaseStatus>
{

    [Description("Spec of the SQL Server database.")]
    public class V1SQLServerDatabaseSpec
    {
        [Description("The name of the SQL Server instance where the database will be created.")]
        public string InstanceName { get; set; } = string.Empty;

        [Description("The name of the database to be created.")]
        public string DatabaseName { get; set; } = string.Empty;
    }

    [Description("Status of the SQL Server database.")]
    public class V1SQLServerDatabaseStatus
    {
        [Description("The current state of the database.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the database.")]
        public string Message { get; set; } = "Awaiting reconciliation.";

        [Description("The last time the database status was checked.")]
        public DateTime? LastChecked { get; set; }
    }
}