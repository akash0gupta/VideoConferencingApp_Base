using FluentMigrator.Builders.Create.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders
{
    public class ContactBuilder : EntityTypeBuilder<Contact>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map Contact properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.RequesterId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.RequesterUserName)))
                .AsString(100)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.AddresseeId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.AddresseeUserName)))
                .AsString(100)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.Status)))
                .AsInt32() // Assuming ContactStatus is an enum
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.Message)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.RequestedAt)))
                .AsDateTime()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.AcceptedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.RejectedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.RejectionReason)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.BlockedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.BlockReason)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.IsFavorite)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.CreatedAt)))
                .AsDateTime()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.UpdatedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.IsDeleted)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.DeletedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(Contact), nameof(Contact.DeletedBy)))
                .AsInt64()
                .Nullable();
        }
    }

}
