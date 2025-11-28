using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;

namespace POS.Api.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;

        public BranchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
        {
            var branches = await _context.Branches
                .Include(b => b.Company)
                .Include(b => b.Users)
                .Include(b => b.Products)
                .ToListAsync();

            return branches.Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                CompanyId = b.CompanyId,
                CompanyName = b.Company?.CompanyName ?? string.Empty,
                BranchName = b.BranchName,
                BranchCode = b.BranchCode,
                Email = b.Email,
                Phone = b.Phone,
                Address = b.Address,
                City = b.City,
                Country = b.Country,
                PostalCode = b.PostalCode,
                IsActive = b.IsActive,
                IsHeadOffice = b.IsHeadOffice,
                CreatedDate = b.CreatedDate,
                TotalUsers = b.Users.Count,
                TotalProducts = b.Products.Count
            });
        }

        public async Task<IEnumerable<BranchDto>> GetBranchesByCompanyIdAsync(int companyId)
        {
            var branches = await _context.Branches
                .Include(b => b.Company)
                .Include(b => b.Users)
                .Include(b => b.Products)
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();

            return branches.Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                CompanyId = b.CompanyId,
                CompanyName = b.Company?.CompanyName ?? string.Empty,
                BranchName = b.BranchName,
                BranchCode = b.BranchCode,
                Email = b.Email,
                Phone = b.Phone,
                Address = b.Address,
                City = b.City,
                Country = b.Country,
                PostalCode = b.PostalCode,
                IsActive = b.IsActive,
                IsHeadOffice = b.IsHeadOffice,
                CreatedDate = b.CreatedDate,
                TotalUsers = b.Users.Count,
                TotalProducts = b.Products.Count
            });
        }

        public async Task<BranchDto?> GetBranchByIdAsync(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Company)
                .Include(b => b.Users)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.BranchId == id);

            if (branch == null)
                return null;

            return new BranchDto
            {
                BranchId = branch.BranchId,
                CompanyId = branch.CompanyId,
                CompanyName = branch.Company?.CompanyName ?? string.Empty,
                BranchName = branch.BranchName,
                BranchCode = branch.BranchCode,
                Email = branch.Email,
                Phone = branch.Phone,
                Address = branch.Address,
                City = branch.City,
                Country = branch.Country,
                PostalCode = branch.PostalCode,
                IsActive = branch.IsActive,
                IsHeadOffice = branch.IsHeadOffice,
                CreatedDate = branch.CreatedDate,
                TotalUsers = branch.Users.Count,
                TotalProducts = branch.Products.Count
            };
        }

        public async Task<BranchDto> CreateBranchAsync(CreateBranchDto createBranchDto)
        {
            var branch = new Branch
            {
                CompanyId = createBranchDto.CompanyId,
                BranchName = createBranchDto.BranchName,
                BranchCode = createBranchDto.BranchCode,
                Email = createBranchDto.Email,
                Phone = createBranchDto.Phone,
                Address = createBranchDto.Address,
                City = createBranchDto.City,
                Country = createBranchDto.Country,
                PostalCode = createBranchDto.PostalCode,
                IsActive = true,
                IsHeadOffice = createBranchDto.IsHeadOffice,
                CreatedDate = DateTime.UtcNow
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return await GetBranchByIdAsync(branch.BranchId) ?? new BranchDto();
        }

        public async Task<bool> UpdateBranchAsync(int id, UpdateBranchDto updateBranchDto)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return false;

            branch.BranchName = updateBranchDto.BranchName;
            branch.BranchCode = updateBranchDto.BranchCode;
            branch.Email = updateBranchDto.Email;
            branch.Phone = updateBranchDto.Phone;
            branch.Address = updateBranchDto.Address;
            branch.City = updateBranchDto.City;
            branch.Country = updateBranchDto.Country;
            branch.PostalCode = updateBranchDto.PostalCode;
            branch.IsActive = updateBranchDto.IsActive;
            branch.IsHeadOffice = updateBranchDto.IsHeadOffice;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return false;

            // Soft delete by marking as inactive
            branch.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BranchExistsAsync(int id)
        {
            return await _context.Branches.AnyAsync(b => b.BranchId == id);
        }
    }
}
