namespace AmesaBackend.Content.DTOs.Responses;

public class TranslationsResponseDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public Dictionary<string, string> Translations { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
