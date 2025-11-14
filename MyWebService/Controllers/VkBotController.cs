using Microsoft.AspNetCore.Mvc;
using MyWebService.Services;
using System.Text.Json;

namespace MyWebService.Controllers
{
    [ApiController]
    [Route("api/vkbot")]
    public class VkBotController : ControllerBase
    {
        private readonly VkBotService _botService;
        private readonly ILogger<VkBotController> _logger;

        public VkBotController(VkBotService botService, ILogger<VkBotController> logger)
        {
            _botService = botService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ProcessWebhook([FromBody] object update)
        {
            try
            {
                var updateJson = update.ToString();
                _logger.LogInformation($"📨 Received webhook: {updateJson}");

                // Парсим JSON чтобы проверить тип запроса
                using var jsonDoc = JsonDocument.Parse(updateJson!);
                var root = jsonDoc.RootElement;

                // Проверяем тип запроса
                if (root.TryGetProperty("type", out var typeProp))
                {
                    string requestType = typeProp.GetString() ?? "";

                    // 🔐 Обработка подтверждения от VK
                    if (requestType == "confirmation")
                    {
                        _logger.LogInformation("✅ Confirmation request - returning: 0cffe0f1");
                        return Ok("0cffe0f1"); // ВАША СТРОКА ПОДТВЕРЖДЕНИЯ
                    }

                    // 💬 Обработка входящих сообщений
                    if (requestType == "message_new")
                    {
                        _logger.LogInformation("💬 New message received");
                        var result = await _botService.ProcessWebhookAsync(updateJson!);
                        return Ok("ok");
                    }
                }

                // Для всех остальных типов возвращаем "ok"
                return Ok("ok");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Webhook processing error");
                return Ok("ok"); // Всегда возвращаем "ok" чтобы VK не повторял запрос
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { status = "VK Bot Service is running", timestamp = DateTime.UtcNow });
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Для тестирования - отправка сообщения пользователю
                return Ok(new { success = true, message = "Message sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class SendMessageRequest
    {
        public long UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}