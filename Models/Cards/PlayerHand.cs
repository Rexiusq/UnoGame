using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Models.Cards
{
    /// <summary>
    /// Bir oyuncunun elindeki kartları yöneten sınıf.
    /// Gerçek kart verileri sadece backend'de tutulur, client'lara sadece kart sayısı gönderilir.
    /// </summary>
    public class PlayerHand
    {
        private readonly List<UnoCard> _cards;

        public int Count => _cards.Count;
        public IReadOnlyList<UnoCard> Cards => _cards.AsReadOnly();

        public PlayerHand()
        {
            _cards = new List<UnoCard>();
        }

        public void AddCard(UnoCard card)
        {
            _cards.Add(card);
        }

        public void AddCards(IEnumerable<UnoCard> cards)
        {
            _cards.AddRange(cards);
        }

        /// <summary>
        /// Elden kartı çıkarır. Renk, tip ve numara eşleşmesine göre ilk bulunanı kaldırır.
        /// </summary>
        public bool RemoveCard(UnoCard card)
        {
            var found = _cards.FirstOrDefault(c => 
                c.Color == card.Color && 
                c.Type == card.Type && 
                c.Number == card.Number);

            if (found != null)
            {
                _cards.Remove(found);
                return true;
            }
            return false;
        }

        public bool HasCard(UnoCard card)
        {
            return _cards.Any(c => 
                c.Color == card.Color && 
                c.Type == card.Type && 
                c.Number == card.Number);
        }

        /// <summary>
        /// Ortadaki karta göre atılabilir kartları döndürür.
        /// </summary>
        public List<UnoCard> GetPlayableCards(UnoCard lastPlayedCard)
        {
            return _cards.Where(card =>
            {
                if (card.Color == UnoCard.CardColor.Wild) return true;

                if (lastPlayedCard.Color == UnoCard.CardColor.Wild)
                {
                    if (lastPlayedCard.ChosenColor.HasValue)
                        return card.Color == lastPlayedCard.ChosenColor.Value;
                    else
                        return true;
                }

                if (card.Color == lastPlayedCard.Color) return true;

                if (card.Type == UnoCard.CardType.Number && 
                    lastPlayedCard.Type == UnoCard.CardType.Number)
                {
                    return card.Number == lastPlayedCard.Number;
                }

                if (card.Type == lastPlayedCard.Type) return true;

                return false;
            }).ToList();
        }

        public bool HasPlayableCard(UnoCard lastPlayedCard)
        {
            return GetPlayableCards(lastPlayedCard).Count > 0;
        }

        public bool IsEmpty => _cards.Count == 0;
    }
}
