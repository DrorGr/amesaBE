namespace AmesaBackend.Services
{
    public interface IContentService
    {
        Task<object> GetContentAsync();
        Task<object> GetContentBySlugAsync(string slug);
    }
}
