﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace KryptIt.Models
{
    public class PasswordEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id_password")]
        public int Id { get; set; }

        [ForeignKey("User")]
        [Column("id_user")]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("site_name")]
        public string SiteName { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("login")]
        public string Login { get; set; }

        [Required]
        [Column("encrypted_password")]
        public string EncryptedPassword { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("otp_enabled")]
        public bool OtpEnabled { get; set; } = false;

        [Column("otp_secret")]
        [MaxLength(255)]
        public string OtpSecret { get; set; }

        [NotMapped]
        public string TagNames => PasswordEntryTag != null
        ? string.Join(", ", PasswordEntryTag.Select(pet => pet.Tag.TagName))
        : string.Empty;

        public ObservableCollection<PasswordEntryTag> PasswordEntryTag { get; set; } = new ObservableCollection<PasswordEntryTag>();

        public ICollection<SharedPassword> SharedPassword { get; set; }
    }
}
