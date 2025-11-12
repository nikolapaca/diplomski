using FakeTrello.DTO;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace FakeTrello.Controller
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
            var cardListDto = await _cardListService.Create(cardList);
            if (!cardListDto.IsSuccess)
            {
                return BadRequest();
            }
            if (cardListDto.Value == null)
                return NotFound($"Card list wasn't created!");
            return Ok(cardListDto.Value);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<CardListDTO>>> GetAllListsByBoard([FromQuery] string boardName, [FromQuery] string boardOwnerUsername)
        {
            if(string.IsNullOrEmpty(boardName) && string.IsNullOrEmpty(boardOwnerUsername))
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
                await _cardListService.Delete(listId);
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
            if(!ModelState.IsValid)
            {
                return BadRequest("List is invalid!");
            }

            try
            {
                await _cardListService.Update(cardListDTO);
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

        [HttpPost("reorderList/{targetIndex}")]
        public async Task<ActionResult> MoveList([FromBody] CardListDTO list, int targetIndex)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("");
            }
            try
            {
                await _cardListService.MoveList(list, targetIndex);
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
