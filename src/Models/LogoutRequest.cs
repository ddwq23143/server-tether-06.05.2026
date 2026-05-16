namespace LessonVersion.Models;

public class LogoutRequest
{
    public int UserId { get; set; }
    public string SessionToken { get; set; }
}