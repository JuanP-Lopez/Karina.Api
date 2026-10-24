namespace Karina.Api.Models;

public class Session
{
    public Guid session_id { get; set; }
    public Guid user_id { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? ended_at { get; set; }
    public DateTime? last_activity { get; set; }
    public User? user { get; set; } = null!;
    public ICollection<Message> messages { get; set; } = new List<Message>();
    public ICollection<Response> responses { get; set; } = new List<Response>();
}