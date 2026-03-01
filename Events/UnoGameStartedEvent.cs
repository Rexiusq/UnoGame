using GameCore.Enums;
using System.Collections.Generic;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyun başladığında fırlatılan event.
    /// Oyuncu listesi ve ilk ortaya konan kartı taşır.
    /// </summary>
    public class UnoGameStartedEvent : BaseUnoEvent
    {
        public List<string> PlayerIds { get; }
        public string InitialCard { get; }

        public UnoGameStartedEvent(
            string gameId, 
            List<string> playerIds,
            string initialCard) 
            : base(
                GameActionType.GameStarted,
                gameId,
                gameId,
                "Oyun başladi!")
        {
            PlayerIds = playerIds;
            InitialCard = initialCard;
        }
    }
}