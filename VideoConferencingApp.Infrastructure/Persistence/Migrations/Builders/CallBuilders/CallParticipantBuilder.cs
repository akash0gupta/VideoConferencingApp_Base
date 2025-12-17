using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.CallBuilders
{
    public class CallParticipantBuilder : EntityTypeBuilder<CallParticipant>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.CallId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_CallParticipants_CallId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.UserId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_CallParticipants_UserId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.JoinedAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.LeftAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.IsAudioEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.IsVideoEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(CallParticipant), nameof(CallParticipant.ConnectionId)))
                .AsString(100)
                .Nullable();

          
        }
    }
}