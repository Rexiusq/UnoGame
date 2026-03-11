using MongoDB.Driver;

namespace UnoGame.Database
{
    /// <summary>
    /// MongoDB üzerinden GameDocument CRUD operasyonlarını yöneten repository.
    /// </summary>
    public class MongoGameRepository
    {
        private readonly IMongoCollection<GameDocument> _games;

        public MongoGameRepository(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _games = database.GetCollection<GameDocument>(settings.GamesCollectionName);
        }

        /// <summary>
        /// Yeni oyun document'ı oluşturur.
        /// </summary>
        public async Task CreateGameAsync(GameDocument document)
        {
            await _games.InsertOneAsync(document);
        }

        /// <summary>
        /// game_id ile document getirir.
        /// </summary>
        public async Task<GameDocument?> GetByGameIdAsync(string gameId)
        {
            var filter = Builders<GameDocument>.Filter.Eq(d => d.GameId, gameId);
            return await _games.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Oyun status'unu günceller ve updated_at'i yeniler.
        /// </summary>
        public async Task UpdateStatusAsync(string gameId, string status)
        {
            var filter = Builders<GameDocument>.Filter.Eq(d => d.GameId, gameId);
            var update = Builders<GameDocument>.Update
                .Set(d => d.Status, status)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _games.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Oyuncu listesini günceller ve updated_at'i yeniler.
        /// </summary>
        public async Task UpdatePlayersAsync(string gameId, List<GamePlayerDocument> players)
        {
            var filter = Builders<GameDocument>.Filter.Eq(d => d.GameId, gameId);
            var update = Builders<GameDocument>.Update
                .Set(d => d.Players, players)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _games.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Sadece updated_at alanını günceller.
        /// </summary>
        public async Task UpdateTimestampAsync(string gameId)
        {
            var filter = Builders<GameDocument>.Filter.Eq(d => d.GameId, gameId);
            var update = Builders<GameDocument>.Update
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _games.UpdateOneAsync(filter, update);
        }
    }
}
