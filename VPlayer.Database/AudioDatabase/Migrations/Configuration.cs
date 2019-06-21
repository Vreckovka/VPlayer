using System.Data.Entity.Migrations;

namespace VPlayer.AudioStorage.AudioDatabase.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<AudioDatabaseContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = true;
            
        }

        protected override void Seed(AudioDatabaseContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
