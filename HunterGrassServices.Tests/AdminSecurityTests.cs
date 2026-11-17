
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace HunterGrassServices.Tests;

public class AdminSecurityTests
{
    // =========================================
    // TEST 1: PROTECT CUSTOMER QUOTE DATA
    // =========================================

    [Fact]
    public async Task GetAdminQuotes_WithoutLogin_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );

        // Act
        var response = await client.GetAsync(
            "/api/admin/quotes"
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 2: PROTECT QUOTE STATUS UPDATES
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_WithoutLogin_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );

        var updateRequest = new
        {
            Status = "Completed"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/admin/quotes/1/status",
            updateRequest
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }
}
