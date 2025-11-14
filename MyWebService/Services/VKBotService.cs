using System.Collections.Concurrent;
using System.Text.Json;
using MyWebService.Models.VkModels;

namespace MyWebService.Services
{
    public class VkBotService
    {
        private readonly string _token;
        private readonly ulong _groupId;
        private readonly string _apiVersion;
        private readonly ErrorLogger _errorLogger;
        private readonly ApiClientService _apiClient;
        private readonly HttpClient _httpClient;

        private static readonly ConcurrentDictionary<long, (string date, string session)> _userSelectedData = new();

        public VkBotService(string token, ulong groupId, string apiVersion, ErrorLogger errorLogger, ApiClientService apiClient, HttpClient httpClient)
        {
            _token = token;
            _groupId = groupId;
            _apiVersion = apiVersion;
            _errorLogger = errorLogger;
            _apiClient = apiClient;
            _httpClient = httpClient;
        }

        // 🔄 Обработка входящих веб-запросов (для веб-версии)
        public async Task<string> ProcessWebhookAsync(string updateJson)
        {
            try
            {
                Console.WriteLine($"📨 Processing webhook: {updateJson}");

                // Парсим JSON чтобы получить данные сообщения
                using var jsonDoc = JsonDocument.Parse(updateJson);
                var root = jsonDoc.RootElement;

                // Проверяем тип события
                if (!root.TryGetProperty("type", out var typeProp))
                    return "ok";

                string eventType = typeProp.GetString() ?? "";

                // Обрабатываем только новые сообщения
                if (eventType != "message_new")
                    return "ok";

                // Получаем данные сообщения
                if (!root.TryGetProperty("object", out var objectProp) ||
                    !objectProp.TryGetProperty("message", out var messageProp))
                    return "ok";

                // Извлекаем текст сообщения и ID пользователя
                string msg = messageProp.TryGetProperty("text", out var textProp)
                    ? textProp.GetString() ?? ""
                    : "";

                long userId = messageProp.TryGetProperty("from_id", out var fromIdProp)
                    ? fromIdProp.GetInt64()
                    : 0;

                if (userId == 0 || string.IsNullOrEmpty(msg))
                    return "ok";

                Console.WriteLine($"💬 Новое сообщение от {userId}: {msg}");

                string reply;
            string? keyboard = null;

            try
            {
                Console.WriteLine($"🔍 Обрабатываю сообщение: '{msg}'");

                // Улучшенная обработка категорий билетов
                if (IsTicketCategoryMessage(msg))
                {
                    Console.WriteLine($"🎫 Определена категория билетов: {msg}");
                    if (_userSelectedData.TryGetValue(userId, out var ticketData))
                    {
                        string selectedCategory = GetTicketCategoryFromMessage(msg);
                        Console.WriteLine($"📊 Получаю тарифы: {ticketData.date}, {ticketData.session}, {selectedCategory}");
                        var tariffResult = await _apiClient.GetFormattedTariffsAsync(ticketData.date, ticketData.session, selectedCategory);
                        reply = tariffResult.message;
                        keyboard = tariffResult.keyboard;
                        Console.WriteLine($"✅ Тарифы получены");
                    }
                    else
                    {
                        reply = "Сначала выберите дату и сеанс 📅";
                        keyboard = KeyboardService.TicketsDateKeyboard();
                        Console.WriteLine($"❌ Нет данных о выборе даты");
                    }
                }
                else
                {
                    Console.WriteLine($"🔍 Проверяю команду: '{msg}'");
                    switch (msg.ToLower())
                    {
                        case "/start":
                        case "начать":
                        case "🚀 начать":
                            Console.WriteLine($"✅ Обрабатываю команду: начало");
                            reply = "Добро пожаловать! Выберите пункт 👇";
                            keyboard = KeyboardService.MainMenuKeyboard();
                            break;

                        case "информация":
                        case "ℹ️ информация":
                            Console.WriteLine($"✅ Обрабатываю команду: информация");
                            reply = "Выберите интересующую информацию 👇";
                            keyboard = KeyboardService.InfoMenuKeyboard();
                            break;

                        case "время работы":
                        case "⏰ время работы":
                            Console.WriteLine($"✅ Обрабатываю команду: время работы");
                            reply = GetWorkingHours();
                            break;

                        case "контакты":
                        case "📞 контакты":
                            Console.WriteLine($"✅ Обрабатываю команду: контакты");
                            reply = GetContacts();
                            break;

                        case "🔙 назад":
                        case "назад":
                            reply = "Главное меню:";
                            keyboard = KeyboardService.MainMenuKeyboard();
                            _userSelectedData.TryRemove(userId, out _);
                            break;

                        case "🔙 к сеансам":
                            if (_userSelectedData.TryGetValue(userId, out var sessionData))
                            {
                                reply = $"Выберите сеанс для даты {sessionData.date}:";
                                var sessionResult = await _apiClient.GetSessionsForDateAsync(sessionData.date);
                                keyboard = sessionResult.keyboard;
                            }
                            else
                            {
                                reply = "Выберите дату для сеанса:";
                                keyboard = KeyboardService.TicketsDateKeyboard();
                            }
                            break;

                        case "🔙 в начало":
                            reply = "Главное меню:";
                            keyboard = KeyboardService.MainMenuKeyboard();
                            _userSelectedData.TryRemove(userId, out _);
                            break;

                        case "🎟 купить билеты":
                        case "билеты":
                            reply = "Выберите дату для сеанса:";
                            keyboard = KeyboardService.TicketsDateKeyboard();
                            break;

                        case "📊 загруженность":
                        case "загруженность":
                            reply = await _apiClient.GetParkLoadAsync();
                            break;

                        default:
                            if (msg.StartsWith("📅")) // выбор даты
                            {
                                string dateStr = msg.Replace("📅", "").Trim();
                                var sessionResult = await _apiClient.GetSessionsForDateAsync(dateStr);
                                reply = sessionResult.message;
                                keyboard = sessionResult.keyboard;

                                // Сохраняем дату для пользователя
                                _userSelectedData[userId] = (dateStr, "");
                            }
                            else if (msg.StartsWith("⏰")) // выбор сеанса
                            {
                                string sessionTime = msg.Replace("⏰", "").Trim();

                                if (!_userSelectedData.TryGetValue(userId, out var currentData))
                                {
                                    reply = "Сначала выберите дату 📅";
                                    keyboard = KeyboardService.TicketsDateKeyboard();
                                }
                                else
                                {
                                    _userSelectedData[userId] = (currentData.date, sessionTime);
                                    reply = $"🎟 *Сеанс: {sessionTime} ({currentData.date})*\n\nВыберите категорию билетов:";
                                    keyboard = KeyboardService.TicketCategoryKeyboard();
                                }
                            }
                            else
                            {
                                reply = "Я вас не понял, попробуйте еще раз 😅";
                            }
                            break;
                    }
                }

                await SendMessageAsync(userId, reply, keyboard);
            }
            catch (Exception ex)
            {
                await _errorLogger.LogErrorAsync(ex, "ERROR", userId,
                    command: msg,
                    additionalData: new
                    {
                        MessageText = msg,
                        UserHasSelectedData = _userSelectedData.ContainsKey(userId)
                    });

                // Отправляем сообщение об ошибке пользователю
                string errorMessage = "Произошла ошибка при обработке запроса. Мы уже работаем над этим! 🛠️";
                await SendMessageAsync(userId, errorMessage, null);
            }
                return "ok";
            }

            catch (Exception ex)
            {
                Console.WriteLine($"💥 Ошибка в ProcessWebhookAsync: {ex.Message}");
                return "ok";
            }
        }

