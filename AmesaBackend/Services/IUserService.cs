using AmesaBackend.DTOs;

namespace AmesaBackend.Services
{
    public interface IUserService
    {
        Task<UserDto> GetUserProfileAsync(Guid userId);
        Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
        Task<List<UserAddressDto>> GetUserAddressesAsync(Guid userId);
        Task<UserAddressDto> AddUserAddressAsync(Guid userId, CreateAddressRequest request);
        Task<UserAddressDto> UpdateUserAddressAsync(Guid userId, Guid addressId, CreateAddressRequest request);
        Task DeleteUserAddressAsync(Guid userId, Guid addressId);
        Task<List<UserPhoneDto>> GetUserPhonesAsync(Guid userId);
        Task<UserPhoneDto> AddUserPhoneAsync(Guid userId, AddPhoneRequest request);
        Task DeleteUserPhoneAsync(Guid userId, Guid phoneId);
        Task<IdentityDocumentDto> UploadIdentityDocumentAsync(Guid userId, UploadIdentityDocumentRequest request);
        Task<IdentityDocumentDto> GetIdentityDocumentAsync(Guid userId);
    }
}
