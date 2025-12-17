namespace VideoConferencingApp.Application.DTOs.Call
{
    public class CallStatisticsDto
    {
        public int TotalCalls { get; set; }
        public int IncomingCalls { get; set; }
        public int OutgoingCalls { get; set; }
        public int MissedCalls { get; set; }
        public int TotalDurationSeconds { get; set; }
        public int AverageDurationSeconds { get; set; }
        public int VideoCalls { get; set; }
        public int VoiceCalls { get; set; }
        public Dictionary<string, int> CallsByDay { get; set; } = new();
    }
}