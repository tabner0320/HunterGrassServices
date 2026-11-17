
using System.Net;
using System.Net.Http.Json;

using HunterGrassServices.Api.Data;
using HunterGrassServices.Api.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace HunterGrassServices.Tests;

public class QuoteRequestTests
{
    // =========================================
    // TEST 1: VALID QUOTE SUBMISSION
    // =========================================

    [Fact]
    public async Task SubmitQuote_ValidRequest_ReturnsCreated()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "Test Customer",
            Email = "test@example.com",
            Phone = "555-0100",
            Address = "123 Test Street",
            Service = "Lawn Mowing",
            Message = "Please provide a quote."
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        // Verify that the quote was saved
        // to the temporary SQLite database.
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var savedQuote = await db.QuoteRequests
            .SingleAsync(q => q.Email == "test@example.com");

        Assert.Equal(
            "Test Customer",
            savedQuote.CustomerName
        );

        Assert.Equal(
            "Lawn Mowing",
            savedQuote.Service
        );

        Assert.Equal(
            "New",
            savedQuote.Status
        );
    }

    // =========================================
    // TEST 2: INVALID EMAIL ADDRESS
    // =========================================

    [Fact]
    public async Task SubmitQuote_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "Test Customer",
            Email = "invalid-email",
            Phone = "555-0100",
            Address = "123 Test Street",
            Service = "Lawn Mowing"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 3: MISSING CUSTOMER NAME
    // =========================================

    [Fact]
    public async Task SubmitQuote_MissingName_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "",
            Email = "test@example.com",
            Phone = "555-0100",
            Address = "123 Test Street",
            Service = "Lawn Mowing"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 4: MISSING PHONE NUMBER
    // =========================================

    [Fact]
    public async Task SubmitQuote_MissingPhone_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "Test Customer",
            Email = "test@example.com",
            Phone = "",
            Address = "123 Test Street",
            Service = "Lawn Mowing"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 5: MISSING ADDRESS
    // =========================================

    [Fact]
    public async Task SubmitQuote_MissingAddress_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "Test Customer",
            Email = "test@example.com",
            Phone = "555-0100",
            Address = "",
            Service = "Lawn Mowing"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 6: MISSING SERVICE
    // =========================================

    [Fact]
    public async Task SubmitQuote_MissingService_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );

        var quote = new QuoteRequest
        {
            CustomerName = "Test Customer",
            Email = "test@example.com",
            Phone = "555-0100",
            Address = "123 Test Street",
            Service = ""
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes",
            quote
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }
}
