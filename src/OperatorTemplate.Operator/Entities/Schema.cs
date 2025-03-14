using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServerSchema")]
public class V1SQLServerSchema : CustomKubernetesEntity<V1SQLServerSchema.V1SQLServerSchemaSpec, V1SQLServerSchema.V1SQLServerSchemaStatus>
{
    [Description("Spec of the SQL Server database schema.")]
    public class V1SQLServerSchemaSpec
    {
        [Description("The SQL Server instance name where the schema will be created.")]
        public string InstanceName { get; set; } = string.Empty;

        [Description("The name of the database to create the schema in.")]
        public string DatabaseName { get; set; } = string.Empty;

        [Description("The name of the schema to be created.")]
        public string SchemaName { get; set; } = string.Empty;

        [Description("The database user that owns this schema.")]
        public string SchemaOwner { get; set; } = "dbo";
    }

    [Description("Status of the SQL Server database schema.")]
    public class V1SQLServerSchemaStatus
    {
        [Description("The current state of the schema.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the schema.")]
        public string Message { get; set; } = "Awaiting reconciliation.";

        [Description("The last time the schema status was checked.")]
        public DateTime? LastChecked { get; set; }
    }
}

