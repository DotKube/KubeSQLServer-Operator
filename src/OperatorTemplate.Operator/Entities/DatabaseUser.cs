

using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServerUser")]
public class V1DatabaseUser : CustomKubernetesEntity<V1DatabaseUser.UserSpec, V1DatabaseUser.UserStatus>
{

    [Description("Spec of the database user.")]
    public class UserSpec
    {
        [Description("The name of the SQL Server instance.")]
        public string SqlServerName { get; set; } = string.Empty;

        [Description("The name of the database where this user will be created.")]
        public string DatabaseName { get; set; } = string.Empty;

        [Description("The login name for the database user.")]
        public string LoginName { get; set; } = string.Empty;

        [Description("The roles assigned to the database user.")]
        public List<string> Roles { get; set; } = new();
    }

    [Description("Status of the database user.")]
    public class UserStatus
    {
        [Description("The current state of the database user.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the database user.")]
        public string Message { get; set; } = "Awaiting reconciliation.";

        [Description("The last time the database user status was checked.")]
        public DateTime? LastChecked { get; set; }
    }
}
