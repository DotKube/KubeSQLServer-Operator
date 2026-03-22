using SqlServerOperator.Entities.V1Alpha1;

namespace SqlServerOperator.Controllers.Services;

public record ResolvedDatabase(string Host, string? DatabaseName, string SecretName);

public interface IDatabaseReferenceResolver
{
    Task<ResolvedDatabase> ResolveAsync(string? databaseRef, string? instanceName, string? databaseName, string namespaceName);
}
