

using VideoConferencingApp.Application.DTOs.UserDto;

namespace VideoConferencingApp.Api.DTOs.AuthDto
{
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
        public bool DeviceRegistered { get; set; }
        public bool RequiresEmailVerification { get; set; }
    }
}
