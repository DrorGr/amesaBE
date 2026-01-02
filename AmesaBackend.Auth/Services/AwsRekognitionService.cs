using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Auth.Services
{
    public class AwsRekognitionService : IAwsRekognitionService
    {
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly ILogger<AwsRekognitionService> _logger;

        public AwsRekognitionService(IAmazonRekognition rekognitionClient, ILogger<AwsRekognitionService> logger)
        {
            _rekognitionClient = rekognitionClient;
            _logger = logger;
        }

        /// <summary>
        /// Detect faces in an image (for liveness detection)
        /// </summary>
        public async Task<DetectFacesResponse> DetectFacesAsync(byte[] imageBytes)
        {
            try
            {
                var request = new DetectFacesRequest
                {
                    Image = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(imageBytes)
                    },
                    Attributes = new List<string> { "ALL" }
                };

                var response = await _rekognitionClient.DetectFacesAsync(request);
                _logger.LogInformation("Detected {FaceCount} face(s) in image", response.FaceDetails?.Count ?? 0);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting faces in image");
                throw;
            }
        }

        /// <summary>
        /// Compare faces between two images (ID photo vs selfie)
        /// </summary>
        public async Task<CompareFacesResponse> CompareFacesAsync(byte[] sourceImageBytes, byte[] targetImageBytes)
        {
            try
            {
                var request = new CompareFacesRequest
                {
                    SourceImage = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(sourceImageBytes)
                    },
                    TargetImage = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(targetImageBytes)
                    },
                    SimilarityThreshold = 70.0f // Minimum similarity threshold (0-100)
                };

                var response = await _rekognitionClient.CompareFacesAsync(request);
                _logger.LogInformation("Face comparison completed. Matches: {MatchCount}, Unmatched: {UnmatchCount}",
                    response.FaceMatches?.Count ?? 0, response.UnmatchedFaces?.Count ?? 0);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing faces");
                throw;
            }
        }

        /// <summary>
        /// Extract text from ID document (OCR)
        /// </summary>
        public async Task<DetectTextResponse> DetectTextAsync(byte[] imageBytes)
        {
            try
            {
                var request = new DetectTextRequest
                {
                    Image = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(imageBytes)
                    }
                };

                var response = await _rekognitionClient.DetectTextAsync(request);
                _logger.LogInformation("Detected {TextCount} text detection(s) in image", response.TextDetections?.Count ?? 0);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting text in image");
                throw;
            }
        }
    }
}

