namespace VideoConferencingApp.Application.DTOs.Call
{
    public class ToggleMediaDto
    {
        public string CallId { get; set; } = string.Empty;
        public bool? AudioEnabled { get; set; }
        public bool? VideoEnabled { get; set; }
    }
}