using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyun bittiğinde fırlatılan event.
    /// Kazanan bilgisi, oyun süresi ve toplam tur sayısını taşır.
    /// </summary>
    public class UnoGameEndedEvent : BaseUnoEvent
    {
        public string? WinnerId { get; }
        public double GameDurationSeconds { get; }
        public int TotalTurns { get; }

        public UnoGameEndedEvent(
            string gameId, 
            string? winnerId,
            double durationSeconds,
            int totalTurns) 
            : base(
                GameActionType.GameEnded,
                winnerId ?? "system",
                gameId,
                winnerId != null 
                    ? $"Oyunu {winnerId} kazandı!" 
                    : "Oyun bitti!")
        {
            WinnerId = winnerId;
            GameDurationSeconds = durationSeconds;
            TotalTurns = totalTurns;
        }
    }
}