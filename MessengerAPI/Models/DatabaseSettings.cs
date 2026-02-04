namespace MessengerAPI.Models
{
    /// <summary>
    /// Класс для хранения настроек подключения к MongoDB
    /// Значения принимаются из appsettings.json
    /// </summary>
    public class DatabaseSettings
    {
        // Строка подклчения к MongoDB
        public string ConnectionString { get; set; } = null!;
        
        // Название базы данных
        public string DatabaseName { get; set; } = null!;

        // Название коллекции пользователей
        public string UsersCollectionName { get; set; } = null!;

        // Название коллекции для сообщений
        public string MessagesCollectionName { get; set; } = null!;

        // Название коллекции для чатов
        public string ChatsCollectionName { get; set; } = null!;
    }
}
