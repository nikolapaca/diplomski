using Pivot.DTO;
using Pivot.Model;
using FluentResults;

namespace Pivot.Auth
{
    public interface ITokenGenerator
    {
        Result<AuthenticationTokenDTO> GenerateToken(User user);
    }
}
