namespace OperatorTemplate.ExternalWorker.Services;

public interface ISqlExecutor
{
    Task ExecuteNonQueryAsync(string connectionString, string commandText, Dictionary<string, object>? parameters = null);
    Task<T?> ExecuteScalarAsync<T>(string connectionString, string commandText, Dictionary<string, object>? parameters = null);
}