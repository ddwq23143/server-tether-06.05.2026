using System.Net;
using LessonVersion;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

var serverIp   = EnvLoader.GetString("SERVER_IP",   "127.0.0.1");
var serverPort = EnvLoader.GetInt("SERVER_PORT", 5000);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse(serverIp), serverPort);
    options.Listen(IPAddress.Loopback, serverPort);
    Console.WriteLine($"✅ Сервер: {serverIp}:{serverPort}");
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();

// ✅ Лимит загрузки файлов — 100 MB
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

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();