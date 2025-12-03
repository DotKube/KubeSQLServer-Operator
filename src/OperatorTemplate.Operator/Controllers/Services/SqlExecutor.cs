using Microsoft.Data.SqlClient;

namespace SqlServerOperator.Controllers.Services;

public class SqlExecutor : ISqlExecutor
{
    public async Task ExecuteNonQueryAsync(string connectionString, string commandText, Dictionary<string, object> parameters)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(commandText, connection);
        foreach (var (key, value) in parameters)
        {
            command.Parameters.AddWithValue(key, value);
        }
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ExecuteScalarAsync<T>(string connectionString, string commandText, Dictionary<string, object>? parameters = null)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(commandText, connection);
        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                command.Parameters.AddWithValue(key, value);
            }
        }

        var result = await command.ExecuteScalarAsync();
        return result is T typedResult ? typedResult : default;
    }
}
