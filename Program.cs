
using HunterGrassServices.Api.Data;
using HunterGrassServices.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.AddOpenApi();

// Register SQLite database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);


builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddSignInManager()
.AddEntityFrameworkStores<AppDbContext>();


builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HunterGrass.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
});

var app = builder.Build();

// Serve the website
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable OpenAPI during development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// POST: Save a customer quote request
app.MapPost("/api/quotes", async (
    QuoteRequest request,
    AppDbContext db) =>
{
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
            validationResults.Select(r => r.ErrorMessage)
        );
    }

    request.Id = 0;
    request.CreatedAt = DateTime.UtcNow;

    db.QuoteRequests.Add(request);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/quotes/{request.Id}",
        new
        {
            request.Id,
            Message = "Quote request saved successfully."
        }
    );
});

app.Run();
