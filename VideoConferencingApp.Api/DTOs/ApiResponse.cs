namespace VideoConferencingApp.Api.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }=string.Empty;
        public T Data {get;set;}
        public string TraceId { get;set;}
        public DateTime Timestamp { get;set;}
    }
}
