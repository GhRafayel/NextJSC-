using System.ComponentModel.DataAnnotations;
namespace Backend.Dtos;

public class ResetCodeDto
{
    [Required]
    [EmailAddress]
    public string Email {get; set;} = "";

    [Required]
    [MinLength(6)] [MaxLength(6)]
    
    public string Code {get; set;} = "";

}