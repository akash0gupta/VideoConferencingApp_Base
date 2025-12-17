namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class BatchNotificationResponse
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalCount { get; set; }
        public List<SingleNotificationResult> Results { get; set; } = new List<SingleNotificationResult>();
        public List<string> FailedTokens { get; set; } = new List<string>();
    }


   
}
