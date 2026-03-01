using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyuncu kart çektiğinde fırlatılan event.
    /// Güvenlik gereği çekilen kartın içeriği paylaşılmaz, sadece adet bilgisi gönderilir.
    /// </summary>
    public class CardDrawnEvent : BaseUnoEvent
    {
        public int CardCount { get; }
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