
using HunterGrassServices.Api.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Security.Claims;
using System.Text.Encodings.Web;

namespace HunterGrassServices.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    // =========================================
    // TEMPORARY TEST DATABASE
    // =========================================

    private readonly string _databasePath =
        Path.Combine(
            Path.GetTempPath(),
            $"huntergrass-tests-{Guid.NewGuid():N}.db"
        );

    // =========================================
    // TEST AUTHENTICATION SETTINGS
    // =========================================

    // Set to true when testing a signed-in
    // user who does not have the Admin role.
    public bool UseRegularUserAuthentication { get; set; }

    // =========================================
    // CONFIGURE TEST APPLICATION
    // =========================================

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // Each factory gets its own SQLite file.
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

                        // Never seed real administrator
                        // credentials during testing.
                        ["AdminSettings:Email"] = "",
                        ["AdminSettings:Password"] = ""
                    }
                );
            }
        );

        // =====================================
        // TEST AUTHENTICATION
        // =====================================

        builder.ConfigureTestServices(services =>
        {
            if (UseRegularUserAuthentication)
            {
                // Register a separate test scheme.
                // Do not register Identity.Application
                // again because Program.cs already
                // registers that scheme.

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme =
                            "TestRegularUser";

                        options.DefaultChallengeScheme =
                            "TestRegularUser";

                        options.DefaultForbidScheme =
                            "TestRegularUser";
                    })
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        RegularUserTestAuthHandler>(
                        "TestRegularUser",
                        _ => { }
                    );
            }
        });

        // =====================================
        // INITIALIZE TEST DATABASE
        // =====================================

        builder.ConfigureServices(services =>
        {
            using var provider =
                services.BuildServiceProvider();

            using var scope =
                provider.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // Create the Identity and quote tables.
            db.Database.EnsureCreated();
        });
    }

    // =========================================
    // CLEAN UP TEST DATABASE
    // =========================================

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Release SQLite connections.
        SqliteConnection.ClearAllPools();

        // Remove temporary database files.
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

// =========================================
// TEST-ONLY AUTHENTICATION HANDLER
// =========================================

internal sealed class RegularUserTestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RegularUserTestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<
            AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        // Simulate an authenticated regular user.
        //
        // Notice that there is no Admin role
        // in the claims below.

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "regular-test-user"
            ),

            new Claim(
                ClaimTypes.Name,
                "regular-test@example.com"
            )
        };

        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name
        );

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name
        );

        return Task.FromResult(
            AuthenticateResult.Success(ticket)
        );
    }
}
