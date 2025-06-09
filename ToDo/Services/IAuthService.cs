using ToDo.Models;
using ToDo.Models.Dto;

namespace ToDo.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDto request);
        Task<TokenResponseDto?> LoginAsync(UserDto request);
        Task<RegisterResultDto> RegisterWithResultAsync(UserDto request);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task LogoutAsync(Guid userId);
        Task<ChangePasswordResultDto> ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    }
}
