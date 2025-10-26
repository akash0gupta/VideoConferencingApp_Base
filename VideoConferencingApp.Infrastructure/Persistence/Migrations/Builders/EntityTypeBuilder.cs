using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders
{
    /// <summary>
    /// Base builder for mapping entities to database tables
    /// </summary>
    public abstract class EntityTypeBuilder<TEntity>: IEntityBuilder where TEntity : BaseEntity
    {
        public abstract void MapEntity(ICreateTableWithColumnSyntax table);
    }
}
