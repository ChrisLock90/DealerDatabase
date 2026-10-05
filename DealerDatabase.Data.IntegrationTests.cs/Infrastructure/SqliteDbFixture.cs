namespace DealerDatabase.Data.IntegrationTests.Infrastructure;

using DealerDatabase.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

internal sealed class SqliteDbFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public DealerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new DealerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
