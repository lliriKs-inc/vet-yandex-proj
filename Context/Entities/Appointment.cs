namespace WebApplication5.Context.Entities;

public record Appointment
(
    Guid Id,
    string Fullname,
    string AnimalType,
    string Nickname,
    string UserPhone,
    DateTime Date,
    string? PhotoUrl = null
);