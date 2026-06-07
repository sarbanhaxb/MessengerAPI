using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MessengerAPI.Models;
using MessengerAPI.DTOs;
using MessengerAPI.Services;
using MessengerAPI.Hubs;

namespace MessengerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Все методы работают только при авторизации
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;
        private readonly IHubContext<ChatHub> _hubContext; // Для отправки real-time сообщений
        private readonly IChatService _chatService;

        // Конструктор принимает сервисы и SignalR Hub Context
        public MessagesController(IMessageService messageService, IUserService userService, IHubContext<ChatHub> hubContext, IChatService chatService)
        {
            _messageService = messageService;
            _userService = userService;
            _hubContext = hubContext; // Для отправки WebSocket сообщений
            _chatService = chatService;
        }

        #region Получить все сообщения чата
        // GET /api/messages/chat/{recipientId}
        [HttpGet("chat/{recipientId}")]
        public async Task<IActionResult> GetChatMessages(string recipientId)
        {
            try
            {
                // Извлечение ID текущего пользовтаеля из JWT токена
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Неавторизован"
                    });
                }

                // Проверяем, существует ли получатель
                var recipient = await _userService.GetByIdAsync(recipientId);
                if(recipient == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Получатель не найден"
                    });
                }

                // Получаем все сообщения между текущими участниками беседы
                var messages = await _messageService.GetChatMessagesAsync(userId, recipientId);

                // Отметка всех сообщений как прочитанных
                await _messageService.MarkAsReadAsync(recipientId, userId);

                // Возвращаем список сообщений с полными данными отправителей
                return Ok(new
                {
                    success = true,
                    messages = messages.Select(m => new
                    {
                        id = m.Id,
                        senderId = m.SenderId,
                        recipientId = m.RecipientId,
                        text = m.Text,
                        isRead = m.IsRead,
                        createdAt = m.CreatedAt,
                        sender = new
                        {
                            id = m.Sender?.Id,
                            name = m.Sender?.Name,
                            email = m.Sender?.Email,
                            avatar = m.Sender?.Avatar,
                        },
                        recipient = new
                        {
                            id = m.Recipient?.Id,
                            name = m.Recipient?.Name,
                            email = m.Recipient?.Email,
                            avatar = m.Recipient?.Avatar
                        },
                        updatedAt = m.UpdatedAt,
                        isEdited = m.EditedAt,
                        editedAt = m.EditedAt
                    })
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка получения сообщений",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Отправить сообщение
        // POST /api/messages/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            try
            {
                // Извлечение ID из текущего пользовательского токена
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if(string.IsNullOrEmpty(senderId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Неавторизован"
                    });
                }

                // Проверка существования получателя
                var recipient = await _userService.GetByIdAsync(request.RecipientId);
                if(recipient == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Получатель не найден"
                    });
                }

                // Создание объекта сообщения
                var message = new Message
                {
                    SenderId = senderId,
                    RecipientId = request.RecipientId,
                    Text = request.Text,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                var createdMessage = await _messageService.CreateAsync(message);

                await _chatService.CreateOrUpdateChatAsync(senderId, request.RecipientId, createdMessage.Id!);

                // Формирование объекта для отправки клиенту
                var messageResponse = new
                {
                    id = createdMessage.Id,
                    senderId = createdMessage.SenderId,
                    recipientId = createdMessage.RecipientId,
                    text = createdMessage.Text,
                    isRead = createdMessage.IsRead,
                    createdAt = createdMessage.CreatedAt,
                    sender = new
                    {
                        id = createdMessage.Sender?.Id,
                        name = createdMessage.Sender?.Name,
                        email = createdMessage.Sender?.Email,
                        avatar = createdMessage.Sender?.Avatar
                    },
                    recipient = new
                    {
                        id = createdMessage.Recipient?.Id,
                        name = createdMessage.Recipient?.Name,
                        email = createdMessage.Recipient?.Email,
                        avatar = createdMessage.Recipient?.Avatar
                    }
                };

                // Отправка сообщения REAL-TIME через SignalR
                // Если получтель онлайн, он моментально получит это сообщение
                await _hubContext.Clients.User(request.RecipientId)
                    .SendAsync("ReceiveMessage", messageResponse);

                // Возврат ответа отправителю
                return CreatedAtAction(nameof(SendMessage), new
                {
                    success = true,
                    message = "Сообщение отправлено",
                    data = messageResponse
                });
            }catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка отправки сообщения",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Редактирование сообщения
        // PUT /api/messages/{messageId}
        [HttpPut("{messageId}")]
        public async Task<IActionResult> UpdateMessage(string messageId, [FromBody] UpdateMessageRequest request)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(senderId))
                    return Unauthorized(new { success = false, message = "Не авторизован" });
                var message = await _messageService.GetByIdAsync(messageId);
                if (message == null)
                    return NotFound(new { success = false, message = "Нет прав" });

                message.Text = request.Text;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;

                var updatedMessage = await _messageService.UpdateAsync(message);

                var messageResponse = new
                {
                    id = updatedMessage.Id,
                    senderId = updatedMessage.SenderId,
                    recipientId = updatedMessage.RecipientId,
                    text = updatedMessage.Text,
                    isEdited = updatedMessage.IsEdited,
                    editedAt = updatedMessage.EditedAt,
                    createdAt = updatedMessage.CreatedAt,
                    sender = new { id = updatedMessage.Sender?.Id, name = updatedMessage.Sender?.Name },
                    recipient = new { id = updatedMessage.Recipient?.Id, name = updatedMessage.Recipient?.Name }
                };

                await _hubContext.Clients.Group(messageId).SendAsync("MessageUpdated", messageResponse);

                return Ok(new
                {
                    success = true,
                    message = "Сообщение отправлено",
                    data = messageResponse
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new {success = false, message = ex.Message});
            }
        }

        #endregion

        #region Удаление сообщения
        // DELETE /api/messages/{messageId}
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(string messageId)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ✅ 1 запрос - получаем сообщение
            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound("Сообщение не найдено");

            // ✅ Проверяем права ДО удаления
            if (message.SenderId != senderId)
                return BadRequest("Нет прав на удаление");

            // ✅ Удаляем
            var result = await _messageService.DeleteMessageAsync(messageId, senderId);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            // ✅ SignalR - recipient уже известен
            await _hubContext.Clients.User(message.RecipientId).SendAsync("MessageDeleted", messageId);

            return Ok(new { success = true, message = "Сообщение удалено" });
        }


        #endregion
    }
}
