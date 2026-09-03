using Pivot.DTO;
using Pivot.Service;
using Pivot.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pivot.Controller
{
    [Authorize]
    [Controller]
    [Route("api/cards")]
    public class CardController : ControllerBase
    {
        private readonly ICardService _cardService;
        public CardController(ICardService cardService)
        {
            _cardService = cardService;
        }

        [HttpPost("{listId:int}")]
        public async Task<ActionResult<CardDTO>> Create(int listId, [FromBody] CardDTO newCard)
        {

            ModelState.Remove("Description");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var userId))
                return Unauthorized();

            var cardDto = await _cardService.Create(listId, newCard, userId);
            if (!cardDto.IsSuccess)
            {
                return BadRequest(cardDto.Errors.First().Message);
            }
            if (cardDto.Value == null)
                return NotFound($"Card list wasn't created!");
            return Ok(cardDto.Value);
        }

        [HttpGet("{listId:int}")]
        public async Task<ActionResult<List<CardDTO>>> GetByListId(int listId)
        {
            if (listId == 0)
            {
                return BadRequest("");
            }
            try
            {
                var cards = await _cardService.GetByListId(listId);
                return Ok(cards.Value);
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

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (id == 0)
            {
                return BadRequest("");
            }

            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.Delete(id, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card deleted successfully." });
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

        /*[HttpPost("assign/{assigneeUsername}")]
        public async Task<ActionResult> AssignToUser([FromBody] CardDTO cardDTO, string assigneeUsername)
        {
            if(!ModelState.IsValid || string.IsNullOrEmpty(assigneeUsername))
            {
                return BadRequest();
            }

        }*/

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] CardDTO cardDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid card!");
            }
            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.Update(cardDto, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card updated successfully." });
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

        [HttpPost("assign/{username}")]
        public async Task<ActionResult> AssignCardToUser([FromBody] CardDTO cardDto, string username)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid card!");
            }
            try
            {
                var creatingUserUsername = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.AssignCardToUser(cardDto, username, creatingUserUsername);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card updated successfully." });
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

        [HttpGet("assignedUser/{cardId}")]
        public async Task<ActionResult<List<UserDTO>>> GetUsersAssignedToCard(int cardId)
        {
            if (cardId == 0)
            {
                return BadRequest("Card doesn't exist!");
            }
            try
            {
                var user = await _cardService.GetUsersAssignedToCard(cardId);
                return Ok(user.Value);
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

        [HttpPost("unassign/{username}")]
        public async Task<ActionResult> RemoveAssignee([FromBody] CardDTO cardDto, string username)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid card!");
            }
            try
            {
                var unassigningUserUsername = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.UnassignCardToUser(cardDto, username, unassigningUserUsername);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card updated successfully." });
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

        [HttpPost("pin/{id:int}")]
        public async Task<ActionResult> TogglePin(int id)
        {
            if (id == 0)
            {
                return BadRequest("");
            }

            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.TogglePin(id, username);

                if (result.IsFailed)
                {
                    return BadRequest(result.Errors.First().Message);
                }

                return Ok(new { Message = "Card pin toggled successfully." });
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

        [HttpPost("reorderInsideList/{newIndex}")]
        public async Task<ActionResult> MoveCardInsideList([FromBody] CardDTO cardDto, int newIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("");
            }
            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.ReorderCardInsideList(cardDto.Id, newIndex, username);

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

        [HttpPost("reorderOutsideList/{targetListId}/{targetIndex}")]
        public async Task<ActionResult> MoveCardOutsideList([FromBody] CardDTO cardDto, int targetListId, int targetIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("");
            }
            try
            {
                var username = HttpContext.User.FindFirst("username")?.Value;
                var result = await _cardService.ReorderCardOutsideList(cardDto.Id, targetListId, targetIndex, username);

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