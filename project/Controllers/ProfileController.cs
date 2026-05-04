using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProiectBanking.Data;
using ProiectBanking.Models;
using ProiectBanking.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ProiectBanking.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.IsInRole("Employee"))
            {
                context.Result = RedirectToAction("Dashboard", "Employee");
                return;
            }
            base.OnActionExecuting(context);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var client = await _context.Clients
                .Include(c => c.BankAccounts)
                    .ThenInclude(a => a.Cards)
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (client == null) return NotFound();

            var accountIds = client.BankAccounts.Select(a => a.Id).ToList();
            var clientIbans = client.BankAccounts.Select(a => a.IBAN).ToList();

            ViewBag.Transactions = await _context.Transactions
                .Include(t => t.SourceAccount)
                .Where(t => accountIds.Contains(t.SourceAccountId) ||
                           (clientIbans.Contains(t.DestinationInfo) && t.Status == "Completed"))
                .OrderByDescending(t => t.TransactionDate).Take(15).ToListAsync();

            return View(client);
        }

        [HttpPost]
        public async Task<IActionResult> OpenAccount(string Currency, string AccountType)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Random random = new Random();
            string generatedIban = "";
            bool isUnique = false;

            while (!isUnique)
            {
                generatedIban = "RO" + random.Next(10, 99) + "BANK" + random.Next(10000000, 99999999) + random.Next(10000000, 99999999);
                if (!_context.BankAccounts.Any(a => a.IBAN == generatedIban)) isUnique = true;
            }

            var newAccount = new BankAccount
            {
                ClientId = userId,
                IBAN = generatedIban,
                Currency = Currency ?? "RON",
                AccountType = AccountType ?? "Debit",
                Balance = 0.00m,
                CreditLimit = (AccountType == "Credit") ? 5000.00m : 0.00m,
                IsActive = true
            };

            _context.BankAccounts.Add(newAccount);
            await _context.SaveChangesAsync();
            TempData["Success"] = "New account opened successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MakeTransfer(int sourceAccountId, string destinationIban, decimal amount, string description)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sourceAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == sourceAccountId && a.ClientId == userId);

            amount = Math.Round(amount, 2);

            if (sourceAccount == null || amount <= 0)
            {
                TempData["Error"] = "Invalid transfer request.";
                return RedirectToAction("Index");
            }

            // Aflăm cine este destinatarul (dacă e din banca noastră)
            var destAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.IBAN == destinationIban);

            // BARIERA DE VALUTĂ (FOREIGN EXCHANGE BLOCK)
            if (destAccount != null && sourceAccount.Currency != destAccount.Currency)
            {
                TempData["Error"] = $"Transfers between different currencies ({sourceAccount.Currency} to {destAccount.Currency}) are not allowed.";
                return RedirectToAction("Index");
            }

            decimal availableFunds = sourceAccount.Balance + sourceAccount.CreditLimit;

            if (availableFunds < amount)
            {
                TempData["Error"] = "Insufficient funds or credit limit exceeded.";
                return RedirectToAction("Index");
            }

            decimal approvalThreshold = 5000.00m;
            bool requiresApproval = amount > approvalThreshold;

            sourceAccount.Balance = Math.Round(sourceAccount.Balance - amount, 2);

            var transaction = new Transaction
            {
                SourceAccountId = sourceAccount.Id,
                DestinationInfo = destinationIban,
                Amount = amount,
                Currency = sourceAccount.Currency,
                Description = description ?? "Funds transfer",
                TransactionType = "Transfer",
                TransactionDate = DateTime.UtcNow,
                Status = requiresApproval ? "Pending Approval" : "Completed"
            };

            if (!requiresApproval)
            {
                if (destAccount != null)
                {
                    destAccount.Balance = Math.Round(destAccount.Balance + amount, 2);
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            if (requiresApproval)
                TempData["Error"] = $"The transfer of {amount:N2} exceeds the security limit of {approvalThreshold}. The funds have been temporarily blocked pending bank officer approval.";
            else
                TempData["Success"] = "Transfer completed successfully!";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var client = await _context.Clients.FindAsync(userId);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string FirstName, string LastName, string PhoneNumber, string Address, string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var client = await _context.Clients.Include(c => c.BankAccounts).ThenInclude(a => a.Cards).FirstOrDefaultAsync(c => c.Id == userId);
            if (client == null) return NotFound();

            bool nameChanged = (client.FirstName != FirstName || client.LastName != LastName);
            client.FirstName = !string.IsNullOrWhiteSpace(FirstName) ? FirstName : client.FirstName;
            client.LastName = !string.IsNullOrWhiteSpace(LastName) ? LastName : client.LastName;
            client.PhoneNumber = PhoneNumber ?? "";
            client.Address = Address ?? "";

            if (!string.IsNullOrEmpty(NewPassword))
            {
                string hashedOldPassword = SecurityHelper.HashPassword(OldPassword);
                if (client.PasswordHash != hashedOldPassword || NewPassword != ConfirmPassword)
                {
                    TempData["Error"] = "Error changing password. Passwords may not match.";
                    return View(client);
                }
                client.PasswordHash = SecurityHelper.HashPassword(NewPassword);
            }

            _context.Update(client);
            await _context.SaveChangesAsync();

            if (nameChanged)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
                    new Claim(ClaimTypes.Name, client.FirstName + " " + client.LastName),
                    new Claim(ClaimTypes.Email, client.Email),
                    new Claim(ClaimTypes.Role, "Client")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }
    }
}
