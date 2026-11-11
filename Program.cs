
using HunterGrassServices.Api.Data;
using HunterGrassServices.Api.Models;
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
