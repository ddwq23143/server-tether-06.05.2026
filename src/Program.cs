using System.Net;
using LessonVersion;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

var serverIp = EnvLoader.GetString("SERVER_IP", "127.0.0.1");
var serverPort = EnvLoader.GetInt("SERVER_PORT", 5000);

builder.WebHost.ConfigureKestrel(options =>
{
    try
    {
        options.Listen(IPAddress.Parse(serverIp), serverPort);
        options.Listen(IPAddress.Loopback, serverPort);
        Console.WriteLine($"✅ Сервер запущен на {serverIp}:{serverPort}");
        Console.WriteLine($"✅ Локальный доступ: http://localhost:{serverPort}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка настройки Kestrel: {ex.Message}");
        throw;
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();