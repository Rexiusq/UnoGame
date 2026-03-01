using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnoGame.Backend.Models
{
    /// <summary>
    /// Oyun durumunun JSON serializasyon modeli.
    /// </summary>
    public class UnoGameStateDto
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("game_status")]
        public string GameStatus { get; set; } = "";

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

    /// <summary>
    /// Kart bilgisi transfer nesnesi.
    /// </summary>
    public class CardDto
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("number")]
        public int? Number { get; set; }
    }

    /// <summary>
    /// Oyuncu durumu transfer nesnesi.
    /// </summary>
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
        public string Status { get; set; } = "";

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }
    }
}