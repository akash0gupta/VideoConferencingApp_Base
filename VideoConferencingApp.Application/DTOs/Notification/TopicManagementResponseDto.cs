namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class TopicManagementResponseDto
    {
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<TopicOperationError> Errors { get; set; } = new List<TopicOperationError>();
    }


   
}
