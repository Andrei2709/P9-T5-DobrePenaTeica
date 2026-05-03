using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectBanking.Data;
using ProiectBanking.Models;
using ProiectBanking.Helpers;
using System.Security.Claims;

namespace ProiectBanking.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ÎNREGISTRARE (REGISTER) ---
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string FirstName, string LastName, string CNP, string Email, string PhoneNumber, string Address, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return View();
            }

            // Verificăm dacă cele două parole coincid!
            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Cele două parole nu se potrivesc! Încearcă din nou.";
                return View();
            }

            if (await _context.Clients.AnyAsync(c => c.Email.ToLower() == Email.ToLower()))
            {
                TempData["Error"] = "This email is already registered.";
                return View();
            }

            var newClient = new Client
            {
                FirstName = FirstName,
                LastName = LastName,
                CNP = string.IsNullOrWhiteSpace(CNP) ? "N/A" : CNP,
                Email = Email,
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? "" : PhoneNumber,
                Address = string.IsNullOrWhiteSpace(Address) ? "" : Address,
                PasswordHash = SecurityHelper.HashPassword(Password),
                Role = "Client",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Clients.Add(newClient);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Account created! Please wait for an administrator to activate your account before logging in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Database Error: " + (ex.InnerException?.Message ?? ex.Message);
                return View();
            }
        }

        // --- AUTENTIFICARE (LOGIN) ---
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["Error"] = "Please enter email and password.";
                return View();
            }

            var user = await _context.Clients.FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower());

            if (user == null)
            {
                TempData["Error"] = $"Nu am găsit niciun cont cu email-ul '{Email}'.";
                return View();
            }

            string hashedPassword = SecurityHelper.HashPassword(Password);
            if (user.PasswordHash != hashedPassword)
            {
                TempData["Error"] = "Parola introdusă este greșită!";
                return View();
            }

            if (user.Role == "Client" && !user.IsActive)
            {
                TempData["Error"] = "Contul tău a fost creat, dar așteaptă activarea unui administrator. Revino mai târziu.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "Client")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (user.Role == "Employee")
            {
                return RedirectToAction("Dashboard", "Employee");
            }

            return RedirectToAction("Index", "Profile");
        }

        // --- DECONECTARE (LOGOUT) ---
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}