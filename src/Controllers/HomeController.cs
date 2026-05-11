using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LessonVersion.Models;

namespace LessonVersion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly string _connectionString;

    public HomeController()
    {
        var connectionBuilder = new SqlConnectionStringBuilder
        {
            DataSource = EnvLoader.GetString("DB_SOURCE", "."),
            InitialCatalog = EnvLoader.GetString("DB_NAME", "Northwind"),
            TrustServerCertificate = EnvLoader.GetBool("DB_TRUST_CERTIFICATE", true),
            ConnectTimeout = EnvLoader.GetInt("DB_TIMEOUT", 30)
        };
        var useIntegratedSecurity = EnvLoader.GetBool("DB_INTEGRATED_SECURITY", true);
        if (useIntegratedSecurity)
        {
            connectionBuilder.IntegratedSecurity = true;
        }
        else
        {
            connectionBuilder.IntegratedSecurity = false;
            connectionBuilder.UserID = EnvLoader.GetString("DB_USER", "");
            connectionBuilder.Password = EnvLoader.GetString("DB_PASSWORD", "");
        }
        _connectionString = connectionBuilder.ConnectionString;
    }

    [HttpPost("savemessage")]
    public IActionResult SaveMessage([FromForm] SaveMessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            const string query = @"INSERT INTO Messages (Username, Message, CreatedAt) 
                                  VALUES (@Username, @Message, GETDATE())";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", request.Username);
            command.Parameters.AddWithValue("@Message", request.Message);
            
            var rowsAffected = command.ExecuteNonQuery();
            return Ok(new { message = $"✅ Сохранено {rowsAffected} записей" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"❌ Ошибка БД: {ex.Message}" });
        }
    }

    [HttpPost("deletemessage")]
    public IActionResult DeleteMessage([FromBody] DeleteMessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            const string query = "DELETE FROM Messages WHERE Id = @Id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", request.Id);
            
            var rowsAffected = command.ExecuteNonQuery();
            
            if (rowsAffected == 0)
                return Ok(new { message = "⚠️ Сообщение не найдено" });
            
            return Ok(new { message = $"✅ Удалено {rowsAffected} записей" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"❌ Ошибка БД: {ex.Message}" });
        }
    }
    
    [HttpGet("getMessages")]
    public IActionResult GetMessages()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            const string query = @"SELECT Id, Username, Message, CreatedAt 
                                  FROM Messages ORDER BY Id DESC";
            
            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            
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
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"❌ Ошибка: {ex.Message}" });
        }
    }
}