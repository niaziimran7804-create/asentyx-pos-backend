using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accountingService;

        public ExpenseService(ApplicationDbContext context, IAccountingService accountingService)
        {
            _context = context;
            _accountingService = accountingService;
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

        public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto, string createdBy)
        {
            var expense = new Models.Expense
            {
                ExpenseName = createExpenseDto.ExpenseName,
                ExpenseAmount = createExpenseDto.ExpenseAmount,
                ExpenseDate = createExpenseDto.ExpenseDate
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Create accounting entry for the expense
            try
            {
                await _accountingService.CreateExpenseEntryAsync(expense.ExpenseId, createdBy);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create accounting entry for expense {expense.ExpenseId}: {ex.Message}");
            }

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


