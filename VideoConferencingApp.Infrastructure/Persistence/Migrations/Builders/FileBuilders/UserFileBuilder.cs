using FluentMigrator.Builders.Create.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders.FileBuilders
{
    public class UserFileBuilder : EntityTypeBuilder<UserFile>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // Map UserFile properties
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.UserId)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FileName)))
                .AsString(255)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.OriginalFileName)))
                .AsString(255)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FileExtension)))
                .AsString(50)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.ContentType)))
                .AsString(255)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FileSize)))
                .AsInt64()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FilePath)))
                .AsString(500)
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FileHash)))
                .AsString(64) // SHA256 hash
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.ThumbnailPath)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.Visibility)))
                .AsInt32()
                .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.Description)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.Tags)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.FolderId)))
                .AsString(50)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.DownloadCount)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.LastAccessedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.ExpiresAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.IsEncrypted)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.EncryptionKey)))
                .AsString(500)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserFile), nameof(UserFile.IsDeleted)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
        }
    }
}
