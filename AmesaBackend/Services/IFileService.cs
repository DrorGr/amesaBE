namespace AmesaBackend.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteFileAsync(string filePath);
    }
}
