using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollaborativeTaskManager.Controllers;

/// <summary>
/// Controller for database initialization and maintenance operations.
/// </summary>
[Route("api/app/database")]
[Authorize]
public class DatabaseController : CollaborativeTaskManagerController
{
    private readonly IBoardAppService _boardAppService;

    public DatabaseController(IBoardAppService boardAppService)
    {
        _boardAppService = boardAppService;
    }

    /// <summary>
    /// Initializes the database by creating any missing tables.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<ActionResult<string>> Initialize()
    {
        var result = await _boardAppService.EnsureTasksTableAsync();
        return Ok(result);
    }
}
