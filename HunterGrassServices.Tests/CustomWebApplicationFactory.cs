
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
using Microsoft.Extensions.Options;

using System.Security.Claims;
using System.Text.Encodings.Web;

namespace HunterGrassServices.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    // =========================================
    // TEST DATABASE
    // =========================================

    private readonly string _databasePath =
        Path.Combine(
            Path.GetTempPath(),
            $"huntergrass-tests-{Guid.NewGuid():N}.db"
        );

    // =========================================
    // AUTHENTICATION SETTINGS
    // =========================================

    public bool UseRegularUserAuthentication { get; set; }

    public bool UseAdminAuthentication { get; set; }

    // =========================================
    // CONFIGURE TEST APPLICATION
    // =========================================

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
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

                        // Prevent real admin credentials
                        // from being used in tests.
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
            if (UseRegularUserAuthentication ||
                UseAdminAuthentication)
            {
                // Tell the test handler whether
                // this simulated user is an admin.

                services.Configure<TestAuthenticationSettings>(
                    options =>
                    {
                        options.IsAdmin =
                            UseAdminAuthentication;
                    }
                );

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
        // CREATE TEMPORARY SQLITE DATABASE
        // =====================================

        builder.ConfigureServices(services =>
        {
            using var provider =
                services.BuildServiceProvider();

            using var scope =
                provider.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();
        });
    }

    // =========================================
    // CLEAN UP TEMPORARY DATABASE
    // =========================================

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();

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
// TEST AUTHENTICATION SETTINGS
// =========================================

internal sealed class TestAuthenticationSettings
{
    public bool IsAdmin { get; set; }
}

// =========================================
// TEST AUTHENTICATION HANDLER
// =========================================

internal sealed class RegularUserTestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly bool _isAdmin;

    public RegularUserTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<TestAuthenticationSettings> testSettings)
        : base(options, logger, encoder)
    {
        _isAdmin = testSettings.Value.IsAdmin;
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        // Create a simulated authenticated user.

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "test-user"
            ),

            new Claim(
                ClaimTypes.Name,
                "test@example.com"
            )
        };

        // Administrators receive the Admin role.
        // Regular users do not.

        if (_isAdmin)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    "Admin"
                )
            );
        }

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
