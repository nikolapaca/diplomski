using FakeTrello.DTO;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeTrello.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            this._notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationDTO>>> GetMyNotifications()
        {
            var username = HttpContext.User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Token does not contain required username.");
            }

            var result = await _notificationService.GetMyNotifications(username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<List<NotificationDTO>>> GetMyUnreadNotifications()
        {
            var username = HttpContext.User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Token does not contain required username.");
            }

            var result = await _notificationService.GetMyUnreadNotifications(username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok(result.Value);
        }

        [HttpPut("{notificationId:int}/read")]
        public async Task<ActionResult> MarkAsRead(int notificationId)
        {
            var username = HttpContext.User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Token does not contain required username.");
            }

            var result = await _notificationService.MarkAsRead(notificationId, username);

            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            return Ok();
        }
    }
}
