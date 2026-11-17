
using HunterGrassServices.Api.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HunterGrassServices.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    // =========================================
    // TEMPORARY TEST DATABASE
    // =========================================

    // Give each test factory its own database.
    // This prevents tests from modifying
    // the real Hunter Grass Services database.

    private readonly string _databasePath =
        Path.Combine(
            Path.GetTempPath(),
            $"huntergrass-tests-{Guid.NewGuid():N}.db"
        );

    // =========================================
    // CONFIGURE TEST APPLICATION
    // =========================================

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // Override the application's SQLite
        // connection string before startup.
        //
        // Pooling=False prevents SQLite from
        // keeping idle file connections open.

        var connectionString =
            $"Data Source={_databasePath};Pooling=False";

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            connectionString
        );

        // =====================================
        // TEST CONFIGURATION
        // =====================================

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString,

                        // Do not use real administrator
                        // credentials in automated tests.
                        ["AdminSettings:Email"] = "",

                        ["AdminSettings:Password"] = ""
                    }
                );
            }
        );

        // =====================================
        // INITIALIZE TEST DATABASE
        // =====================================

        builder.ConfigureServices(services =>
        {
            // Create a temporary service provider
            // to initialize the test database.

            using var provider =
                services.BuildServiceProvider();

            using var scope =
                provider.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // Create Identity and quote tables
            // in the isolated test database.
            db.Database.EnsureCreated();
        });
    }

    // =========================================
    // CLEAN UP TEST DATABASE
    // =========================================

    protected override void Dispose(bool disposing)
    {
        // Stop and dispose the test application.
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Release pooled SQLite connections.
        SqliteConnection.ClearAllPools();

        // Delete the temporary database and
        // any SQLite journal sidecar files.
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = _databasePath + suffix;

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
