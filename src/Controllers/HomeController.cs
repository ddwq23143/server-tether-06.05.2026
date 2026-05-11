using Microsoft.AspNetCore.Mvc;
using LessonVersion.Models;
using Microsoft.Data.SqlClient;

namespace LessonVersion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("savemessage")]
    public IActionResult SaveMessage([FromForm] Model model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        SqlConnectionStringBuilder builder = new()
        {
            DataSource = ".",
            InitialCatalog = "Northwind",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };
    
        try
        {
            using SqlConnection connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            
            string query = @"INSERT INTO Messages (Username, Message, CreatedAt) 
                         VALUES (@Username, @Message, GETDATE())";
        
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", model.Username);
            command.Parameters.AddWithValue("@Message", model.Message);
        
            int rowsAffected = command.ExecuteNonQuery();
        
            return Ok(new { message = $"Сохранено {rowsAffected} записей" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при сохранении в БД: {ex.Message}");
        }
    }
}
