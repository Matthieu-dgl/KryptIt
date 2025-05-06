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

        public DbSet<PasswordEntry> PasswordEntry { get; set; }
        public DbSet<PasswordEntryTag> PasswordEntryTag { get; set; }
        public DbSet<SharedPassword> SharedPassword { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Tag> Tag { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordEntry>().ToTable("PasswordEntry");
            modelBuilder.Entity<PasswordEntryTag>().HasKey(pet => new { pet.PasswordEntryId, pet.TagId });
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
