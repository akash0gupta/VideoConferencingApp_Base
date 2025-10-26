using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Defines the category or profile of a migration.
    /// </summary>
    public enum MigrationProcessType
    {
        /// <summary>
        /// The type of migration process does not matter; it should run in any process.
        /// </summary>
        NoMatter,

        /// <summary>
        /// A core schema migration, typically run on fresh installations.
        /// </summary>
        Installation,

        /// <summary>
        /// An update migration for existing databases.
        /// </summary>
        Update
    }
}
