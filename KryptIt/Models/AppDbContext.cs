using System.Data.Entity;
using System.Data.SQLite;
using System.Data.Entity.ModelConfiguration.Conventions;
using System;

namespace KryptIt.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<PasswordEntry> PasswordEntries { get; set; }

        public AppDbContext() : base("name=SQLiteConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordEntry>().ToTable("PasswordEntries");
        }
    }
}
