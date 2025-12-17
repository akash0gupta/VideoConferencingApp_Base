using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.PresenceEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.CallBuilders
{
    public class UserPresenceBuilder : EntityTypeBuilder<UserPresence>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.UserId)))
                .AsString(50)
                .NotNullable()
                .Unique("UX_UserPresences_UserId")
                .Indexed("IX_UserPresences_UserId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.Status)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(1) // Offline = 1
                .Indexed("IX_UserPresences_Status");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.CustomMessage)))
                .AsString(200)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.LastSeen)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime)
                .Indexed("IX_UserPresences_LastSeen");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.StatusChangedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserPresence), nameof(UserPresence.IsDeleted)))
               .AsInt16()
               .Nullable();

         
        }
    }
}