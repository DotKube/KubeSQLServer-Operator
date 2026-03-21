using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities.V1Alpha1;

public class IdentitySpec
{
    [Description("The type of authentication to use for the SQL Server connection. Valid values are SqlLogin, WorkloadIdentity, and AppRegistration.")]
    public AuthType AuthType { get; set; } = AuthType.SqlLogin;

    [Description("The name of the Kubernetes secret containing SQL Server credentials (must contain 'password' and 'username' keys).")]
    public string? SecretName { get; set; }

    [Description("The name of the ServiceAccount to use for Workload Identity.")]
    public string? ServiceAccountName { get; set; }

    [Description("The Client ID of the Managed Identity or App Registration.")]
    public string? ClientId { get; set; }

    [Description("The Tenant ID for the Managed Identity or App Registration.")]
    public string? TenantId { get; set; }
}

public enum AuthType
{
    SqlLogin,
    WorkloadIdentity,
    AppRegistration
}