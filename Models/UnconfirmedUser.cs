namespace Karina.Api.Models;

public class UnconfirmedUser
{
    public Guid user_id { get; set; }
    public string name { get; set; } = "";
    public string wa_id { get; set; } = "";
    public DateTime registered_at { get; set; }
    // public ICollection<Session> sessions { get; set; } = new List<Session>();
    // public ICollection<Message> messages { get; set; } = new List<Message>();
}