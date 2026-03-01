using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyun bittiğinde fırlatılan event
    /// 
    /// Backend'e şunları bildirir:
    /// - Kazanan kim?
    /// - Final skorlar ne?
    /// - Oyun "Completed" durumda
    /// 
    /// Frontend'de:
    /// - "Oyun Bitti!" ekranı
    /// - Kazanan animasyonu
    /// - Skor tablosu
    /// - "Yeni Oyun" veya "Ana Menü" butonları
    /// </summary>
    public class UnoGameEndedEvent : BaseUnoEvent
    {
        /// <summary>
        /// Kazanan oyuncunun ID'si
        /// null = Berabere veya oyun iptal
        /// </summary>
        public string? WinnerId { get; }

        /// <summary>
        /// Oyun süresi (saniye)
        /// İstatistik için
        /// </summary>
        public double GameDurationSeconds { get; }

        /// <summary>
        /// Toplam kaç tur oynandı?
        /// </summary>
        public int TotalTurns { get; }

        public UnoGameEndedEvent(
            string gameId, 
            string? winnerId,
            double durationSeconds,
            int totalTurns) 
            : base(
                GameActionType.GameEnded,  // ⭐ GameCore enum
                winnerId ?? "system",  // Winner yoksa system
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