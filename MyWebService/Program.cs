using MyWebService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Добавляем HttpClient
builder.Services.AddHttpClient();

// Регистрируем наши сервисы
builder.Services.AddSingleton<ErrorLogger>();
builder.Services.AddScoped<ApiClientService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var configuration = provider.GetRequiredService<IConfiguration>();

    // Получаем настройки из конфигурации
    var vkToken = configuration["VkBot:Token"] ?? "your_default_token_here";
    var apiVersion = configuration["VkBot:ApiVersion"] ?? "5.131";

    return new ApiClientService(httpClient, vkToken, apiVersion);
});
builder.Services.AddScoped<VkBotService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var errorLogger = provider.GetRequiredService<ErrorLogger>();
    var apiClient = provider.GetRequiredService<ApiClientService>();
    var httpClient = provider.GetRequiredService<HttpClient>();

    var vkToken = configuration["VkBot:Token"] ?? "your_default_token_here";
    var groupId = ulong.Parse(configuration["VkBot:GroupId"] ?? "233846417");
    var apiVersion = configuration["VkBot:ApiVersion"] ?? "5.131";

    return new VkBotService(vkToken, groupId, apiVersion, errorLogger, apiClient, httpClient);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Для начала уберем Swagger чтобы проект запустился
    // Позже добавим когда установим пакеты
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();