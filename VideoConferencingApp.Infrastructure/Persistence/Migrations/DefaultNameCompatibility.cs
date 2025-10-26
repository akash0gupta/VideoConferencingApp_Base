using VideoConferencingApp.Domain.Entities.User;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    public class DefaultNameCompatibility : INameCompatibility
    {
        public Dictionary<Type, string> TableNames { get; } = new()
        {
            // Convention: Master tables are prefixed with "mst_", transactional with "tbl_"
            { typeof(User), "mst_User" },
        };

        public Dictionary<Type, string> ViewNames { get; } = new();

        public Dictionary<(Type, string), string> ColumnNames { get; } = new()
        {
            // User Table Column Mappings
            { (typeof(User), nameof(User.Id)), "UserId" },
            { (typeof(User), nameof(User.IsDeleted)), "SoftDeleted" }
        };

        Dictionary<Type, string> INameCompatibility.DatabaseNames =>  new ();
        public string GetTableName(Type type)=> TableNames.TryGetValue(type, out var specificName) ? specificName : type.Name;      
        public  string GetColumnName(Type type, string propertyName)=> ColumnNames.TryGetValue((type, propertyName), out var specificName) ? specificName : propertyName;
       
    }
}