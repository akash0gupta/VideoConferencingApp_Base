using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    public static class MigrationManager
    {
        /// <summary>
        /// Finds and runs all pending database migrations.
        /// </summary>
        public static IHost RunMigrations(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

                // This simple command tells FluentMigrator to find and apply all migrations
                // that have a version number higher than the last one recorded in the database.
                migrationRunner.MigrateUp();
            }
            return host;
        }
    }
}