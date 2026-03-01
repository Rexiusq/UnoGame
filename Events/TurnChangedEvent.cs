using GameCore.Enums;

namespace UnoGame.Events
{
    /// <summary>
    /// Tur değiştiğinde fırlatılan event
    /// 
    /// Frontend'de çok önemli!
    /// - Sırası gelen oyuncuya bildirim: "Senin sıran!"
    /// - UI'da aktif oyuncuyu vurgula
    /// - Timer başlat (30 saniye süre ver)
    /// 
    /// Backend bunu alınca:
    /// - "Şu anda kiminle sırası" bilgisini günceller
    /// - Timeout mekanizması başlatır
    /// </summary>
    public class TurnChangedEvent : BaseUnoEvent
    {
        /// <summary>
        /// Sırası gelen oyuncunun ID'si
        /// </summary>
        public string NewCurrentPlayerId { get; }

        /// <summary>
        /// Tur numarası
        /// İstatistik ve replay için kullanılır
        /// </summary>
        public int TurnNumber { get; }

        public TurnChangedEvent(
            string gameId, 
            string newCurrentPlayerId,
            int turnNumber) 
            : base(
                GameActionType.TurnStarted,  // ⭐ GameCore enum
                newCurrentPlayerId,  // Yeni tur sahibi
                gameId,
                $"Sıra {newCurrentPlayerId} oyuncusuna geçti")
        {
            NewCurrentPlayerId = newCurrentPlayerId;
            TurnNumber = turnNumber;
        }
    }
}