using Pivot.DTO;
using Pivot.Model;
using FluentResults;

namespace Pivot.Service.Contract
{
    public interface IUserService
    {
        Task<Result<List<UserDTO>>> GetAll();
        Task<Result<UserDTO>> GetById(int? id);
        Task<User> GetUserByUsername(string username);
        Task<Result<UserDTO>> Create(UserDTO userDTO);
        Task<Result> ChangePassword(string username, PasswordChangeDTO dto);

    }
}
