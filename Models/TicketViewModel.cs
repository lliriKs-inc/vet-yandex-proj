namespace WebApplication5.Models;

public class TicketViewModel
{
    public Guid AppointmentId { get; set; }
    public string Fullname { get; set; } = "";
    public string UserPhone { get; set; } = "";
    public string AnimalType { get; set; } = "";
    public string Nickname { get; set; } = "";
    public DateTime DateLocal { get; set; }

    public string Cabinet { get; set; } = "";
    public string QrPayload { get; set; } = "";
}
