using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantContext _tenantContext;

        public BranchService(ApplicationDbContext context, TenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
        {
            var query = _context.Branches
                .Include(b => b.Company)
                .Include(b => b.Users)
                .Include(b => b.Products)
                .AsQueryable();

            // Filter by company for Company Admins
            // Super Admins (no CompanyId) see all branches
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == _tenantContext.CompanyId.Value);
            }

            var branches = await query.ToListAsync();

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
            // Company Admins can only get branches from their own company
            if (_tenantContext.CompanyId.HasValue && companyId != _tenantContext.CompanyId.Value)
            {
                return Enumerable.Empty<BranchDto>();
            }

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
            var query = _context.Branches
                .Include(b => b.Company)
                .Include(b => b.Users)
                .Include(b => b.Products)
                .AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == _tenantContext.CompanyId.Value);
            }

            var branch = await query.FirstOrDefaultAsync(b => b.BranchId == id);

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
            // If Company Admin, enforce they can only create branches in their company
            if (_tenantContext.CompanyId.HasValue && createBranchDto.CompanyId != _tenantContext.CompanyId.Value)
            {
                throw new InvalidOperationException("Cannot create branch for another company.");
            }

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
            var query = _context.Branches.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == _tenantContext.CompanyId.Value);
            }

            var branch = await query.FirstOrDefaultAsync(b => b.BranchId == id);
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
            var query = _context.Branches.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == _tenantContext.CompanyId.Value);
            }

            var branch = await query.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch == null)
                return false;

            // Soft delete by marking as inactive
            branch.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BranchExistsAsync(int id)
        {
            var query = _context.Branches.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == _tenantContext.CompanyId.Value);
            }

            return await query.AnyAsync(b => b.BranchId == id);
        }
    }
}
