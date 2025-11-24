using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            var expenses = await _expenseService.GetAllExpensesAsync();
            return Ok(expenses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetExpense(int id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
                return NotFound();

            return Ok(expense);
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense([FromBody] CreateExpenseDto createExpenseDto)
        {
            var expense = await _expenseService.CreateExpenseAsync(createExpenseDto);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.ExpenseId }, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseDto updateExpenseDto)
        {
            var result = await _expenseService.UpdateExpenseAsync(id, updateExpenseDto);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var result = await _expenseService.DeleteExpenseAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}


