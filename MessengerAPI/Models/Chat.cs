using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessengerAPI.Models
{
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // ID первого участника
        [BsonElement("user1Id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string User1Id { get; set; } = null!;

        // ID второго участника
        [BsonElement("user2Id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string User2Id { get; set;} = null!;

        // ID последнего сообщения в чате (для быстрого доступа к превью)
        [BsonElement("lastMessageId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? LastMessageId { get; set;}

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt {  get; set; } = DateTime.UtcNow;
    }
}
