using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Domain.Entities.PresenceEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Attributes;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.CallBuilders;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.ChatBuilders;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.UserBuilders;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.EntityMigrations
{
    // Use the new, simple attribute.
    [ProjectMigrationAttribute("20251011093006", "Create initial User table")]
    public class InitialSchema : SchemaMigration
    {
        public InitialSchema(INameCompatibility naming) : base(naming) { }

        public override void Up()
        {
            //Create.TableFor<User, UserBuilder>(Naming);
            //Create.TableFor<RefreshToken, RefreshTokenBuilder>(Naming);
            //Create.TableFor<LoginAttempt, LoginAttemptBuilder>(Naming);
           // Create.TableFor<UserSession, UserSessionBuilder>(Naming);
            //Create.TableFor<Contact, ContactBuilder>(Naming);
            //Create.TableFor<UserDeviceToken, UserDeviceTokenBuilder>(Naming);


            //Alter.Table(Naming.GetTableName(typeof(User)))
            //   .AddColumn(Naming.GetColumnName(typeof(User), nameof(User.PasswordHash)))
            //       .AsString(250)
            //       .Nullable();

            //Alter.Table(Naming.TableNames[typeof(User)])
            //   .AddColumn(Naming.GetColumnName(typeof(User), nameof(User.IsDeleted)))
            //       .AsBoolean()
            //       .Nullable();

            //Core File Manager Tables
            //Create.TableFor<UserFile, UserFileBuilder>(Naming);
            //Create.TableFor<TFileShare, TFileShareBuilder>(Naming);
            //Create.TableFor<TFileAccess, TFileAccessBuilder>(Naming);
            //Create.TableFor<UserFolder, UserFolderBuilder>(Naming);

            //// Optional Support Tables
            //Create.TableFor<UserStorageQuota, UserStorageQuotaBuilder>(Naming);
            //Create.TableFor<FileActivity, FileActivityBuilder>(Naming);

            //// Create indexes for better performance
            //Create.Index("IX_UserFile_UserId")
            //    .OnTable(NameCompatibilityManager.GetTableName(typeof(UserFile)))
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.UserId)))
            //    .Ascending();

            //Create.Index("IX_UserFile_FileHash")
            //    .OnTable(NameCompatibilityManager.GetTableName(typeof(UserFile)))
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FileHash)))
            //    .Ascending();

            //Create.Index("IX_FileShare_ShareToken")
            //    .OnTable(NameCompatibilityManager.GetTableName(typeof(TFileShare)))
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(TFileShare), nameof(TFileShare.ShareToken)))
            //    .Unique();

            //Create.Index("IX_FileAccess_FileId")
            //    .OnTable(NameCompatibilityManager.GetTableName(typeof(TFileAccess)))
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(TFileAccess), nameof(TFileAccess.FileId)))
            //    .Ascending();

            //Create.Index("IX_UserFolder_UserId_FolderId")
            //    .OnTable(NameCompatibilityManager.GetTableName(typeof(UserFolder)))
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.UserId)))
            //    .Ascending()
            //    .OnColumn(NameCompatibilityManager.GetColumnName(typeof(UserFolder), nameof(UserFolder.FolderId)))
            //    .Ascending();


            //// Chat tables
            //Create.TableFor<Message, MessageBuilder>(Naming);
            //Create.TableFor<MessageReceipt, MessageReceiptBuilder>(Naming);
            //Create.TableFor<ChatGroup, ChatGroupBuilder>(Naming);
            //Create.TableFor<GroupMember, GroupMemberBuilder>(Naming);

            //// Call tables
            //Create.TableFor<Call, CallBuilder>(Naming);
            //Create.TableFor<CallParticipant, CallParticipantBuilder>(Naming);

            //// Presence tables
            //Create.TableFor<UserPresence, UserPresenceBuilder>(Naming);
            //Create.TableFor<UserConnection, UserConnectionBuilder>(Naming);
        }


    }
}