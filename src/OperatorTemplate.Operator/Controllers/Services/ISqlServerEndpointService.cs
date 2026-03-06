namespace SqlServerOperator.Controllers.Services;

public interface ISqlServerEndpointService
{
    /// <summary>
    /// Gets the SQL Server endpoint, checking both internal SQLServer and ExternalSQLServer resources.
    /// </summary>
    Task<string> GetSqlServerEndpointAsync(string instanceName, string namespaceName);
}