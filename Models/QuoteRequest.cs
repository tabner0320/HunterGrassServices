
using System.ComponentModel.DataAnnotations;

namespace HunterGrassServices.Api.Models;

public class QuoteRequest
{
    public int Id { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Service { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
