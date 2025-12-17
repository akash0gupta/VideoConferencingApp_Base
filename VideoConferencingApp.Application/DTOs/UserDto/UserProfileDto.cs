using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.UserDto
{

        /// <summary>
        /// User profile information DTO
        /// </summary>
        public class UserProfileDto
        {
            public long Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string DisplayName { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string PhoneNumber { get; set; }
            public string Bio { get; set; }
            public bool IsOnline { get; set; }
            public DateTime? LastSeen { get; set; }
            public bool EmailVerified { get; set; }
            public DateTime? EmailVerifiedAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public UserRole Role { get; set; }

            // Additional computed properties
            public bool IsContact { get; set; }
            public bool IsBlocked { get; set; }
            public bool HasPendingRequest { get; set; }
            public ContactStatus? ContactStatus { get; set; }
            public int MutualContactsCount { get; set; }
        }

    public class UserSecuritySettingsDto
    {
        public bool TwoFactorEnabled { get; set; }
        public DateTime? TwoFactorEnabledAt { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string LastLoginIp { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public List<UserDeviceDto> ActiveDevices { get; set; }
    }

}

