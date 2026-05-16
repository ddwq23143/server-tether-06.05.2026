using System.Net;
using LessonVersion;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

var serverPort = EnvLoader.GetInt("SERVER_PORT", 5000);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, serverPort);
    Console.WriteLine($"✅ Сервер запущен на http://0.0.0.0:{serverPort}");
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Assets")),
    RequestPath = "/Assets"
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

Console.WriteLine($"🌐 Публичный IP: {await GetPublicIpAsync()}");
Console.WriteLine($"📁 Корневая папка: {builder.Environment.ContentRootPath}");
Console.WriteLine($"📁 Папка Assets существует: {Directory.Exists(Path.Combine(builder.Environment.ContentRootPath, "Assets"))}");
Console.WriteLine($"🖼️ Файл иконки существует: {File.Exists(Path.Combine(builder.Environment.ContentRootPath, "Assets", "Icons", "ico.jpg"))}");

app.Run();

async Task<string> GetPublicIpAsync()
{
    try
    {
        using var client = new HttpClient();
        return await client.GetStringAsync("https://api.ipify.org");
    }
    catch
    {
        return "Не удалось определить";
    }
}