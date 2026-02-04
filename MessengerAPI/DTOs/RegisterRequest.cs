using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    /// <summary>
    /// DTO - класс, который описывает, какие данные клиент должен отправить на сервер, например, для регистрации нужны: имя, email, пароль.
    /// Нужны для валидации данных, безопасности, читаемости кода.
    /// </summary>
    // DTO для запроса регистрации
    public class RegisterRequest
    {
        //[Required] - антоация поля, обозначающая его обязательность для ввода. Если клиент не отправит, сервер вернет ошибку 
        [Required(ErrorMessage = "Имя обязательно для ввода")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")] // проверка корректности введенного email по формату
        public string Email { get; set; } = null!;

        [Required(ErrorMessage ="Пароль обязателен")]
        [MinLength(6, ErrorMessage ="Пароль должен быть минимум 6 символов")]
        public string Password { get; set; } = null!;
    }
}
