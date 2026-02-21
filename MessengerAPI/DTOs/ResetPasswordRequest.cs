using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Минимум 6 символов")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Токен обязателен")]
        public string Token { get; set; } = null!;
    }
}
