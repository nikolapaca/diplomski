using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FakeTrello.Service
{
    public class CardImageService : ICardImageService
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private readonly ICardImageRepository _cardImageRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IUserService _userService;
        private readonly IUserBoardService _userBoardService;
        private readonly IBoardActivityService _boardActivityService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public CardImageService(
            ICardImageRepository cardImageRepository,
            ICardRepository cardRepository,
            IUserService userService,
            IUserBoardService userBoardService,
            IBoardActivityService boardActivityService,
            IMapper mapper,
            IWebHostEnvironment environment)
        {
            _cardImageRepository = cardImageRepository;
            _cardRepository = cardRepository;
            _userService = userService;
            _userBoardService = userBoardService;
            _boardActivityService = boardActivityService;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task<Result<CardImageDTO>> Upload(int cardId, IFormFile file, string username)
        {
            if (file == null || file.Length == 0)
                return Result.Fail("No file was uploaded.");

            if (file.Length > MaxFileSizeBytes)
                return Result.Fail("Image is too large (max 5 MB).");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return Result.Fail("Only jpg, jpeg, png, gif, and webp images are allowed.");

            var user = await _userService.GetUserByUsername(username);
            if (user == null)
                return Result.Fail("This user doesn't exist!");

            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
                return Result.Fail("You don't have access to this board.");

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var cardFolder = Path.Combine(webRoot, "uploads", "cards", cardId.ToString());
            Directory.CreateDirectory(cardFolder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var absolutePath = Path.Combine(cardFolder, storedFileName);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/cards/{cardId}/{storedFileName}";

            var image = new CardImage
            {
                CardId = cardId,
                FileName = file.FileName,
                FilePath = relativePath,
                UploadedByUserId = user.Id,
                UploadedAt = DateTime.UtcNow
            };

            var created = await _cardImageRepository.Create(image);
            created.UploadedByUser = user;

            await _boardActivityService.Create(
                boardId: card.CardList.BoardId,
                creatingUserId: user.Id,
                type: ActivityType.ATTACHMENT_ADDED,
                message: $"{user.Username} added attachment '{file.FileName}' to card '{card.Name}'.",
                cardId: card.Id
            );

            return Result.Ok(_mapper.Map<CardImage, CardImageDTO>(created));
        }

        public async Task<Result<List<CardImageDTO>>> GetByCard(int cardId, string username)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
                return Result.Fail("You don't have access to this board.");

            var images = await _cardImageRepository.GetByCardId(cardId);

            return Result.Ok(_mapper.Map<List<CardImageDTO>>(images));
        }

        public async Task<Result> Delete(int imageId, string username)
        {
            var image = await _cardImageRepository.GetById(imageId);
            if (image == null)
                return Result.Fail("Image doesn't exist.");

            var card = await _cardRepository.GetById(image.CardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            var isUploader = image.UploadedByUser != null &&
                string.Equals(image.UploadedByUser.Username, username, StringComparison.OrdinalIgnoreCase);
            var isBoardOwner = await _userBoardService.IsUserOwnerOfBoard(username, card.CardList.BoardId);

            if (!isUploader && !isBoardOwner)
                return Result.Fail("You can only delete images you uploaded.");

            var deletingUser = await _userService.GetUserByUsername(username);

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var absolutePath = Path.Combine(webRoot, image.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            await _cardImageRepository.Delete(imageId);

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            if (deletingUser != null)
            {
                await _boardActivityService.Create(
                    boardId: card.CardList.BoardId,
                    creatingUserId: deletingUser.Id,
                    type: ActivityType.ATTACHMENT_REMOVED,
                    message: $"{deletingUser.Username} removed attachment '{image.FileName}' from card '{card.Name}'.",
                    cardId: card.Id
                );
            }

            return Result.Ok();
        }
    }
}