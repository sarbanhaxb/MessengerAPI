using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessengerAPI.Services;

namespace MessengerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;

        public ChatsController(IChatService chatService, IUserService userService, IMessageService messageService)
        {
            _chatService = chatService;
            _userService = userService;
            _messageService = messageService;
        }

        #region Получить все чаты пользователя
        // GET /api/chats
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { success = false, message = "Неавторизован" });
                }

                var chats = await _chatService.GetUserChatsAsync(currentUserId);

                var chatsList = new List<object>();

                foreach (var chat in chats)
                {
                    // Получение Id собеседника
                    var otherUserId = chat.User1Id == currentUserId ? chat.User2Id : chat.User1Id;

                    // Получение данных собеседника
                    var otherUser = await _userService.GetByIdAsync(otherUserId);

                    if (otherUser == null) continue;

                    // Получение последнего сообщения (если есть)
                    object? lastMessageInfo = null;
                    if (!string.IsNullOrEmpty(chat.LastMessageId))
                    {
                        var lastMessage = await _messageService.GetByIdAsync(chat.LastMessageId);
                        if (lastMessage != null)
                        {
                            lastMessageInfo = new
                            {
                                id = lastMessage.Id,
                                text = lastMessage.Text,
                                senderId = lastMessage.SenderId,
                                createdAt = lastMessage.CreatedAt,
                                isRead = lastMessage.IsRead
                            };
                        }
                    }
                    chatsList.Add(new
                    {
                        chatId = chat.Id,
                        user = new
                        {
                            id = otherUser.Id,
                            name = otherUser.Name,
                            email = otherUser.Email,
                            avatar = otherUser.Avatar,
                            status = otherUser.Status,
                            position = otherUser.Position
                        },
                        lastMessage = lastMessageInfo,
                        updatedAt = chat.UpdatedAt
                    });
                }
                return Ok(new { success = true, chats = chatsList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка получения чатов",
                    error = ex.Message
                });
            }
        }
        #endregion
    }
}
