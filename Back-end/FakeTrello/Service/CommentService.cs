using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;

namespace FakeTrello.Service
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IUserService _userService;
        private readonly IUserBoardService _userBoardService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public CommentService(
            ICommentRepository commentRepository,
            ICardRepository cardRepository,
            IUserService userService,
            IUserBoardService userBoardService,
            INotificationService notificationService,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _cardRepository = cardRepository;
            _userService = userService;
            _userBoardService = userBoardService;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<Result<CommentDTO>> Create(int cardId, string text, string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
                return Result.Fail("This user doesn't exist!");

            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
                return Result.Fail("You don't have access to this board.");

            var comment = new Comment
            {
                CardId = cardId,
                UserId = user.Id,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _commentRepository.Create(comment);
            created.User = user;

            foreach (var assignee in card.Assignees)
            {
                if (assignee.UserId == user.Id)
                    continue;

                await _notificationService.Create(
                    recipientUserId: assignee.UserId,
                    creatingUserId: user.Id,
                    type: NotificationType.NEW_COMMENT_ON_CARD,
                    message: $"{username} commented on card '{card.Name}'.",
                    boardId: card.CardList.BoardId,
                    cardId: card.Id
                );
            }

            return Result.Ok(_mapper.Map<Comment, CommentDTO>(created));
        }

        public async Task<Result<List<CommentDTO>>> GetByCard(int cardId, string username)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
                return Result.Fail("You don't have access to this board.");

            var comments = await _commentRepository.GetByCardId(cardId);

            return Result.Ok(_mapper.Map<List<CommentDTO>>(comments));
        }

        public async Task<Result> Delete(int commentId, string username)
        {
            var comment = await _commentRepository.GetById(commentId);
            if (comment == null)
                return Result.Fail("Comment doesn't exist.");

            var card = await _cardRepository.GetById(comment.CardId);
            if (card == null)
                return Result.Fail("Card doesn't exist.");

            var isAuthor = string.Equals(comment.User.Username, username, StringComparison.OrdinalIgnoreCase);
            var isBoardOwner = await _userBoardService.IsUserOwnerOfBoard(username, card.CardList.BoardId);

            if (!isAuthor && !isBoardOwner)
                return Result.Fail("You can only delete your own comments.");

            await _commentRepository.Delete(commentId);

            return Result.Ok();
        }
    }
}
