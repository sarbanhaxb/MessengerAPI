using MessengerAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MessengerAPI.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<Chat> _chats;

        public ChatService(IOptions<DatabaseSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _chats = database.GetCollection<Chat>(settings.Value.ChatsCollectionName);

            CreateIndexes();
        }

        /// <summary>
        /// Создание индексов для оптимизации запросов
        /// </summary>
        private void CreateIndexes()
        {
            // Индекс для поиска чата по двум пользователям
            var indexKeys = Builders<Chat>.IndexKeys
                .Ascending(c => c.User1Id)
                .Ascending(c => c.User2Id);
            _chats.Indexes.CreateOne(new CreateIndexModel<Chat>(indexKeys));

            // Индекс для сортировки чатов по времени обновления
            var updatedAtIndex = Builders<Chat>.IndexKeys.Descending(c => c.UpdatedAt);
            _chats.Indexes.CreateOne(new CreateIndexModel<Chat>(updatedAtIndex));
        }

        /// <summary>
        /// Получение чата между двумя пользователями
        /// </summary>
        public async Task<Chat?> GetChatAsync(string user1Id, string user2Id)
        {
            // Нормализация порядка ID (меньший всегда первый)
            // Гарантия что чат user1-user2 и "user2-user1" - это один и тот же документ
            var (userId1, userId2) = user1Id.CompareTo(user2Id) < 0 ? (user1Id, user2Id) : (user2Id, user1Id);

            return await _chats.Find(c => c.User1Id == user1Id && c.User2Id == user2Id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Создать или обновить чат при отправке сообщения
        /// </summary>
        public async Task<Chat?> CreateOrUpdateChatAsync(string user1Id, string user2Id, string lastMessageId)
        {
            // Нормализация порядка
            var (userId1, userId2) = user1Id.CompareTo(user2Id) < 0 ? (user1Id, user2Id) : (user2Id, user1Id);

            // Проверка существования чата
            var existingChat = await GetChatAsync(userId1, userId2);

            if (existingChat != null)
            {
                // Обновление существующего чата
                var update = Builders<Chat>.Update
                    .Set(c => c.LastMessageId, lastMessageId)
                    .Set(c => c.UpdatedAt, DateTime.UtcNow);

                await _chats.UpdateOneAsync(c => c.Id == existingChat.Id, update);

                existingChat.LastMessageId = lastMessageId;
                existingChat.UpdatedAt = DateTime.UtcNow;
                return existingChat;
            }
            else
            {
                // Создание нового чата
                var newChat = new Chat
                {
                    User1Id = userId1,
                    User2Id = userId2,
                    LastMessageId = lastMessageId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                await _chats.InsertOneAsync(newChat);
                return newChat;
            }
        }

        /// <summary>
        /// Получить все чаты пользователя
        /// </summary>
        public async Task<List<Chat>> GetUserChatsAsync(string userId) => await _chats.Find(c => c.User1Id == userId || c.User2Id == userId).SortByDescending(c => c.UpdatedAt).ToListAsync();

    }
}
