namespace Karina.Api.Models.KarinaAgent;

public class KarinaRequest
{
    public string app_name { get; set; } = "";

    public string user_id { get; set; } = "";

    public string session_id { get; set; } = "";

    public List<new_message>? new_message { get; set; } 
}

public class new_message
{
    public string role { get; set; } = "";

    public List<parts>? parts { get; set; }
}

public class parts
{
    public string text { get; set; } = "";
}