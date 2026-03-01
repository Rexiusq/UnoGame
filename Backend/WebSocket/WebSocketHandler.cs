using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using UnoGame.Backend.Protocol;

namespace UnoGame.Backend.WebSocket
{
    /// <summary>
    /// WebSocket üzerinden gelen client komutlarını (JSON-RPC 2.0) işleyen handler.
    /// 
    /// Desteklenen komutlar:
    /// game.play_card, game.draw_card, game.pass_turn,
    /// game.call_uno, game.get_hand, game.get_state
    /// </summary>
    public class WebSocketHandler
    {
        private readonly Core.UnoGame _game;
        private readonly WebSocketConnectionManager _connectionManager;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public WebSocketHandler(Core.UnoGame game, WebSocketConnectionManager connectionManager)
        {
            _game = game;
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Bağlantıyı kaydeder ve bağlantı kapanana kadar mesaj dinleme döngüsünü çalıştırır.
        /// </summary>
        public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket socket, string playerId)
        {
            _connectionManager.AddConnection(playerId, socket);
            await SendWelcomeMessage(playerId);

            var buffer = new byte[4096];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"👋 {playerId} bağlantıyı kapatıyor...");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"📩 {playerId}'den mesaj: {message}");

                        var response = await ProcessMessageAsync(playerId, message);
                        
