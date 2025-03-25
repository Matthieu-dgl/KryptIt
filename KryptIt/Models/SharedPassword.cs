using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KryptIt.Models
{
    public class SharedPassword
    {
        [Key]
        [Column("id_shared")]
        public int Id { get; set; }

        [ForeignKey("PasswordEntry")]
        [Column("id_password")]
        public int PasswordEntryId { get; set; }

        public PasswordEntry PasswordEntry { get; set; }

        [ForeignKey("User")]
        [Column("id_user")]
        public int UserId { get; set; }

        public User User { get; set; }

        [Column("permission")]
        public bool Permission { get; set; }
    }
}
