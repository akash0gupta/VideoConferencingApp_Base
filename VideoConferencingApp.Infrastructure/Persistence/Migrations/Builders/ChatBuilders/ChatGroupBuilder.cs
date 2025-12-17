using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.ChatBuilders
{
    public class ChatGroupBuilder : EntityTypeBuilder<ChatGroup>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.GroupName)))
                .AsString(100)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.Description)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.AvatarUrl)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.CreatedBy)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_ChatGroups_CreatedBy");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.CreatedAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.DeletedBy)))
 .AsInt16()
 .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.IsDeleted)))
            .AsBoolean()
            .Nullable();
        }
    }
}