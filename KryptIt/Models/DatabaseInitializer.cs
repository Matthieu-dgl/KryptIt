using System.Data.Entity;

namespace KryptIt.Models
{
    public class DatabaseInitializer : CreateDatabaseIfNotExists<AppDbContext>
    {
        protected override void Seed(AppDbContext context)
        {
            base.Seed(context);
        }
    }
}
