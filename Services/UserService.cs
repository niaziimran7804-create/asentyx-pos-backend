using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantContext _tenantContext;

        public UserService(ApplicationDbContext context, TenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var query = _context.Users.AsQueryable();

            // Filter by company for Company Admins
            // Super Admins (no CompanyId) see all users
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _tenantContext.CompanyId.Value);
            }

            var users = await query.ToListAsync();
            
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Age = u.Age,
                Gender = u.Gender,
                Role = u.Role,
                Salary = u.Salary,
                JoinDate = u.JoinDate,
                Birthdate = u.Birthdate,
                NID = u.NID,
                Phone = u.Phone,
                HomeTown = u.HomeTown,
                CurrentCity = u.CurrentCity,
                Division = u.Division,
                BloodGroup = u.BloodGroup,
                PostalCode = u.PostalCode,
                CompanyId = u.CompanyId,
                BranchId = u.BranchId
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var query = _context.Users.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _tenantContext.CompanyId.Value);
            }

            var user = await query.FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return null;

            return new UserDto
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
                NID = user.NID,
                Phone = user.Phone,
                HomeTown = user.HomeTown,
                CurrentCity = user.CurrentCity,
                Division = user.Division,
                BloodGroup = user.BloodGroup,
                PostalCode = user.PostalCode,
                CompanyId = user.CompanyId,
                BranchId = user.BranchId
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            // Validate branch assignment is required
            if (!createUserDto.BranchId.HasValue)
            {
                throw new InvalidOperationException("User must be assigned to a branch.");
            }

            // If Company Admin, enforce they can only create users in their company
            if (_tenantContext.CompanyId.HasValue && createUserDto.CompanyId != _tenantContext.CompanyId.Value)
            {
                throw new InvalidOperationException("Cannot create user for another company.");
            }

            var user = new Models.User
            {
                UserId = createUserDto.UserId,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Password = createUserDto.Password, // In production, hash with BCrypt
                Email = createUserDto.Email,
                Age = createUserDto.Age,
                Gender = createUserDto.Gender,
                Role = createUserDto.Role,
                Salary = createUserDto.Salary,
                JoinDate = DateTime.UtcNow,
                Birthdate = createUserDto.Birthdate,
                NID = createUserDto.NID,
                Phone = createUserDto.Phone,
                HomeTown = createUserDto.HomeTown,
                CurrentCity = createUserDto.CurrentCity,
                Division = createUserDto.Division,
                BloodGroup = createUserDto.BloodGroup,
                PostalCode = createUserDto.PostalCode,
                CompanyId = createUserDto.CompanyId,
                BranchId = createUserDto.BranchId.Value
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(user.Id) ?? new UserDto();
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var query = _context.Users.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _tenantContext.CompanyId.Value);
            }

            var user = await query.FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return false;

            user.FirstName = updateUserDto.FirstName;
            user.LastName = updateUserDto.LastName;
            user.Email = updateUserDto.Email;
            user.Age = updateUserDto.Age;
            user.Gender = updateUserDto.Gender;
            user.Role = updateUserDto.Role;
            user.Salary = updateUserDto.Salary;
            user.Birthdate = updateUserDto.Birthdate;
            user.Phone = updateUserDto.Phone;
            user.HomeTown = updateUserDto.HomeTown;
            user.CurrentCity = updateUserDto.CurrentCity;
            user.Division = updateUserDto.Division;
            user.BloodGroup = updateUserDto.BloodGroup;
            user.PostalCode = updateUserDto.PostalCode;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var query = _context.Users.AsQueryable();

            // Filter by company for Company Admins
            if (_tenantContext.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _tenantContext.CompanyId.Value);
            }

            var user = await query.FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


