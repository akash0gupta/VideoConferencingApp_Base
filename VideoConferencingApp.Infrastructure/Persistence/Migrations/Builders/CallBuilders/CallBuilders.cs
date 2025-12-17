using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.CallBuilders
{
    public class CallBuilder : EntityTypeBuilder<Call>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.CallerId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_Calls_CallerId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.ReceiverId)))
                .AsString(50)
                .Nullable()
                .Indexed("IX_Calls_ReceiverId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.GroupId)))
                .AsString(50)
                .Nullable()
                .Indexed("IX_Calls_GroupId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.Type)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0); // Voice = 0

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.Status)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
                .Indexed("IX_Calls_Status");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.InitiatedAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime)
                .Indexed("IX_Calls_InitiatedAt");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.ConnectedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.EndedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.DurationSeconds)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.EndReason)))
                .AsString(200)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.Metadata)))
                .AsString(int.MaxValue)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.DeletedBy)))
               .AsInt16()
               .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Call), nameof(Call.IsDeleted)))
            .AsBoolean()
            .Nullable();
        }
    }
}