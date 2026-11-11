namespace Karina.Api.Models;

public class Automation
{
    public Guid automation_id { get; set; }

    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    public ICollection<UserAutomation> users_automations { get; set; }
        = new List<UserAutomation>();
}