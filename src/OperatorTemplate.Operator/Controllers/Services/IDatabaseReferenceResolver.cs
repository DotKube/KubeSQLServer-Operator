using SqlServerOperator.Entities.V1Alpha1;

namespace SqlServerOperator.Controllers.Services;

public record ResolvedDatabase(string InstanceName, string? DatabaseName);

public interface IDatabaseReferenceResolver
{
    Task<ResolvedDatabase> ResolveAsync(string? databaseRef, string? instanceName, string? databaseName, string namespaceName);
}