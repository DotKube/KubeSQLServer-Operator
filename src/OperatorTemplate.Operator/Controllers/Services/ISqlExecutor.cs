namespace SqlServerOperator.Controllers.Services;

public interface ISqlExecutor
{
    /// <summary>
    /// Executes a SQL command that does not return a result set (INSERT, UPDATE, DELETE, CREATE, etc.).
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="commandText">The SQL command text to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and values for parameterized queries.</param>
    Task ExecuteNonQueryAsync(string connectionString, string commandText, Dictionary<string, object> parameters);

    /// <summary>
    /// Executes a SQL query and returns the first column of the first row in the result set.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="commandText">The SQL query text to execute.</param>
    /// <param name="parameters">Optional dictionary of parameter names and values for parameterized queries.</param>
    /// <returns>The first column of the first row, or default(T) if no result.</returns>
    Task<T?> ExecuteScalarAsync<T>(string connectionString, string commandText, Dictionary<string, object>? parameters = null);
}
