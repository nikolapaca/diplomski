using FakeTrello.DTO;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeTrello.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/boards")]
    public class BoardController : ControllerBase
    {
        private readonly IBoardService _boardService;
        private readonly IUserBoardService _userBoardService;

        public BoardController(IBoardService boardService, IUserBoardService userBoardService)
        {
            _boardService = boardService;
            _userBoardService = userBoardService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BoardDTO>>> GetAll()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (username is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var boards = await _boardService.GetAllByUsername(username);
            if (!boards.IsSuccess)
            {
                return StatusCode(500, "Error on server!");
            }
            if (boards.Value == null || boards.Value.Count == 0)
            {
                return NotFound("No boards in database");
            }
            return Ok(boards.Value);
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<BoardDTO>> GetById(int id)
        {
            var board = await _boardService.GetById(id);
            if (!board.IsSuccess)
            {
                return StatusCode(500, "Error on server!");
            }
            if (board.Value == null)
                return NotFound($"Board with id {id} not found");
            return Ok(board.Value);
        }

        [HttpPost]
        public async Task<ActionResult<BoardDTO>> Create([FromBody] BoardDTO boardDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (username is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var board = await _boardService.Create(boardDto, username);
            if (!board.IsSuccess)
            {
                return BadRequest("Board with this name for this user already exists!");
            }
            if (board.Value == null)
                return NotFound($"Board wasn't created!");
            return Ok(board.Value);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<BoardDTO>>> GetBySearchQuery([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query must be at least 1 characters long.");
            }

            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (username is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var boards = await _boardService.GetBySearchFilter(username, query);
            if (boards.IsFailed)
            {
                return BadRequest(boards.Errors.Select(e => e.Message).ToList());
            }
            if (boards == null || boards.Value.Count == 0)
            {
                return NotFound();
            }
            return Ok(boards.Value);
        }

        [HttpGet("{name}/{username}")]
        public async Task<ActionResult<List<BoardDTO>>> GetByNameAndUsername(string name, string username)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("");
            }

            var board = await _boardService.GetResultByNameAndOwnerUsername(name, username);
            if (board.IsFailed)
            {
                return BadRequest(board.Errors.Select(e => e.Message).ToList());
            }
            if (board == null)
            {
                return NotFound();
            }
            return Ok(board.Value);
        }

        [HttpGet("{name}/{ownerUsername}/members")]
        public async Task<ActionResult<List<BoardMemberDTO>>> GetMembers(string name, string ownerUsername)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ownerUsername))
            {
                return BadRequest("");
            }

            var requestingUsername = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (requestingUsername is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var board = await _boardService.GetByNameAndOwnerUsername(name, ownerUsername);
            if (board == null)
            {
                return NotFound("Board doesn't exist.");
            }

            if (!string.Equals(requestingUsername, ownerUsername, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only the board owner can view the member list.");
            }

            var members = await _userBoardService.GetMembers(board.Id);
            return Ok(members);
        }

        [HttpDelete("{name}/{username}")]
        public async Task<ActionResult> Delete(string name, string username)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("");
            }

            try
            {
                await _boardService.Delete(name, username);
                return Ok(new { Message = "Board deleted successfully." });
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

        [HttpPut]
        public async Task<ActionResult<BoardDTO>> Update([FromBody] BoardUpdateDTO boardDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid board!");
            }

            var updatedBoard = await _boardService.Update(boardDTO);
            if (updatedBoard.IsFailed)
            {
                return BadRequest("Couldn't update!");
            }
            if (updatedBoard == null)
            {
                return NotFound();
            }
            return Ok(updatedBoard.Value);
        }

        [HttpPost("collaborator/add/{username}")]
        public async Task<ActionResult> AddCollaboratorToBoard([FromBody] BoardDTO boardDto, string username)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(username))
            {
                return BadRequest();
            }
            try
            {
                var result = await _boardService.AddCollaboratorToBoard(boardDto, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Collaborator added successfully." });
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

        [HttpPost("collaborator/remove/{username}")]
        public async Task<ActionResult> RemoveCollaboratorFromBoard([FromBody] BoardDTO boardDto, string username)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(username))
            {
                return BadRequest();
            }
            try
            {
                var result = await _boardService.RemoveCollaboratorFromBoard(boardDto, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Collaborator deleted successfully." });
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

        [HttpPost("favorite/{name}/{ownerUsername}")]
        public async Task<ActionResult> ToggleFavorite(string name, string ownerUsername)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ownerUsername))
            {
                return BadRequest();
            }

            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (username is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _boardService.ToggleFavorite(name, ownerUsername, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(new { Message = "Favorite toggled successfully." });
        }

        [HttpPost("archive/{name}/{username}")]
        public async Task<ActionResult> Archive(string name, string username)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
            {
                return BadRequest();
            }

            var requestingUsername = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (requestingUsername is null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _boardService.Archive(name, username, requestingUsername);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(new { Message = "Board archived successfully." });
        }

        [HttpPost("leave")]
        public async Task<ActionResult> LeaveBoard([FromBody] BoardDTO boardDto)
        {
            var username = HttpContext.User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Token does not contain required username.");
            }

            var result = await _boardService.LeaveBoard(boardDto, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok();
        }
    }
}