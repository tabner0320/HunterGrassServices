
using System.ComponentModel.DataAnnotations;

namespace HunterGrassServices.Api.Models;

public class AdminLoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Password { get; set; } = string.Empty;
}
