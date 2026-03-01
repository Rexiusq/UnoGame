using GameCore.Enums;
using System.Collections.Generic;

namespace UnoGame.Events
{
    /// <summary>
    /// Oyun başladığında fırlatılan event
    /// 
    /// Backend'e şunları bildirir:
    /// - Oyun artık "InProgress" durumda
    /// - Oyuncu listesi kesinleşti
    /// - İlk tur başladı
    /// 
    /// Frontend'de:
    /// - "Oyun Başladı!" ekranı gösterilir
    /// - Kartlar dağıtılır (animasyon)
    /// - Oyun tahtası aktif hale gelir
    /// </summary>
    public class UnoGameStartedEvent : BaseUnoEvent
    {
        /// <summary>
        /// Oyundaki oyuncu ID'leri
        /// </summary>
        public List<string> PlayerIds { get; }

        /// <summary>
        /// İlk ortaya konan kart
        /// Herkes bunu görebilir
        /// </summary>
        public string InitialCard { get; }

        public UnoGameStartedEvent(
            string gameId, 
            List<string> playerIds,
            string initialCard) 
            : base(
                GameActionType.GameStarted,  //  GameCore enum
                gameId,  // PlayerId yerine GameId (sistemsel event)
                gameId,
                "Oyun başladi!")
        {
            PlayerIds = playerIds;
            InitialCard = initialCard;
        }
    }
}