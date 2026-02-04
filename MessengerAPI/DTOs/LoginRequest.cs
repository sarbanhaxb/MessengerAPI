using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    /// <summary>
    /// DTO для входа
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = null!;
    }
}
