using System;
using System.ComponentModel.DataAnnotations;

namespace KryptIt.Models
{
    public class PasswordEntry
    {
        [Key]
        public int Id { get; set; }
        public string NomCompte { get; set; }
        public string MotDePasse { get; set; }
        public string URL { get; set; }
        public DateTime DateAjout { get; set; }
    }
}
