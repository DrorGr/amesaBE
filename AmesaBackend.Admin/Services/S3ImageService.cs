using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Admin.Services
{
    public interface IS3ImageService
    {
        Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, Guid houseId);
        Task<bool> DeleteImageAsync(string imageUrl);
        Task<List<string>> GetHouseImagesAsync(Guid houseId);
        Task<string> GeneratePresignedUrlAsync(string imageUrl, int expirationMinutes = 60);
    }

    public class S3ImageService : IS3ImageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3ImageService> _logger;
        private readonly string _bucketName;
        private readonly string _region;

        public S3ImageService(
            IAmazonS3 s3Client,
            IConfiguration configuration,
            ILogger<S3ImageService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _bucketName = _configuration["AWS:S3:ImageBucket"] ?? "amesa-house-images";
            _region = _configuration["AWS:Region"] ?? "eu-north-1";
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, Guid houseId)
        {
            try
            {
                // Generate unique file name: houses/{houseId}/{timestamp}-{originalFileName}
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var sanitizedFileName = SanitizeFileName(fileName);
                var key = $"houses/{houseId}/{timestamp}-{sanitizedFileName}";

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = imageStream,
                    ContentType = contentType,
                    // Note: Public ACL removed due to S3 Block Public Access settings
                    // Images will be accessed via presigned URLs or bucket policy
                    Metadata =
                    {
                        ["house-id"] = houseId.ToString(),
                        ["uploaded-at"] = DateTime.UtcNow.ToString("O")
                    }
                };

                var response = await _s3Client.PutObjectAsync(request);

                // Generate public URL
                var imageUrl = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";

                _logger.LogInformation("Image uploaded successfully: {ImageUrl} for house {HouseId}", imageUrl, houseId);

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to S3: {FileName} for house {HouseId}", fileName, houseId);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                // Extract key from URL
                var key = ExtractKeyFromUrl(imageUrl);
                if (string.IsNullOrEmpty(key))
                {
                    _logger.LogWarning("Invalid S3 URL format: {ImageUrl}", imageUrl);
                    return false;
                }

                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request);

                _logger.LogInformation("Image deleted successfully: {ImageUrl}", imageUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from S3: {ImageUrl}", imageUrl);
                return false;
            }
        }

        public async Task<List<string>> GetHouseImagesAsync(Guid houseId)
        {
            try
            {
                var prefix = $"houses/{houseId}/";
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = prefix
                };

                var response = await _s3Client.ListObjectsV2Async(request);
                var imageUrls = response.S3Objects
                    .Select(obj => $"https://{_bucketName}.s3.{_region}.amazonaws.com/{obj.Key}")
                    .ToList();

                return imageUrls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing images for house {HouseId}", houseId);
                return new List<string>();
            }
        }

        public async Task<string> GeneratePresignedUrlAsync(string imageUrl, int expirationMinutes = 60)
        {
            try
            {
                var key = ExtractKeyFromUrl(imageUrl);
                if (string.IsNullOrEmpty(key))
                {
                    return imageUrl; // Return original URL if key extraction fails
                }

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                var presignedUrl = await _s3Client.GetPreSignedURLAsync(request);
                return presignedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL for {ImageUrl}", imageUrl);
                return imageUrl; // Return original URL on error
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove path separators and sanitize
            var sanitized = Path.GetFileName(fileName);
            sanitized = string.Join("_", sanitized.Split(Path.GetInvalidFileNameChars()));
            return sanitized;
        }

        private string? ExtractKeyFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                // Extract key from path (remove leading /)
                var key = uri.AbsolutePath.TrimStart('/');
                return key;
            }
            catch
            {
                return null;
            }
        }
    }
}

