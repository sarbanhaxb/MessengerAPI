using System.ComponentModel.DataAnnotations;

namespace MessengerAPI.DTOs
{
    public class PasswordResetRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
