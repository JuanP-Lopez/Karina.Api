namespace Karina.Api.DTOs.Automations;

public class AutomationDto
{
    public Guid AutomationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}