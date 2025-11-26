using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync();
        Task<ExpenseDto?> GetExpenseByIdAsync(int id);
        Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto, string createdBy);
        Task<bool> UpdateExpenseAsync(int id, CreateExpenseDto updateExpenseDto);
        Task<bool> DeleteExpenseAsync(int id);
    }
}



