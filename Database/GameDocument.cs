using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UnoGame.Database
{
    /// <summary>
    /// MongoDB games collection'ındaki document modeli.
    /// Rust tarafındaki GameInfo struct'ına birebir karşılık gelir.
    /// </summary>
    public class GameDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("game_id")]
        public string GameId { get; set; } = "";

        [BsonElement("name")]
        public string Name { get; set; } = "UNO";

        /// <summary>
        /// WAITING, IN_PROGRESS, COMPLETED, CANCELLED
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "WAITING";

        [BsonElement("min_players")]
        public int MinPlayers { get; set; } = 2;

        [BsonElement("max_players")]
        public int MaxPlayers { get; set; } = 10;

        [BsonElement("created_by")]
        public string CreatedBy { get; set; } = "";

        [BsonElement("players")]
        public List<GamePlayerDocument> Players { get; set; } = new();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
