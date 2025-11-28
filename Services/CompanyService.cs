using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;

namespace POS.Api.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync()
        {
            var companies = await _context.Companies
                .Include(c => c.Branches)
                .Include(c => c.Users)
                .ToListAsync();

            return companies.Select(c => new CompanyDto
            {
                CompanyId = c.CompanyId,
                CompanyName = c.CompanyName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                City = c.City,
                Country = c.Country,
                PostalCode = c.PostalCode,
                TaxNumber = c.TaxNumber,
                RegistrationNumber = c.RegistrationNumber,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                SubscriptionEndDate = c.SubscriptionEndDate,
                SubscriptionPlan = c.SubscriptionPlan,
                TotalBranches = c.Branches.Count,
                TotalUsers = c.Users.Count
            });
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(int id)
        {
            var company = await _context.Companies
                .Include(c => c.Branches)
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null)
                return null;

            return new CompanyDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                City = company.City,
                Country = company.Country,
                PostalCode = company.PostalCode,
                TaxNumber = company.TaxNumber,
                RegistrationNumber = company.RegistrationNumber,
                IsActive = company.IsActive,
                CreatedDate = company.CreatedDate,
                SubscriptionEndDate = company.SubscriptionEndDate,
                SubscriptionPlan = company.SubscriptionPlan,
                TotalBranches = company.Branches.Count,
                TotalUsers = company.Users.Count
            };
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto createCompanyDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the company
                var company = new Company
                {
                    CompanyName = createCompanyDto.CompanyName,
                    Email = createCompanyDto.Email,
                    Phone = createCompanyDto.Phone,
                    Address = createCompanyDto.Address,
                    City = createCompanyDto.City,
                    Country = createCompanyDto.Country,
                    PostalCode = createCompanyDto.PostalCode,
                    TaxNumber = createCompanyDto.TaxNumber,
                    RegistrationNumber = createCompanyDto.RegistrationNumber,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    SubscriptionPlan = createCompanyDto.SubscriptionPlan,
                    SubscriptionEndDate = createCompanyDto.SubscriptionEndDate ?? DateTime.UtcNow.AddMonths(1)
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // Create head office branch
                var headBranch = new Branch
                {
                    CompanyId = company.CompanyId,
                    BranchName = "Head Office",
                    BranchCode = "HO",
                    Email = company.Email,
                    Phone = company.Phone,
                    Address = company.Address,
                    City = company.City,
                    Country = company.Country,
                    PostalCode = company.PostalCode,
                    IsActive = true,
                    IsHeadOffice = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Branches.Add(headBranch);
                await _context.SaveChangesAsync();

                // Create admin user for the company
                var adminUser = new User
                {
                    UserId = createCompanyDto.AdminUserId,
                    FirstName = createCompanyDto.AdminFirstName,
                    LastName = createCompanyDto.AdminLastName,
                    Password = createCompanyDto.AdminPassword, // In production, hash this
                    Email = createCompanyDto.AdminEmail,
                    Phone = createCompanyDto.AdminPhone,
                    Role = "CompanyAdmin",
                    CompanyId = company.CompanyId,
                    BranchId = headBranch.BranchId,
                    JoinDate = DateTime.UtcNow,
                    Birthdate = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetCompanyByIdAsync(company.CompanyId) ?? new CompanyDto();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateCompanyAsync(int id, UpdateCompanyDto updateCompanyDto)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
                return false;

            company.CompanyName = updateCompanyDto.CompanyName;
            company.Email = updateCompanyDto.Email;
            company.Phone = updateCompanyDto.Phone;
            company.Address = updateCompanyDto.Address;
            company.City = updateCompanyDto.City;
            company.Country = updateCompanyDto.Country;
            company.PostalCode = updateCompanyDto.PostalCode;
            company.TaxNumber = updateCompanyDto.TaxNumber;
            company.RegistrationNumber = updateCompanyDto.RegistrationNumber;
            company.IsActive = updateCompanyDto.IsActive;
            company.SubscriptionPlan = updateCompanyDto.SubscriptionPlan;
            company.SubscriptionEndDate = updateCompanyDto.SubscriptionEndDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCompanyAsync(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
                return false;

            // Soft delete by marking as inactive
            company.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompanyExistsAsync(int id)
        {
            return await _context.Companies.AnyAsync(c => c.CompanyId == id);
        }
    }
}
