using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KryptIt.Models
{
    public class User
    {
        [Key]
        [Column("id_user")]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [Column("password")]
        public string Password { get; set; }

        [Required, EmailAddress]
        [Column("email")]
        public string Email { get; set; }

        [Column("twofactor_enable")]
        public bool TwoFactorEnabled { get; set; }

        [Column("twofactor_secret")]
        public string TwoFactorSecret { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        public ICollection<PasswordEntry> PasswordEntry { get; set; }

        public ICollection<SharedPassword> SharedPassword { get; set; }
    }
}
