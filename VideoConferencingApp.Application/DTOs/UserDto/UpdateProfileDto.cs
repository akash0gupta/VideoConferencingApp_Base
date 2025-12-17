using System.ComponentModel.DataAnnotations;

namespace VideoConferencingApp.Application.DTOs.UserDto
{
    /// <summary>
    /// DTO for updating user profile
    /// </summary>
    public class UpdateProfileDto
    {
        [StringLength(100, MinimumLength = 2)]
        public string DisplayName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens and underscores")]
        public string Username { get; set; }
    }

}

