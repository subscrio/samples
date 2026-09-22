using Microsoft.Data.SqlClient;

namespace KilnBook;

public sealed class ReservationStore(string connectionString)
{
    public async Task InstallAsync()
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF SCHEMA_ID('kilnbook') IS NULL EXEC('CREATE SCHEMA kilnbook');
            IF OBJECT_ID('kilnbook.accounts') IS NULL
                CREATE TABLE kilnbook.accounts (customer_key nvarchar(100) NOT NULL PRIMARY KEY);
            IF OBJECT_ID('kilnbook.reservations') IS NULL
                CREATE TABLE kilnbook.reservations (
                    id uniqueidentifier NOT NULL PRIMARY KEY,
                    customer_key nvarchar(100) NOT NULL REFERENCES kilnbook.accounts(customer_key),
                    recurring bit NOT NULL,
                    active bit NOT NULL DEFAULT 1
                );
            IF NOT EXISTS (SELECT 1 FROM kilnbook.accounts WHERE customer_key = 'clay-room')
                INSERT INTO kilnbook.accounts VALUES ('clay-room');
            IF NOT EXISTS (SELECT 1 FROM kilnbook.accounts WHERE customer_key = 'river-studio')
                INSERT INTO kilnbook.accounts VALUES ('river-studio');
            IF NOT EXISTS (SELECT 1 FROM kilnbook.accounts WHERE customer_key = 'visitor')
                INSERT INTO kilnbook.accounts VALUES ('visitor');
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Reservation?> TryCreateAsync(string customerKey, bool recurring, int limit)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@customer", customerKey);

        // Every booking writer locks the same account row before counting or inserting.
        command.CommandText = "SELECT customer_key FROM kilnbook.accounts WITH (UPDLOCK, HOLDLOCK) WHERE customer_key = @customer;";
        if (await command.ExecuteScalarAsync() is null)
            throw new InvalidOperationException("Booking account was not provisioned.");

        command.CommandText = "SELECT COUNT(*) FROM kilnbook.reservations WHERE customer_key = @customer AND active = 1;";
        var activeCount = (int)(await command.ExecuteScalarAsync())!;
        if (activeCount >= limit)
            return null;

        var id = Guid.NewGuid();
        command.CommandText = "INSERT INTO kilnbook.reservations (id, customer_key, recurring) VALUES (@id, @customer, @recurring);";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@recurring", recurring);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return new Reservation(id, customerKey, recurring, activeCount + 1, limit);
    }

    public async Task<int> CountAsync(string customerKey)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM kilnbook.reservations WHERE customer_key = @customer AND active = 1;";
        command.Parameters.AddWithValue("@customer", customerKey);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}

public record Reservation(Guid Id, string CustomerKey, bool Recurring, int ActiveCount, int Limit);
