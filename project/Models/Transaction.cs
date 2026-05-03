using System;
using System.ComponentModel.DataAnnotations;

namespace ProiectBanking.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int SourceAccountId { get; set; }
        public BankAccount SourceAccount { get; set; }

        [Required]
        public string DestinationInfo { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string Description { get; set; }

        public string TransactionType { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Completed";
    }
}