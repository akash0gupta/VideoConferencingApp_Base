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
    public class RefreshTokenBuilder : EntityTypeBuilder<RefreshToken>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map RefreshToken properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.Token)))
                .AsString(500) // Tokens can be long
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.UserId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.CreatedAt)))
                .AsDateTime()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.ExpiresAt)))
                .AsDateTime()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.IsRevoked)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.RevokedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.ReplacedByToken)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.IpAddress)))
                .AsString(45) // Accommodates IPv6
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(RefreshToken), nameof(RefreshToken.UserAgent)))
                .AsString(500)
                .Nullable();
        }
    }
}
