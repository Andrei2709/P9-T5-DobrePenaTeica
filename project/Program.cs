using Microsoft.EntityFrameworkCore;
using ProiectBanking.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using ProiectBanking.Data;

var builder = WebApplication.CreateBuilder(args);

// Ad?ug?m contextul bazei de date ?i îi spunem s? foloseasc? PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Daca cineva vrea sa intre pe Profil si nu e logat, il trimite aici
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Sesiunea expira dupa 15 minute
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
