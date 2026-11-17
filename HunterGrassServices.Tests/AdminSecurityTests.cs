
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace HunterGrassServices.Tests;

public class AdminSecurityTests
{
    // =========================================
    // TEST 1: BLOCK UNAUTHENTICATED ACCESS
    // =========================================

    [Fact]
    public async Task GetAdminQuotes_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );

        var response = await client.GetAsync(
            "/api/admin/quotes"
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 2: BLOCK UNAUTHENTICATED UPDATES
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_WithoutLogin_ReturnsUnauthorized()
    {
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

        var response = await client.PutAsJsonAsync(
            "/api/admin/quotes/1/status",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 3: BLOCK NON-ADMIN USERS
    // =========================================

    [Fact]
    public async Task GetAdminQuotes_RegularUser_ReturnsForbidden()
    {
        // Create a test application with a
        // simulated authenticated regular user.

        using var factory = new CustomWebApplicationFactory
        {
            UseRegularUserAuthentication = true
        };

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,

                // Use HTTPS for security tests.
                BaseAddress = new Uri("https://localhost")
            }
        );

        var response = await client.GetAsync(
            "/api/admin/quotes"
        );

        // A signed-in user without the Admin
        // role must receive HTTP 403 Forbidden.

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }
}
