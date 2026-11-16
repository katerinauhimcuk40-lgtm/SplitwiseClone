using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Extensions;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Interfaces;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public ExpensesController(IExpenseService expenses)
    {
        _expenses = expenses;
    }

    /// <summary>Витрати групи (потрібно бути її учасником).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetExpenses([FromQuery] int groupId, CancellationToken ct)
        => Ok(await _expenses.GetGroupExpensesAsync(groupId, User.GetUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> GetExpense(int id, CancellationToken ct)
        => Ok(await _expenses.GetByIdAsync(id, User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(CreateExpenseRequest request, CancellationToken ct)
    {
        var expense = await _expenses.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(int id, CancellationToken ct)
    {
        await _expenses.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
