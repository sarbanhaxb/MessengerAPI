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

        // Конструктор принимает сервисы и SignalR Hub Context
        public MessagesController(IMessageService messageService, IUserService userService, IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _userService = userService;
            _hubContext = hubContext; // Для отправки WebSocket сообщений
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
                        createAt = m.IsRead,
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
                        }
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
    }
}
