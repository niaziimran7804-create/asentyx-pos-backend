using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        string GenerateJwtToken(UserDto user, int? companyId = null, int? branchId = null);
    }
}


