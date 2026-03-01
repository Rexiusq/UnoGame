using GameCore.Models;
using GameCore.Enums;
using GameCore.Interfaces;
using System;

namespace UnoGame.Events
{
    /// <summary>
    /// Tüm UNO event'lerinin temel sınıfı.
    /// BaseGameAction'dan türeyerek ActionId, Timestamp gibi ortak alanları devralır.
    /// </summary>
    public abstract class BaseUnoEvent : BaseGameAction
    {
        public string GameId { get; }
        public string Message { get; set; }

        protected BaseUnoEvent(
            GameActionType actionType, 
            string playerId, 
            string gameId,
            string message = "") 
            : base(actionType, playerId)
        {
            GameId = gameId;
            Message = message;
        }

        public override bool Validate(IGameState state)
        {
            return true;
        }

        public override void Execute(IGameState state)
        {
            // Event'ler bildirim amaçlıdır, state değişikliği oyun motorunda yapılır
        }
    }
}