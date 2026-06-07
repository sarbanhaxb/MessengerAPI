using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    public class UpdateMessageRequest
    {
        [Required, MinLength(1)]
        public string Text { get; set; }
    }
}
