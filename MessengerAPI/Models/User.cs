using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessengerAPI.Models
{
    public class User
    {
        [BsonId] // Обознчение, что это Id документа для MongoDB
        [BsonRepresentation(BsonType.ObjectId)] // Конвертирует ObjectId в строку для удобства работы.
                                                // В MongoDB ObjectId выглядит примерно так: "507f1f77bcf86cd799439011".
                                                // BsonRepresentation конвертирует Id из MongoDB в Id С#. В данном случае в string.
        public string? Id { get; set; }

        // Далее BsonElement указывает название поля в MongoDB
        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("password")]
        public string Password { get; set; } = null!;

        [BsonElement("avatar")]
        public string? Avatar { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "offline";

        [BsonElement("position")]
        public string? Position { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("passwordResetToken")]
        public string? PasswordResetToken { get; set; }

        [BsonElement("passwordResetExpires")]
        public DateTime? PasswordResetExpires { get; set; }
    }
}
