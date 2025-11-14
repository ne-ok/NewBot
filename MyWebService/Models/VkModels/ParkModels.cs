using System.Text.Json.Serialization;

namespace MyWebService.Models.VkModels
{
    public class ParkLoadResponse
    {
        public int Count { get; set; }
        public int Load { get; set; }
    }

    public class SessionResponse
    {
        public SessionItem[] Data { get; set; } = Array.Empty<SessionItem>();
    }

    public class SessionItem
    {
        [JsonPropertyName("TimeStart")]
        public string TimeStart { get; set; } = "";

        [JsonPropertyName("TimeEnd")]
        public string TimeEnd { get; set; } = "";

        [JsonPropertyName("PlacesFree")]
        public int PlacesFree { get; set; }

        [JsonPropertyName("PlacesTotal")]
        public int PlacesTotal { get; set; }

        [JsonPropertyName("availableCount")]
        public int AvailableCount { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("sessionTime")]
        public string SessionTime { get; set; } = "";

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = "";

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = "";
    }

    public class TariffResponse
    {
        public TariffItem[] Data { get; set; } = Array.Empty<TariffItem>();
    }

    public class TariffItem
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("Price")]
        public decimal Price { get; set; }

        [JsonPropertyName("name")]
        public string NameAlt { get; set; } = "";

        [JsonPropertyName("price")]
        public decimal PriceAlt { get; set; }
    }
}