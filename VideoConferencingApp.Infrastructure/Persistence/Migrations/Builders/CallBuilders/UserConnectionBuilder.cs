using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.PresenceEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.CallBuilders
{
    public class UserConnectionBuilder : EntityTypeBuilder<UserConnection>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.UserId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_UserConnections_UserId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.ConnectionId)))
                .AsString(100)
                .NotNullable()
                .Unique("UX_UserConnections_ConnectionId")
                .Indexed("IX_UserConnections_ConnectionId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.DeviceId)))
                .AsString(100)
                .Nullable()
                .Indexed("IX_UserConnections_DeviceId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.DeviceName)))
                .AsString(100)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.UserAgent)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.IpAddress)))
                .AsString(50)
                .Nullable()
                .Indexed("IX_UserConnections_IpAddress");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.ConnectedAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.LastActivityAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime)
                .Indexed("IX_UserConnections_LastActivityAt");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserConnection), nameof(UserConnection.IsDeleted)))
           .AsInt16()
           .Nullable();

        }
    }
}