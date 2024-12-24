using KubeOps.Operator.Finalizer;
using SqlServerOperator.Entities;

namespace SqlServerOperator.Finalizers;

public class SQLServerFinalizer(ILogger<SQLServerFinalizer> logger) : IResourceFinalizer<V1SQLServer>
{
    public Task FinalizeAsync(V1SQLServer entity)
    {
        logger.LogInformation("entity {1} called {0}.", nameof(FinalizeAsync), entity.Metadata.Name);
        // Add logic for cleaning up SQL Server resources if necessary
        return Task.CompletedTask;
    }
}
