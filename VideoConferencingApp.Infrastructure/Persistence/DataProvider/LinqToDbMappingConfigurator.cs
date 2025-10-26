using LinqToDB.Mapping;
using System.Reflection;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{
    /// <summary>
    /// A service that builds the linq2db mapping schema once at application startup.
    /// This is the bridge between our naming rules and the linq2db engine.
    /// </summary>
    public static class LinqToDbMappingConfigurator
    {
        private static MappingSchema? _mappingSchema;
        private static readonly object _lock = new();

        public static MappingSchema GetMappingSchema()
        {
            lock (_lock)
            {
                if (_mappingSchema == null)
                {
                    // 1. Create a new, empty mapping schema.
                    var schema = new MappingSchema();

                    // 2. Tell it to use our custom reader as its single source of truth for mapping.
                    schema.AddMetadataReader(new CustomMetadataReader());

                    _mappingSchema = schema;
                }
                return _mappingSchema!;
            }
        }
    }
}