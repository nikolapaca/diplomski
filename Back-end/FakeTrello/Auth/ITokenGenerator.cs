using FakeTrello.DTO;
using FakeTrello.Model;
using FluentResults;

namespace FakeTrello.Auth
{
    public interface ITokenGenerator
    {
        Result<AuthenticationTokenDTO> GenerateToken(User user);
    }
}
