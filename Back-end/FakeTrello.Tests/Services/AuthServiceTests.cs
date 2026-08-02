using AutoMapper;
using FakeTrello.Auth;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly PasswordHasher<User> _passwordHasher = new();

        private AuthService CreateService() => new(
            _tokenGenerator.Object,
            _userRepository.Object,
            _mapper.Object);

        private User MakeConfirmedUser(string username, string plainPassword)
        {
            var user = new User { Id = 1, Username = username, EmailConfirmed = true };
            user.Password = _passwordHasher.HashPassword(user, plainPassword);
            return user;
        }

        [Fact]
        public async Task LogIn_UnknownUsername_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.LogIn(new CredentialsDTO { Username = "ghost", Password = "whatever" });

            Assert.True(result.IsFailed);
            _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LogIn_EmailNotConfirmed_ReturnsFail()
        {
            var user = new User { Id = 1, Username = "pending", EmailConfirmed = false };
            _userRepository.Setup(r => r.GetByUsername("pending")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.LogIn(new CredentialsDTO { Username = "pending", Password = "whatever" });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("confirm your email"));
            _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LogIn_WrongPassword_ReturnsFail()
        {
            var user = MakeConfirmedUser("member", "correct-password");
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.LogIn(new CredentialsDTO { Username = "member", Password = "wrong-password" });

            Assert.True(result.IsFailed);
            _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LogIn_CorrectCredentials_ReturnsToken()
        {
            var user = MakeConfirmedUser("member", "correct-password");
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _tokenGenerator.Setup(t => t.GenerateToken(user))
                .Returns(Result.Ok(new AuthenticationTokenDTO { AccessToken = "jwt-token" }));

            var service = CreateService();
            var result = await service.LogIn(new CredentialsDTO { Username = "member", Password = "correct-password" });

            Assert.True(result.IsSuccess);
            Assert.Equal("jwt-token", result.Value.AccessToken);
        }

        [Fact]
        public async Task ConfirmEmail_InvalidToken_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByConfirmationToken("bad-token")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.ConfirmEmail("bad-token");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Invalid token"));
        }

        [Fact]
        public async Task ConfirmEmail_ExpiredToken_ReturnsFail()
        {
            var user = new User
            {
                Id = 1,
                Username = "pending",
                EmailConfirmed = false,
                EmailConfirmationToken = "old-token",
                EmailConfirmationTokenExpiration = DateTime.UtcNow.AddHours(-1)
            };
            _userRepository.Setup(r => r.GetByConfirmationToken("old-token")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.ConfirmEmail("old-token");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("expired"));
            _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmEmail_AlreadyConfirmed_ReturnsFail()
        {
            var user = new User
            {
                Id = 1,
                Username = "already",
                EmailConfirmed = true,
                EmailConfirmationToken = "some-token",
                EmailConfirmationTokenExpiration = DateTime.UtcNow.AddHours(1)
            };
            _userRepository.Setup(r => r.GetByConfirmationToken("some-token")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.ConfirmEmail("some-token");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("already confirmed"));
        }

        [Fact]
        public async Task ConfirmEmail_ValidToken_ConfirmsUser()
        {
            var user = new User
            {
                Id = 1,
                Username = "pending",
                EmailConfirmed = false,
                EmailConfirmationToken = "valid-token",
                EmailConfirmationTokenExpiration = DateTime.UtcNow.AddHours(1)
            };
            _userRepository.Setup(r => r.GetByConfirmationToken("valid-token")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.ConfirmEmail("valid-token");

            Assert.True(result.IsSuccess);
            Assert.True(user.EmailConfirmed);
            Assert.Null(user.EmailConfirmationToken);
            _userRepository.Verify(r => r.Update(user), Times.Once);
        }
    }
}
