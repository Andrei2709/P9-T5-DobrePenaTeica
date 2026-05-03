using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectBanking.Data;
using ProiectBanking.Models;

namespace ProiectBanking.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var pendingClients = await _context.Clients
                .Where(c => c.Role == "Client" && !c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveClients = await _context.Clients
                .Include(c => c.BankAccounts)
                .Where(c => c.Role == "Client" && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.PendingTransactions = await _context.Transactions
                .Include(t => t.SourceAccount)
                .ThenInclude(a => a.Client)
                .Where(t => t.Status == "Pending Approval")
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return View(pendingClients);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveClient(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null)
            {
                client.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Account for {client.FirstName} has been approved and activated!";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RejectClient(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                TempData["Success"] = "The account has been rejected and permanently deleted.";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> DepositMoney(int accountId, decimal amount)
        {
            amount = Math.Round(amount, 2); // FIX ZECIMALE

            if (amount <= 0) return RedirectToAction("Dashboard");

            var account = await _context.BankAccounts.Include(a => a.Client).FirstOrDefaultAsync(a => a.Id == accountId);
            if (account != null)
            {
                account.Balance = Math.Round(account.Balance + amount, 2); // FIX ZECIMALE

                var transaction = new Transaction
                {
                    SourceAccountId = account.Id,
                    DestinationInfo = "Cash Deposit",
                    Amount = amount,
                    Currency = account.Currency,
                    Description = "Branch Cash Deposit",
                    TransactionType = "Deposit",
                    TransactionDate = DateTime.UtcNow,
                    Status = "Completed"
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{amount:N2} {account.Currency} successfully deposited into {account.Client.FirstName}'s account.";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveTransaction(int transactionId)
        {
            var transaction = await _context.Transactions.Include(t => t.SourceAccount).FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction != null && transaction.Status == "Pending Approval")
            {
                transaction.Status = "Completed";

                var destAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.IBAN == transaction.DestinationInfo);
                if (destAccount != null)
                {
                    destAccount.Balance = Math.Round(destAccount.Balance + transaction.Amount, 2); // FIX ZECIMALE
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"The transaction of {transaction.Amount:N2} has been approved and processed!";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RejectTransaction(int transactionId)
        {
            var transaction = await _context.Transactions.Include(t => t.SourceAccount).FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction != null && transaction.Status == "Pending Approval")
            {
                transaction.Status = "Rejected";

                if (transaction.SourceAccount != null)
                {
                    transaction.SourceAccount.Balance = Math.Round(transaction.SourceAccount.Balance + transaction.Amount, 2); // FIX ZECIMALE
                }

                await _context.SaveChangesAsync();
                TempData["Error"] = $"Transaction blocked. The funds have been returned to the client's account.";
            }
            return RedirectToAction("Dashboard");
        }
    }
}