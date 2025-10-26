using FluentMigrator.Builders.Create.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain;
using VideoConferencingApp.Domain.Entities.User;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Builders
{
    /// <summary>
    /// Configures the database mapping for the User entity.
    /// </summary>
    public class UserBuilder : EntityTypeBuilder<User>
    {
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(ICreateTableWithColumnSyntax table)
        {
            // === Core Identity ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.Username)))
                .AsString(100)
                .NotNullable()
                .Unique(); // Usernames should be unique

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.Email)))
                .AsString(255)
                .NotNullable()
                .Unique(); // Emails should be unique

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.PasswordHash)))
                 .AsString(255) // Length to accommodate various hashing algorithms
                 .NotNullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.Role)))
                .AsInt32() // Assuming UserRole is an enum
                .NotNullable();

            // === Profile Information ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.DisplayName)))
                .AsString(150)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.ProfilePictureUrl)))
                .AsString(2048) // Standard max URL length
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.PhoneNumber)))
                .AsString(25)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.Bio)))
                .AsString(500)
                .Nullable();

            // === Status and Activity ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.IsOnline)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LastSeen)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LastLoginDate)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LastLoginIp)))
                .AsString(45) // Accommodates IPv6 addresses
                .Nullable();

            // === Email Verification ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.EmailVerified)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.EmailVerifiedAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.EmailVerificationToken)))
                .AsString(255)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.EmailVerificationTokenExpiry)))
                .AsDateTime()
                .Nullable();

            // === Password Management ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.PasswordResetToken)))
                .AsString(255)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.PasswordResetTokenExpiry)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LastPasswordChangeAt)))
                .AsDateTime()
                .NotNullable();

            // === Two-Factor Authentication ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.TwoFactorEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.TwoFactorEnabledAt)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.TwoFactorSecret)))
                .AsString(100)
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.TwoFactorBackupCodes)))
                .AsString(1000) // Storing multiple codes, possibly as JSON or delimited string
                .Nullable();

            // === Account Lockout and Security ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LockoutEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.LockoutEnd)))
                .AsDateTime()
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.AccessFailedCount)))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.SecurityStamp)))
                .AsString(255)
                .Nullable();

            // === Registration Metadata ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.RegistrationIp)))
                .AsString(45) // Accommodates IPv6 addresses
                .Nullable();

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.RegistrationUserAgent)))
                .AsString(500)
                .Nullable();

            // === Notification Preferences ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.EmailNotificationsEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.PushNotificationsEnabled)))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.DeviceToken)))
                .AsString(500) // Device tokens can be long
                .Nullable();

            // === Soft Delete ===
            table.WithColumn(NameCompatibilityManager.GetColumnName(typeof(User), nameof(User.IsDeleted)))
                 .AsBoolean()
                 .NotNullable()
                 .WithDefaultValue(false);
        }
    }
}
