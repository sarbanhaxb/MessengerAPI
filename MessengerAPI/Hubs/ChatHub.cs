using MessengerAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MessengerAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUserService _userService;

        //Статический словарь для хранения подключений
        // Ключ: userId, значение: connectionId (уникальный ID WebSocket подключения)
        private static readonly Dictionary<string, string> _connections = new();

        public ChatHub(IUserService userService)
        {
            _userService = userService;
        }

        #region Пользователь подключается
        // Вызывается автоматически при подключении клиента к SignalR
        public override async Task OnConnectedAsync()
        {
            // Извлечение userId из JWT токена
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Сохранение связки userId -> connectionId
                _connections[userId] = Context.ConnectionId;

                // Обновление статуса пользователя в БД на "Online"
                await _userService.UpdateStatusAsync(userId, "online");

                // Сообщение всем подключенным клиентам, что пользователь онлайн
                await Clients.All.SendAsync("UserStatusChange", new
                {
                    userId,
                    status = "online"
                });
            }

            // Вызов базового метода
            await base.OnConnectedAsync();
        }
        #endregion

        #region Пользователь отключается
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(!string.IsNullOrEmpty(userId))
            {
                // Удаление из словаря с подключениями
                _connections.Remove(userId);

                //Обновление статуса на offline
                await _userService.UpdateStatusAsync(userId, "offline");

                // Сообщение всем, что пользователь офлайн
                await Clients.All.SendAsync("UserStatusChange", new
                {
                    userId,
                    status = "offline"
                });
            }
            await base.OnDisconnectedAsync(exception);
        }
        #endregion

        #region Отправка сообщения
        // Клиент вызывает: connection.invoke("SendMessage", recipientId, messageData)
        public async Task SendMessage(string recipientId, object message)
        {
            // Проверка состояния получателя (онлайн/офлайн)
            if (_connections.TryGetValue(recipientId, out var connectionId))
            {
                // Отправляем сообщение только этому пользователю
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
            }
            // Если получатель офлайн, ничего не делаем
        }
        #endregion

        #region Индикатор набора текста
        // Клиент вызывает connection.invoke("Typing", recipientId, true/false)
        public async Task Typing(string recipientId, bool isTyping)
        {
            // Если получатель онлайн, отправляем ему уведомление
            if (_connections.TryGetValue(recipientId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", new { isTyping });
            }
        }
        #endregion
    }
}
