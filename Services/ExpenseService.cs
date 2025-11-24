using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
        {
            var expenses = await _context.Expenses.ToListAsync();
            return expenses.Select(e => new ExpenseDto
            {
                ExpenseId = e.ExpenseId,
                ExpenseName = e.ExpenseName,
                ExpenseAmount = e.ExpenseAmount,
                ExpenseDate = e.ExpenseDate
            });
        }

        public async Task<ExpenseDto?> GetExpenseByIdAsync(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return null;

            return new ExpenseDto
            {
                ExpenseId = expense.ExpenseId,
                ExpenseName = expense.ExpenseName,
                ExpenseAmount = expense.ExpenseAmount,
                ExpenseDate = expense.ExpenseDate
            };
        }

        public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto)
        {
            var expense = new Models.Expense
            {
                ExpenseName = createExpenseDto.ExpenseName,
                ExpenseAmount = createExpenseDto.ExpenseAmount,
                ExpenseDate = createExpenseDto.ExpenseDate
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return await GetExpenseByIdAsync(expense.ExpenseId) ?? new ExpenseDto();
        }

        public async Task<bool> UpdateExpenseAsync(int id, CreateExpenseDto updateExpenseDto)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return false;

            expense.ExpenseName = updateExpenseDto.ExpenseName;
            expense.ExpenseAmount = updateExpenseDto.ExpenseAmount;
            expense.ExpenseDate = updateExpenseDto.ExpenseDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return false;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


