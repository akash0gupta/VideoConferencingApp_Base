using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.UserEntities
{
    public class User:BaseEntity,ISoftDeletedEntity
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string Bio { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIp { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public string EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public DateTime LastPasswordChangeAt { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? TwoFactorEnabledAt { get; set; }
        public string TwoFactorSecret { get; set; }
        public string TwoFactorBackupCodes { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public string SecurityStamp { get; set; }
        public string RegistrationIp { get; set; }
        public string RegistrationUserAgent { get; set; }
        public UserRole Role { get; set; }
        public bool IsDeleted { get ; set; }
    }
}
