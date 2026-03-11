using GameCore.Interfaces;
using UnoGame.Events;
using UnoGame.Database;
using UnoGame.Models.States;

namespace UnoGame.Backend.Services
{
    /// <summary>
    /// Oyun event'lerini dinleyip MongoDB'ye game document yazan servis.
    /// JsonRpcBackendService ile aynı IAsyncGameEventListener pattern'ini kullanır.
    /// </summary>
    public class GameInfoService : IAsyncGameEventListener
    {
        private readonly string _gameId;
        private readonly MongoGameRepository _repository;
        private readonly UnoGameState _gameState;
        private readonly IReadOnlyList<IPlayer> _players;

        public GameInfoService(
            string gameId,
            MongoGameRepository repository,
            UnoGameState gameState,
            IReadOnlyList<IPlayer> players)
        {
            _gameId = gameId;
            _repository = repository;
            _gameState = gameState;
            _players = players;
        }

        public async Task OnGameEventAsync(IGameAction action)
        {
            try
            {
                switch (action)
                {
                    case UnoGameStartedEvent evt:
                        await HandleGameStartedAsync(evt);
                        break;

                    case CardPlayedEvent:
                    case CardDrawnEvent:
                    case TurnChangedEvent:
                        await _repository.UpdateTimestampAsync(_gameId);
                        break;

                    case UnoGameEndedEvent:
                        await _repository.UpdateStatusAsync(_gameId, "COMPLETED");
                        Console.WriteLine("📦 MongoDB: Oyun COMPLETED olarak güncellendi.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GameInfoService hatası: {ex.Message}");
            }
        }

        private async Task HandleGameStartedAsync(UnoGameStartedEvent evt)
        {
            var playerDocs = _players.Select(p => new GamePlayerDocument
            {
                UserId = p.Id,
                Username = p.Name,
                Status = "ACTIVE",
                JoinedAt = p.JoinedAt
            }).ToList();

            var document = new GameDocument
            {
                GameId = _gameId,
                Name = "UNO",
                Status = "IN_PROGRESS",
                MinPlayers = 2,
                MaxPlayers = 10,
                CreatedBy = _players.First().Id,
                Players = playerDocs,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.CreateGameAsync(document);
            Console.WriteLine($"📦 MongoDB: Oyun document'ı oluşturuldu → game_id: {_gameId}");
        }
    }
}
