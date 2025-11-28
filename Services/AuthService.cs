using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Data;
using POS.Api.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace POS.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.UserId == loginDto.UserId);

            if (user == null)
                return null;

            // Verify password (in production, use BCrypt)
            if (user.Password != loginDto.Password) // Simple check - should use BCrypt in production
                return null;

            // Check if user's company is active
            if (user.CompanyId.HasValue && user.Company != null && !user.Company.IsActive)
                return null;

            // Check if user's branch is active
            if (user.BranchId.HasValue && user.Branch != null && !user.Branch.IsActive)
                return null;

            var userDto = new UserDto
            {
                Id = user.Id,
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Age = user.Age,
                Gender = user.Gender,
                Role = user.Role,
                Salary = user.Salary,
                JoinDate = user.JoinDate,
                Birthdate = user.Birthdate,
                Phone = user.Phone,
                CurrentCity = user.CurrentCity
            };

            var token = GenerateJwtToken(userDto, user.CompanyId, user.BranchId);

            return new LoginResponseDto
            {
                Token = token,
                User = userDto
            };
        }

        public string GenerateJwtToken(UserDto user, int? companyId = null, int? branchId = null)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!");
            var issuer = _configuration["Jwt:Issuer"] ?? "POS.Api";
            var audience = _configuration["Jwt:Audience"] ?? "POS.Client";
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserId),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.UserId),
                new Claim("Role", user.Role)
            };

            // Add company and branch claims if available
            if (companyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
            }

            if (branchId.HasValue)
            {
                claims.Add(new Claim("BranchId", branchId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}


