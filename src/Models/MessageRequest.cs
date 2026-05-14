namespace LessonVersion.Models;

public class MessageRequest
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Text { get; set; }
}