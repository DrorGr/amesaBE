namespace AmesaBackend.Content.DTOs.Responses;

public class LanguageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public string? FlagUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}
