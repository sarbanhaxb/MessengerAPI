using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessengerAPI.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // ID отправителя (ссылка на Uder.Id)
        [BsonElement("senderId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SenderId { get; set; } = null!;

        // ID получателя (ссылка на User.Id)
        [BsonElement("recipientId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RecipientId { get; set; } = null!;

        // Текст сообщения
        [BsonElement("text")]
        public string Text { get; set; } = null!;

        // Прочитано ли сообщение
        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonIgnore] // MongoDB будет игнорировать данное поле
        public User? Sender { get; set; } // Полные данные отправителя
        [BsonIgnore]
        public User? Recipient { get; set; } // Полные данные получателя

        [BsonElement("isEdited")]
        public bool IsEdited { get; set; } = false;

        [BsonElement("editedAt")]
        public DateTime? EditedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt {  get; set; } = DateTime.UtcNow;
    }
}