        // 📤 Отправка сообщения
        private async Task SendMessageAsync(long userId, string message, string? keyboard)
        {
            try
            {
                Console.WriteLine($"📤 Отправляю сообщение пользователю {userId}");
                Console.WriteLine($"📝 Текст: {message}");

                string sendUrl = $"https://api.vk.com/method/messages.send?user_id={userId}" +
                                $"&random_id={Environment.TickCount}" +
                                $"&message={Uri.EscapeDataString(message)}" +
                                $"&access_token={_token}&v={_apiVersion}";

                if (keyboard != null)
                {
                    sendUrl += $"&keyboard={Uri.EscapeDataString(keyboard)}";
                    Console.WriteLine($"⌨️ Добавляю клавиатуру");
                }

                Console.WriteLine($"🌐 Выполняю запрос к VK API...");
                var response = await _httpClient.GetStringAsync(sendUrl);
                Console.WriteLine($"✅ Ответ VK: {response}");
                Console.WriteLine($"✅ Сообщение отправлено пользователю {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки сообщения пользователю {userId}: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
            }
        }

        // 🔍 Проверка, является ли сообщение выбором категории билетов
        private static bool IsTicketCategoryMessage(string message)
        {
            var lowerMsg = message.ToLower();
            return lowerMsg.Contains("взрос") ||
                   lowerMsg.Contains("детск") ||
                   lowerMsg.Contains("adult") ||
                   lowerMsg.Contains("child") ||
                   lowerMsg.Contains("kids") ||
                   lowerMsg == "👤" || lowerMsg == "👶" ||
                   lowerMsg == "взрослые" || lowerMsg == "детские" ||
                   lowerMsg == "взрослые билеты" || lowerMsg == "детские билеты" ||
                   lowerMsg == "👤 взрослые" || lowerMsg == "👶 детские" ||
                   lowerMsg == "👤 взрослые билеты" || lowerMsg == "👶 детские билеты";
        }

        // 🔍 Определение категории билетов из сообщения
        private static string GetTicketCategoryFromMessage(string message)
        {
            var lowerMsg = message.ToLower();
            return (lowerMsg.Contains("взрос") || lowerMsg.Contains("adult") || lowerMsg == "👤") ? "adult" : "child";
        }

        // 🎯 Приветственное сообщение
        private static string GetWelcomeMessage()
        {
            return "🌊 ДОБРО ПОЛОЖАЛОВАТЬ В ЦЕНТР YES!\n\n" +
"Я ваш персональный помощник для организации незабываемого отдыха! 🎯\n\n" +

"🎟 УМНАЯ ПОКУПКА БИЛЕТОВ\n" +
"- Выбор идеальной даты посещения\n" +
"- Подбор сеанса с учетом загруженности\n" +
"- Раздельный просмотр тарифов: взрослые/детские\n" +
"- Прозрачные цены без скрытых комиссий\n" +
"- Мгновенный переход к безопасной оплате онлайн\n\n" +

"📊 ОНЛАЙН-МОНИТОРИНГ ЗАГРУЖЕННОСТИ\n" +
"- Реальная картина посещаемости в реальном времени\n" +
"- Точное количество гостей в аквапарке\n" +
"- Процент заполненности для комфортного планирования\n" +
"- Рекомендации по лучшему времени для визита\n\n" +

"ℹ️ ПОЛНАЯ ИНФОРМАЦИЯ О ЦЕНТРЕ\n" +
"- Актуальное расписание всех зон и аттракционов\n" +
"- Контакты и способы связи с администрацией\n" +
"- Информация о временно закрытых объектах\n" +
"- Все необходимое для комфортного планирования\n\n" +

"🚀 Начните прямо сейчас!\n" +
"Выберите раздел в меню ниже, и я помогу организовать ваш идеальный визит! ✨\n\n" +
"💫 Центр YES - где рождаются воспоминания!";
        }

        // ⏰ Время работы
        private static string GetWorkingHours()
        {
            return "🏢 Режим работы точек Центра YES:\n\n" +

                   "🌊 Аквапарк\n" +
                   "⏰ 10:00 - 21:00 │ 📅 Ежедневно\n" +
                   "💧 Бассейны, горки, сауны\n\n" +

                   "🍽️ Ресторан\n" +
                   "⏰ 10:00 - 21:00 │ 📅 Ежедневно\n" +
                   "🍕 Кухня европейская и азиатская\n\n" +

                   "🎮 Игровой центр\n" +
                   "⏰ 10:00 - 18:00 │ 📅 Ежедневно\n" +
                   "🎯 Автоматы и симуляторы\n\n" +

                   "🦖 Динопарк\n" +
                   "⏰ 10:00 - 18:00 │ 📅 Ежедневно\n" +
                   "🦕 Интерактивные экспонаты\n\n" +

                   "🏨 Гостиница\n" +
                   "⏰ Круглосуточно │ 📅 Ежедневно\n" +
                   "🛏️ Номера различных категорий\n\n" +

                   "🔴 Временно не работают:\n" +
                   "• 🧗‍ Веревочный парк\n" +
                   "• 🧗‍ Скалодром\n" +
                   "• 🎡 Парк аттракционов\n" +
                   "• 🍔 MasterBurger\n\n" +

                   "📞 *Уточнить информацию:* (8172) 33-06-06";
        }

        // 📞 Контакты
        private static string GetContacts()
        {
            return "📞 Контакты Центра YES\n\n" +

                    "📱 Телефон для связи:\n" +
                    "• Основной: (8172) 33-06-06\n" +
                    "• Ресторан: 8-800-200-67-71\n\n" +

                    "📧 Электронная почта:\n" +
                    "yes@yes35.ru\n\n" +

                    "🌐 Мы в соцсетях:\n" +
                    "ВКонтакте: vk.com/yes35\n" +
                    "Telegram: t.me/CentreYES35\n" +
                    "WhatsApp: ссылка в профиле\n\n" +

                    "⏰ Часы работы call-центра:\n" +
                    "🕙 09:00 - 22:00";
        }
    }
}