using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Context;
using WebApplication5.Context.Entities;
using WebApplication5.Models;

namespace WebApplication5.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _appDbContext;

    public HomeController(ILogger<HomeController> logger,  SignInManager<AppUser> signInManager,  UserManager<AppUser> userManager, AppDbContext appDbContext)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _appDbContext = appDbContext;
    }
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var appointments = await _appDbContext.Appointments
            .Where(a => a.UserPhone == user.PhoneNumber)
            .OrderBy(a => a.Date)
            .ToListAsync();
        
        var model = new DashboardViewModel
        {
            FullName = $"{user.Surname} {user.Name} {user.Patronymic}",
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            PetsCount = 0,
            Appointments = appointments
        };

        return View(model);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var appointment = await _appDbContext.Appointments.FindAsync(id);
        if (appointment == null)
            return NotFound();

        var model = new CreateAppointmentViewModel
        {
            FullName = appointment.Fullname,
            AnimalType = appointment.AnimalType,
            Nickname = appointment.Nickname,
            PhoneNumber = appointment.UserPhone,
            Date = appointment.Date.ToLocalTime().Date,
            Time = appointment.Date.ToLocalTime().TimeOfDay
        };

        ViewBag.AppointmentId = appointment.Id;
        return View(model);
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CreateAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AppointmentId = id;
            return View(model);
        }

        var appointment = await _appDbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (appointment == null)
            return NotFound();
        
        var localDateTime = model.Date.Add(model.Time);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
            localDateTime,
            TimeZoneInfo.Local 
        );
        
        appointment = appointment with
        {
            Fullname = model.FullName,
            AnimalType = model.AnimalType,
            Nickname = model.Nickname,
            UserPhone = model.PhoneNumber,
            Date = utcDateTime
        };

        _appDbContext.Update(appointment);
        await _appDbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        _appDbContext.Remove(await _appDbContext.Appointments.FindAsync(id));
        await _appDbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    
    
    [HttpGet]
    public async Task<IActionResult> TicketCreator()
    {
        var user = await _userManager.GetUserAsync(User);

        var model = new CreateAppointmentViewModel
        {
            FullName = $"{user.Surname} {user.Name} {user.Patronymic}",
            PhoneNumber = user.PhoneNumber,
            Date = DateTime.Today
        };

        return View(model);
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> TicketCreator(CreateAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        var localDateTime = model.Date.Add(model.Time);

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
            localDateTime,
            TimeZoneInfo.Local 
        );
        var user = await _userManager.GetUserAsync(User);
        var fullname = $"{user.Surname} {user.Name} {user.Patronymic}";
        _appDbContext.Appointments.Add(new Appointment(Guid.NewGuid(), fullname, model.AnimalType, model.Nickname, user.PhoneNumber, utcDateTime));
        _appDbContext.SaveChanges();

        return RedirectToAction("Index", "Home");
    }
    public async Task<IActionResult> Admin()
    {
        var model = await _appDbContext.Appointments
            .AsNoTracking()
            .OrderBy(a => a.Date)
            .ToListAsync();
        return View(model);
    }
    
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            false,
            false);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Неверный логин или пароль");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Home");
    }
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.Phone,
            Surname = model.Surname,
            Name = model.Name,
            Patronymic = model.Patronymic
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}