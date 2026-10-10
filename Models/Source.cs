namespace Karina.Api.Models;

public class Source
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string description { get; set; } = "";
    public ICollection<Message> messages { get; set; } = new List<Message>();
    public ICollection<Response> responses { get; set; } = new List<Response>();
}