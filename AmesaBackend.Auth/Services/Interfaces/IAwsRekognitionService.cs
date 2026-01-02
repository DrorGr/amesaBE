using Amazon.Rekognition.Model;

namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface IAwsRekognitionService
    {
        Task<DetectFacesResponse> DetectFacesAsync(byte[] imageBytes);
        Task<CompareFacesResponse> CompareFacesAsync(byte[] sourceImageBytes, byte[] targetImageBytes);
        Task<DetectTextResponse> DetectTextAsync(byte[] imageBytes);
    }
}






















