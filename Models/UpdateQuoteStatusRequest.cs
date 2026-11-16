
using System.ComponentModel.DataAnnotations;

namespace HunterGrassServices.Api.Models;

public class UpdateQuoteStatusRequest
{
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}
