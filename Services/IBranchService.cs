using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
        Task<IEnumerable<BranchDto>> GetBranchesByCompanyIdAsync(int companyId);
        Task<BranchDto?> GetBranchByIdAsync(int id);
        Task<BranchDto> CreateBranchAsync(CreateBranchDto createBranchDto);
        Task<bool> UpdateBranchAsync(int id, UpdateBranchDto updateBranchDto);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> BranchExistsAsync(int id);
    }
}
