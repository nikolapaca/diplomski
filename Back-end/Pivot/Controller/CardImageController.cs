using Pivot.DTO;
using Pivot.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pivot.Controller
{
    [Route("api/card-images")]
    [ApiController]
    [Authorize]
    public class CardImageController : ControllerBase
    {
        private readonly ICardImageService _cardImageService;

        public CardImageController(ICardImageService cardImageService)
        {
            _cardImageService = cardImageService;
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<ActionResult<List<CardImageDTO>>> GetByCard(int cardId)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _cardImageService.GetByCard(cardId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpPost("card/{cardId:int}")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<CardImageDTO>> Upload(int cardId, IFormFile file)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _cardImageService.Upload(cardId, file, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpDelete("{imageId:int}")]
        public async Task<ActionResult> Delete(int imageId)
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _cardImageService.Delete(imageId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok();
        }
    }
}
