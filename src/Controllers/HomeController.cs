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
            InitialCatalog = EnvLoader.GetString("DB_NAME", "TetherDB"),
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

    [HttpPost("signUp")]
    public IActionResult SignUp([FromBody] RegisterRequest user)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(user.Username) ||
            string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.Password))
        {
            return BadRequest(new { error = "Все поля обязательны" });
        }

        if (user.Password.Length < 6)
            return BadRequest(new { error = "Пароль должен быть не менее 6 символов" });

        if (!user.Email.Contains('@'))
            return BadRequest(new { error = "Некорректный email" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            const string checkQuery = @"
                SELECT COUNT(*) FROM Users 
                WHERE Username = @Username OR Email = @Email";

            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Username", user.Username.Trim());
            checkCmd.Parameters.AddWithValue("@Email", user.Email.Trim().ToLower());

            var exists = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (exists > 0)
                return Conflict(new { error = "Логин или email уже занят" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

            const string insertQuery = @"
                INSERT INTO Users (Username, Email, Password, CreatedAt) 
                VALUES (@Username, @Email, @Password, GETDATE())";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Username", user.Username.Trim());
            command.Parameters.AddWithValue("@Email", user.Email.Trim().ToLower());
            command.Parameters.AddWithValue("@Password", passwordHash);

            command.ExecuteNonQuery();

            Console.WriteLine($"Новый пользователь зарегистрирован: {user.Username}");
            return Ok(new { message = "Регистрация успешна" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignUp error: {ex.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
        }
    }

    [HttpPost("signIn")]
    public IActionResult SignIn([FromBody] LoginRequest user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) ||
            string.IsNullOrWhiteSpace(user.Password))
            return BadRequest(new { message = "Введите логин и пароль" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                SELECT Id, Username, Email, Password 
                FROM Users 
                WHERE Username = @Username";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username.Trim());

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return Unauthorized(new { message = "Неверный логин или пароль" });

            var storedHash = reader["Password"]?.ToString() ?? "";
            var userId = reader["Id"];
            var username = reader["Username"]?.ToString();

            reader.Close();

            bool isValid;
            if (storedHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(user.Password, storedHash);
            }
            else
            {
                isValid = storedHash == user.Password;
                if (isValid)
                {
                    var newHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    const string updateQuery = "UPDATE Users SET Password = @Hash WHERE Id = @Id";
                    using var updateCmd = new SqlCommand(updateQuery, connection);
                    updateCmd.Parameters.AddWithValue("@Hash", newHash);
                    updateCmd.Parameters.AddWithValue("@Id", userId);
                    updateCmd.ExecuteNonQuery();
                    Console.WriteLine($"Пароль пользователя {username} обновлён до BCrypt");
                }
            }

            if (!isValid)
                return Unauthorized(new { message = "Неверный логин или пароль" });

            const string updateLastLogin = @"
                UPDATE Users 
                SET LastLoginAt = GETDATE() 
                WHERE Id = @UserId";

            using var updCmd = new SqlCommand(updateLastLogin, connection);
            updCmd.Parameters.AddWithValue("@UserId", userId);
            updCmd.ExecuteNonQuery();
            
            Console.WriteLine($"Вход: {username}");
            return Ok(new
            {
                message = "Вы успешно вошли",
                userId = userId,
                username = username
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignIn error: {ex.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
        }
    }

    [HttpPost("sendMessage")]
    public IActionResult SendMessage([FromBody] MessageRequest msg)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(msg.Text))
            return BadRequest(new { error = "Текст сообщения обязателен" });

        if (msg.SenderId <= 0 || msg.ReceiverId <= 0)
            return BadRequest(new { error = "ID Отправилтеля или получателя должны быть больше 0" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string insertQuery = @"
                INSERT INTO Messages (SenderId, ReceiverId, Text, Status) 
                VALUES (@SenderId, @ReceiverId, @Text, 'Sent')";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@SenderId", msg.SenderId);
            command.Parameters.AddWithValue("@ReceiverId", msg.ReceiverId);
            command.Parameters.AddWithValue("@Text", msg.Text.Trim());

            command.ExecuteNonQuery();

            Console.WriteLine($"Сообщение от {msg.SenderId} доставлено {msg.ReceiverId}: {msg.Text}");

            return Ok(new { message = "Сообщение доставлено" });
        }
        catch (Exception e)
        {
            Console.WriteLine($@"Ошибка {e.Message}");
            return Ok(new { message = e.Message });
        }
    }

    [HttpGet("getMessages")]
    public IActionResult GetMessages([FromQuery] int senderId, [FromQuery] int receiverId)
    {
        if (senderId <= 0 || receiverId <= 0)
            return BadRequest(new { error = "Id не должно быть пустым" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                SELECT Id, SenderId, ReceiverId, Text, SentAt, Status 
                FROM Messages 
                WHERE (SenderId = @SenderId AND ReceiverId = @ReceiverId) 
                   OR (SenderId = @ReceiverId AND ReceiverId = @SenderId)
                ORDER BY SentAt ASC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SenderId", senderId);
            command.Parameters.AddWithValue("@ReceiverId", receiverId);

            var list = new List<MessageResponse>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MessageResponse
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    SenderId = Convert.ToInt32(reader["SenderId"]),
                    ReceiverId = Convert.ToInt32(reader["ReceiverId"]),
                    Text = reader["Text"].ToString(),
                    SendAt = Convert.ToDateTime(reader["SentAt"]),
                    Status = reader["Status"].ToString()
                });
            }

            return Ok(list);
        }
        catch (Exception e)
        {
            Console.WriteLine($@"Ошибка сообщений {e.Message}");
            return Ok(new { message = e.Message });
        }
    }

    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            const string query = "SELECT Id, Username FROM Users ORDER BY Username";
            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            var users = new List<object>();
            while (reader.Read())
            {
                users.Add(new { id = reader["Id"], username = reader["Username"] });
            }

            return Ok(users);
        }
        catch (Exception e)
        {
            return Ok(new { message = e.Message });
        }
    }

    [HttpPost("markDelivered")]
    public IActionResult MarkDelivered(int senderId, int receiverId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            const string query = @"
                UPDATE Messages
                SET Status = 'Delivered',
                    DeliveredAt = GETDATE()
                WHERE SenderId = @SenderId
                  AND ReceiverId = @ReceiverId
                  AND Status = 'Sent'";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SenderId", senderId);
            command.Parameters.AddWithValue("@ReceiverId", receiverId);
            command.ExecuteNonQuery();

            return Ok(new { message = "Сообщения доставлены" });
        }
        catch (Exception e)
        {
            return Ok(new { message = e.Message });
        }
    }

    [HttpPost("markRead")]
    public IActionResult MarkRead(int senderId, int receiverId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                UPDATE Messages
                SET Status = 'Read',
                    ReadAt = GETDATE()
                WHERE SenderId = @SenderId
                  AND ReceiverId = @ReceiverId
                  AND Status = 'Delivered'";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SenderId", senderId);
            command.Parameters.AddWithValue("@ReceiverId", receiverId);

            command.ExecuteNonQuery();

            return Ok(new { message = "Сообщения прочитаны" });
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpGet("searchUsers")]
    public IActionResult SearchUsers([FromQuery] int currentUserId, [FromQuery] string? query = null)
    {
        if (currentUserId <= 0)
            return BadRequest(new { error = "Укажите currentUserId" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string chatUsersQuery = @"
                SELECT DISTINCT 
                    CASE 
                        WHEN m.SenderId = @CurrentUserId THEN m.ReceiverId
                        ELSE m.SenderId
                    END as ChatUserId
                FROM Messages m
                WHERE m.SenderId = @CurrentUserId OR m.ReceiverId = @CurrentUserId";

            var chatUserIds = new HashSet<int>();
            using (var chatCmd = new SqlCommand(chatUsersQuery, connection))
            {
                chatCmd.Parameters.AddWithValue("@CurrentUserId", currentUserId);
                using var chatReader = chatCmd.ExecuteReader();
                while (chatReader.Read())
                {
                    chatUserIds.Add(Convert.ToInt32(chatReader["ChatUserId"]));
                }
            }

            string searchQuery = @"
                SELECT Id, Username, Email, CreatedAt
                FROM Users
                WHERE Id != @CurrentUserId";

            if (!string.IsNullOrWhiteSpace(query))
            {
                searchQuery += " AND Username LIKE @SearchTerm";
            }

            searchQuery += " ORDER BY Username";

            using var command = new SqlCommand(searchQuery, connection);
            command.Parameters.AddWithValue("@CurrentUserId", currentUserId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                command.Parameters.AddWithValue("@SearchTerm", "%" + query.Trim() + "%");
            }

            var users = new List<object>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var userId = Convert.ToInt32(reader["Id"]);
                users.Add(new
                {
                    id = userId,
                    username = reader["Username"].ToString(),
                    email = reader["Email"].ToString(),
                    createdAt = Convert.ToDateTime(reader["CreatedAt"]),
                    hasChat = chatUserIds.Contains(userId)
                });
            }

            return Ok(users);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ошибка поиска: {e.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {e.Message}" });
        }
    }

    [HttpGet("profile/{userId}")]
    public IActionResult GetProfile(int userId)
    {
        if (userId <= 0)
            return BadRequest(new { error = "Неверный Id пользователя" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                SELECT Id, Username, Email, CreatedAt, LastLoginAt 
                FROM Users 
                WHERE Id = @UserId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return NotFound(new { Error = "Пользователь не найден" });
            }

            var profile = new ProfileResponse
            {
                Id = Convert.ToInt32(reader["Id"]),
                Username = reader["Username"].ToString(),
                Email = reader["Email"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                LastLoginAt = reader["LastLoginAt"] != DBNull.Value
                    ? Convert.ToDateTime(reader["LastLoginAt"])
                    : null
            };

            return Ok(profile);
        }
        catch (Exception e)
        {
            Console.WriteLine($"GetProfile error: {e.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {e.Message}" });
        }
    }

    [HttpPut("updateUsername")]
    public IActionResult UpdateUsername([FromBody] UpdateUsernameRequest request)
    {
        if(!ModelState.IsValid)
            return BadRequest(ModelState);
        
        if(request.UserId <= 0)
            return BadRequest(new {error = "Неверный id пользователя"});

        if (string.IsNullOrWhiteSpace(request.NewUsername))
            return BadRequest(new { error = "Имя пользователя должно содержать минимум 3 символа" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string authQuery = @"
                SELECT Id, Username, Password 
                FROM Users 
                WHERE Id = @UserId";

            using var authCmd = new SqlCommand(authQuery, connection);
            authCmd.Parameters.AddWithValue("@UserId", request.UserId);

            using var reader = authCmd.ExecuteReader();
            if (!reader.Read())
                return NotFound(new { error = "Пользователь не найден" });

            var storedHash = reader["Password"]?.ToString() ?? "";
            reader.Close();

            bool isValid;
            if (storedHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(request.Password, storedHash);
            }
            else
            {
                isValid = storedHash == request.Password;
            }

            if (!isValid)
                return Unauthorized(new { error = "Неверный пароль" });

            const string checkQuery = @"
                SELECT COUNT(*) FROM Users 
                WHERE Username = @NewUsername AND Id != @UserId";

            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@NewUsername", request.NewUsername.Trim());
            checkCmd.Parameters.AddWithValue("@UserId", request.UserId);

            var exists = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (exists > 0)
                return Conflict(new { error = "Имя пользователя уже занято" });

            const string updateQuery = @"
                UPDATE Users 
                SET Username = @NewUsername 
                WHERE Id = @UserId";

            using var updateCmd = new SqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@NewUsername", request.NewUsername.Trim());
            updateCmd.Parameters.AddWithValue("@UserId", request.UserId);
            updateCmd.ExecuteNonQuery();

            Console.WriteLine($"Имя пользователя {request.UserId} изменено на {request.NewUsername}");

            return Ok(new
            {
                message = "Имя пользователя успешно изменено",
                newUsername = request.NewUsername.Trim(),
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"UpdateUsername error: {e.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {e.Message}" });
        }
    }

    [HttpPut("changePassword")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.UserId <= 0)
            return BadRequest(new { error = "Неверный ID пользователя" });

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = "Новый пароль не может быть пустым" });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { error = "Новый пароль должен содержать минимум 6 символов" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string authQuery = @"
                SELECT Id, Password 
                FROM Users 
                WHERE Id = @UserId";

            using var authCmd = new SqlCommand(authQuery, connection);
            authCmd.Parameters.AddWithValue("@UserId", request.UserId);

            using var reader = authCmd.ExecuteReader();
            if (!reader.Read())
                return NotFound(new { error = "Пользователь не найден" });

            var storedHash = reader["Password"]?.ToString() ?? "";
            reader.Close();

            bool isValid;
            if (storedHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, storedHash);
            }
            else
            {
                isValid = storedHash == request.CurrentPassword;
            }

            if (!isValid)
                return Unauthorized(new { error = "Неверный текущий пароль" });

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            const string updateQuery = @"
                UPDATE Users 
                SET Password = @NewPassword 
                WHERE Id = @UserId";

            using var updateCmd = new SqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@NewPassword", newHash);
            updateCmd.Parameters.AddWithValue("@UserId", request.UserId);

            updateCmd.ExecuteNonQuery();

            Console.WriteLine($"Пароль пользователя {request.UserId} изменён");

            return Ok(new { message = "Пароль успешно изменён" });
        }
        catch (Exception e)
        {
            Console.WriteLine($"ChangePassword error: {e.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {e.Message}" });
        }
    }
    
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        if (request.UserId <= 0)
            return BadRequest(new { error = "Неверный ID пользователя" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string updateQuery = @"
                UPDATE Users 
                SET LastLogoutAt = GETDATE() 
                WHERE Id = @UserId";

            using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.ExecuteNonQuery();

            Console.WriteLine($"Пользователь {request.UserId} вышел из системы");
            
            return Ok(new { message = "Вы успешно вышли из системы" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
        }
    }

    [HttpPost("updateLastLogin")]
    public IActionResult UpdateLastLogin([FromBody] int userId)
    {
        if (userId <= 0)
            return BadRequest(new { error = "Неверный ID пользователя" });

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                UPDATE Users 
                SET LastLoginAt = GETDATE() 
                WHERE Id = @UserId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.ExecuteNonQuery();

            return Ok(new { message = "Время входа обновлено" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateLastLogin error: {ex.Message}");
            return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
        }
    }
}