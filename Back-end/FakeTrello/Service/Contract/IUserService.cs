using FakeTrello.DTO;
using FakeTrello.Model;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface IUserService
    {
        Task<Result<List<UserDTO>>> GetAll();
        Task<Result<UserDTO>> GetById(int? id);
        Task<User> GetUserByUsername(string username);
        Task<Result<UserDTO>> Create(UserDTO userDTO);
        Task<Result<UserDTO>> Update(string username, UserDTO userDTO);
        Task<Result> ChangePassword(string username, PasswordChangeDTO dto);

    }
}
