
using HunterGrassServices.Api.Data;
using HunterGrassServices.Api.Models;
using HunterGrassServices.Api.Services;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// =========================================
// APPLICATION SERVICES
// =========================================

// Register OpenAPI
builder.Services.AddOpenApi();

// Register SQLite database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    );
});

// =========================================
// ANTIFORGERY PROTECTION
// =========================================

builder.Services.AddAntiforgery(options =>
{
    // JavaScript sends this header with
    // authenticated state-changing requests.
    options.HeaderName = "X-CSRF-TOKEN";

    // Configure the antiforgery cookie.
    options.Cookie.Name =
        "HunterGrass.Antiforgery";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Strict;
});

// =========================================
// RATE LIMITING
// =========================================

builder.Services.AddRateLimiter(options =>
{
    // Return HTTP 429 when the limit is exceeded.
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Limit administrator login attempts.
    options.AddPolicy(
        "AdminLoginPolicy",
        httpContext =>
        {
            var clientIp =
                httpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
                ?? "unknown";

            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,

                            Window =
                                TimeSpan.FromMinutes(1),

                            QueueLimit = 0,

                            AutoReplenishment = true
                        }
                );
        }
    );
});

// =========================================
// ASP.NET CORE IDENTITY
// =========================================

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        // Unique administrator email addresses.
        options.User.RequireUniqueEmail = true;

        // Password requirements.
        options.Password.RequiredLength = 12;

        options.Password.RequireDigit = true;

        options.Password.RequireUppercase = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireNonAlphanumeric = true;

        // Account lockout settings.
        options.Lockout.AllowedForNewUsers = true;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>();

// =========================================
// AUTHENTICATION
// =========================================

builder.Services
    .AddAuthentication(
        IdentityConstants.ApplicationScheme
    )
    .AddIdentityCookies();

// Configure administrator authentication cookie.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HunterGrass.Admin";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    // Administrator session lifetime.
    options.ExpireTimeSpan =
        TimeSpan.FromMinutes(30);

    options.SlidingExpiration = true;

    // Administrator login page.
    options.LoginPath = "/admin/login/";

    // Browser requests redirect to login.
    // API requests return HTTP 401.
    options.Events.OnRedirectToLogin = context =>
    {
        if (
            context.Request.Path.StartsWithSegments(
                "/api"
            )
        )
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(
                "/admin/login/"
            );
        }

        return Task.CompletedTask;
    };

    // Return HTTP 403 for insufficient permissions.
    options.Events.OnRedirectToAccessDenied =
        context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
});

// =========================================
// AUTHORIZATION
// =========================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole("Admin");
        }
    );
});

// =========================================
// BUILD APPLICATION
// =========================================

var app = builder.Build();

// =========================================
// INITIALIZE ADMINISTRATOR ROLE
// =========================================

await AdminInitializer.InitializeAsync(
    app.Services
);

// =========================================
// APPLICATION MIDDLEWARE
// =========================================

// Redirect HTTP traffic to HTTPS.
app.UseHttpsRedirection();

// Serve public website and static assets.
app.UseDefaultFiles();

app.UseStaticFiles();

// Enable OpenAPI in Development only.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Match incoming requests to endpoints.
app.UseRouting();

// Apply rate limiting.
app.UseRateLimiter();

// Authenticate before authorizing.
app.UseAuthentication();

app.UseAuthorization();

// =========================================
// PUBLIC CUSTOMER QUOTE REQUEST API
// =========================================

// POST: Save a customer quote request.
app.MapPost("/api/quotes", async (
    QuoteRequest request,
    AppDbContext db) =>
{
    // Validate customer information.
    var validationResults =
        new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(
        request,
        new ValidationContext(request),
        validationResults,
        validateAllProperties: true
    );

    if (!isValid)
    {
        return Results.BadRequest(
            validationResults.Select(
                result => result.ErrorMessage
            )
        );
    }

    // Set server-generated values.
    request.Id = 0;

    request.CreatedAt = DateTime.UtcNow;

    // Save the quote request to SQLite.
    db.QuoteRequests.Add(request);

    await db.SaveChangesAsync();

    // Return HTTP 201 Created.
    return Results.Created(
        $"/api/quotes/{request.Id}",
        new
        {
            request.Id,

            Message =
                "Quote request saved successfully."
        }
    );
});

// =========================================
// ADMINISTRATOR QUOTE MANAGEMENT API
// =========================================

// GET: Retrieve real customer quote requests.
//
// SECURITY:
// Only authenticated users with the Admin role
// can access customer quote information.

