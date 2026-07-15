using Microsoft.AspNetCore.Authorization;

namespace FakeTrello.Hub
{
    [Authorize]
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.FindFirst("username")?.Value;

            Console.WriteLine($"SignalR connected user: {username}");

            if (!string.IsNullOrEmpty(username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{username}");
                Console.WriteLine($"Added to group user-{username}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.FindFirst("username")?.Value;

            if (!string.IsNullOrEmpty(username))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{username}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinBoardGroup(string ownerUsername, string boardName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetBoardGroupName(ownerUsername, boardName));
        }

        public async Task LeaveBoardGroup(string ownerUsername, string boardName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroupName(ownerUsername, boardName));
        }

        public static string GetBoardGroupName(string ownerUsername, string boardName)
        {
            return $"board-{ownerUsername}-{boardName}";
        }
    }
}