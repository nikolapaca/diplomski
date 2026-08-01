using FakeTrello.DTO;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeTrello.Controller
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authenticationService;
        

        public AuthController(IAuthService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationTokenDTO>> LogIn([FromBody] CredentialsDTO credentialsDTO)
        {
            var token = await _authenticationService.LogIn(credentialsDTO);
            if (!token.IsSuccess)
            {
                return BadRequest("Email or password are incorrect, please try again!");
            }
            if (token.Value == null)
            {
                return NotFound("No users in database");
            }
            return Ok(token.Value);
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail([FromBody] string token)
        {
            var result = await _authenticationService.ConfirmEmail(token);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(new
            {
                message = "Email confirmed!"
            });
        }
    }
}
