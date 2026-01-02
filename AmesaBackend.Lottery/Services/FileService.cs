using Amazon.S3;
using Amazon.S3.Model;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace AmesaBackend.Lottery.Services
{
    public class FileService : IFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly string _bucketName;

        public FileService(
            IAmazonS3 s3Client,
            IConfiguration configuration,
            ILogger<FileService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _bucketName = _configuration["Aws:S3:ImageBucketName"] ?? _configuration["Aws:S3:BucketName"] ?? "amesa-house-images-prod";
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

        public async Task<Dictionary<string, string>> UploadImageWithSizesAsync(
            Stream originalImageStream,
            string houseId,
            string imageId)
        {
            var uploadedUrls = new Dictionary<string, string>();
            var sizes = new Dictionary<string, (int width, int height, int quality)>
            {
                ["thumbnail"] = (300, 225, 80),
                ["mobile"] = (500, 375, 85),
                ["carousel"] = (800, 600, 85),
                ["detail"] = (1200, 900, 90),
                ["full"] = (1600, 1200, 90)
            };

            try
            {
                // Load original image into memory for reuse
                originalImageStream.Position = 0;
                using var originalMemoryStream = new MemoryStream();
                await originalImageStream.CopyToAsync(originalMemoryStream);
                originalMemoryStream.Position = 0;

                foreach (var size in sizes)
                {
                    try
                    {
                        // Reload image from memory stream for each size (ImageSharp images are not thread-safe and can't be cloned easily)
                        originalMemoryStream.Position = 0;
                        using var image = await Image.LoadAsync(originalMemoryStream);

                        // Resize maintaining aspect ratio
                        var resizeOptions = new ResizeOptions
                        {
                            Size = new Size(size.Value.width, size.Value.height),
                            Mode = ResizeMode.Max, // Maintain aspect ratio
                            Sampler = KnownResamplers.Lanczos3 // High quality resampling
                        };

                        image.Mutate(x => x.Resize(resizeOptions));

                        // Process WebP version
                        var webpStream = new MemoryStream();
                        var webpEncoder = new WebpEncoder
                        {
                            Quality = size.Value.quality
                        };
                        await image.SaveAsync(webpStream, webpEncoder);
                        webpStream.Position = 0;

                        var webpKey = $"houses/{houseId}/{imageId}/{size.Key}.webp";
                        var webpUrl = await UploadStreamToS3Async(webpStream, webpKey, "image/webp");
                        uploadedUrls[$"{size.Key}_webp"] = webpUrl;

                        // Reload image again for JPEG (since we already mutated it for WebP)
                        originalMemoryStream.Position = 0;
                        using var jpegImage = await Image.LoadAsync(originalMemoryStream);
                        jpegImage.Mutate(x => x.Resize(resizeOptions));

                        // Process JPEG fallback
                        var jpegStream = new MemoryStream();
                        var jpegEncoder = new JpegEncoder
                        {
                            Quality = size.Value.quality
                        };
                        await jpegImage.SaveAsync(jpegStream, jpegEncoder);
                        jpegStream.Position = 0;

                        var jpegKey = $"houses/{houseId}/{imageId}/{size.Key}.jpg";
                        var jpegUrl = await UploadStreamToS3Async(jpegStream, jpegKey, "image/jpeg");
                        uploadedUrls[$"{size.Key}_jpg"] = jpegUrl;

                        _logger.LogInformation("Uploaded {Size} images for house {HouseId}, image {ImageId}", size.Key, houseId, imageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process image for size {Size}", size.Key);
                        // Continue with next size
                    }
                }

                return uploadedUrls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image with sizes");
                throw;
            }
        }

        private async Task<string> UploadStreamToS3Async(Stream stream, string key, string contentType)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.PutObjectAsync(request);

                // Use CloudFront URL if configured, otherwise S3 URL
                var cloudFrontUrl = _configuration["Aws:CloudFront:ImageDistributionUrl"];
                if (!string.IsNullOrEmpty(cloudFrontUrl))
                {
                    return $"{cloudFrontUrl.TrimEnd('/')}/{key}";
                }

                var url = $"https://{_bucketName}.s3.amazonaws.com/{key}";
                _logger.LogInformation("File uploaded to S3: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading stream to S3");
                throw;
            }
        }
    }
}

