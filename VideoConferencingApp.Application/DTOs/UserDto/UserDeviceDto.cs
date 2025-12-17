using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.UserDto
{
    /// <summary>
    /// User device DTO
    /// </summary>
    public class UserDeviceDto
    {
        public long Id { get; set; }
        public string DeviceName { get; set; }
        public string DeviceModel { get; set; }
        public DevicePlatform Platform { get; set; }
        public string OsVersion { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsCurrentDevice { get; set; }
    }


}

