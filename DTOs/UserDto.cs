namespace POS.Api.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime Birthdate { get; set; }
        public string? NID { get; set; }
        public string? Phone { get; set; }
        public string? HomeTown { get; set; }
        public string? CurrentCity { get; set; }
        public string? Division { get; set; }
        public string? BloodGroup { get; set; }
        public int? PostalCode { get; set; }
    }

    public class CreateUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = "Salesman";
        public decimal Salary { get; set; }
        public DateTime Birthdate { get; set; }
        public string? NID { get; set; }
        public string? Phone { get; set; }
        public string? HomeTown { get; set; }
        public string? CurrentCity { get; set; }
        public string? Division { get; set; }
        public string? BloodGroup { get; set; }
        public int? PostalCode { get; set; }
    }

    public class UpdateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime Birthdate { get; set; }
        public string? Phone { get; set; }
        public string? HomeTown { get; set; }
        public string? CurrentCity { get; set; }
        public string? Division { get; set; }
        public string? BloodGroup { get; set; }
        public int? PostalCode { get; set; }
    }
}

