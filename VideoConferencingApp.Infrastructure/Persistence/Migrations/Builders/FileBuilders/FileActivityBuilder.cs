using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class FileActivityBuilder : EntityTypeBuilder<FileActivity>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.FileId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.UserId)))
                .AsInt64()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.ActivityType)))
                .AsString(50)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.Details)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.IpAddress)))
                .AsString(45)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(FileActivity), nameof(FileActivity.OccurredAt)))
                .AsDateTime()
                .NotNullable();
        }
    }
}
