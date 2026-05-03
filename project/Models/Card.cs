using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectBanking.Models
{
    public class Card
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string CardHolderName { get; set; }

        [Required]
        public string ExpiryDate { get; set; }

        [Required]
        public string CVV { get; set; }

        [Required]
        public string CardType { get; set; } // Visa / Mastercard

        public bool IsBlocked { get; set; } = false;

        public int BankAccountId { get; set; }

        [ForeignKey("BankAccountId")]
        public BankAccount? Account { get; set; }
    }
}