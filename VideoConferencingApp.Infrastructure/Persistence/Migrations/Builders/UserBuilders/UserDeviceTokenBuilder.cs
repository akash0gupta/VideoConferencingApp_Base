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
    public class UserDeviceTokenBuilder : EntityTypeBuilder<UserDeviceToken>
    {
        /// <summary>
        /// Maps the UserDeviceToken entity to database table columns
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // === Foreign Key ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.UserId)))
                .AsInt64()
                .NotNullable()
                .Indexed("IX_UserDeviceToken_UserId"); // Index for faster lookups by user

            // === Device Token ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.DeviceToken)))
                .AsString(512) // FCM/APNs tokens can be quite long
                .NotNullable()
                .Indexed("IX_UserDeviceToken_DeviceToken"); // Index for token lookups

            // === Platform Information ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.Platform)))
                .AsInt32() // Assuming DevicePlatform is an enum (iOS=0, Android=1, Web=2)
                .NotNullable();

            // === Device Identification ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.DeviceId)))
                .AsString(255) // Unique device identifier
                .Nullable()
                .Indexed("IX_UserDeviceToken_DeviceId"); // Index for device-based queries

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.DeviceName)))
                .AsString(255) // e.g., "John's iPhone", "Samsung Galaxy S21"
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.DeviceModel)))
                .AsString(255) // e.g., "iPhone 14 Pro", "SM-G998B"
                .Nullable();

            // === Software Versions ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.OsVersion)))
                .AsString(512) // e.g., "iOS 17.1", "Android 14"
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.AppVersion)))
                .AsString(50) // e.g., "1.0.0", "2.3.1-beta"
                .Nullable();

            // === Activity Tracking ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.LastUsedAt)))
                .AsDateTime()
                .Nullable()
                .Indexed("IX_UserDeviceToken_LastUsedAt"); // Index for cleanup/maintenance queries

            // === Soft Delete ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(UserDeviceToken), nameof(UserDeviceToken.IsDeleted)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false)
                .Indexed("IX_UserDeviceToken_IsDeleted"); // Index for filtering deleted records
        }
    }
}
