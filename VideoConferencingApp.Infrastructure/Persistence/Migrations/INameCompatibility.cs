using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Provides a centralized service for resolving database object names (tables, columns, etc.)
    /// based on application entity types and property names.
    /// </summary>
    public partial interface INameCompatibility
    {
        /// <summary>
        /// Gets a mapping from a C# Type to its corresponding database table name.
        /// </summary>
        Dictionary<Type, string> TableNames { get; }

        /// <summary>
        /// Gets a mapping from a C# Type to its corresponding database view name.
        /// </summary>
        Dictionary<Type, string> ViewNames { get; }

        /// <summary>
        /// Gets a mapping from a (Type, PropertyName) tuple to its corresponding database column name.
        /// </summary>
        Dictionary<(Type, string), string> ColumnNames { get; }

        Dictionary<Type, string> DatabaseNames { get; }


        string GetTableName(Type type);

        string GetColumnName(Type type, string propertyName);

    }
}
