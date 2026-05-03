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
    public class TransfersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransfersController(ApplicationDbContext context)
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
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var myAccounts = _context.BankAccounts.Where(a => a.ClientId == userId && a.IsActive).ToList();
            return View(myAccounts);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTransfer(int SourceAccountId, string DestinationIban, decimal Amount, string Description)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var sourceAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == SourceAccountId && a.ClientId == userId);
            var destinationAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.IBAN == DestinationIban);

            if (sourceAccount == null || destinationAccount == null)
            {
                TempData["Error"] = "Invalid source or destination account.";
                return RedirectToAction("Index");
            }

            decimal availableFunds = sourceAccount.Balance + sourceAccount.CreditLimit;

            if (Amount <= 0 || availableFunds < Amount)
            {
                TempData["Error"] = "Insufficient funds (including credit limit).";
                return RedirectToAction("Index");
            }

            if (sourceAccount.Currency != destinationAccount.Currency)
            {
                TempData["Error"] = $"Currency mismatch ({destinationAccount.Currency}).";
                return RedirectToAction("Index");
            }

            sourceAccount.Balance -= Amount;
            destinationAccount.Balance += Amount;

            _context.Transactions.Add(new Transaction { SourceAccountId = sourceAccount.Id, DestinationInfo = destinationAccount.IBAN, Amount = Amount, Currency = sourceAccount.Currency, Description = Description, TransactionType = "Transfer Out", TransactionDate = DateTime.UtcNow, Status = "Completed" });
            _context.Transactions.Add(new Transaction { SourceAccountId = destinationAccount.Id, DestinationInfo = sourceAccount.IBAN, Amount = Amount, Currency = destinationAccount.Currency, Description = Description, TransactionType = "Deposit", TransactionDate = DateTime.UtcNow, Status = "Completed" });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Successfully transferred {Amount:N2} {sourceAccount.Currency}.";
            return RedirectToAction("Index");
        }
    }
}