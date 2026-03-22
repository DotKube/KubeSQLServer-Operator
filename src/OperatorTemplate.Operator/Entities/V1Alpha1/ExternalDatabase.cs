using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using System;

namespace SqlServerOperator.Entities.V1Alpha1;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "ExternalDatabase")]
public class V1Alpha1ExternalDatabase : CustomKubernetesEntity<V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseSpec, V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseStatus>
{
    [Description("Spec of the external SQL Server database.")]
    public class V1Alpha1ExternalDatabaseSpec
    {
        [Description("The hostname or IP address of the SQL Server instance.")]
        public string ServerUrl { get; set; } = string.Empty;

        [Description("The name of the existing database.")]
        public string DatabaseName { get; set; } = string.Empty;

        [Description("The name of the Kubernetes secret containing SQL Server credentials.")]
        public string SecretName { get; set; } = string.Empty;
    }

    [Description("Status of the external SQL Server database.")]
    public class V1Alpha1ExternalDatabaseStatus
    {
        [Description("The current state of the database.")]
        public string State { get; set; } = "Pending";

        [Description("A message indicating the current status of the database.")]
        public string Message { get; set; } = "Awaiting verification.";

        [Description("The last time the database status was checked.")]
        public DateTime? LastChecked { get; set; }

        [Description("Whether the database is available on the server.")]
        public bool IsAvailable { get; set; } = false;
    }
}