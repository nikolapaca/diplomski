using FakeTrello.Auth;
using FakeTrello.Data.Contract;
using FakeTrello.Data;
using FakeTrello.Mapper;
using FakeTrello.Repository;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;

namespace FakeTrello
{
    public static class Startup
    {
        public static IServiceCollection ConfigureAuth(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            SetupCore(services);
            SetupInfrastructure(services);
            return services;
        }

        private static void SetupCore(IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenGenerator, JWTGenerator>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IBoardService, BoardService>();
            services.AddScoped<IUserBoardService, UserBoardService>();
            services.AddScoped<ICardListService, CardListService>();
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICollaboratorService, CollaboratorService>();
            services.AddScoped<ICardAssigneeService, CardAssigneeService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBoardActivityService, BoardActivityService>();
        }

        private static void SetupInfrastructure(IServiceCollection services)
        {
            services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
            services.AddScoped(typeof(IBoardRepository), typeof(BoardRepository));
            services.AddScoped(typeof(IUserBoardRepository), typeof(UserBoardRepository));
            services.AddScoped(typeof(ICardListRepository), typeof(CardListRepository));
            services.AddScoped(typeof(ICardRepository), typeof(CardRepository));
            services.AddScoped(typeof(ICardAssigneeRepository), typeof(CardAssigneeRepository));
            services.AddScoped(typeof(INotificationRepository), typeof(NotificationRepository));
            services.AddScoped(typeof(IBoardActivityRepository), typeof(BoardActivityRepository));
        }
    }
}
