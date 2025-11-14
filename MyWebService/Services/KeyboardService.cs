using System.Text.Json;

namespace MyWebService.Services
{
    public class KeyboardService
    {
        // 🎛 Главное меню
        public static string MainMenuKeyboard() => JsonSerializer.Serialize(new
        {
            one_time = false,
            buttons = new[]
            {
                new[] {
                    new { action = new { type = "text", label = "ℹ️ Информация" }, color = "primary" },
                    new { action = new { type = "text", label = "🎟 Купить билеты" }, color = "positive" },
                    new { action = new { type = "text", label = "📊 Загруженность" }, color = "secondary" }
                }
            }
        });

        // ℹ️ Меню информации
        public static string InfoMenuKeyboard() => JsonSerializer.Serialize(new
        {
            one_time = false,
            buttons = new[]
            {
                new[] {
                    new { action = new { type = "text", label = "⏰ Время работы" }, color = "primary" },
                    new { action = new { type = "text", label = "📞 Контакты" }, color = "primary" }
                },
                new[] {
                    new { action = new { type = "text", label = "🔙 Назад" }, color = "negative" }
                }
            }
        });

        // 🎟 Меню выбора даты билетов
        public static string TicketsDateKeyboard()
        {
            var buttons = new object[3][];

            // Первый ряд: сегодня, завтра, послезавтра
            var row1 = new object[3];
            for (int i = 0; i < 3; i++)
            {
                string dateStr = DateTime.Now.AddDays(i).ToString("dd.MM.yyyy");
                row1[i] = new { action = new { type = "text", label = $"📅 {dateStr}" }, color = "primary" };
            }
            buttons[0] = row1;

            // Второй ряд: +3 дня, +4 дня
            var row2 = new object[2];
            for (int i = 3; i < 5; i++)
            {
                string dateStr = DateTime.Now.AddDays(i).ToString("dd.MM.yyyy");
                row2[i - 3] = new { action = new { type = "text", label = $"📅 {dateStr}" }, color = "primary" };
            }
            buttons[1] = row2;

            // Кнопка назад
            buttons[2] = new[] { new { action = new { type = "text", label = "🔙 Назад" }, color = "negative" } };

            return JsonSerializer.Serialize(new { one_time = true, buttons });
        }

        // 🎟 Меню выбора категории билетов (взрослые/детские)
        public static string TicketCategoryKeyboard() => JsonSerializer.Serialize(new
        {
            one_time = true,
            buttons = new[]
            {
                new[] {
                    new { action = new { type = "text", label = "👤 Взрослые билеты" }, color = "primary" },
                    new { action = new { type = "text", label = "👶 Детские билеты" }, color = "positive" }
                },
                new[] {
                    new { action = new { type = "text", label = "🔙 Назад" }, color = "negative" }
                }
            }
        });

        public static string BackKeyboard() => JsonSerializer.Serialize(new
        {
            one_time = true,
            buttons = new[] { new[] { new { action = new { type = "text", label = "🔙 Назад" }, color = "negative" } } }
        });

        public static string TariffSelectionKeyboard(string currentCategory) => JsonSerializer.Serialize(new
        {
            one_time = false,
            buttons = new object[][]
            {
                new object[]
                {
                    new { action = new { type = "open_link", link = "https://yes35.ru/aquapark/tickets", label = "🎟 Купить на сайте" } }
                },
                new object[]
                {
                    new { action = new { type = "text", label = "👤 Взрослые" }, color = currentCategory == "adult" ? "positive" : "primary" },
                    new { action = new { type = "text", label = "👶 Детские" }, color = currentCategory == "child" ? "positive" : "primary" }
                },
                new object[]
                {
                    new { action = new { type = "text", label = "🔙 К сеансам" }, color = "secondary" },
                    new { action = new { type = "text", label = "🔙 В начало" }, color = "negative" }
                }
            }
        });
    }
}