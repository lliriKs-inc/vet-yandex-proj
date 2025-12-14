using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models;

public class CreateAppointmentViewModel
{
    [Required]
    [Display(Name = "ФИО")]
    public string FullName { get; set; }

    [Required]
    [Display(Name = "Тип животного")]
    public string AnimalType { get; set; }

    [Required]
    [Display(Name = "Кличка животного")]
    public string Nickname { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Номер телефона")]
    public string PhoneNumber { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateTime Date { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Время")]
    public TimeSpan Time { get; set; }
}