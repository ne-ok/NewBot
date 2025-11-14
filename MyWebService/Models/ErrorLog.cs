namespace MyWebService.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ErrorLevel { get; set; } = "ERROR";
        public string ErrorMessage { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public long? UserId { get; set; }
        public long? ChatId { get; set; }
        public string? Command { get; set; }
        public string? AdditionalData { get; set; }
    }
}