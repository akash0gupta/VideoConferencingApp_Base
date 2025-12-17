using Microsoft.Win32;
using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Application.DTOs.Notification;

namespace VideoConferencingApp.Api.DTOs.AuthDto
{
    public record RegisterRequestDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool AcceptTerms { get; set; }

        // Device Information
        public DeviceInfo DeviceInfo { get; set; }
    }
}
