namespace Karina.Api.Models;

public class Message
{
    public Guid id { get; set; }
    public Guid user_id { get; set; }
    public User user { get; set; } = null!;
    public Guid session_id { get; set; }
    public Session session { get; set; } = null!;
    public string header { get; set; } = "";
    public string content { get; set; } = "";
    public int source { get; set; }
    public Source? source_nav { get; set; } = null!;
    public DateTime sent_at { get; set; }
    public Response? response { get; set; }
}