using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Entities.User;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Attributes;
using VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.EntityMigrations
{
    // Use the new, simple attribute.
    [ProjectMigrationAttribute("20251011093002", "Create initial User table")]
    public class InitialSchema : SchemaMigration
    {
        public InitialSchema(INameCompatibility naming) : base(naming) { }

        public override void Up()
        {
            Create.TableFor<User, UserBuilder>(Naming);
            Create.TableFor<RefreshToken, RefreshTokenBuilder>(Naming);
            Create.TableFor<LoginAttempt, LoginAttemptBuilder>(Naming);
            Create.TableFor<UserSession, UserSessionBuilder>(Naming);
            Create.TableFor<Contact, ContactBuilder>(Naming);


            //Alter.Table(Naming.GetTableName(typeof(User)))
            //   .AddColumn(Naming.GetColumnName(typeof(User),nameof(User.PasswordHash)))
            //       .AsString(250)  
            //       .Nullable();

            //Alter.Table(Naming.TableNames[typeof(User)])
            //   .AddColumn(Naming.GetColumnName(typeof(User), nameof(User.IsDeleted)))
            //       .AsBoolean()
            //       .Nullable();
        }


    }
}