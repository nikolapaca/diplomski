using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace FakeTrello.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(IUserRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<Result<UserDTO>> Create(UserDTO userDto)
        {
            var existingUserUsername = await _repository.GetByUsername(userDto.Username);
            var existingUserEmail = await _repository.GetByEmail(userDto.Email);
            if (existingUserUsername != null || existingUserEmail != null)
                return Result.Fail("User already exists!");

            var newUser = _mapper.Map<UserDTO, User>(userDto);
            newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);
            User user = await _repository.Create(newUser);
            return Result.Ok(_mapper.Map<User, UserDTO>(user));
        }

        public async Task<Result<UserDTO>> Update (UserDTO userDto)
        {
            var newUser = _mapper.Map<UserDTO, User>(userDto);
            User user = await _repository.Update(newUser);
            return Result.Ok(_mapper.Map<User, UserDTO>(user));
        }

        public async Task<Result<UserDTO>> Delete (int id)
        {
            var user = await _repository.GetById(id);
            if (user == null)
                return Result.Fail("This user doesn't exist!");
            await _repository.Delete(user.Id);
            return Result.Ok(_mapper.Map<User, UserDTO>(user));
        }

        public async Task<Result<List<UserDTO>>> GetAll ()
        {
            var users = await _repository.GetAll();
            var usersDto = _mapper.Map<List<User>, List<UserDTO>>(users);
            return Result.Ok(usersDto);
        }

        public async Task<Result<UserDTO>> GetById(int? id)
        {
            var user = await _repository.GetById(id);
            var userDto = _mapper.Map<User, UserDTO>(user);
            return Result.Ok(userDto);
        }

        public async Task<User> GetUserByUsername(string username)
        {
            var user = await _repository.GetByUsername(username);
            return user;
        }

        public async Task<Result<UserDTO>> GetUserDTOByUsername(string username)
        {
            var user = await _repository.GetByUsername(username);
            var userDto = _mapper.Map<User, UserDTO>(user);
            return Result.Ok(userDto);
        }
    }
}
