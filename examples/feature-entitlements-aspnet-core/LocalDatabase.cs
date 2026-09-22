using Microsoft.Data.SqlClient;

namespace KilnBook;

public static class LocalDatabase
{
    public static string ConnectionString(string databaseName) => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = databaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true
    }.ConnectionString;

    public static async Task CreateAsync(string databaseName)
    {
        ValidateName(databaseName);
        await using var connection = new SqlConnection(ConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@name) IS NULL CREATE DATABASE [{databaseName}];";
        command.Parameters.AddWithValue("@name", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task DropVerificationDatabaseAsync(string databaseName)
    {
        ValidateName(databaseName);
        if (!databaseName.StartsWith("KilnBookEntitlements_Check_", StringComparison.Ordinal))
            throw new InvalidOperationException("Only a generated verification database can be removed.");
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(ConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];";
        await command.ExecuteNonQueryAsync();
    }

    private static void ValidateName(string databaseName)
    {
        if (databaseName != "KilnBookEntitlements" &&
            !(databaseName.StartsWith("KilnBookEntitlements_Check_", StringComparison.Ordinal) &&
              Guid.TryParseExact(databaseName["KilnBookEntitlements_Check_".Length..], "N", out _)))
            throw new InvalidOperationException("Unexpected sample database name.");
    }
}
