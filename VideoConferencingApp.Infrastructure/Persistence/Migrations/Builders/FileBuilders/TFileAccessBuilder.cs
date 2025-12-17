using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class TFileAccessBuilder : EntityTypeBuilder<Domain.Entities.FileEntities.TFileAccess>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map TFileAccess properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(TFileAccess.FileId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(TFileAccess.UserId)))
                .AsInt64()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(Domain.Entities.FileEntities.TFileAccess.AccessToken)))
                .AsString(100)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(TFileAccess.AccessType)))
                .AsInt32()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(Domain.Entities.FileEntities.TFileAccess.IpAddress)))
                .AsString(45) // Accommodates IPv6
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(TFileAccess.UserAgent)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Domain.Entities.FileEntities.TFileAccess), nameof(Domain.Entities.FileEntities.TFileAccess.AccessedAt)))
                .AsDateTime()
                .NotNullable();
        }
    }
}
