using Microsoft.Data.SqlClient;

namespace ReleaseBoard;

public sealed class LocalDatabase : IAsyncDisposable
{
    private readonly string name = $"ReleaseBoardDemo_{Guid.NewGuid():N}";
    private bool created;
    public string ConnectionString => BuildConnectionString(name);

    private static string BuildConnectionString(string database) =>
        new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

    public async Task CreateAsync()
    {
        await using var connection = new SqlConnection(BuildConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
        created = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!created) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^ReleaseBoardDemo_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Unexpected test database name.");
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(BuildConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
    }
}
