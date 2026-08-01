using AutoMapper;
using FakeTrello.Auth;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using NuGet.Protocol.Core.Types;

namespace FakeTrello.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(ITokenGenerator tokenGenerator, IUserRepository userReposiory, IMapper mapper)
        {
            _tokenGenerator = tokenGenerator;
            _userRepository = userReposiory;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<User>();
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
    }
}
