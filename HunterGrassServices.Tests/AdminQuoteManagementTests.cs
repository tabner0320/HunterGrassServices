using System.Net;
using System.Net.Http.Json;

using HunterGrassServices.Api.Data;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace HunterGrassServices.Tests;

public class AdminQuoteManagementTests
{
    // =========================================
    // HELPER: CREATE ADMIN CLIENT
    // =========================================

    private static HttpClient CreateAdminClient(
        CustomWebApplicationFactory factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost")
            });
    }

    // =========================================
    // HELPER: GET ANTIFORGERY TOKEN
    // =========================================

    private static async Task<string> GetAntiforgeryTokenAsync(
        HttpClient client)
    {
        using var response = await client.GetAsync(
            "/api/admin/antiforgery");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<
            AntiforgeryTokenResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        return result.Token;
    }

    // =========================================
    // HELPER: SEND STATUS UPDATE
    // =========================================

    private static async Task<HttpResponseMessage> UpdateStatusAsync(
        HttpClient client,
        int quoteId,
        string status,
        string? antiforgeryToken = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/admin/quotes/{quoteId}/status");

        request.Content = JsonContent.Create(
            new { Status = status });

        // Must match Program.cs antiforgery configuration.
        if (!string.IsNullOrWhiteSpace(antiforgeryToken))
        {
            request.Headers.Add(
                "X-CSRF-TOKEN",
                antiforgeryToken);
        }

        var response = await client.SendAsync(request);

        // Print the response to help troubleshoot failures.
        var responseBody =
            await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"Status: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            $"Response: {responseBody}");

        return response;
    }

    // =========================================
    // TEST 1: VALID STATUS UPDATE
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_ValidStatus_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        // Create a valid quote through the public API.
        var quoteRequest = new
        {
            CustomerName = "Test Customer",
            Email = "customer@example.com",
            Phone = "5025551234",
            Address = "123 Test Street",
            Service = "Lawn Mowing",
            Message = "Integration test quote"
        };

        using var createResponse = await client.PostAsJsonAsync(
            "/api/quotes",
            quoteRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<
            CreatedQuoteResponse>();

        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // Retrieve antiforgery token and matching cookie.
        var token = await GetAntiforgeryTokenAsync(client);

        // Update the newly created quote.
        using var response = await UpdateStatusAsync(
            client,
            created.Id,
            "Contacted",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<
            UpdatedQuoteResponse>();

        Assert.NotNull(updated);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Contacted", updated.Status);
    }

    // =========================================
    // TEST 2: INVALID STATUS
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_InvalidStatus_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        var token = await GetAntiforgeryTokenAsync(client);

        using var response = await UpdateStatusAsync(
            client,
            1,
            "Cancelled",
            token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        // Confirm that status validation rejected the request,
        // rather than an antiforgery failure.
        var result = await response.Content.ReadFromJsonAsync<
            MessageResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "Status must be New, Contacted, or Completed.",
            result.Message);
    }

    // =========================================
    // TEST 3: QUOTE DOES NOT EXIST
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_MissingQuote_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        var token = await GetAntiforgeryTokenAsync(client);

        using var response = await UpdateStatusAsync(
            client,
            999999,
            "Completed",
            token);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<
            MessageResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "Quote request not found.",
            result.Message);
    }

    // =========================================
    // TEST 4: MISSING ANTIFORGERY TOKEN
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_WithoutToken_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        // Intentionally omit the antiforgery token.
        using var response = await UpdateStatusAsync(
            client,
            1,
            "Completed");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<
            MessageResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "Invalid or missing security token.",
            result.Message);
    }


    // =========================================
    // TEST 5: UNAUTHENTICATED USER
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_UnauthenticatedUser_IsDenied()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = CreateAdminClient(factory);

        using var response = await UpdateStatusAsync(
            client,
            1,
            "Completed");

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Unexpected status code: {response.StatusCode}");
    }

    // =========================================
    // TEST 6: REGULAR USER
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_RegularUser_IsForbidden()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseRegularUserAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        using var response = await UpdateStatusAsync(
            client,
            1,
            "Completed");

        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Unexpected status code: {response.StatusCode}");
    }

    // =========================================
    // TEST 7: HTTP INSTEAD OF HTTPS
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_HttpRequest_IsRejected()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        // Use HTTP intentionally to test HTTPS enforcement.
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost/api/admin/quotes/1/status");

        request.Content = JsonContent.Create(
            new { Status = "Completed" });

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<
            MessageResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "HTTPS is required.",
            result.Message);
    }


    // =========================================
    // TEST 8: SQLITE DATABASE PERSISTENCE
    // =========================================

    [Fact]
    public async Task UpdateQuoteStatus_ValidStatus_PersistsInDatabase()
    {
        using var factory = new CustomWebApplicationFactory
        {
            UseAdminAuthentication = true
        };

        using var client = CreateAdminClient(factory);

        // Create a quote.
        var quoteRequest = new
        {
            CustomerName = "Persistence Test Customer",
            Email = "persistence@example.com",
            Phone = "5025551234",
            Address = "123 Persistence Street",
            Service = "Lawn Mowing",
            Message = "Testing SQLite persistence"
        };

        using var createResponse = await client.PostAsJsonAsync(
            "/api/quotes",
            quoteRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreatedQuoteResponse>();

        Assert.NotNull(created);

        // Retrieve antiforgery token.
        var token = await GetAntiforgeryTokenAsync(client);

        // Update the status.
        using var updateResponse = await UpdateStatusAsync(
            client,
            created.Id,
            "Contacted",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // Create a separate database context scope.
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Read the saved quote from SQLite.
        var savedQuote = await db.QuoteRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(q => q.Id == created.Id);

        Assert.NotNull(savedQuote);

        // Verify the saved status.
        Assert.Equal("Contacted", savedQuote.Status);
    }


    // =========================================
    // RESPONSE MODELS
    // =========================================

    private sealed class AntiforgeryTokenResponse
    {
        public string Token { get; set; } = "";
    }

    private sealed class CreatedQuoteResponse
    {
        public int Id { get; set; }
    }

    private sealed class UpdatedQuoteResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
    }

    private sealed class MessageResponse
    {
        public string Message { get; set; } = "";
    }
}
