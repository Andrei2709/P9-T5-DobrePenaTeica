using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectBanking.Models
{
    public class BankAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IBAN { get; set; }

        [Required]
        public string Currency { get; set; }

        [Required]
        public string AccountType { get; set; } // "Debit" sau "Credit"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } 

        public bool IsActive { get; set; } = true;
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}