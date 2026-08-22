using FakeTrello.DTO;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FakeTrello.Service.Contract
{
    public interface ICardImageService
    {
        Task<Result<CardImageDTO>> Upload(int cardId, IFormFile file, string username);
        Task<Result<List<CardImageDTO>>> GetByCard(int cardId, string username);
        Task<Result> Delete(int imageId, string username);
    }
}
