namespace Karina.Api.Models;

public class UserAutomation
{
    public Guid id { get; set; }

    public Guid user_id { get; set; }

    public Guid automation_id { get; set; }

    public User User { get; set; } = null!;

    public Automation Automation { get; set; } = null!;
}