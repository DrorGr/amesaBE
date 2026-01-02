namespace AmesaBackend.Content.DTOs.Responses;

public class TranslationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
}
