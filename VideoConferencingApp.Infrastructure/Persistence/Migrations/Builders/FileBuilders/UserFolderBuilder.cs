using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class UserFolderBuilder : EntityTypeBuilder<UserFolder>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map UserFolder properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.UserId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.FolderId)))
                .AsString(50)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.FolderName)))
                .AsString(255)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.ParentFolderId)))
                .AsString(50)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.Path)))
                .AsString(500)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.Visibility)))
                .AsInt32()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.IsDeleted)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
        }
    }
}
