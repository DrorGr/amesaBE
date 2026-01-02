namespace AmesaBackend.Lottery.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteFileAsync(string filePath);
        Task<Dictionary<string, string>> UploadImageWithSizesAsync(Stream originalImageStream, string houseId, string imageId);
    }
}

