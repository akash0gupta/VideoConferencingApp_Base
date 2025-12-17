using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Call
{
    public class InitiateCallDto
    {
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public CallsType CallType { get; set; } = CallsType.Voice;
    }
}