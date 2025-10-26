namespace VideoConferencingApp.Api.Models
{
    public class ErrorResponse
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public IDictionary<string, string[]> Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
