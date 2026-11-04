namespace Karina.Api.DTOs.User;

public class UserStatisticsResponse
{
    public long total_messages { get; set; }
    public long whatsapp_messages { get; set; }
    public long website_messages { get; set; }
    public long whatsapp_responses { get; set; }
    public long website_responses { get; set; }
}