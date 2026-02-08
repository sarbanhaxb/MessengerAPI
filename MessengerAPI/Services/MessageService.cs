using MessengerAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MessengerAPI.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMongoCollection<Message> _messages;
        private readonly IUserService _userService;

        // Конструктор принимает настройки БД и UserService
        // UserService нужен, чтобы загрузить данные отправителя и получателя
        public MessageService(IOptions<DatabaseSettings> settings, IUserService userService)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _messages = database.GetCollection<Message>(settings.Value.MessagesCollectionName);
            _userService = userService;
        }

        //Создать новое сообщение
        public async Task<Message> CreateAsync(Message message)
        {
            // Сохранение сообщения в БД
            await _messages.InsertOneAsync(message);

            // Загружаем данные отправителя и получателя
            // Это нужно, чтобы вернуть клиенту полный объект сообщения
            message.Sender = await _userService.GetByIdAsync(message.SenderId);
            message.Recipient = await _userService.GetByIdAsync(message.RecipientId);

            return message;
        }

        // Получить все сообщения между двумя пользователями
        public async Task<List<Message>> GetChatMessagesAsync(string userId, string recipientId)
        {

            // Создаём сложный фильтр:
            // Нужны сообщения, где:
            // (отправитель = userId И получатель = recipientId)
            // ИЛИ
            // (отправитель = recipientId И получатель = userId)
            var filter = Builders<Message>.Filter.Or(
                Builders<Message>.Filter.And(
                    Builders<Message>.Filter.Eq(m => m.SenderId, userId),
                    Builders<Message>.Filter.Eq(m => m.RecipientId, recipientId)
                ),
                Builders<Message>.Filter.And(
                    Builders<Message>.Filter.Eq(m => m.SenderId, recipientId),
                    Builders<Message>.Filter.Eq(m => m.RecipientId, userId)
                )
            );

            // Выполняем запрос и сортируем по времени создания (старые в начале)
            var messages = await _messages.Find(filter)
                .SortBy(m => m.CreatedAt)
                .ToListAsync();

            // Для каждого сообщения загружаем полные данные пользователей
            // В БД хранятся только ID, а клиенту нужны имена, аватары и т.д.
            foreach(var message in messages)
            {
                message.Sender = await _userService.GetByIdAsync(message.SenderId);
                message.Recipient = await _userService.GetByIdAsync(message.RecipientId);
            }
            return messages;
        }

        // Отметить все сообщения как прочитанные
        public async Task MarkAsReadAsync(string senderId, string recipientId)
        {
            // Фильтр: сообщения от senderId к recipientId, которые ещё не прочитаны
            var filter = Builders<Message>.Filter.And(
                Builders<Message>.Filter.Eq(m => m.SenderId, senderId),
                Builders<Message>.Filter.Eq(m => m.RecipientId, recipientId),
                Builders<Message>.Filter.Eq(m => m.IsRead, false)
            );

            // Обновление: установить IsRead = true
            var update = Builders<Message>.Update.Set(m => m.IsRead, true);

            await _messages.UpdateManyAsync(filter, update);
        }


        // Получить сообщение по Id
        public async Task<Message?> GetByIdAsync(string messageId)
        {
            var message = await _messages.Find(m => m.Id == messageId).FirstOrDefaultAsync();

            if (message != null)
            {
                // Загрузка данных отправителя и получателя
                message.Sender = await _userService.GetByIdAsync(message.SenderId);
                message.Recipient = await _userService.GetByIdAsync(message.RecipientId);
            }

            return message;
        }
    }
}
