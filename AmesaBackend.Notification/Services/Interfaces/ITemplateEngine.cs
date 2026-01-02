namespace AmesaBackend.Notification.Services.Interfaces
{
    public interface ITemplateEngine
    {
        Task<string> RenderTemplateAsync(string templateName, string language, string channel, Dictionary<string, object> variables);
    }
}












