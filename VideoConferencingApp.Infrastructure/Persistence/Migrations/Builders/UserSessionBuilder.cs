using FluentMigrator.Builders.Create.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.User;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders
{
    public class UserSessionBuilder : EntityTypeBuilder<UserSession>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map UserSession properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserSession), nameof(UserSession.UserId)))
                .AsInt64()
                .NotNullable();
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserSession), nameof(UserSession.IpAddress)))
                .AsString(45) // Accommodates IPv6
                .Nullable();
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserSession), nameof(UserSession.UserAgent)))
                .AsString(500)
                .Nullable();
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserSession), nameof(UserSession.CreatedAt)))
                .AsDateTime()
                .NotNullable();
        }
    }
}
