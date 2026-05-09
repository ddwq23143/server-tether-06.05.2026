using Microsoft.AspNetCore.Mvc;
using LessonVersion.Models;

namespace LessonVersion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : Controller
{
    [HttpPost("savemessage")]
    public IActionResult SaveMessage([FromForm] Model model)
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("Не прошли проверку");
        }

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "messages.txt");
        string record = $"\nИмя: {model.Username} | Сообщение: {model.Message}";
        System.IO.File.AppendAllText(filePath , record);
        
        return Ok();
    }
}