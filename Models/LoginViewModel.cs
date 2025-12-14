using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}