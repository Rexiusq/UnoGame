using GameCore.Enums;
using UnoGame.Models.Cards;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyuncu kart attığında fırlatılan event.
    /// Atılan kart bilgisi ve kalan kart sayısını taşır.
    /// </summary>
    public class CardPlayedEvent : BaseUnoEvent
    {
        public UnoCard Card { get; }
        public int RemainingCards { get; }

        public CardPlayedEvent(
            string playerId, 
            string gameId, 
            UnoCard card,
            int remainingCards) 
            : base(
                GameActionType.PlayerAction,
                playerId, 
                gameId,
                $"Oyuncu {playerId} {card} kartını attı")
        {
            Card = card;
            RemainingCards = remainingCards;
        }

        public override string ToJson()
        {
            return base.ToJson();
        }
    }
}