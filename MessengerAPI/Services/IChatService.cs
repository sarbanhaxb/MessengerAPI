using MessengerAPI.Models;

namespace MessengerAPI.Services
{
    /// <summary>
    /// Интерфейс для работы с чатами
    /// </summary>
    public interface IChatService
    {
        // Получить чат между пользователями
        Task<Chat?> GetChatAsync(string user1Id, string user2Id);

        // Создать или обновить чат
        Task<Chat?> CreateOrUpdateChatAsync(string user1Id, string user2Id, string lastMessageId);

        // Получить все чаты пользователя
        Task<List<Chat>> GetUserChatsAsync(string userId);
    }
}
