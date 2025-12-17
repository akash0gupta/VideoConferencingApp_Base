using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;
namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.ChatBuilders
{
    public class MessageReceiptBuilder : EntityTypeBuilder<MessageReceipt>
    {
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(MessageReceipt), nameof(MessageReceipt.MessageId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_MessageReceipts_MessageId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(MessageReceipt), nameof(MessageReceipt.UserId)))
                .AsString(50)
                .NotNullable()
                .Indexed("IX_MessageReceipts_UserId");

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(MessageReceipt), nameof(MessageReceipt.Status)))
                .AsInt32()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(MessageReceipt), nameof(MessageReceipt.Timestamp)))
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);

        }
    }
}