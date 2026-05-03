using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProiectBanking.Data;
using ProiectBanking.Models;
using System.Security.Claims;

namespace ProiectBanking.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
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
            var myAccounts = await _context.BankAccounts.Where(a => a.ClientId == userId && a.IsActive).ToListAsync();
            return View(myAccounts);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int SourceAccountId, string Provider, string UtilityCode, decimal Amount)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == SourceAccountId && a.ClientId == userId);

            if (account == null)
            {
                TempData["Error"] = "Invalid account selected.";
                return RedirectToAction("Index");
            }

            decimal availableFunds = account.Balance + account.CreditLimit;

            if (Amount <= 0 || availableFunds < Amount)
            {
                TempData["Error"] = $"Fonduri insuficiente. Fonduri disponibile (inclusiv credit): {availableFunds:N2} {account.Currency}.";
                return RedirectToAction("Index");
            }

            account.Balance -= Amount;

            _context.Transactions.Add(new Transaction { SourceAccountId = account.Id, DestinationInfo = Provider, Amount = Amount, Currency = account.Currency, Description = $"Utility Payment - Ref: {UtilityCode}", TransactionType = "Payment", TransactionDate = DateTime.UtcNow, Status = "Completed" });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Payment of {Amount:N2} {account.Currency} to {Provider} was successful!";
            return RedirectToAction("Index");
        }
    }
}