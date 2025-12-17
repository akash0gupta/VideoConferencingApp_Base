using FluentMigrator.Builders.Create.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.UserBuilders
{
    public class LoginAttemptBuilder : EntityTypeBuilder<LoginAttempt>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map LoginAttempt properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.UsernameOrEmail)))
                .AsString(255)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.IpAddress)))
                .AsString(45) // Accommodates IPv6
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.UserAgent)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.IsSuccessful)))
                .AsBoolean()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.FailureReason)))
                .AsString(255)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.AttemptedAt)))
                .AsDateTime()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.UserId)))
                .AsInt64()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(LoginAttempt), nameof(LoginAttempt.Location)))
                .AsString(255)
                .Nullable();
        }
    }
}
