namespace UnoGame.Database
{
    /// <summary>
    /// appsettings.json'daki MongoDb bölümüne karşılık gelen ayar modeli.
    /// </summary>
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "uno_game_db";
        public string GamesCollectionName { get; set; } = "games";
    }
}
