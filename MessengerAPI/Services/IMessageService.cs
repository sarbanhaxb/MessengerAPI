using MessengerAPI.Models;

namespace MessengerAPI.Services
{
    /// <summary>
    /// Интерфейс для работы с сообщениями
    /// </summary>
    public interface IMessageService
    {
        // Получить все сообщения между двумя пользоватялемя
        Task<List<Message>> GetChatMessagesAsync(string userId, string recipientId);

        // Создать новое сообщение
        Task<Message> CreateAsync(Message message);

        // Отметить все сообщения как прочитанные
        Task MarkAsReadAsync(string senderId, string recipientId);

        // Получить сообщение по Id
        Task<Message?> GetByIdAsync(string messageId);
    }
}
