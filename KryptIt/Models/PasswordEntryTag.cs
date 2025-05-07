using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KryptIt.Models
{
    public class PasswordEntryTag
    {
        [Key]
        [ForeignKey("PasswordEntry")]
        [Column("id_password")]
        public int PasswordEntryId { get; set; }

        public PasswordEntry PasswordEntry { get; set; }

        [Key]
        [ForeignKey("Tag")]
        [Column("id_tag")]
        public int TagId { get; set; }

        public Tag Tag { get; set; }
    }
}
