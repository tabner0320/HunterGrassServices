
using System.ComponentModel.DataAnnotations;

namespace HunterGrassServices.Api.Models;

public class QuoteRequest
{
    // =========================================
    // QUOTE IDENTIFICATION
    // =========================================

    public int Id { get; set; }

    // =========================================
    // CUSTOMER INFORMATION
    // =========================================

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    // =========================================
    // REQUESTED LAWN CARE SERVICE
    // =========================================

    [Required]
    public string Service { get; set; } = string.Empty;

    public string? Message { get; set; }

    // =========================================
    // QUOTE REQUEST STATUS
    // =========================================

    // New       = Customer submitted a request.
    // Contacted = Administrator contacted customer.
    // Completed = Request has been completed.

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";

    // =========================================
    // REQUEST CREATION DATE
    // =========================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
