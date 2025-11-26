using System.ComponentModel.DataAnnotations;

namespace POS.Api.DTOs
{
    public class UpdateReturnStatusRequest
    {
        [Required]
        public string ReturnStatus { get; set; } = string.Empty;
    }
}
