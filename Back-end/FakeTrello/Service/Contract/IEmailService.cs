namespace FakeTrello.Service.Contract
{
    public interface IEmailService
    {
        Task SendConfirmationEmail(string email, string token);
    }
}
