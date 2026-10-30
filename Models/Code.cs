namespace Karina.Api.Models;

public class RandomCode
{   
    public Guid id { get; set; }
    public string code { get; set; } = string.Empty;

    public Guid user_id { get; set; }

    public string wa_id { get; set; } = "";

}