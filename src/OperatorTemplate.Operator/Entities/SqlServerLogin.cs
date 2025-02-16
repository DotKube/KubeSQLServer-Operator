

using k8s.Models;
using KubeOps.Operator.Entities;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServerLogin")]
public class V1SQLServerLogin : CustomKubernetesEntity<V1SQLServerLogin.V1SQLServerLoginSpec, V1SQLServerLogin.V1SQLServerLoginStatus>
{
    public class V1SQLServerLoginSpec
    {
        public string SqlServerName { get; set; } = string.Empty;
        public string LoginName { get; set; } = string.Empty;
        public string AuthenticationType { get; set; } = "SQL";
        public string? SecretName { get; set; }
    }

    public class V1SQLServerLoginStatus
    {
        public string State { get; set; } = "Pending";
        public string Message { get; set; } = "Awaiting reconciliation.";
        public DateTime? LastChecked { get; set; }
    }
}
