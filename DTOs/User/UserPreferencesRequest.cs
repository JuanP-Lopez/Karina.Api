namespace Karina.Api.DTOs.User;

public class UserPreferencesRequest
{
    public string nickname { get; set; } = string.Empty;
    public string tone { get; set; } = string.Empty;
    public string language { get; set; } = string.Empty;
}