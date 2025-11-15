using Amazon.S3;
using Amazon.S3.Model;
using AmesaBackend.Lottery.Services;

namespace AmesaBackend.Lottery.Services
{
    public class FileService : IFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(
            IAmazonS3 s3Client,
            IConfiguration configuration,
            ILogger<FileService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var bucketName = _configuration["Aws:S3:BucketName"] ?? "amesa-lottery-uploads";
                var key = $"houses/{Guid.NewGuid()}_{fileName}";

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.PutObjectAsync(request);

                var url = $"https://{bucketName}.s3.amazonaws.com/{key}";
                _logger.LogInformation("File uploaded to S3: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to S3");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var bucketName = _configuration["Aws:S3:BucketName"] ?? "amesa-lottery-uploads";
                var key = filePath.Replace($"https://{bucketName}.s3.amazonaws.com/", "");

                await _s3Client.DeleteObjectAsync(bucketName, key);
                _logger.LogInformation("File deleted from S3: {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from S3");
                return false;
            }
        }
    }
}

