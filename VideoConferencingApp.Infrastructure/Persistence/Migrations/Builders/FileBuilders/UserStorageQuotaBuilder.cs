using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class UserStorageQuotaBuilder : EntityTypeBuilder<UserStorageQuota>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserStorageQuota), nameof(UserStorageQuota.UserId)))
                .AsInt64()
                .NotNullable()
                .Unique();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserStorageQuota), nameof(UserStorageQuota.StorageLimit)))
                .AsInt64()
                .NotNullable()
                .WithDefaultValue(5368709120); // 5GB default

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserStorageQuota), nameof(UserStorageQuota.UsedStorage)))
                .AsInt64()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserStorageQuota), nameof(UserStorageQuota.LastCalculatedAt)))
                .AsDateTime()
                .Nullable();
        }
    }
}
