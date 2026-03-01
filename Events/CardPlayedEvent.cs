using GameCore.Enums;
using UnoGame.Models.Cards;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyuncu kart attığında fırlatılan event
    /// 
    /// Backend'e şu bilgileri taşır:
    /// - Hangi oyuncu attı? (PlayerId)
    /// - Hangi kartı attı? (Card)
    /// - Hangi oyunda oldu? (GameId)
    /// - Ne zaman oldu? (Timestamp - BaseGameAction'dan)
    /// 
    /// Backend bunu alınca:
    /// 1. Database'deki game state'i günceller
    /// 2. Diğer oyunculara broadcast eder
    /// 3. UI güncellenir
    /// </summary>
    public class CardPlayedEvent : BaseUnoEvent
    {
        /// <summary>
        /// Atılan kart
        /// Backend bu bilgiyi client'lara göndererek
        /// "Ahmet Red 7 attı" animasyonunu gösterir
        /// </summary>
        public UnoCard Card { get; }

        /// <summary>
        /// Atıldıktan sonraki kart sayısı
        /// UNO deme mekanizması için gerekli
        /// Eğer 1 kart kaldıysa "UNO!" diye bağırması gerekir
        /// </summary>
        public int RemainingCards { get; }

        public CardPlayedEvent(
            string playerId, 
            string gameId, 
            UnoCard card,
            int remainingCards) 
            : base(
                GameActionType.PlayerAction,  // ⭐ GameCore enum
                playerId, 
                gameId,
                $"Oyuncu {playerId} {card} kartını attı")
        {
            Card = card;
            RemainingCards = remainingCards;
        }

        /// <summary>
        /// Backend'e gönderilecek JSON için override
        /// Ekstra bilgiler ekleyebiliriz
        /// </summary>
        public override string ToJson()
        {
            // Burada custom JSON serialization yapılabilir
            // Şimdilik base implementation yeterli
            return base.ToJson();
        }
    }
}