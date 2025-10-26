using FluentMigrator;
using FluentMigrator.Builders.Create;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Model;
using LinqToDB.Mapping;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations
{
    public static class MigrationExtensions
    {
        public static ICreateTableWithColumnSyntax TableFor<TEntity, TBuilder>(this ICreateExpressionRoot createExpression, INameCompatibility naming)
            where TEntity : BaseEntity
            where TBuilder : EntityTypeBuilder<TEntity>, new()
        {
            var entityType = typeof(TEntity);

            var tableName = NameCompatibilityManager.GetTableName(entityType);
            var pkColumnName = NameCompatibilityManager.GetColumnName(entityType, nameof(BaseEntity.Id));
            var createdAtColumnName = NameCompatibilityManager.GetColumnName(entityType, nameof(BaseEntity.CreatedOnUtc));
            var modifiedAtColumnName = NameCompatibilityManager.GetColumnName(entityType, nameof(BaseEntity.UpdatedOnUtc));
            var isActiveColumnName = NameCompatibilityManager.GetColumnName(entityType, nameof(BaseEntity.IsActive));

            var table = createExpression.Table(tableName);

            table
                .WithColumn(pkColumnName).AsInt64().PrimaryKey().Identity().NotNullable()
                .WithColumn(createdAtColumnName).AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn(modifiedAtColumnName).AsDateTime2().Nullable()
                .WithColumn(isActiveColumnName).AsBoolean().Nullable();

            var builder = new TBuilder();
            builder.MapEntity(table); // Pass nothing to configure

            return table;
        }

        

        public static ICreateTableColumnOptionOrForeignKeyCascadeOrWithColumnSyntax ForeignKey<TPrimary>(
            this ICreateTableColumnOptionOrWithColumnSyntax column,
            string primaryTableName = null,
            string primaryColumnName = null,
            Rule onDelete = Rule.Cascade) where TPrimary : BaseEntity
        {
            var primaryEntityType = typeof(TPrimary);

            if (string.IsNullOrEmpty(primaryTableName))
                primaryTableName = NameCompatibilityManager.GetTableName(primaryEntityType);

            if (string.IsNullOrEmpty(primaryColumnName))
                primaryColumnName = NameCompatibilityManager.GetColumnName(primaryEntityType, nameof(BaseEntity.Id));

            return column.Indexed()
                         .ForeignKey(primaryTableName, primaryColumnName)
                         .OnDelete(onDelete);
        }
    }
}