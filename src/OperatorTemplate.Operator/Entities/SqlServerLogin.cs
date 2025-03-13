

using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace SqlServerOperator.Entities;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "SQLServerLogin")]
public class V1SQLServerLogin : CustomKubernetesEntity<V1SQLServerLogin.V1SQLServerLoginSpec, V1SQLServerLogin.V1SQLServerLoginStatus>
{
    [Description("Spec of the SQL Server login.")]
    public class V1SQLServerLoginSpec
    {
        [Description("The name of the SQL Server instance.")]
        public string SqlServerName { get; set; } = string.Empty;

        [Description("The login name for authentication.")]
        public string LoginName { get; set; } = string.Empty;

        [Description("The authentication type for the login (e.g., SQL, Windows).")]
        public string AuthenticationType { get; set; } = "SQL";

        [Description("The name of the Kubernetes secret storing authentication credentials.")]
        public string? SecretName { get; set; }
    }

    [Description("Status of the SQL Server login.")]
    public class V1SQLServerLoginStatus
    {
        [Description("The current state of the SQL Server login.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the SQL Server login.")]
        public string Message { get; set; } = "Awaiting reconciliation.";

        [Description("The last time the login status was checked.")]
        public DateTime? LastChecked { get; set; }
    }
}
