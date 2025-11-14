using System.Text.Json.Serialization;

namespace MyWebService.Models.VkModels
{
    public class MessageItem
    {
        public string Text { get; set; } = "";

        [JsonPropertyName("from_id")]
        public long FromId { get; set; }
    }
}