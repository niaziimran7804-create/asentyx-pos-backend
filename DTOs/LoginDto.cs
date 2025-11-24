using System.ComponentModel.DataAnnotations;

namespace POS.Api.DTOs
{
    public class LoginDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new UserDto();
    }
}

