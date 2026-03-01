using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Models.Cards
{
    /// <summary>
    /// Bir oyuncunun elindeki kartları yöneten sınıf
    /// 
    /// Güvenlik: Bu sınıf backend'de kalır.
    /// Client'lara sadece kart sayısı gönderilir, gerçek kartlar gönderilmez.
    /// Sadece kart sahibi oyuncuya özel response'da kartları görebilir.
    /// </summary>
    public class PlayerHand
    {
        private readonly List<UnoCard> _cards;

        /// <summary>Eldeki kart sayısı (güvenli — herkese gösterilebilir)</summary>
        public int Count => _cards.Count;

        /// <summary>Eldeki kartlar (sadece backend ve kart sahibi erişebilir)</summary>
        public IReadOnlyList<UnoCard> Cards => _cards.AsReadOnly();

        public PlayerHand()
        {
            _cards = new List<UnoCard>();
        }

        /// <summary>
        /// Ele kart ekler (desteden çekilen kartlar)
        /// </summary>
        public void AddCard(UnoCard card)
        {
            _cards.Add(card);
        }

        /// <summary>
        /// Ele birden fazla kart ekler
        /// </summary>
        public void AddCards(IEnumerable<UnoCard> cards)
        {
            _cards.AddRange(cards);
        }

        /// <summary>
        /// Elden belirtilen kartı çıkarır (atıldığında)
        /// Kartın renk, tip ve numara eşleşmesine göre ilk bulunanı çıkarır
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

        /// <summary>
        /// Belirtilen kart elde var mı kontrol eder
        /// </summary>
        public bool HasCard(UnoCard card)
        {
            return _cards.Any(c => 
                c.Color == card.Color && 
                c.Type == card.Type && 
                c.Number == card.Number);
        }

        /// <summary>
        /// Ortadaki karta göre atılabilir kartlar listesini döndürür
        /// (UnoGameRules.CanPlayCard ile aynı mantık)
        /// </summary>
        public List<UnoCard> GetPlayableCards(UnoCard lastPlayedCard)
        {
            return _cards.Where(card =>
            {
                // Kural 1: Wild kartlar her zaman atılır
                if (card.Color == UnoCard.CardColor.Wild) return true;

                // Kural 2: Önceki kart Wild ise, seçilen renge bak
                if (lastPlayedCard.Color == UnoCard.CardColor.Wild)
                {
                    if (lastPlayedCard.ChosenColor.HasValue)
                        return card.Color == lastPlayedCard.ChosenColor.Value;
                    else
                        return true; // ChosenColor yoksa her kart atılabilir
                }

                // Kural 3: Renk eşleşmesi
                if (card.Color == lastPlayedCard.Color) return true;

                // Kural 4: Sayı kartlarında numara eşleşmesi
                if (card.Type == UnoCard.CardType.Number && 
                    lastPlayedCard.Type == UnoCard.CardType.Number)
                {
                    return card.Number == lastPlayedCard.Number;
                }

                // Kural 5: Özel kart tipi eşleşmesi (Skip-Skip, Reverse-Reverse vb.)
                if (card.Type == lastPlayedCard.Type) return true;

                return false;
            }).ToList();
        }

        /// <summary>
        /// Ortadaki karta karşı atılabilir kart var mı?
        /// </summary>
        public bool HasPlayableCard(UnoCard lastPlayedCard)
        {
            return GetPlayableCards(lastPlayedCard).Count > 0;
        }

        /// <summary>El boş mu? (Oyun bitiş kontrolü)</summary>
        public bool IsEmpty => _cards.Count == 0;
    }
}
