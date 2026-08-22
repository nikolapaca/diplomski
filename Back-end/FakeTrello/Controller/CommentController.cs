using FakeTrello.DTO;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeTrello.Controller
{
    [Route("api/comments")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<ActionResult<List<CommentDTO>>> GetByCard(int cardId)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _commentService.GetByCard(cardId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpPost("card/{cardId:int}")]
        public async Task<ActionResult<CommentDTO>> Create(int cardId, [FromBody] CommentDTO commentDto)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _commentService.Create(cardId, commentDto.Text, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpDelete("{commentId:int}")]
        public async Task<ActionResult> Delete(int commentId)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _commentService.Delete(commentId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok();
        }
    }
}
