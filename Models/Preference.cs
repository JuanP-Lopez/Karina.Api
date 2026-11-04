namespace Karina.Api.Models;

public class Preference
{
    public Guid id { get; set; }

    public Guid user_id { get; set; }

    public string nickname { get; set; } = string.Empty;

    public string tone { get; set; } = string.Empty;

    public string language { get; set; } = string.Empty;
}