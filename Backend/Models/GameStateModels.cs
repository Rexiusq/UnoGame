using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnoGame.Backend.Models
{

    /// UnoGameState'den türetilmiş, backend'e gönderilecek tam oyun durumu
    public class UnoGameStateDto
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("game_status")]
        public string GameStatus { get; set; } = ""; // WAITING, IN_PROGRESS, COMPLETED, CANCELLED

        [JsonPropertyName("current_round")]
        public int CurrentRound { get; set; }

        [JsonPropertyName("current_turn_number")]
        public int CurrentTurnNumber { get; set; }

        [JsonPropertyName("current_player_id")]
        public string CurrentPlayerId { get; set; } = "";

        [JsonPropertyName("is_clockwise")]
        public bool IsClockwise { get; set; }

        [JsonPropertyName("draw_penalty")]
        public int DrawPenalty { get; set; }

        [JsonPropertyName("last_played_card")]
        public CardDto? LastPlayedCard { get; set; }

        [JsonPropertyName("players")]
        public List<PlayerStateDto> Players { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }


    /// Kart bilgisi - UnoCard'dan türetilmiş
    public class CardDto
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = ""; // RED, BLUE, GREEN, YELLOW, WILD

        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // NUMBER, SKIP, REVERSE, DRAW_TWO, WILD, WILD_DRAW_FOUR

        [JsonPropertyName("number")]
        public int? Number { get; set; } // 0-9 veya null
    }


    /// Oyuncu durumu - Player + game-specific bilgiler
    public class PlayerStateDto
    {
        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; } = "";

        [JsonPropertyName("player_name")]
        public string PlayerName { get; set; } = "";

        [JsonPropertyName("card_count")]
        public int CardCount { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = ""; // ACTIVE, WAITING, DISCONNECTED, ELIMINATED, WINNER

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }
    }
}