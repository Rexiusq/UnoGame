using GameCore.Interfaces;
using GameCore.Enums;
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Rules
{
    /// <summary>
    /// UNO oyun kurallarını tanımlayan sınıf.
    /// IGameRules interface'ini implemente ederek kart geçerliliği, oyun sonu ve kazanan tespiti yapar.
    /// </summary>
    public class UnoGameRules : IGameRules
    {
        public int MinPlayers => 2;
        public int MaxPlayers => 10;

        public bool CanStartGame(IReadOnlyList<IPlayer> players)
        {
            var activeCount = players.Count(p => 
                p.Status == PlayerStatus.Active || 
                p.Status == PlayerStatus.Waiting);
            
            return activeCount >= MinPlayers && activeCount <= MaxPlayers;
        }

        public bool ValidateAction(IGameAction action, IGameState state)
        {
            return true;
        }

        /// <summary>
        /// Herhangi bir oyuncunun kart sayısı 0 ise oyun biter.
        /// </summary>
        public bool IsGameOver(IGameState state)
        {
            if (state is UnoGameState unoState)
            {
                return unoState.PlayerCardCounts.Any(p => p.Value == 0);
            }
            return false;
        }

        public IPlayer? GetWinner(IGameState state)
        {
            return null;
        }

        /// <summary>
        /// Eli ilk bitiren oyuncuyu kazanan olarak döndürür.
        /// </summary>
        public IPlayer? FindWinner(IGameState state, IReadOnlyList<IPlayer> players)
        {
            if (state is UnoGameState unoState)
            {
                var winnerEntry = unoState.PlayerCardCounts
                    .FirstOrDefault(p => p.Value == 0);

                if (winnerEntry.Key != null)
                {
                    return players.FirstOrDefault(p => p.Id == winnerEntry.Key);
                }
            }
            return null;
        }

        /// <summary>
        /// Bir kartın ortadaki karta göre atılıp atılamayacağını kontrol eder.
        /// Kurallar: Wild her zaman geçerli → seçilen renk eşleşmesi → renk eşleşmesi → sayı eşleşmesi → tip eşleşmesi.
        /// </summary>
        public bool CanPlayCard(UnoCard cardToPlay, UnoCard lastCard)
        {
            if (cardToPlay.Color == UnoCard.CardColor.Wild)
                return true;

            if (lastCard.Color == UnoCard.CardColor.Wild)
            {
                if (lastCard.ChosenColor.HasValue)
                    return cardToPlay.Color == lastCard.ChosenColor.Value;
                else
                    return true;
            }

            if (cardToPlay.Color == lastCard.Color)
                return true;

            if (cardToPlay.Type == UnoCard.CardType.Number && 
                lastCard.Type == UnoCard.CardType.Number)
            {
                return cardToPlay.Number == lastCard.Number;
            }

            if (cardToPlay.Type == lastCard.Type)
                return true;

            return false;
        }
    }
}