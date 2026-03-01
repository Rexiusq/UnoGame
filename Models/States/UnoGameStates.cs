using GameCore.Models;
using UnoGame.Models.Cards;
using System.Collections.Generic;

namespace UnoGame.Models.States
{
    /// <summary>
    /// UNO oyununun anlık durumunu tutan sınıf.
    /// BaseGameState'den türeyerek GameId, Status, CreatedAt gibi ortak alanları devralır.
    /// </summary>
    public class UnoGameState : BaseGameState
    {
        /// <summary>Ortadaki son atılan kart.</summary>
        public UnoCard? LastPlayedCard { get; set; }

        /// <summary>Oyunun yönü. true = saat yönü, false = ters yön.</summary>
        public bool IsClockwise { get; set; }

        /// <summary>Bir sonraki oyuncunun çekmesi gereken ceza kartı sayısı (+2 / +4).</summary>
        public int DrawPenalty { get; set; }

        /// <summary>Her oyuncunun elindeki kart sayısı. Key: PlayerId, Value: kart sayısı.</summary>
        public Dictionary<string, int> PlayerCardCounts { get; set; }

        public UnoGameState(string gameId) : base(gameId)
        {
            IsClockwise = true;
            DrawPenalty = 0;
            PlayerCardCounts = new Dictionary<string, int>();
        }
    }
}