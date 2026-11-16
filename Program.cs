
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

// Register OpenAPI.
builder.Services.AddOpenApi();

// Register SQLite database.
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

    options.Cookie.Name = "HunterGrass.Antiforgery";
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
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Limit administrator login attempts.
    options.AddPolicy(
        "AdminLoginPolicy",
        httpContext =>
        {
            var clientIp =
                httpContext.Connection.RemoteIpAddress?
                    .ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
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

    options.ExpireTimeSpan =
        TimeSpan.FromMinutes(30);

    options.SlidingExpiration = true;

    options.LoginPath = "/admin/login/";

    // API requests return HTTP 401.
    // Browser requests redirect to login.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect("/admin/login/");
        }

        return Task.CompletedTask;
    };

    // Insufficient permissions return HTTP 403.
    options.Events.OnRedirectToAccessDenied = context =>
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
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

// =========================================
// BUILD APPLICATION
// =========================================

var app = builder.Build();

// =========================================
// INITIALIZE ADMINISTRATOR ROLE
// =========================================

await AdminInitializer.InitializeAsync(app.Services);

// =========================================
// APPLICATION MIDDLEWARE
// =========================================

// Redirect HTTP traffic to HTTPS.
app.UseHttpsRedirection();

// Serve the public website and static assets.
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
    var validationResults = new List<ValidationResult>();

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
    // Customers cannot choose the quote status.
    request.Id = 0;
    request.Status = "New";
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
            Message = "Quote request saved successfully."
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

    // Retrieve newest quotes first.
    var quotes = await db.QuoteRequests
        .AsNoTracking()
        .OrderByDescending(quote => quote.CreatedAt)
        .ToListAsync();

    return Results.Ok(quotes);
})
.RequireAuthorization("AdminOnly");

// =========================================
// ADMINISTRATOR QUOTE STATUS UPDATE API
// =========================================

// PUT: Update the status of an existing quote.
//
// Allowed statuses:
// New, Contacted, Completed.
//
// SECURITY:
// Requires HTTPS, Admin role and a valid
// antiforgery request token.

app.MapPut("/api/admin/quotes/{id:int}/status", async (
    int id,
    UpdateQuoteStatusRequest request,
    AppDbContext db,
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    // Require HTTPS.
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message = "HTTPS is required."
        });
    }

    // Prevent caching of the response.
    httpContext.Response.Headers.CacheControl =
        "no-store";

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
            Message = "Invalid or missing security token."
        });
    }

    // Accept only supported quote statuses.
    string[] allowedStatuses =
    {
        "New",
        "Contacted",
        "Completed"
    };

    var requestedStatus = request.Status?.Trim();

    if (string.IsNullOrWhiteSpace(requestedStatus))
    {
        return Results.BadRequest(new
        {
            Message = "Quote status is required."
        });
    }

    var normalizedStatus = allowedStatuses
        .FirstOrDefault(status =>
            status.Equals(
                requestedStatus,
                StringComparison.OrdinalIgnoreCase
            )
        );

    if (normalizedStatus is null)
    {
        return Results.BadRequest(new
        {
            Message =
                "Status must be New, Contacted, or Completed."
        });
    }

    // Find the quote request in SQLite.
    var quote = await db.QuoteRequests.FindAsync(id);

    if (quote is null)
    {
        return Results.NotFound(new
        {
            Message = "Quote request not found."
        });
    }

    // Update the tracked database entity.
    quote.Status = normalizedStatus;

    // Persist the status change to SQLite.
    await db.SaveChangesAsync();

    // Return the updated status.
    return Results.Ok(new
    {
        quote.Id,
        quote.Status,
        Message = "Quote status updated successfully."
    });
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
            Message = "Invalid or missing security token."
        });
    }

    // Validate login request.
    var validationResults = new List<ValidationResult>();

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

    return Results.Ok(new
    {
        Message = "Administrator login successful."
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
            Message = "Invalid or missing security token."
        });
    }

    // Clear administrator authentication cookie.
    await signInManager.SignOutAsync();

    // Prevent caching.
    httpContext.Response.Headers.CacheControl =
        "no-store";

    return Results.Ok(new
    {
        Message = "Administrator logout successful."
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
    // Locate protected dashboard HTML.
    var dashboardPath = Path.Combine(
        environment.ContentRootPath,
        "Protected",
        "Admin",
        "index.html"
    );

    if (!File.Exists(dashboardPath))
    {
        return Results.NotFound(new
        {
            Message = "Administrator dashboard not found."
        });
    }

    // Prevent caching of protected HTML.
    httpContext.Response.Headers.CacheControl =
        "no-store";

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
