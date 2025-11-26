using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IReturnService
    {
        Task<ReturnDto> CreateWholeReturnAsync(WholeReturnRequest request);
        Task<ReturnDto> CreatePartialReturnAsync(PartialReturnRequest request);
        Task<IEnumerable<ReturnDto>> GetAllReturnsAsync();
        Task<ReturnDto?> GetReturnByIdAsync(int id);
        Task<ReturnSummaryDto> GetReturnSummaryAsync();
        Task<bool> UpdateReturnStatusAsync(int id, UpdateReturnStatusRequest request, int userId);
    }
}
