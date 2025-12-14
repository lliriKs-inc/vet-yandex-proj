using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    public HomeController(ILogger<HomeController> logger,  SignInManager<AppUser> signInManager,  UserManager<AppUser> userManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
    }
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        var model = new DashboardViewModel
        {
            FullName = $"{user.Surname} {user.Name} {user.Patronymic}",
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            PetsCount = 3,
            Appointments = new List<AppointmentViewModel>()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    [Authorize(Roles = "User")]
    public IActionResult VetClinicPortal()
    {
        return View();
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

    [HttpPost]
    public async Task<IActionResult> TicketCreator(CreateAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        var fullname = $"{user.Surname} {user.Name} {user.Patronymic}";
        var appointment = new Appointment(Guid.NewGuid(), fullname, model.AnimalType, model.Nickname, user.PhoneNumber, model.Date);
        _appDbContext.Add(appointment);
        await _appDbContext.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
    public IActionResult Admin()
    {
        return View();
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
            return RedirectToAction("VetClinicPortal", "Home");

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