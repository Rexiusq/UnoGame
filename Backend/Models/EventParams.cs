using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnoGame.Backend.Models
{

    /// Oyun başladığında gönderilir
    public class GameStartedParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("game_type")]
        public string GameType { get; set; } = "UNO";

        [JsonPropertyName("players")]
        public List<PlayerStateDto> Players { get; set; } = new();

        [JsonPropertyName("initial_card")]
        public CardDto InitialCard { get; set; } = new();

        [JsonPropertyName("first_player_id")]
        public string FirstPlayerId { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    /// Kart atıldığında gönderilir
    public class CardPlayedParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; } = "";

        [JsonPropertyName("card")]
        public CardDto Card { get; set; } = new();

        [JsonPropertyName("remaining_cards")]
        public int RemainingCards { get; set; }

        [JsonPropertyName("is_uno")]
        public bool IsUno { get; set; } // 1 kart kaldı mı?

        [JsonPropertyName("game_state")]
        public UnoGameStateDto GameState { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    /// Kart çekildiğinde gönderilir
    public class CardDrawnParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; } = "";

        [JsonPropertyName("cards_drawn")]
        public int CardsDrawn { get; set; }

        [JsonPropertyName("total_cards")]
        public int TotalCards { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = ""; // VOLUNTARY, DRAW_PENALTY, NO_PLAYABLE_CARD

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    /// Tur değiştiğinde gönderilir
    public class TurnChangedParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("previous_player_id")]
        public string PreviousPlayerId { get; set; } = "";

        [JsonPropertyName("current_player_id")]
        public string CurrentPlayerId { get; set; } = "";

        [JsonPropertyName("turn_number")]
        public int TurnNumber { get; set; }

        [JsonPropertyName("is_clockwise")]
        public bool IsClockwise { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    /// Oyun bittiğinde gönderilir
    public class GameEndedParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("winner_id")]
        public string? WinnerId { get; set; }

        [JsonPropertyName("winner_name")]
        public string? WinnerName { get; set; }

        [JsonPropertyName("final_scores")]
        public Dictionary<string, int> FinalScores { get; set; } = new();

        [JsonPropertyName("total_turns")]
        public int TotalTurns { get; set; }

        [JsonPropertyName("duration_seconds")]
        public double DurationSeconds { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    /// Oyuncu bağlantısı koptuğunda
    public class PlayerDisconnectedParams
    {
        [JsonPropertyName("game_id")]
        public string GameId { get; set; } = "";

        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; } = "";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = ""; // TIMEOUT, MANUAL_EXIT, CONNECTION_LOSS

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}