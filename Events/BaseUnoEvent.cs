using GameCore.Models;     //  BaseGameAction
using GameCore.Enums;      // GameActionType
using GameCore.Interfaces; //  IGameState
using System;

namespace UnoGame.Events
{
    /// <summary>
    /// Tüm UNO eventlerinin ortak temel sınıfı
    /// 
    ///  GAMECORE KULLANIMI: BaseGameAction'dan türüyor
    /// 
    /// BaseGameAction bize ne sağlıyor?
    /// - ActionId (benzersiz event ID)
    /// - ActionType (event tipi enum)
    /// - PlayerId (hangi oyuncu tetikledi)
    /// - Timestamp (ne zaman oldu)
    /// - ToJson() (JSON'a çevirme)
    /// 
    /// Biz sadece UNO'ya özel bilgileri ekliyoruz!
    /// 
    /// SOLID: Single Responsibility
    /// Bu class sadece event verisi tutar, iş mantığı yapmaz
    /// </summary>
    public abstract class BaseUnoEvent : BaseGameAction  //  GameCore'dan türetme
    {
        /// <summary>
        /// Event'in oluştuğu oyunun ID'si
        /// Backend hangi oyunu güncelleyeceğini buradan bilir
        /// </summary>
        public string GameId { get; }

        /// <summary>
        /// Event mesajı - loglama ve debug için
        /// Örnek: "Ahmet Red 7 kartını attı"
        /// </summary>
        public string Message { get; set; }

        protected BaseUnoEvent(
            GameActionType actionType, 
            string playerId, 
            string gameId,
            string message = "") 
            : base(actionType, playerId)  //  Parent constructor çağrısı
        {
            GameId = gameId;
            Message = message;
        }

        /// <summary>
        /// Validation - şimdilik her event geçerli
        /// İleride özel validasyonlar eklenebilir
        /// </summary>
        public override bool Validate(IGameState state)
        {
            return true;
        }

        /// <summary>
        /// Execute - eventler state değiştirmez, sadece bilgi taşır
        /// State değişikliği oyun mantığında (UnoGame) olur
        /// </summary>
        public override void Execute(IGameState state)
        {
            // Event'ler sadece bildirim amaçlı
            // State değişikliği zaten oyun mantığında yapıldı
        }
    }
}