using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Context;
using Amazon.S3;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("VetClinic");

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
    var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

    if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("(!) S3 credentials not found in environment variables!");
    }

    var config = new AmazonS3Config
    {
        ServiceURL = "https://storage.yandexcloud.net",
        ForcePathStyle = true
    };

    return new AmazonS3Client(accessKey, secretKey, config);
});



builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Retry logic для миграций (на случай временной недоступности БД)
    var maxRetries = 5;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Applying database migrations (attempt {Attempt}/{MaxRetries})...", i + 1, maxRetries);
            context.Database.Migrate();
            logger.LogInformation("Migrations applied successfully");
            break;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            logger.LogWarning(ex, "Migration failed, retrying in 5 seconds...");
            await Task.Delay(5000);
        }
    }

    // Создание ролей
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "User", "Doctor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}



app.MapStaticAssets();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "ticket",
    pattern: "ticket/{id:guid}",
    defaults: new { controller = "Home", action = "Ticket" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health");

app.Run();
