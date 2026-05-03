using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProiectBanking.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required, StringLength(13)]
        public string CNP { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "Client";

        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BankAccount> BankAccounts { get; set; }
    }
}