using Pivot.DTO;
using Pivot.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pivot.Controller
{
    [Route("api/activities")]
    [ApiController]
    [Authorize]
    public class BoardActivityController : ControllerBase
    {
        private readonly IBoardActivityService _activityService;

        public BoardActivityController(IBoardActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpGet("{ownerUsername}/{boardName}")]
        public async Task<ActionResult<List<BoardActivityDTO>>> GetByBoard(
            string ownerUsername,
            string boardName)
        {
            var result = await _activityService.GetByBoard(boardName, ownerUsername);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<ActionResult<List<BoardActivityDTO>>> GetByCard(int cardId)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _activityService.GetByCard(cardId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }
    }
}