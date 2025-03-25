using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KryptIt.Models
{
    public class Tag
    {
        [Key]
        [Column("id_tag")]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        [Column("tag_name")]
        public string TagName { get; set; }

        public ICollection<PasswordEntryTag> PasswordEntryTag { get; set; }
    }
}
