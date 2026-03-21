using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities.V1Alpha1;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "ExternalDatabase")]
public class V1Alpha1ExternalDatabase : CustomKubernetesEntity<V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseSpec, V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseStatus>
{
    [Description("Spec of the external database.")]
    public class V1Alpha1ExternalDatabaseSpec
    {
        [Description("The hostname or IP address of the SQL Server instance (e.g., myserver.database.windows.net, 10.0.0.5, mydatabase.rds.amazonaws.com).")]
        public string Host { get; set; } = string.Empty;

        [Description("The port number for the SQL Server connection.")]
        public int Port { get; set; } = 1433;

        [Description("The name of the database to be managed.")]
        public string DatabaseName { get; set; } = string.Empty;

        [Description("Whether to use SSL/TLS encryption for the connection.")]
        public bool UseEncryption { get; set; } = true;

        [Description("Whether to trust the server certificate (set to true for self-signed certificates).")]
        public bool TrustServerCertificate { get; set; } = false;

        [Description("The identity configuration for connecting to the external database.")]
        public IdentitySpec Identity { get; set; } = new();

        [Description("Optional connection string parameters as key-value pairs (e.g., for Azure AD authentication, connection timeout, etc.).")]
        public Dictionary<string, string>? AdditionalConnectionProperties { get; set; }
    }

    [Description("Status of the external database.")]
    public class V1Alpha1ExternalDatabaseStatus
    {
        [Description("The current state of the connection.")]
        public string State { get; set; } = "Pending";

        [Description("A message providing details on the current connection status.")]
        public string Message { get; set; } = "Awaiting verification.";

        [Description("The last time the database status was checked.")]
        public DateTime? LastChecked { get; set; }

        [Description("Whether the connection to the external database is healthy.")]
        public bool IsConnected { get; set; } = false;
    }
}