namespace Pivot.Service.Contract
{
    public interface IEmailService
    {
        Task SendConfirmationEmail(string email, string token);
        Task SendPasswordResetEmail(string email, string token);
    }
}
