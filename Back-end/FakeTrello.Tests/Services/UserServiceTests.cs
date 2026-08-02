using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly PasswordHasher<User> _passwordHasher = new();

        private UserService CreateService() => new(
            _userRepository.Object,
            _mapper.Object,
            _emailService.Object);

        private User MakeUser(string username, string plainPassword)
        {
            var user = new User { Id = 1, Username = username };
            user.Password = _passwordHasher.HashPassword(user, plainPassword);
            return user;
        }

        [Fact]
        public async Task ChangePassword_ConfirmationDoesNotMatch_ReturnsFail()
        {
            var service = CreateService();

            var result = await service.ChangePassword("someone", new PasswordChangeDTO
            {
                CurrentPassword = "old",
                NewPassword = "newpass1",
                ConfirmNewPassword = "newpass2"
            });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("does not match"));
            _userRepository.Verify(r => r.GetByUsername(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ReturnsFail()
        {
            var user = MakeUser("member", "correct-old-password");
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.ChangePassword("member", new PasswordChangeDTO
            {
                CurrentPassword = "wrong-old-password",
                NewPassword = "newpass1",
                ConfirmNewPassword = "newpass1"
            });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Current password is incorrect"));
            _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePassword_CorrectCurrentPassword_UpdatesHashAndSucceeds()
        {
            var user = MakeUser("member", "correct-old-password");
            var originalHash = user.Password;
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.ChangePassword("member", new PasswordChangeDTO
            {
                CurrentPassword = "correct-old-password",
                NewPassword = "brand-new-password",
                ConfirmNewPassword = "brand-new-password"
            });

            Assert.True(result.IsSuccess);
            Assert.NotEqual(originalHash, user.Password);
            _userRepository.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_UnknownUser_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.ChangePassword("ghost", new PasswordChangeDTO
            {
                CurrentPassword = "whatever",
                NewPassword = "newpass1",
                ConfirmNewPassword = "newpass1"
            });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("not found"));
        }

        [Fact]
        public async Task Create_UsernameAlreadyTaken_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("taken")).ReturnsAsync(new User { Id = 1, Username = "taken" });
            _userRepository.Setup(r => r.GetByEmail("new@example.com")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.Create(new UserDTO { Username = "taken", Email = "new@example.com" });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));
            _userRepository.Verify(r => r.Create(It.IsAny<User>()), Times.Never);
            _emailService.Verify(e => e.SendConfirmationEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Create_EmailAlreadyTaken_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("newuser")).ReturnsAsync((User)null);
            _userRepository.Setup(r => r.GetByEmail("taken@example.com"))
                .ReturnsAsync(new User { Id = 1, Email = "taken@example.com" });

            var service = CreateService();
            var result = await service.Create(new UserDTO { Username = "newuser", Email = "taken@example.com" });

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));
            _userRepository.Verify(r => r.Create(It.IsAny<User>()), Times.Never);
        }
    }
}
