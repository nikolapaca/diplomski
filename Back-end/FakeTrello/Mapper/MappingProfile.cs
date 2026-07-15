using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;

namespace FakeTrello.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile() { 
            CreateMap<UserDTO, User>().ReverseMap();
            CreateMap<BoardDTO, Board>().ReverseMap();
            CreateMap<CardListDTO, CardList>().ReverseMap();
            CreateMap<Notification, NotificationDTO>()
            .ForMember(
                dest => dest.BoardName,
                opt => opt.MapFrom(src =>
                    src.Board != null ? src.Board.Name : null)
            )
            .ForMember(
                dest => dest.BoardOwnerUsername,
                opt => opt.MapFrom(src =>
                    src.Board != null
                        ? src.Board.UserBoards
                        .Where(ub => ub.UserRole == UserRole.OWNER)
                        .Select(ub => ub.User.Username)
                        .FirstOrDefault()
                        : null)
            );
            CreateMap<NotificationDTO, Notification>();

            CreateMap<CardDTO, Card>().ReverseMap().ForMember(
                dest => dest.AssignedUserUsernames,
                opt => opt.MapFrom(src => src.Assignees.Select(a => a.UserBoard.User.Username).ToList())
            );

            CreateMap<BoardActivity, BoardActivityDTO>()
                .ForMember(
                    dest => dest.CreatingUsername,
                    opt => opt.MapFrom(src => src.CreatingUser.Username)
                );
            CreateMap<BoardActivityDTO, BoardActivity>();
        }
    }
}
