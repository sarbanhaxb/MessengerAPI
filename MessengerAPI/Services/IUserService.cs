using MessengerAPI.Models;

namespace MessengerAPI.Services
{
    /// <summary>
    /// Интерфейс для UserService
    /// Описывает, какие методы должны быть реализованы
    /// </summary>
    public interface IUserService
    {
        // Получить всех пользователей, кроме указанного
        Task<List<User>> GetAllUsersAsync(string excludeUserId);

        // Получить пользователя по ID
        Task<User?> GetByIdAsync(string id);

        // Получить пользователя по email (для входа)
        Task<User?> GetByEmailAsync(string email);

        // Создать нового пользователя 
        Task<User> CreateAsync(User user);

        // Обновить данные пользователя
        Task UpdateAsync(string id, User user);

        // Обновить статус (online / offline)
        Task UpdateStatusAsync(string id, string status);

        Task<bool> RequestPasswordResetAsync(string email);

        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
