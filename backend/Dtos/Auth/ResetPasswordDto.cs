using System.ComponentModel.DataAnnotations;
namespace Backend.Dtos;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email {get; set;} = "";

    [Required]
    // [MinLength(6)]
    public string Password {get; set;} = "";

    [Required]
    // [MinLength(6)]
    public string ConfirmPassword {get; set;} = "";

}