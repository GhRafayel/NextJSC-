using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class RefreshDto
{
    [Required]
    public string RefreshToken { get; set; } = "";
}