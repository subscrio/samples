using Microsoft.Data.SqlClient;
public sealed class LocalDatabase : IAsyncDisposable {
    public string Name { get; } = "SubscrioBlog_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => Build(Name);
    private static string Build(string name) => new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = name, IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString;
    public async Task CreateAsync() {
        await using var c = new SqlConnection(Build("master")); await c.OpenAsync();
        await using var cmd = c.CreateCommand(); cmd.CommandText = $"CREATE DATABASE [{Name}]"; await cmd.ExecuteNonQueryAsync();
    }
    public async ValueTask DisposeAsync() {
        if (!System.Text.RegularExpressions.Regex.IsMatch(Name, "^SubscrioBlog_[a-f0-9]{32}$")) throw new InvalidOperationException("Unexpected database name");
        SqlConnection.ClearAllPools();
        await using var c = new SqlConnection(Build("master")); await c.OpenAsync();
        await using var cmd = c.CreateCommand(); cmd.CommandText = $"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}]"; await cmd.ExecuteNonQueryAsync();
    }
}
