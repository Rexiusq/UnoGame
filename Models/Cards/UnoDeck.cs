using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Models.Cards
{
    /// <summary>
    /// Standart 108 kartlık UNO destesini yöneten sınıf.
    /// Çekme destesi ve atılmış kartlar yığınını tutar.
    /// </summary>
    public class UnoDeck
    {
        private readonly List<UnoCard> _drawPile;
        private readonly List<UnoCard> _discardPile;
        private readonly Random _random;

        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;

        public UnoDeck()
        {
            _random = new Random();
            _drawPile = new List<UnoCard>();
            _discardPile = new List<UnoCard>();
            BuildDeck();
            Shuffle();
        }

        /// <summary>
        /// 108 kartlık standart UNO destesini oluşturur.
        /// 4 renk × (1×0 + 2×1-9 + 2×Skip + 2×Reverse + 2×DrawTwo) + 4×Wild + 4×WildDrawFour
        /// </summary>
        private void BuildDeck()
        {
            _drawPile.Clear();

            var colors = new[] 
            { 
                UnoCard.CardColor.Red, 
                UnoCard.CardColor.Blue, 
                UnoCard.CardColor.Green, 
                UnoCard.CardColor.Yellow 
            };

            foreach (var color in colors)
            {
                _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, 0));

                for (int number = 1; number <= 9; number++)
                {
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, number));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, number));
                }

                for (int i = 0; i < 2; i++)
                {
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Skip));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Reverse));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.DrawTwo));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                _drawPile.Add(new UnoCard(UnoCard.CardColor.Wild, UnoCard.CardType.Wild));
                _drawPile.Add(new UnoCard(UnoCard.CardColor.Wild, UnoCard.CardType.WildDrawFour));
            }
        }

        /// <summary>
        /// Fisher-Yates algoritmasıyla desteyi karıştırır.
        /// </summary>
        public void Shuffle()
        {
            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }
        }

        /// <summary>
        /// Desteden belirtilen sayıda kart çeker.
        /// Deste biterse atılmış kartlar yeniden karıştırılır.
        /// </summary>
        public List<UnoCard> Draw(int count = 1)
        {
            var drawnCards = new List<UnoCard>();

            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    ReshuffleDiscardPile();
                }

                if (_drawPile.Count == 0)
                {
                    Console.WriteLine("⚠️ Destede ve atılmış kartlarda kart kalmadı!");
                    break;
                }

                var card = _drawPile[0];
                _drawPile.RemoveAt(0);
                drawnCards.Add(card);
            }

            return drawnCards;
        }

        /// <summary>
        /// Kartı atılmış kartlar yığınına ekler.
        /// </summary>
        public void Discard(UnoCard card)
        {
            _discardPile.Add(card);
        }

        /// <summary>
        /// Oyun başlangıcı için geçerli bir ilk kart çeker.
        /// Wild veya özel kart çıkarsa geçerli bir sayı kartı bulana kadar tekrar çeker.
        /// </summary>
        public UnoCard DrawInitialCard()
        {
            while (true)
            {
                var cards = Draw(1);
                if (cards.Count == 0) 
                    throw new InvalidOperationException("Destede kart kalmadı!");

                var card = cards[0];

                if (card.Type == UnoCard.CardType.Number)
                {
                    Discard(card);
                    return card;
                }

                _drawPile.Add(card);
                Shuffle();
            }
        }

        /// <summary>
        /// Atılmış kartları çekme destesine geri koyar ve karıştırır.
        /// En üstteki kart korunur.
        /// </summary>
        private void ReshuffleDiscardPile()
        {
            if (_discardPile.Count <= 1) return;

            var topCard = _discardPile[_discardPile.Count - 1];
            _discardPile.RemoveAt(_discardPile.Count - 1);

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            _discardPile.Add(topCard);

            Shuffle();
            Console.WriteLine("🔄 Deste yeniden karıştırıldı!");
        }
    }
}
