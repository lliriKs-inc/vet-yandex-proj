using WebApplication5.Context.Entities;

namespace WebApplication5.Models;

public class DashboardViewModel
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }

    public int PetsCount { get; set; }

    public List<Appointment> Appointments { get; set; } = new();
}