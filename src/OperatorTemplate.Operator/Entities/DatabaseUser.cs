

using k8s.Models;
using KubeOps.Operator.Entities;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServerUser")]
public class V1DatabaseUser : CustomKubernetesEntity<V1DatabaseUser.UserSpec, V1DatabaseUser.UserStatus>
{
    public class UserSpec
    {
        public string SqlServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string LoginName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public class UserStatus
    {
        public string State { get; set; } = "Pending";
        public string Message { get; set; } = "Awaiting reconciliation.";
        public DateTime? LastChecked { get; set; }
    }
}
