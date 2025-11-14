using System.Text.Json;
using MyWebService.Models.VkModels;

namespace MyWebService.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _vkToken;
        private readonly string _apiVersion;

        public ApiClientService(HttpClient httpClient, string vkToken, string apiVersion = "5.131")
        {
            _httpClient = httpClient;
            _vkToken = vkToken;
            _apiVersion = apiVersion;
        }

        // 📊 Загруженность аквапарка
        public async Task<string> GetParkLoadAsync()
        {
            try
            {
                var requestData = new { SiteID = "1" };
                var response = await _httpClient.PostAsJsonAsync("https://apigateway.nordciti.ru/v1/aqua/CurrentLoad", requestData);
                if (!response.IsSuccessStatusCode)
                    return "Не удалось получить данные о загруженности 😔";

                var data = await response.Content.ReadFromJsonAsync<ParkLoadResponse>();
                if (data == null)
                    return "Не удалось обработать ответ 😔";

                string loadStatus = data.Load switch
                {
                    < 30 => "Мало людей 🟢",
                    < 70 => "Средняя загруженность 🟡",
                    _ => "Много людей 🔴"
                };

                return $"📊 Загруженность аквапарка:\n\n" +
                       $"👥 В данный {data.Count} человек\n" +
                       $"📈 {data.Load}% ({loadStatus})\n\n" +
                       $"💡 Небольшой совет: Лучшее время для посещения, это утром - ведь там будет меньше людей!";
            }
            catch
            {
                return "Ошибка при получении загруженности 😔";
            }
        }

        // 🎟 Получение сеансов для даты
        public async Task<(string message, string keyboard)> GetSessionsForDateAsync(string date)
        {
            try
            {
                var sessionsUrl = $"https://apigateway.nordciti.ru/v1/aqua/getSessionsAqua?date={date}";
                var sessionsResponse = await _httpClient.GetAsync(sessionsUrl);

                if (!sessionsResponse.IsSuccessStatusCode)
                    return ($"⚠️ Ошибка при загрузке сеансов на {date}", KeyboardService.TicketsDateKeyboard());

                var sessionsJson = await sessionsResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Сырые данные сеансов: {sessionsJson}");

                var sessionsData = JsonSerializer.Deserialize<JsonElement>(sessionsJson);

                if (!sessionsData.TryGetProperty("result", out var sessionsArray) || sessionsArray.GetArrayLength() == 0)
                    return ($"😔 На {date} нет доступных сеансов.", KeyboardService.TicketsDateKeyboard());

                string text = $"🎟 *Доступные сеансы на {date}:*\n\n";
                var buttonsList = new List<object[]>();

                foreach (var s in sessionsArray.EnumerateArray())
                {
                    string timeStart = s.TryGetProperty("startTime", out var ts) ? ts.GetString() ?? "" : "";
                    string timeEnd = s.TryGetProperty("endTime", out var te) ? te.GetString() ?? "" : "";
                    int placesFree = s.TryGetProperty("availableCount", out var pf) ? pf.GetInt32() : 0;
                    int placesTotal = s.TryGetProperty("totalCount", out var pt) ? pt.GetInt32() : 0;
                    string sessionTime = s.TryGetProperty("sessionTime", out var st) ? st.GetString() ?? $"{timeStart}-{timeEnd}" : $"{timeStart}-{timeEnd}";

                    if (placesFree == 0) continue;

                    string availability = placesFree < 10 ? "🔴 Мало мест!" : "🟢 Есть места";
                    text += $"⏰ *{sessionTime}* | {availability}\n";
                    text += $"   Свободно: {placesFree}/{placesTotal} мест\n\n";

                    buttonsList.Add(new object[]
                    {
                        new { action = new { type = "text", label = $"⏰ {sessionTime}" }, color = "primary" }
                    });
                }

                if (buttonsList.Count == 0)
                    return ($"😔 На {date} нет свободных мест.", KeyboardService.TicketsDateKeyboard());

                buttonsList.Add(new object[]
                {
                    new { action = new { type = "text", label = "🔙 Назад" }, color = "negative" }
                });

                string keyboard = JsonSerializer.Serialize(new { one_time = true, buttons = buttonsList });
                return (text, keyboard);
            }
            catch (Exception ex)
            {
                return ($"Ошибка при получении сеансов 😔\n{ex.Message}", KeyboardService.TicketsDateKeyboard());
            }
        }

        // 🎟 Получение тарифов
        public async Task<(string message, string keyboard)> GetFormattedTariffsAsync(string date, string sessionTime, string category)
        {
            try
            {
                var tariffsUrl = $"https://apigateway.nordciti.ru/v1/aqua/getTariffsAqua?date={date}";
                var tariffsResponse = await _httpClient.GetAsync(tariffsUrl);

                if (!tariffsResponse.IsSuccessStatusCode)
                    return ($"⚠️ Ошибка при загрузке тарифов", KeyboardService.BackKeyboard());

                var tariffsJson = await tariffsResponse.Content.ReadAsStringAsync();
                var tariffsData = JsonSerializer.Deserialize<JsonElement>(tariffsJson);

                if (!tariffsData.TryGetProperty("result", out var tariffsArray) || tariffsArray.GetArrayLength() == 0)
                    return ($"⚠️ Не удалось получить тарифы", KeyboardService.BackKeyboard());

                string categoryTitle = category == "adult" ? "👤 ВЗРОСЛЫЕ БИЛЕТЫ" : "👶 ДЕТСКИЕ БИЛЕТЫ";
                string text = $"🎟 *{categoryTitle}*\n";
                text += $"⏰ Сеанс: {sessionTime}\n";
                text += $"📅 Дата: {date}\n\n";

                var filteredTariffs = new List<(string name, decimal price)>();
                var seenTariffs = new HashSet<string>();

                foreach (var t in tariffsArray.EnumerateArray())
                {
                    string name = t.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                    decimal price = t.TryGetProperty("Price", out var p) ? p.GetDecimal() : 0;

                    if (string.IsNullOrEmpty(name))
                        name = t.TryGetProperty("name", out var n2) ? n2.GetString() ?? "" : "";

                    if (price == 0)
                        price = t.TryGetProperty("price", out var p2) ? p2.GetDecimal() : 0;

                    string tariffKey = $"{name.ToLower()}_{price}";

                    if (seenTariffs.Contains(tariffKey)) continue;
                    seenTariffs.Add(tariffKey);

                    string nameLower = name.ToLower();
                    bool isAdult = nameLower.Contains("взрос") ||
                                  nameLower.Contains("adult") ||
                                  (nameLower.Contains("вип") && !nameLower.Contains("дет")) ||
                                  (nameLower.Contains("взр") && !nameLower.Contains("дет")) ||
                                  (price > 1000 && !nameLower.Contains("дет"));

                    bool isChild = nameLower.Contains("детск") ||
                                  nameLower.Contains("child") ||
                                  nameLower.Contains("kids") ||
                                  nameLower.Contains("дет") ||
                                  (price < 1000 && nameLower.Contains("билет") && !nameLower.Contains("взр"));

                    if ((category == "adult" && isAdult && !isChild) ||
                        (category == "child" && isChild && !isAdult))
                    {
                        filteredTariffs.Add((name, price));
                    }
                }

                if (filteredTariffs.Count == 0)
                {
                    text += "😔 Нет доступных билетов этой категории\n";
                    text += "💡 Попробуйте выбрать другую категорию";
                }
                else
                {
                    var groupedTariffs = filteredTariffs
                        .GroupBy(t => FormatTicketName(t.name))
                        .Select(g => g.First())
                        .OrderByDescending(t => t.price)
                        .ToList();

                    foreach (var (name, price) in groupedTariffs)
                    {
                        string emoji = price > 2000 ? "💎 VIP" : price > 1000 ? "⭐ Стандарт" : "🎫 Эконом";
                        string formattedName = FormatTicketName(name);
                        text += $"{emoji} *{formattedName}*: {price}₽\n";
                    }

                    text += $"\n💡 Примечания:\n";
                    text += $"• Детский билет - для детей от 4 до 12 лет\n";
                    text += $"• Дети до 4 лет - бесплатно (с взрослым)\n";
                    text += $"• VIP билеты включают дополнительные услуги";
                }

                text += $"\n🔗 *Купить онлайн:* yes35.ru";

                return (text, KeyboardService.TariffSelectionKeyboard(category));
            }
            catch (Exception ex)
            {
                return ($"Ошибка при получении тарифов 😔\n{ex.Message}", KeyboardService.BackKeyboard());
            }
        }

        private static string FormatTicketName(string name)
        {
            var formatted = name
                .Replace("Билет", "")
                .Replace("билет", "")
                .Replace("Вип", "VIP")
                .Replace("весь день", "Весь день")
                .Replace("взрослый", "")
                .Replace("детский", "")
                .Replace("вечерний", "Вечерний")
                .Replace("  ", " ")
                .Trim();

            if (formatted.StartsWith("VIP") || formatted.StartsWith("Вип"))
            {
                formatted = "VIP" + formatted.Substring(3).Trim();
            }

            return string.IsNullOrEmpty(formatted) ? "Стандартный" : formatted;
        }
    }
}