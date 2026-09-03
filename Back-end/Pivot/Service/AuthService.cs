using AutoMapper;
using Pivot.Auth;
using Pivot.DTO;
using Pivot.Model;
using Pivot.Repository.Contract;
using Pivot.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using NuGet.Protocol.Core.Types;

namespace Pivot.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;

        public AuthService(ITokenGenerator tokenGenerator, IUserRepository userReposiory, IMapper mapper, IEmailService emailService)
        {
            _tokenGenerator = tokenGenerator;
            _userRepository = userReposiory;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<User>();
            _emailService = emailService;
        }

        public async Task<Result<AuthenticationTokenDTO>> LogIn(CredentialsDTO credentialsDTO)
        {
            var user = await _userRepository.GetByUsername(credentialsDTO.Username);
            if (user == null) return Result.Fail("");
            if (!user.EmailConfirmed) return Result.Fail("Please confirm your email first.");
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, credentialsDTO.Password);
            if (user == null || verificationResult != PasswordVerificationResult.Success) return Result.Fail("User not found!");
            return _tokenGenerator.GenerateToken(user);
        }

        public async Task<Result> ConfirmEmail(string token)
        {
            var user = await _userRepository.GetByConfirmationToken(token);

            if (user == null)
                return Result.Fail("Invalid token");

            if (user.EmailConfirmationTokenExpiration < DateTime.UtcNow)
                return Result.Fail("Token expired");

            if (user.EmailConfirmed == true)
                return Result.Fail("Email already confirmed!");


            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiration = null;


            await _userRepository.Update(user);

            return Result.Ok();
        }

        public async Task<Result> ForgotPassword(string email)
        {
            var user = await _userRepository.GetByEmail(email);

            if (user == null || !user.EmailConfirmed)
                return Result.Ok();

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);

            await _userRepository.Update(user);

            await _emailService.SendPasswordResetEmail(user.Email, user.PasswordResetToken);

            return Result.Ok();
        }

        public async Task<Result> ResetPassword(ResetPasswordDTO resetPasswordDto)
        {
            var user = await _userRepository.GetByResetToken(resetPasswordDto.Token);

            if (user == null)
                return Result.Fail("Invalid or expired reset link.");

            if (user.PasswordResetTokenExpiration < DateTime.UtcNow)
                return Result.Fail("Invalid or expired reset link.");

            user.Password = _passwordHasher.HashPassword(user, resetPasswordDto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiration = null;

            await _userRepository.Update(user);

            return Result.Ok();
        }
    }
}
