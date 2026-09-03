using Pivot.DTO;
using Pivot.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pivot.Controller
{
    [Authorize]
    [Controller]
    [Route("api/cardLists")]
    public class CardListController : ControllerBase
    {
        private readonly ICardListService _cardListService;
        public CardListController(ICardListService cardListService)
        {
            _cardListService = cardListService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CardListDTO cardList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var username = HttpContext.User.FindFirst("username")?.Value;
            var cardListDto = await _cardListService.Create(cardList, username);
            if (!cardListDto.IsSuccess)
            {
                return BadRequest(cardListDto.Errors.First().Message);
            }
            if (cardListDto.Value == null)
                return NotFound($"Card list wasn't created!");
            return Ok(cardListDto.Value);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<CardListDTO>>> GetAllListsByBoard([FromQuery] string boardName, [FromQuery] string boardOwnerUsername)
        {
            if (string.IsNullOrEmpty(boardName) && string.IsNullOrEmpty(boardOwnerUsername))
            {
                return BadRequest();
            }

            var list = await _cardListService.GetByBoardNameAndBoardOwner(boardName, boardOwnerUsername);
            if (!list.IsSuccess)
            {
                return BadRequest();
            }
            if (list.Value == null)
                return NotFound($"No lists found");
            return Ok(list.Value);
        }

        [HttpDelete("{listId:int}")]
        public async Task<ActionResult> Delete(int listId)
        {
            if (listId == 0)
            {
                return BadRequest();
            }

            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardListService.Delete(listId, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "CardList deleted successfully." });
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
        public async Task<ActionResult> Update([FromBody] CardListDTO cardListDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("List is invalid!");
            }

            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardListService.Update(cardListDTO, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "CardList updated successfully." });
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

        [HttpPost("pin/{listId:int}")]
        public async Task<ActionResult> TogglePin(int listId)
        {
            if (listId == 0)
            {
                return BadRequest();
            }

            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardListService.TogglePin(listId, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "CardList pin toggled successfully." });
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

        [HttpPost("reorderList/{targetIndex}")]
        public async Task<ActionResult> MoveList([FromBody] CardListDTO list, int targetIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("");
            }
            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardListService.MoveList(list, targetIndex, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card moved successfully." });
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