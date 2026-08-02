using FakeTrello.DTO;
using FakeTrello.Model;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface IBoardService
    {
        Task<Result<List<BoardDTO>>> GetAllByUsername(string username);
        Task<Result<BoardDTO>> GetById(int id);
        Task<Result<List<BoardDTO>>> GetBySearchFilter(string username, string searchQuery);
        Task<Board?> GetByNameAndOwnerUsername(string name, string username);
        Task<Result<BoardDTO>> GetResultByNameAndOwnerUsername(string name, string username);
        Task<Result<BoardDTO>> Create(BoardDTO boardDTO, string username);
        Task<Result<BoardDTO>> Update(BoardUpdateDTO boardDto, string requestingUsername);
        Task<Result> Delete(string name, string username, string requestingUsername);
        Task<Result> AddCollaboratorToBoard(BoardDTO boardDto, string username, string requestingUsername);
        Task<Result> RemoveCollaboratorFromBoard(BoardDTO boardDto, string username, string requestingUsername);
        Task<Result> LeaveBoard(BoardDTO boardDto, string username);
        Task<Result> ToggleFavorite(string boardName, string boardOwnerUsername, string username);
        Task<Result> Archive(string name, string ownerUsername, string requestingUsername);
    }
}