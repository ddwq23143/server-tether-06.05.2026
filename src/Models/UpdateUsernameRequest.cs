namespace LessonVersion.Models;

public class UpdateUsernameRequest
{
    public int UserId { get; set; }
    public string NewUsername { get; set; }
    public string Password { get; set; }
}