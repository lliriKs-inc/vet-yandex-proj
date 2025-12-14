using Microsoft.AspNetCore.Identity;

namespace WebApplication5.Context;

public class AppUser : IdentityUser
{
    public string Surname { get; set; }
    public string Name { get; set; }
    public string Patronymic { get; set; }
}