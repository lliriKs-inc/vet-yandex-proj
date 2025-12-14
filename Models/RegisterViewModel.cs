using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models;

public class RegisterViewModel
{
    [Required]
    public string Surname { get; set; }
    
    [Required]
    public string Name { get; set; }
    public string Patronymic { get; set; }
    
    [Required]
    [Phone]
    public string Phone { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
}