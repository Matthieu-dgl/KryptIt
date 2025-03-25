using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KryptIt.Models
{
    public class PasswordEntryTag
    {
        [Key, Column(Order = 0)]
        [ForeignKey("PasswordEntry")]
        public int PasswordEntryId { get; set; }

        public PasswordEntry PasswordEntry { get; set; }

        [Key, Column(Order = 1)]
        [ForeignKey("Tag")]
        public int TagId { get; set; }

        public Tag Tag { get; set; }
    }
}
