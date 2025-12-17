using Mapster;
using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Notification;

namespace VideoConferencingApp.Api.DTOs.AuthDto
{
    public record LoginRequestDto:IMapFrom<LoginDto>
    {
        [Required]
        public string UsernameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        // Device Information
        public DeviceInfo DeviceInfo { get; set; }        
    }
}
