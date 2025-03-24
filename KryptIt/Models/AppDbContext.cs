using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace KryptIt.Models
{
    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            Database.SetInitializer(new DatabaseInitializer());
        }

        public AppDbContext() : base("name=MariaDbConnection")
        {
        }

        public DbSet<PasswordEntry> PasswordEntries { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordEntry>().ToTable("PasswordEntries");
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
