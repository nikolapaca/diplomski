using AutoMapper;
using Pivot.Data.Contract;
using Pivot.DTO;
using Pivot.Model;
using Pivot.Repository.Contract;
using Pivot.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Pivot.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository repository, IMapper mapper, IEmailService emailService, IUnitOfWork unitOfWork, ILogger<UserService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<User>();
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserDTO>> Create(UserDTO userDto)
        {
            var existingUserUsername = await _repository.GetByUsername(userDto.Username);
            var existingUserEmail = await _repository.GetByEmail(userDto.Email);
            if (existingUserUsername != null || existingUserEmail != null)
                return Result.Fail("User already exists!");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newUser = _mapper.Map<UserDTO, User>(userDto);
                newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);
                newUser.EmailConfirmed = false;
                newUser.EmailConfirmationToken = Guid.NewGuid().ToString();
                newUser.EmailConfirmationTokenExpiration = DateTime.UtcNow.AddHours(24);
                User user = await _repository.Create(newUser);

                await _emailService.SendConfirmationEmail(
                    user.Email,
                    user.EmailConfirmationToken
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok(_mapper.Map<User, UserDTO>(user));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Registration failed for username {Username}.", userDto.Username);
                return Result.Fail("Registration failed. Please try again in a moment.");
            }
        }

        public async Task<Result<UserDTO>> Delete(int id)
        {
            var user = await _repository.GetById(id);
            if (user == null)
                return Result.Fail("This user doesn't exist!");
            await _repository.Delete(user.Id);
            return Result.Ok(_mapper.Map<User, UserDTO>(user));
        }

        public async Task<Result<List<UserDTO>>> GetAll()
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

        public async Task<Result> ChangePassword(string username, PasswordChangeDTO dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return Result.Fail("New password does not match!");

            var user = await GetUserByUsername(username);

            if (user == null)
                return Result.Fail("User not found.");

            var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);

            if (passwordCheck == PasswordVerificationResult.Failed)
                return Result.Fail("Current password is incorrect!");

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);

            await _repository.Update(user);

            return Result.Ok();
        }
    }
}