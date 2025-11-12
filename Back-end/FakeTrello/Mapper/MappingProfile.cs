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
            CreateMap<CardDTO, Card>().ReverseMap().ForMember(
                dest => dest.AssignedUserUsername,
                opt => opt.MapFrom(src => src.User.Username)
            );
        }
    }
}
