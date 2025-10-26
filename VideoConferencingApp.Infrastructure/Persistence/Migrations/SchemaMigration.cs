using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// A base class for all database schema migrations in the application.
    /// It provides a consistent foundation and can be extended with common helper methods.
    /// </summary>
    public abstract class SchemaMigration : ForwardOnlyMigration
    {
        protected readonly INameCompatibility Naming;

        protected SchemaMigration(INameCompatibility naming)
        {
            Naming = naming;
        }

        // Keep this parameterless constructor for FluentMigrator's non-DI scanning if needed,
        // but the DI constructor will be used when running the application.
        protected SchemaMigration() { }
       
    }
}
