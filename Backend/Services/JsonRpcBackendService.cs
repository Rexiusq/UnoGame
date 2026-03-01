using GameCore.Interfaces;
using GameCore.Enums;
using GameCore.Managers;
using UnoGame.Events;
using UnoGame.Backend.Protocol;
using UnoGame.Backend.Models;
using UnoGame.Models.States;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace UnoGame.Backend.Services
{
    /// <summary>
    /// JSON-RPC formatında event'leri backend'e gönderen servis
    /// IAsyncGameEventListener implement eder — oyun eventlerini dinler
    /// 
    /// Fix #7: FirstPlayerId TurnManager'dan alınır
    /// Fix #8: ConvertGameState TODO'ları çözüldü
    /// Fix #9: PreviousPlayerId doldurulur
    /// </summary>
    public class JsonRpcBackendService : IAsyncGameEventListener
    {
        private readonly string _gameId;
        private readonly UnoGameState _gameState;
        private readonly List<GameCore.Models.Player> _players;
        private readonly ITurnManager? _turnManager;

        public JsonRpcBackendService(
            string gameId, 
            UnoGameState gameState, 
            IReadOnlyList<IPlayer> players,
            ITurnManager? turnManager = null)
        {
            _gameId = gameId;
            _gameState = gameState;
            _players = players.Cast<GameCore.Models.Player>().ToList();
            _turnManager = turnManager;
        }

        public async Task OnGameEventAsync(IGameAction action)
        {
            try
            {
                switch (action)
                {
                    case UnoGameStartedEvent evt:
                        await SendGameStartedAsync(evt);
                        break;

                    case CardPlayedEvent evt:
                        await SendCardPlayedAsync(evt);
                        break;

                    case CardDrawnEvent evt:
                        await SendCardDrawnAsync(evt);
                        break;

                    case TurnChangedEvent evt:
                        await SendTurnChangedAsync(evt);
                        break;

                    case UnoGameEndedEvent evt:
                        await SendGameEndedAsync(evt);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Backend servis hatası: {ex.Message}");
            }
        }

        private async Task SendGameStartedAsync(UnoGameStartedEvent evt)
        {
            // Fix #7: FirstPlayerId'yi TurnManager'dan al
            string firstPlayerId = _turnManager?.CurrentPlayer.Id ?? evt.PlayerIds.First();

            var @params = new GameStartedParams
            {
                GameId = _gameId,
                GameType = "UNO",
                Players = ConvertPlayers(),
                InitialCard = ConvertCard(_gameState.LastPlayedCard!),
                FirstPlayerId = firstPlayerId,
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<GameStartedParams>
            {
                Method = "game.started",
                Params = @params
            };

            await SendToBackendAsync(request);
        }

        private async Task SendCardPlayedAsync(CardPlayedEvent evt)
        {
            var @params = new CardPlayedParams
            {
                GameId = _gameId,
                PlayerId = evt.PlayerId,
                Card = ConvertCard(evt.Card),
                RemainingCards = evt.RemainingCards,
                IsUno = evt.RemainingCards == 1,
                GameState = ConvertGameState(),
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<CardPlayedParams>
            {
                Method = "game.card_played",
                Params = @params
            };

            await SendToBackendAsync(request);
        }

        private async Task SendCardDrawnAsync(CardDrawnEvent evt)
        {
            var @params = new CardDrawnParams
            {
                GameId = _gameId,
                PlayerId = evt.PlayerId,
                CardsDrawn = evt.CardCount,
                TotalCards = evt.TotalCards,
                Reason = "VOLUNTARY",
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<CardDrawnParams>
            {
                Method = "game.card_drawn",
                Params = @params
            };

            await SendToBackendAsync(request);
        }

        private async Task SendTurnChangedAsync(TurnChangedEvent evt)
        {
            // Fix #9: PreviousPlayerId — TurnManager'dan al
            var turnMgr = _turnManager as TurnManager;
            string previousPlayerId = turnMgr?.PreviousPlayer?.Id ?? "";

            var @params = new TurnChangedParams
            {
                GameId = _gameId,
                PreviousPlayerId = previousPlayerId,
                CurrentPlayerId = evt.NewCurrentPlayerId,
                TurnNumber = evt.TurnNumber,
                IsClockwise = _gameState.IsClockwise,
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<TurnChangedParams>
            {
                Method = "game.turn_changed",
                Params = @params
            };

            await SendToBackendAsync(request);
        }

        private async Task SendGameEndedAsync(UnoGameEndedEvent evt)
        {
            var winner = _players.FirstOrDefault(p => p.Id == evt.WinnerId);

            var @params = new GameEndedParams
            {
                GameId = _gameId,
                WinnerId = evt.WinnerId,
                WinnerName = winner?.Name,
                FinalScores = _players.ToDictionary(p => p.Id, p => p.Score),
                TotalTurns = evt.TotalTurns,
                DurationSeconds = evt.GameDurationSeconds,
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<GameEndedParams>
            {
                Method = "game.ended",
                Params = @params
            };

            await SendToBackendAsync(request);
        }

        private async Task SendToBackendAsync<T>(JsonRpcRequest<T> request)
        {
            await Task.Delay(50); // Network simülasyonu

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            Console.WriteLine($"\n📤 JSON-RPC REQUEST → Backend Server");
            Console.WriteLine(json);
        }

        // Helper: UnoCard → CardDto
        private CardDto ConvertCard(UnoGame.Models.Cards.UnoCard card)
        {
            return new CardDto
            {
                Color = card.Color.ToString().ToUpperInvariant(),
                Type = ConvertCardType(card.Type),
                Number = card.Number
            };
        }

        private string ConvertCardType(UnoGame.Models.Cards.UnoCard.CardType type)
        {
            return type switch
            {
                UnoGame.Models.Cards.UnoCard.CardType.Number => "NUMBER",
                UnoGame.Models.Cards.UnoCard.CardType.Skip => "SKIP",
                UnoGame.Models.Cards.UnoCard.CardType.Reverse => "REVERSE",
                UnoGame.Models.Cards.UnoCard.CardType.DrawTwo => "DRAW_TWO",
                UnoGame.Models.Cards.UnoCard.CardType.Wild => "WILD",
                UnoGame.Models.Cards.UnoCard.CardType.WildDrawFour => "WILD_DRAW_FOUR",
                _ => "UNKNOWN"
            };
        }

        // Helper: Players → PlayerStateDto[]
        private List<PlayerStateDto> ConvertPlayers()
        {
            return _players.Select(p => new PlayerStateDto
            {
                PlayerId = p.Id,
                PlayerName = p.Name,
                CardCount = _gameState.PlayerCardCounts.GetValueOrDefault(p.Id, 0),
                Score = p.Score,
                Status = p.Status.ToString().ToUpperInvariant(),
                JoinedAt = p.JoinedAt
            }).ToList();
        }

        // Helper: Full GameState — Fix #8: TODO'lar çözüldü
        private UnoGameStateDto ConvertGameState()
        {
            return new UnoGameStateDto
            {
                GameId = _gameId,
                GameStatus = ConvertGameStatus(_gameState.Status),
                CurrentRound = _gameState.CurrentRound,
                CurrentTurnNumber = _turnManager?.CurrentTurnNumber ?? 0,
                CurrentPlayerId = _turnManager?.CurrentPlayer.Id ?? "",
                IsClockwise = _gameState.IsClockwise,
                DrawPenalty = _gameState.DrawPenalty,
                LastPlayedCard = _gameState.LastPlayedCard != null ? ConvertCard(_gameState.LastPlayedCard) : null,
                Players = ConvertPlayers(),
                CreatedAt = _gameState.CreatedAt,
                UpdatedAt = _gameState.UpdatedAt ?? DateTime.UtcNow
            };
        }

        private string ConvertGameStatus(GameStatus status)
        {
            return status switch
            {
                GameStatus.Waiting => "WAITING",
                GameStatus.InProgress => "IN_PROGRESS",
                GameStatus.Paused => "PAUSED",
                GameStatus.Completed => "COMPLETED",
                GameStatus.Cancelled => "CANCELLED",
                _ => "UNKNOWN"
            };
        }
    }
}