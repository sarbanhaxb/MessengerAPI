using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    // DTO для отправки сообщения
    public class MessageRequest
    {
        // ID получателя
        [Required(ErrorMessage = "RecipientId обязателен")]
        public string RecipientId { get; set; } = null!;

        // Текст сообщения
        [Required(ErrorMessage = "Текст сообщения обязателен")]
        public string Text { get; set; } = null!;
    }
}
