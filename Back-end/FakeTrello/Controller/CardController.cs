using FakeTrello.DTO;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeTrello.Controller
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
                return BadRequest();
            }
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var userId))
                return Unauthorized();

            var cardDto = await _cardService.Create(listId, newCard, userId);
            if (!cardDto.IsSuccess)
            {
                return BadRequest();
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
                await _cardService.Delete(id);
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
                await _cardService.Update(cardDto);
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
                await _cardService.AssignCardToUser(cardDto, username, creatingUserUsername);
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
            if(cardId == 0)
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
                await _cardService.UnassignCardToUser(cardDto, username, unassigningUserUsername);
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

        [HttpPost("reorderInsideList/{newIndex}")]
        public async Task<ActionResult> MoveCardInsideList([FromBody] CardDTO cardDto, int newIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("");
            }
            try
            {
                await _cardService.ReorderCardInsideList(cardDto.Id, newIndex);
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
                await _cardService.ReorderCardOutsideList(cardDto.Id,targetListId, targetIndex);
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
