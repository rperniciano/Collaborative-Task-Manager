using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// Application service interface for Board operations.
/// </summary>
public interface IBoardAppService : IApplicationService
{
    /// <summary>
    /// Gets the current user's board with its columns.
    /// If the user doesn't have a board, creates one automatically.
    /// </summary>
    Task<BoardWithColumnsDto> GetBoardAsync();
}
