using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface IAuthService
    {
        Task<Result<AuthenticationTokenDTO>> LogIn(CredentialsDTO credentials);
        Task<Result> ConfirmEmail(string token);
        Task<Result> ForgotPassword(string email);
        Task<Result> ResetPassword(ResetPasswordDTO resetPasswordDto);
    }
}
