using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Context;

namespace WebApplication5.Controllers;

[ApiController]
[Route("internal/ticket")]
public class InternalTicketController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<InternalTicketController> _logger;

    public InternalTicketController(AppDbContext db, ILogger<InternalTicketController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        // 1) Проверяем секрет, чтобы endpoint был доступен только “своим” (Cloud Function/сервер)
        var expected = Environment.GetEnvironmentVariable("TICKET_INTERNAL_SECRET");
        var actual = Request.Headers["X-Internal-Secret"].ToString();

        if (string.IsNullOrWhiteSpace(expected) || actual != expected)
        {
            _logger.LogWarning("Forbidden internal ticket request (bad secret) for {Id}", id);
            return Forbid();
        }

        // 2) Достаём appointment из БД
        var appt = await _db.Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appt is null)
            return NotFound();

        // 3) Кабинет пока вычисляем детерминированно из Guid (без миграций БД)
        var cabinet = MakeCabinet(appt.Id);

        // 4) Возвращаем строго то, что нужно для PDF
        return Ok(new
        {
            id = appt.Id,
            fullname = appt.Fullname,
            userPhone = appt.UserPhone,
            animalType = appt.AnimalType,
            nickname = appt.Nickname,
            dateUtc = appt.Date,
            cabinet = cabinet,
            qrPayload = appt.Id.ToString()
        });
    }

    private static string MakeCabinet(Guid id)
    {
        var bytes = id.ToByteArray();
        var number = 100 + (bytes[0] % 900); // 100..999
        return $"К-{number}";
    }
}
