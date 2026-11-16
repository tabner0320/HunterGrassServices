
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
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// =========================================
// ANTIFORGERY PROTECTION
// =========================================

builder.Services.AddAntiforgery(options =>
{
    // Header used by JavaScript to send the token
    options.HeaderName = "X-CSRF-TOKEN";

    // Configure the antiforgery cookie
    options.Cookie.Name = "HunterGrass.Antiforgery";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Strict;
});

// =========================================
// RATE LIMITING
// =========================================

builder.Services.AddRateLimiter(options =>
{
    // Return HTTP 429 when the rate limit is exceeded
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Administrator login rate limiting policy
    options.AddPolicy("AdminLoginPolicy", httpContext =>
    {
        // Identify the requesting client
        var clientIp =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        // Allow five login requests per minute per IP
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// =========================================
// ASP.NET CORE IDENTITY
// =========================================

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    // Require unique email addresses
    options.User.RequireUniqueEmail = true;

    // Password security requirements
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Account lockout settings
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

builder.Services.AddAuthentication(
    IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

// Configure administrator authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HunterGrass.Admin";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Lax;

    // Authentication session settings
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    // Set the administrator login page
    options.LoginPath = "/admin/login/";

    // Redirect browser visitors to login,
    // but return HTTP 401 for API requests
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
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

    // Return 403 for insufficient permissions
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
    // Require authentication and the Admin role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
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
// MIDDLEWARE
// =========================================

// Redirect HTTP requests to HTTPS first
app.UseHttpsRedirection();

// Serve the public website and static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable OpenAPI during development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Match incoming requests to endpoints
app.UseRouting();

// Apply rate limiting policies
app.UseRateLimiter();

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

// =========================================
// CUSTOMER QUOTE REQUEST API
// =========================================

// POST: Save a customer quote request
app.MapPost("/api/quotes", async (
    QuoteRequest request,
    AppDbContext db) =>
{
    // Validate customer information
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
            validationResults.Select(r => r.ErrorMessage)
        );
    }

    // Assign server-generated values
    request.Id = 0;
    request.CreatedAt = DateTime.UtcNow;

    // Save quote request to SQLite
    db.QuoteRequests.Add(request);

    await db.SaveChangesAsync();

    // Return successful response
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
// ANTIFORGERY TOKEN API
// =========================================

// GET: Generate an antiforgery token
app.MapGet("/api/admin/antiforgery", (
    HttpContext httpContext,
    IAntiforgery antiforgery) =>
{
    // Require HTTPS
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message = "HTTPS is required."
        });
    }

    // Generate and store antiforgery tokens
    var tokens = antiforgery.GetAndStoreTokens(
        httpContext
    );

    // Prevent caching of the security token
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return the request token to the login page
    return Results.Ok(new
    {
        Token = tokens.RequestToken
    });
})
.AllowAnonymous();

// =========================================
// ADMINISTRATOR LOGIN API
// =========================================

// POST: Authenticate an administrator
app.MapPost("/api/admin/login", async (
    AdminLoginRequest request,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    // Require HTTPS before processing credentials
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message =
                "HTTPS is required for administrator login."
        });
    }

    // Validate the antiforgery token
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

    // Validate the login request
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
            validationResults.Select(r => r.ErrorMessage)
        );
    }

    // Find the user by email address
    var user = await userManager.FindByEmailAsync(
        request.Email.Trim()
    );

    // Reject unknown users
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

    // Verify administrator role
    var isAdmin = await userManager.IsInRoleAsync(
        user,
        "Admin"
    );

    // Reject users without administrator permissions
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

    // Verify password and create authentication cookie
    var result = await signInManager.PasswordSignInAsync(
        user,
        request.Password,
        isPersistent: false,
        lockoutOnFailure: true
    );

    // Reject unsuccessful authentication attempts
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

    // Successful administrator login
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

// POST: Sign out the administrator
app.MapPost("/api/admin/logout", async (
    SignInManager<IdentityUser> signInManager,
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    // Require HTTPS
    if (!httpContext.Request.IsHttps)
    {
        return Results.BadRequest(new
        {
            Message =
                "HTTPS is required for administrator logout."
        });
    }

    // Validate the antiforgery security token
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

    // Sign out the administrator and clear
    // the authentication cookie
    await signInManager.SignOutAsync();

    // Prevent caching of the logout response
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return successful logout response
    return Results.Ok(new
    {
        Message = "Administrator logout successful."
    });
})
.RequireAuthorization("AdminOnly");

// =========================================
// PROTECTED ADMINISTRATOR DASHBOARD
// =========================================

// GET: Display the administrator dashboard
app.MapGet("/admin/", (
    HttpContext httpContext,
    IWebHostEnvironment environment) =>
{
    // Locate the protected dashboard HTML
    var dashboardPath = Path.Combine(
        environment.ContentRootPath,
        "Protected",
        "Admin",
        "index.html"
    );

    // Verify that the dashboard file exists
    if (!File.Exists(dashboardPath))
    {
        return Results.NotFound(new
        {
            Message =
                "Administrator dashboard not found."
        });
    }

    // Prevent caching of the protected dashboard
    httpContext.Response.Headers.CacheControl =
        "no-store";

    // Return the administrator dashboard
    return Results.File(
        dashboardPath,
        contentType: "text/html"
    );
})
.RequireAuthorization("AdminOnly");

// Protect and redirect the old dashboard URL
app.MapGet("/admin/index.html", () =>
    Results.Redirect("/admin/"))
.RequireAuthorization("AdminOnly");

// =========================================
// START APPLICATION
// =========================================

app.Run();
