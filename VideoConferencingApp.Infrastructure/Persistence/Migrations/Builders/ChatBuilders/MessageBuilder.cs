using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.ChatBuilders
{
    public class MessageBuilder : EntityTypeBuilder<Message>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.SenderId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_Messages_SenderId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.ReceiverId)))
                .AsString(50)
                .Nullable()
                .Indexed("IX_Messages_ReceiverId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.GroupId)))
                .AsString(50)
                .Nullable()
                .Indexed("IX_Messages_GroupId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.Content)))
                .AsString(int.MaxValue)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.Type)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.Status)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(1)
                .Indexed("IX_Messages_Status");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.SentAt)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.DeliveredAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.ReadAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.ReplyToMessageId)))
                .AsString(50)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Message), nameof(Message.Metadata)))
                .AsString(int.MaxValue)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(ChatGroup), nameof(ChatGroup.IsDeleted)))
            .AsBoolean()
            .Nullable();
        }
    }
}