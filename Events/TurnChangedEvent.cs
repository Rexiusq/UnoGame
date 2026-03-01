using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Tur değiştiğinde fırlatılan event.
    /// Sırası gelen oyuncunun bilgisini ve tur numarasını taşır.
    /// </summary>
    public class TurnChangedEvent : BaseUnoEvent
    {
        public string NewCurrentPlayerId { get; }
        public int TurnNumber { get; }

        public TurnChangedEvent(
            string gameId, 
            string newCurrentPlayerId,
            int turnNumber) 
            : base(
                GameActionType.TurnStarted,
                newCurrentPlayerId,
                gameId,
                $"Sıra {newCurrentPlayerId} oyuncusuna geçti")
        {
            NewCurrentPlayerId = newCurrentPlayerId;
            TurnNumber = turnNumber;
        }
    }
}