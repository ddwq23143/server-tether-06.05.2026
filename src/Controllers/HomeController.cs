using Microsoft.AspNetCore.Mvc;
using LessonVersion.Models;
using Microsoft.Data.SqlClient;

namespace LessonVersion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{

    SqlConnectionStringBuilder builder = new()
    {
        DataSource = ".", 
        InitialCatalog = "Northwind",
        IntegratedSecurity = true, 
        TrustServerCertificate = true,
        ConnectTimeout = 30
    };
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("savemessage")]
    public IActionResult SaveMessage([FromForm] SaveMessageRequest saveMessageRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
    
        try
        {
            using SqlConnection connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            
            string query = @"INSERT INTO Messages (Username, Message, CreatedAt) 
                         VALUES (@Username, @Message, GETDATE())";
        
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", saveMessageRequest.Username);
            command.Parameters.AddWithValue("@Message", saveMessageRequest.Message);
        
            int rowsAffected = command.ExecuteNonQuery();
        
            return Ok(new { message = $"Сохранено {rowsAffected} записей" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при сохранении в БД: {ex.Message}");
        }
    }

    [HttpPost("deletemessage")]
    public IActionResult DeleteMessage([FromBody] DeleteMessageRequest message)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
    
        try
        {
            using SqlConnection connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            
            string query = @"DELETE FROM Messages 
                         WHERE Id  = @Id";
        
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", message.Id);
            
            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                return Ok(new { message = "Собщение не найдено" });
            }
            
            return Ok(new { message = $"Удалено {rowsAffected} записей" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при сохранении в БД: {ex.Message}");
        }
    }
    
    [HttpGet("getMessages")]
    public IActionResult GetMessages()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            using SqlConnection connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            
            string query = @"SELECT Id, Username, Message, CreatedAt 
                           FROM Messages 
                           ORDER BY Id DESC";

            
            using SqlCommand command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();
            
            var messages = new List<object>();
            
            while (reader.Read())
            {
                messages.Add(new
                {
                    Id = reader["Id"],
                    Username = reader["Username"].ToString(),
                    Message = reader["Message"].ToString(),
                    CreatedAt = reader["CreatedAt"]
                });
            }
            
            return Ok(messages);
        }
        catch (Exception e)
        {
            return Ok(new { message = $"Ошибка сохранения {e.Message}" });
        }
    }
}