app.MapGet("/api/admin/quotes", async (
    AppDbContext db,
    HttpContext httpContext) =>
{
    // Prevent caching of customer information.
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Retrieve quotes without EF change tracking.
    // Newest customer requests appear first.
    var quotes = await db.QuoteRequests
        .AsNoTracking()
        .OrderByDescending(
            quote => quote.CreatedAt
        )
        .ToListAsync();

    // Return quote requests as JSON.
    return Results.Ok(quotes);
})
.RequireAuthorization("AdminOnly");

// =========================================
// ANTIFORGERY TOKEN API
// =========================================

// GET: Generate an antiforgery security token.
app.MapGet("/api/admin/antiforgery", (
    HttpContext httpContext,
    IAntiforgery antiforgery) =>
{
    // Require HTTPS.
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message = "HTTPS is required."
        });
    }

    // Generate and store antiforgery tokens.
    var tokens = antiforgery.GetAndStoreTokens(
        httpContext
    );

    // Prevent token response caching.
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return the request token.
    return Results.Ok(new
    {
        Token = tokens.RequestToken
    });
})
.AllowAnonymous();

// =========================================
// ADMINISTRATOR LOGIN API
// =========================================

// POST: Authenticate an administrator.
app.MapPost("/api/admin/login", async (
    AdminLoginRequest request,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    // Require HTTPS.
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message =
                "HTTPS is required for administrator login."
        });
    }

    // Validate antiforgery security token.
    try
    {
        await antiforgery.ValidateRequestAsync(
            httpContext
        );
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new
        {
            Message =
                "Invalid or missing security token."
        });
    }

    // Validate login request.
    var validationResults =
        new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(
        request,
        new ValidationContext(request),
        validationResults,
        validateAllProperties: true
    );

    if (!isValid)
    {
        return Results.BadRequest(
            validationResults.Select(
                result => result.ErrorMessage
            )
        );
    }

    // Find account by email address.
    var user = await userManager.FindByEmailAsync(
        request.Email.Trim()
    );

    // Reject unknown accounts.
    if (user is null)
    {
        return Results.Json(
            new
            {
                Message = "Invalid email or password."
            },
            statusCode:
                StatusCodes.Status401Unauthorized
        );
    }

    // Verify the administrator role.
    var isAdmin = await userManager.IsInRoleAsync(
        user,
        "Admin"
    );

    if (!isAdmin)
    {
        return Results.Json(
            new
            {
                Message = "Invalid email or password."
            },
            statusCode:
                StatusCodes.Status401Unauthorized
        );
    }

    // Verify password and create login cookie.
    var result =
        await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true
        );

    // Reject unsuccessful login attempts.
    if (!result.Succeeded)
    {
        return Results.Json(
            new
            {
                Message = "Invalid email or password."
            },
            statusCode:
                StatusCodes.Status401Unauthorized
        );
    }

    // Return successful login response.
    return Results.Ok(new
    {
        Message =
            "Administrator login successful."
    });
})
.AllowAnonymous()
.RequireRateLimiting("AdminLoginPolicy");

// =========================================
// ADMINISTRATOR LOGOUT API
// =========================================

// POST: Sign out the administrator.
app.MapPost("/api/admin/logout", async (
    SignInManager<IdentityUser> signInManager,
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    // Require HTTPS.
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message =
                "HTTPS is required for administrator logout."
        });
    }

    // Validate antiforgery security token.
    try
    {
        await antiforgery.ValidateRequestAsync(
            httpContext
        );
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new
        {
            Message =
                "Invalid or missing security token."
        });
    }

    // Clear the administrator authentication cookie.
    await signInManager.SignOutAsync();

    // Prevent logout response caching.
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return successful logout response.
    return Results.Ok(new
    {
        Message =
            "Administrator logout successful."
    });
})
.RequireAuthorization("AdminOnly");

// =========================================
// PROTECTED ADMINISTRATOR DASHBOARD
// =========================================

// GET: Display protected administrator dashboard.
app.MapGet("/admin/", (
    HttpContext httpContext,
    IWebHostEnvironment environment) =>
{
    // Locate the protected dashboard HTML.
    var dashboardPath = Path.Combine(
        environment.ContentRootPath,
        "Protected",
        "Admin",
        "index.html"
    );

    // Check whether the file exists.
    if (!File.Exists(dashboardPath))
    {
        return Results.NotFound(new
        {
            Message =
                "Administrator dashboard not found."
        });
    }

    // Prevent caching of protected HTML.
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return the protected dashboard.
    return Results.File(
        dashboardPath,
        contentType: "text/html"
    );
})
.RequireAuthorization("AdminOnly");

// Redirect the old dashboard URL.
// Authorization is still required.
app.MapGet(
    "/admin/index.html",
    () => Results.Redirect("/admin/")
)
.RequireAuthorization("AdminOnly");

// =========================================
// START APPLICATION
// =========================================

app.Run();
