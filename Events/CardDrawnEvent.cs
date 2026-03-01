using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyuncu kart çektiğinde fırlatılan event
    /// 
    /// GÜVENLİK: Gerçek kartı göstermiyoruz!
    /// Sadece "kaç kart çekti" bilgisini gönderiyoruz.
    /// Çünkü bu event tüm oyunculara broadcast edilecek.
    /// 
    /// Gerçek kart bilgisi:
    /// - Backend'de saklanır
    /// - Sadece ilgili oyuncuya özel response'da gönderilir
    /// </summary>
    public class CardDrawnEvent : BaseUnoEvent
    {
        /// <summary>
        /// Kaç kart çekildi?
        /// Genellikle 1, ama +2 ve +4 durumlarında 2 veya 4 olabilir
        /// </summary>
        public int CardCount { get; }

        /// <summary>
        /// Çektikten sonraki toplam kart sayısı
        /// </summary>
        public int TotalCards { get; }

        public CardDrawnEvent(
            string playerId, 
            string gameId, 
            int cardCount,
            int totalCards) 
            : base(
                GameActionType.PlayerAction,
                playerId, 
                gameId,
                $"Oyuncu {playerId} {cardCount} kart çekti")
        {
            CardCount = cardCount;
            TotalCards = totalCards;
        }
    }
}