using System.Text.Json.Serialization;

namespace MyWebService.Models.VkModels
{
    public class LongPollServerResponse
    {
        public LongPollServer Response { get; set; } = null!;
    }

    public class LongPollServer
    {
        public string Key { get; set; } = null!;
        public string Server { get; set; } = null!;
        public string Ts { get; set; } = null!;
    }

    public class LongPollUpdate
    {
        public string Ts { get; set; } = null!;
        public UpdateItem[] Updates { get; set; } = Array.Empty<UpdateItem>();
    }

    public class UpdateItem
    {
        public string Type { get; set; } = null!;
        public UpdateObject? Object { get; set; }
    }

    public class UpdateObject
    {
        [JsonPropertyName("message")]
        public MessageItem? Message { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }
    }
}