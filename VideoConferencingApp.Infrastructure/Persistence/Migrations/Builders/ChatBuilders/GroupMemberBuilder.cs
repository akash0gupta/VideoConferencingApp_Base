using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.ChatBuilders
{
    public class GroupMemberBuilder : EntityTypeBuilder<GroupMember>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.GroupId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_GroupMembers_GroupId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.UserId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_GroupMembers_UserId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.Role)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(2); // Member = 2

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.JoinedAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.LastReadAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(GroupMember), nameof(GroupMember.DeletedBy)))
 .AsInt16()
 .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.IsDeleted)))
            .AsBoolean()
            .Nullable();
        }
    }
}