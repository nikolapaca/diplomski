using FakeTrello.DTO;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FakeTrello.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICollaboratorService _collaboratorService;

        public UserController(IUserService userService, ICollaboratorService collaboratorService)
        {
            _userService = userService;
            _collaboratorService = collaboratorService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDTO>>> GetAll()
        {
            var users = await _userService.GetAll();
            if (!users.IsSuccess)
            {
                return StatusCode(500, "Error on server!");
            }
            if(users.Value == null ||  users.Value.Count == 0)
            {
                return NotFound("No users in database");
            }
            return Ok(users.Value);
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<UserDTO>> GetById(int id)
        {
            var user = await _userService.GetById(id);
            if(!user.IsSuccess)
            {     
                return StatusCode(500, "Error on server!");
            }
            if (user.Value == null)
                return NotFound($"User with id {id} not found");
            return Ok(user.Value);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> Create([FromBody] UserDTO userdto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _userService.Create(userdto);
            if (!user.IsSuccess)
            {
                return BadRequest("User with this username already exists!");
            }
            if (user.Value == null)
                return NotFound($"User wasn't created!");
            return Ok(user.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserDTO>> Update([FromBody] UserDTO userdto)
        {
            var user = await _userService.Update(userdto);
            if (!user.IsSuccess)
                return StatusCode(500, "Error on server!");
            if (user.Value == null)
                return NotFound("User wasn't updated");
            return Ok(user.Value);
        }
        [HttpGet("search/offBoard")]
        public async Task<ActionResult<List<UserDTO>>> GetUsersNotOnBoard([FromQuery] string? searchTerm,
            [FromQuery] string boardName,
            [FromQuery] string boardOwnerUsername)
       {
            if(string.IsNullOrEmpty(boardName) || string.IsNullOrEmpty(boardOwnerUsername)){
                return BadRequest();
            }
            try
            {
                var users = await _collaboratorService.GetUsersNotOnBoard(searchTerm, boardName, boardOwnerUsername);
                return Ok(users.Value);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }

        [HttpGet("search/onBoard")]
        public async Task<ActionResult<List<UserDTO>>> GetUsersOnBoard([FromQuery] string? searchTerm,
            [FromQuery] string boardName,
            [FromQuery] string boardOwnerUsername)
        {
            if (string.IsNullOrEmpty(boardName) || string.IsNullOrEmpty(boardOwnerUsername))
            {
                return BadRequest();
            }
            try
            {
                var users = await _collaboratorService.GetUsersOnBoard(searchTerm, boardName, boardOwnerUsername);
                return Ok(users.Value);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }

        [HttpGet("search/assignableOnBoard")]
        public async Task<ActionResult<List<UserDTO>>> GetAssignableUsersOnBoard([FromQuery] string? searchTerm,
            [FromQuery] string boardName,
            [FromQuery] string boardOwnerUsername,
            [FromQuery] int cardId)
        {
            if (string.IsNullOrEmpty(boardName) || string.IsNullOrEmpty(boardOwnerUsername))
            {
                return BadRequest();
            }
            try
            {
                var users = await _collaboratorService.GetAssignableUsersOnBoard(searchTerm, boardName, boardOwnerUsername, cardId);
                return Ok(users.Value);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserDTO>> GetUserByUsername()
        {
            var usernameClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usernameClaim))
            {
                return Unauthorized("Token does not contain required user identifier.");
            }
            try
            {
                var user = await _userService.GetUserByUsername(usernameClaim);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
