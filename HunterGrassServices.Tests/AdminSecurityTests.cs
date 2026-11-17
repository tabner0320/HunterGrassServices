
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace HunterGrassServices.Tests;

public class AdminSecurityTests
{
    // =========================================
    // TEST 1: UNAUTHENTICATED QUOTE ACCESS
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
    // TEST 2: UNAUTHENTICATED STATUS UPDATE
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
    // TEST 3: REGULAR USER IS FORBIDDEN
    // =========================================

    [Fact]
    public async Task GetAdminQuotes_RegularUser_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseRegularUserAuthentication = true
        };

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            }
        );

        var response = await client.GetAsync(
            "/api/admin/quotes"
        );

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }

    // =========================================
    // TEST 4: ADMIN USER IS AUTHORIZED
    // =========================================

    [Fact]
    public async Task GetAdminQuotes_AdminUser_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            }
        );

        var response = await client.GetAsync(
            "/api/admin/quotes"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }
}
