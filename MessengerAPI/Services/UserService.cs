using MessengerAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MessengerAPI.Services
{

    /// <summary>
    /// Реализация IUserService
    /// </summary>
    public class UserService : IUserService
    {
        // Ссылка на коллекцию "users" в MongoDB
        private readonly IMongoCollection<User> _users;


        /// <summary>
        /// Конструктор - вызывается автоматически при создании сервиса
        /// </summary>
        /// <param name="settings">IOptions<DatabaseSettings> - автоматически подтягивает настройки из фалйа appsettings.json</param>
        public UserService(IOptions<DatabaseSettings> settings)
        {
            // Создание клиента MongoDB
            var client = new MongoClient(settings.Value.ConnectionString);

            // Подключение к БД
            var database = client.GetDatabase(settings.Value.DatabaseName);

            // Получение коллекции users
            _users = database.GetCollection<User>(settings.Value.UsersCollectionName);
        }

        // Создать нового пользователя
        public async Task<User> CreateAsync(User user)
        {
            await _users.InsertOneAsync(user);
            return user;
        }

        // Получить всех пользователей, кроме текущего
        public async Task<List<User>> GetAllUsersAsync(string excludeUserId) => await _users.Find(u => u.Id != excludeUserId).ToListAsync();

        // Получить пользователя по email
        public async Task<User?> GetByEmailAsync(string email) => await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

        // Получить пользователя по ID
        public async Task<User?> GetByIdAsync(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        // Обновить данные пользователя по ID
        public async Task UpdateAsync(string id, User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _users.ReplaceOneAsync(u => u.Id == id, user);
        }

        // Обновить статус User
        public async Task UpdateStatusAsync(string id, string status)
        {
            var update = Builders<User>.Update
                .Set(u => u.Status, status)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            await _users.UpdateOneAsync(u => u.Id == id, update);
        }

        // Восстановление пароля
        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return false;

            // Генерация токена (32 символа)
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Токен истекает через 15 минут
            var expires = DateTime.UtcNow.AddMinutes(15);

            // Сохранение токена
            var update = Builders<User>.Update
                .Set(u => u.PasswordResetToken, token)
                .Set(u => u.PasswordResetExpires, expires);

            await _users.UpdateOneAsync(u => u.Id == user.Id, update);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await GetByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            if (user.PasswordResetToken != token)
            {
                return false;
            }

            if (user.PasswordResetExpires < DateTime.UtcNow)
            {
                return false;
            }

            // Проверка токена и срока действия
            if (user.PasswordResetToken?.Trim() != token?.Trim() ||
                user.PasswordResetExpires < DateTime.UtcNow)
            {
                return false;
            }

            if (user.PasswordResetExpires < DateTime.UtcNow)
            {
                return false;
            }

            // Хеширование нового пароля
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Обновление пароля + очистка токена
            var update = Builders<User>.Update
                .Set(u => u.Password, hashedPassword)
                .Set(u => u.PasswordResetToken, (string?)null)
                .Set(u => u.PasswordResetExpires, (DateTime?)null);

            await _users.UpdateOneAsync(u => u.Id == user.Id, update);

            return true;
        }


    }
}
