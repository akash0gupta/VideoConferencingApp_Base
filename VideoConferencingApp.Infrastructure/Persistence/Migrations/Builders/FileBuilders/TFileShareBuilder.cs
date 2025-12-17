using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class TFileShareBuilder : EntityTypeBuilder<TFileShare>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map TFileShare properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.FileId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.SharedById)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.SharedWithUserId)))
                .AsInt64()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.ShareToken)))
                .AsString(100)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.Permission)))
                .AsInt32()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.ExpiresAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.Password)))
                .AsString(255)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.AccessCount)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.MaxAccessCount)))
                .AsInt32()
                .Nullable();
        }
    }
}
