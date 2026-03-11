using MongoDB.Bson.Serialization.Attributes;

namespace UnoGame.Database
{
    /// <summary>
    /// Oyuncu alt-document modeli. 
    /// Rust tarafındaki GamePlayerDoc struct'ına karşılık gelir.
    /// </summary>
    public class GamePlayerDocument
    {
        [BsonElement("user_id")]
        public string UserId { get; set; } = "";

        [BsonElement("username")]
        public string Username { get; set; } = "";

        /// <summary>
        /// ACTIVE, DISCONNECTED, LEFT
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "ACTIVE";

        [BsonElement("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
