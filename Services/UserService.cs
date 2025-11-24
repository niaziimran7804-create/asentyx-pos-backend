using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
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
                PostalCode = u.PostalCode
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
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
                PostalCode = user.PostalCode
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
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
                PostalCode = createUserDto.PostalCode
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(user.Id) ?? new UserDto();
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
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
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


