using GameCore.Interfaces;
using UnoGame.Events;
using UnoGame.Backend.Protocol;
using UnoGame.Backend.Models;
using UnoGame.Models.States;
using GameCore.Enums;
using GameCore.Managers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace UnoGame.Backend.WebSocket
{
    /// <summary>
    /// Oyun event'lerini WebSocket üzerinden tüm bağlı client'lara broadcast eden listener.
    /// Her event JSON-RPC formatında serialize edilip gönderilir.
    /// </summary>
    public class WebSocketEventListener : IAsyncGameEventListener
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly UnoGameState _gameState;
        private readonly IReadOnlyList<IPlayer> _players;
        private readonly ITurnManager? _turnManager;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public WebSocketEventListener(
            WebSocketConnectionManager connectionManager,
            UnoGameState gameState,
            IReadOnlyList<IPlayer> players,
            ITurnManager? turnManager = null)
        {
            _connectionManager = connectionManager;
            _gameState = gameState;
            _players = players;
            _turnManager = turnManager;
        }

        public async Task OnGameEventAsync(IGameAction action)
        {
            try
            {
                string? jsonMessage = action switch
                {
                    UnoGameStartedEvent evt => BuildGameStartedMessage(evt),
                    CardPlayedEvent evt => BuildCardPlayedMessage(evt),
                    CardDrawnEvent evt => BuildCardDrawnMessage(evt),
                    TurnChangedEvent evt => BuildTurnChangedMessage(evt),
                    UnoGameEndedEvent evt => BuildGameEndedMessage(evt),
                    _ => null
                };

                if (jsonMessage != null)
                {
                    await _connectionManager.BroadcastAsync(jsonMessage);
                    Console.WriteLine($"📡 WebSocket Broadcast: {action.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket broadcast hatası: {ex.Message}");
            }
        }

        private string BuildGameStartedMessage(UnoGameStartedEvent evt)
        {
            string firstPlayerId = _turnManager?.CurrentPlayer.Id ?? evt.PlayerIds.First();

            var @params = new GameStartedParams
            {
                GameId = _gameState.GameId,
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

            return JsonSerializer.Serialize(request, _jsonOptions);
        }

        private string BuildCardPlayedMessage(CardPlayedEvent evt)
        {
            var @params = new CardPlayedParams
            {
                GameId = _gameState.GameId,
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

            return JsonSerializer.Serialize(request, _jsonOptions);
        }

        private string BuildCardDrawnMessage(CardDrawnEvent evt)
        {
            var @params = new CardDrawnParams
            {
                GameId = _gameState.GameId,
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

            return JsonSerializer.Serialize(request, _jsonOptions);
        }

        private string BuildTurnChangedMessage(TurnChangedEvent evt)
        {
            var turnMgr = _turnManager as TurnManager;

            var @params = new TurnChangedParams
            {
                GameId = _gameState.GameId,
                PreviousPlayerId = turnMgr?.PreviousPlayer?.Id ?? "",
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

            return JsonSerializer.Serialize(request, _jsonOptions);
        }

        private string BuildGameEndedMessage(UnoGameEndedEvent evt)
        {
            var winner = _players.FirstOrDefault(p => p.Id == evt.WinnerId);

            var @params = new GameEndedParams
            {
                GameId = _gameState.GameId,
                WinnerId = evt.WinnerId,
                WinnerName = winner?.Name,
                FinalScores = _players.ToDictionary(p => p.Id, p => 0),
                TotalTurns = evt.TotalTurns,
                DurationSeconds = evt.GameDurationSeconds,
                Timestamp = evt.Timestamp
            };

            var request = new JsonRpcRequest<GameEndedParams>
            {
                Method = "game.ended",
                Params = @params
            };

            return JsonSerializer.Serialize(request, _jsonOptions);
        }

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

        private List<PlayerStateDto> ConvertPlayers()
        {
            return _players.Select(p => new PlayerStateDto
            {
                PlayerId = p.Id,
                PlayerName = p.Name,
                CardCount = _gameState.PlayerCardCounts.GetValueOrDefault(p.Id, 0),
                Score = 0,
                Status = p.Status.ToString().ToUpperInvariant(),
                JoinedAt = p.JoinedAt
            }).ToList();
        }

        private UnoGameStateDto ConvertGameState()
        {
            return new UnoGameStateDto
            {
                GameId = _gameState.GameId,
                GameStatus = _gameState.Status switch
                {
                    GameStatus.Waiting => "WAITING",
                    GameStatus.InProgress => "IN_PROGRESS",
                    GameStatus.Completed => "COMPLETED",
                    _ => "UNKNOWN"
                },
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
    }
}
