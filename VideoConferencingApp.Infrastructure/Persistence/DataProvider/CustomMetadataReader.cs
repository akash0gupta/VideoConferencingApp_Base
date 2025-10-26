using LinqToDB.Mapping;
using LinqToDB.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{
    /// <summary>
    /// This is the definitive bridge between our NameCompatibilityManager's rules and the linq2db engine.
    /// It implements the IMetadataReader interface exactly as required by your version of the library.
    /// </summary>
    public class CustomMetadataReader : IMetadataReader
    {
        private readonly string _objectId = $".{nameof(CustomMetadataReader)}.{Guid.NewGuid()}";

        // --- IMPLEMENTATION OF THE 'GetAttributes(Type type)' METHOD ---
        public MappingAttribute[] GetAttributes(Type type)
        {
            var attributes = new List<MappingAttribute>();

            // For any class that is a BaseEntity, we provide a TableAttribute.
            if (typeof(BaseEntity).IsAssignableFrom(type))
            {
                var tableName = NameCompatibilityManager.GetTableName(type);
                attributes.Add(new TableAttribute(tableName));
            }

            return attributes.ToArray();
        }

        // --- IMPLEMENTATION OF THE 'GetAttributes(Type type, MemberInfo memberInfo)' METHOD ---
        public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
        {
            var attributes = new List<MappingAttribute>();

            // Provide a ColumnAttribute for every property.
            var columnName = NameCompatibilityManager.GetColumnName(type, memberInfo.Name);
            attributes.Add(new ColumnAttribute(columnName));

            // If the property is the 'Id' property, ALSO add PrimaryKey and Identity attributes.
            if (memberInfo.Name == nameof(BaseEntity.Id))
            {
                attributes.Add(new PrimaryKeyAttribute());
                attributes.Add(new IdentityAttribute());
            }

            return attributes.ToArray();
        }

        public MemberInfo[] GetDynamicColumns(Type type)
        {
            // We are not using dynamic columns, so we return an empty array.
            return Array.Empty<MemberInfo>();
        }

        public string GetObjectID()
        {
            // As per the interface documentation, this just needs to be a unique ID.
            return _objectId;
        }
    }
}
