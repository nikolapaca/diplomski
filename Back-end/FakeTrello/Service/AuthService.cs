using AutoMapper;
using FakeTrello.Auth;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;

namespace FakeTrello.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userReposiory;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(ITokenGenerator tokenGenerator, IUserRepository userReposiory, IMapper mapper)
        {
            _tokenGenerator = tokenGenerator;
            _userReposiory = userReposiory;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<Result<AuthenticationTokenDTO>> LogIn(CredentialsDTO credentialsDTO)
        {
            var user = await _userReposiory.GetByUsername(credentialsDTO.Username);
            if (user == null)
                return Result.Fail("");
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, credentialsDTO.Password);
            if (user == null || verificationResult != PasswordVerificationResult.Success) return Result.Fail("User not found!");
            return _tokenGenerator.GenerateToken(user);
        }
    }
}
