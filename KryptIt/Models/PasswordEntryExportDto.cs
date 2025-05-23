using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryptIt.Models
{
    public class PasswordEntryExportDto
    {
        public string SiteName { get; set; }
        public string Login { get; set; }
        public string EncryptedPassword { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