                        if (response != null)
                        {
                            await _connectionManager.SendToPlayerAsync(playerId, response);
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"⚠️ WebSocket hatası ({playerId}): {ex.Message}");
            }
            finally
            {
                _connectionManager.RemoveConnection(playerId);

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Bağlantı kapatıldı",
                        CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Gelen JSON-RPC mesajını parse edip ilgili handler'a yönlendirir.
        /// </summary>
        private async Task<string?> ProcessMessageAsync(string playerId, string rawMessage)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawMessage);
                var root = doc.RootElement;

                var method = root.GetProperty("method").GetString() ?? "";
                var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";

                Console.WriteLine($"🔧 Komut: {method}");

                return method switch
                {
                    "game.play_card" => await HandlePlayCard(playerId, root, id),
                    "game.draw_card" => await HandleDrawCard(playerId, id),
                    "game.pass_turn" => HandlePassTurn(playerId, id),
                    "game.call_uno" => HandleCallUno(playerId, id),
                    "game.get_hand" => HandleGetHand(playerId, id),
                    "game.get_state" => HandleGetState(id),
                    _ => BuildErrorResponse(id, -32601, $"Bilinmeyen metod: {method}")
                };
            }
            catch (JsonException ex)
            {
                return BuildErrorResponse("", -32700, $"Geçersiz JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BuildErrorResponse("", -32603, $"Sunucu hatası: {ex.Message}");
            }
        }

        private Task<string> HandlePlayCard(string playerId, JsonElement root, string id)
        {
            try
            {
                var paramsEl = root.GetProperty("params");
                var cardEl = paramsEl.GetProperty("card");

                var color = ParseColor(cardEl.GetProperty("color").GetString() ?? "");
                var type = ParseCardType(cardEl.GetProperty("type").GetString() ?? "");
                int? number = cardEl.TryGetProperty("number", out var numProp) && numProp.ValueKind != JsonValueKind.Null
                    ? numProp.GetInt32() 
                    : null;

                var card = new UnoCard(color, type, number);

                UnoCard.CardColor? chosenColor = null;
                if (paramsEl.TryGetProperty("chosen_color", out var chosenProp) && 
                    chosenProp.ValueKind != JsonValueKind.Null)
                {
                    chosenColor = ParseColor(chosenProp.GetString() ?? "");
                }

                _game.PlayCard(playerId, card, chosenColor);

                return Task.FromResult(BuildSuccessResponse(id, new { success = true, message = $"Kart atıldı: {card}" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(BuildErrorResponse(id, -32000, ex.Message));
            }
        }

        private Task<string> HandleDrawCard(string playerId, string id)
        {
            try
            {
                _game.DrawCard(playerId);

                var cards = _game.GetPlayerCards(playerId);
                return Task.FromResult(BuildSuccessResponse(id, new 
                { 
                    success = true, 
                    message = "Kart cekildi",
                    total_cards = cards?.Count ?? 0,
                    can_play_drawn = _game.CanPlayDrawnCard,
                    has_drawn = _game.HasDrawnThisTurn
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(BuildErrorResponse(id, -32000, ex.Message));
            }
        }

        private string HandlePassTurn(string playerId, string id)
        {
            try
            {
                _game.PassTurn(playerId);
                return BuildSuccessResponse(id, new { success = true, message = "Pas gecildi, sira sonraki oyuncuda" });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(id, -32000, ex.Message);
            }
        }

        private string HandleCallUno(string playerId, string id)
        {
            try
            {
                _game.CallUno(playerId);
                return BuildSuccessResponse(id, new { success = true, message = "UNO!" });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(id, -32000, ex.Message);
            }
        }

        /// <summary>
        /// Oyuncunun kendi kartlarını döndürür. Güvenlik: sadece isteyen oyuncunun kartları gönderilir.
        /// </summary>
        private string HandleGetHand(string playerId, string id)
        {
            var cards = _game.GetPlayerCards(playerId);
            if (cards == null)
            {
                return BuildErrorResponse(id, -32000, "Oyuncu bulunamadı veya oyun başlamadı");
            }

            var playableCards = _game.GetPlayableCards(playerId);

            var cardList = cards.Select(c => new
            {
                color = c.Color.ToString().ToUpperInvariant(),
                type = ConvertCardType(c.Type),
                number = c.Number,
                playable = playableCards?.Exists(p => 
                    p.Color == c.Color && p.Type == c.Type && p.Number == c.Number) ?? false
            }).ToList();

            return BuildSuccessResponse(id, new
            {
                player_id = playerId,
                cards = cardList,
                card_count = cardList.Count,
                current_turn = _game.CurrentTurnManager?.CurrentPlayer.Id == playerId,
                has_drawn = _game.HasDrawnThisTurn && _game.CurrentTurnManager?.CurrentPlayer.Id == playerId,
                can_play_drawn = _game.CanPlayDrawnCard && _game.CurrentTurnManager?.CurrentPlayer.Id == playerId,
                draw_penalty = ((UnoGameState)_game.State).DrawPenalty,
                game_finished = _game.IsGameFinished
            });
        }

        private string HandleGetState(string id)
        {
            var stateJson = _game.GetStateJson();
            return BuildSuccessResponse(id, new
            {
                state = JsonSerializer.Deserialize<JsonElement>(stateJson),
                current_player = _game.CurrentTurnManager?.CurrentPlayer.Id ?? "",
                connected_players = _connectionManager.ConnectedPlayerIds
            });
        }

        private async Task SendWelcomeMessage(string playerId)
        {
            var lastCard = _game.LastPlayedCard;
            object? lastCardDto = lastCard != null ? new
            {
                color = lastCard.Color == UnoCard.CardColor.Wild 
                    ? (lastCard.ChosenColor != null ? lastCard.ChosenColor.ToString()!.ToUpperInvariant() : "WILD")
                    : lastCard.Color.ToString().ToUpperInvariant(),
                type = ConvertCardType(lastCard.Type),
                number = lastCard.Number
            } : null;

            var welcome = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "server.welcome",
                @params = new
                {
                    message = $"Hoş geldin {playerId}! UNO oyun sunucusuna bağlandın.",
                    player_id = playerId,
                    connected_players = _connectionManager.ConnectedPlayerIds,
                    last_played_card = lastCardDto,
                    current_player = _game.CurrentTurnManager?.CurrentPlayer.Id ?? "",
                    draw_penalty = ((UnoGameState)_game.State).DrawPenalty,
                    game_finished = _game.IsGameFinished
                }
            }, _jsonOptions);

            await _connectionManager.SendToPlayerAsync(playerId, welcome);
        }

        // --- JSON-RPC Response Builders ---

        private string BuildSuccessResponse(string id, object result)
        {
            var response = new JsonRpcResponse<object>
            {
                Id = id,
                Result = result
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }

        private string BuildErrorResponse(string id, int code, string message)
        {
            var response = new
            {
                jsonrpc = "2.0",
                error = new { code, message },
                id
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }

        // --- Parsers ---

        private UnoCard.CardColor ParseColor(string color)
        {
            var normalized = color.Replace("İ", "I").Replace("ı", "i").ToUpperInvariant();
            return normalized switch
            {
                "RED" => UnoCard.CardColor.Red,
                "BLUE" => UnoCard.CardColor.Blue,
                "GREEN" => UnoCard.CardColor.Green,
                "YELLOW" => UnoCard.CardColor.Yellow,
                "WILD" => UnoCard.CardColor.Wild,
                _ => throw new ArgumentException($"Geçersiz renk: {color}")
            };
        }

        private UnoCard.CardType ParseCardType(string type)
        {
            var normalized = type.Replace("İ", "I").Replace("ı", "i").ToUpperInvariant();
            return normalized switch
            {
                "NUMBER" => UnoCard.CardType.Number,
                "SKIP" => UnoCard.CardType.Skip,
                "REVERSE" => UnoCard.CardType.Reverse,
                "DRAW_TWO" => UnoCard.CardType.DrawTwo,
                "WILD" => UnoCard.CardType.Wild,
                "WILD_DRAW_FOUR" => UnoCard.CardType.WildDrawFour,
                _ => throw new ArgumentException($"Geçersiz kart tipi: {type}")
            };
        }

        private string ConvertCardType(UnoCard.CardType type)
        {
            return type switch
            {
                UnoCard.CardType.Number => "NUMBER",
                UnoCard.CardType.Skip => "SKIP",
                UnoCard.CardType.Reverse => "REVERSE",
                UnoCard.CardType.DrawTwo => "DRAW_TWO",
                UnoCard.CardType.Wild => "WILD",
                UnoCard.CardType.WildDrawFour => "WILD_DRAW_FOUR",
                _ => "UNKNOWN"
            };
        }
    }
}
