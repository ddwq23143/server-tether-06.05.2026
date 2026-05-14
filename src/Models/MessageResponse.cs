namespace LessonVersion.Models;

public class MessageResponse
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Text { get; set; }
    public DateTime SendAt { get; set; }
    public string Status { get; set; }
}