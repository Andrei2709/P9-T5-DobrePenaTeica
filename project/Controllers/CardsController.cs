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
    public class CardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CardsController(ApplicationDbContext context)
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
            var userCards = await _context.Cards
                .Include(c => c.Account)
                .Where(c => c.Account.ClientId == userId)
                .ToListAsync();

            ViewBag.Accounts = await _context.BankAccounts
                .Where(a => a.ClientId == userId && a.IsActive)
                .ToListAsync();

            return View(userCards);
        }

        [HttpPost]
        public async Task<IActionResult> IssueCard(int BankAccountId, string CardType)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            bool alreadyHasCard = await _context.Cards.AnyAsync(c => c.BankAccountId == BankAccountId);
            if (alreadyHasCard)
            {
                TempData["Error"] = "This account already has an active card.";
                return RedirectToAction("Index");
            }

            var client = await _context.Clients.FindAsync(userId);
            Random res = new Random();
            string cardNumber = "";
            bool isUnique = false;

            while (!isUnique)
            {
                string firstDigit = CardType == "Visa" ? "4" : "5";
                cardNumber = $"{firstDigit}{res.Next(100, 999)} {res.Next(1000, 9999)} {res.Next(1000, 9999)} {res.Next(1000, 9999)}";
                if (!await _context.Cards.AnyAsync(c => c.CardNumber == cardNumber)) isUnique = true;
            }

            var newCard = new Card
            {
                BankAccountId = BankAccountId,
                CardNumber = cardNumber,
                CardHolderName = (client.FirstName + " " + client.LastName).ToUpper(),
                ExpiryDate = DateTime.Now.AddYears(4).ToString("MM/yy"),
                CVV = res.Next(100, 999).ToString(),
                CardType = CardType,
                IsBlocked = false
            };

            _context.Cards.Add(newCard);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Card issued successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlock(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card != null)
            {
                card.IsBlocked = !card.IsBlocked;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card != null)
            {
                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Card permanently removed.";
            }
            return RedirectToAction("Index");
        }
    }
}