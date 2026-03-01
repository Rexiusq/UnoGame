using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Models.Cards
{
    /// <summary>
    /// UNO kart destesi (108 kart)
    /// 
    /// Standart UNO destesi:
    /// - 4 renk (Red, Blue, Green, Yellow) x her renk:
    ///   - 1 adet 0 numaralı kart
    ///   - 2 adet 1-9 numaralı kartlar (toplam 18)
    ///   - 2 adet Skip kartı
    ///   - 2 adet Reverse kartı
    ///   - 2 adet DrawTwo kartı
    /// - 4 adet Wild (Joker) kartı
    /// - 4 adet WildDrawFour kartı
    /// Toplam: (1 + 18 + 2 + 2 + 2) x 4 + 4 + 4 = 108 kart
    /// </summary>
    public class UnoDeck
    {
        private readonly List<UnoCard> _drawPile;    // Çekme destesi
        private readonly List<UnoCard> _discardPile;  // Atılmış kartlar
        private readonly Random _random;

        /// <summary>Çekme destesindeki kart sayısı</summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>Atılmış kart sayısı</summary>
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
        /// Standart 108 kartlık UNO destesini oluşturur
        /// </summary>
        private void BuildDeck()
        {
            _drawPile.Clear();

            // Her renk için kartları ekle
            var colors = new[] 
            { 
                UnoCard.CardColor.Red, 
                UnoCard.CardColor.Blue, 
                UnoCard.CardColor.Green, 
                UnoCard.CardColor.Yellow 
            };

            foreach (var color in colors)
            {
                // 0 numara — her renkten 1 adet
                _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, 0));

                // 1-9 numaraları — her renkten 2'şer adet
                for (int number = 1; number <= 9; number++)
                {
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, number));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Number, number));
                }

                // Özel kartlar — her renkten 2'şer adet
                for (int i = 0; i < 2; i++)
                {
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Skip));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.Reverse));
                    _drawPile.Add(new UnoCard(color, UnoCard.CardType.DrawTwo));
                }
            }

            // Wild kartlar — 4 adet
            for (int i = 0; i < 4; i++)
            {
                _drawPile.Add(new UnoCard(UnoCard.CardColor.Wild, UnoCard.CardType.Wild));
                _drawPile.Add(new UnoCard(UnoCard.CardColor.Wild, UnoCard.CardType.WildDrawFour));
            }
        }

        /// <summary>
        /// Desteyi karıştırır (Fisher-Yates shuffle)
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
        /// Desteden belirtilen sayıda kart çeker
        /// Deste biterse, atılmış kartlardan yeniden oluşturur
        /// </summary>
        public List<UnoCard> Draw(int count = 1)
        {
            var drawnCards = new List<UnoCard>();

            for (int i = 0; i < count; i++)
            {
                // Deste bittiyse, atılmış kartlardan yeniden oluştur
                if (_drawPile.Count == 0)
                {
                    ReshuffleDiscardPile();
                }

                // Hâlâ kart yoksa (çok nadir durum), dur
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
        /// Kartı atılmış kartlar yığınına ekler
        /// </summary>
        public void Discard(UnoCard card)
        {
            _discardPile.Add(card);
        }

        /// <summary>
        /// Oyun başlangıcı için geçerli bir ilk kart çeker
        /// (Wild veya özel kart çıkarsa tekrar çeker)
        /// </summary>
        public UnoCard DrawInitialCard()
        {
            while (true)
            {
                var cards = Draw(1);
                if (cards.Count == 0) 
                    throw new InvalidOperationException("Destede kart kalmadı!");

                var card = cards[0];

                // İlk kart normal bir sayı kartı olmalı
                if (card.Type == UnoCard.CardType.Number)
                {
                    Discard(card);
                    return card;
                }

                // Geçersiz ilk kart — tekrar desteye koy ve karıştır
                _drawPile.Add(card);
                Shuffle();
            }
        }

        /// <summary>
        /// Atılmış kartları çekme destesine geri koyar ve karıştırır
        /// En üstteki atılmış kartı korur
        /// </summary>
        private void ReshuffleDiscardPile()
        {
            if (_discardPile.Count <= 1) return;

            // En son atılan kartı koru
            var topCard = _discardPile[_discardPile.Count - 1];
            _discardPile.RemoveAt(_discardPile.Count - 1);

            // Geri kalanları çekme destesine aktar
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            _discardPile.Add(topCard);

            Shuffle();
            Console.WriteLine("🔄 Deste yeniden karıştırıldı!");
        }
    }
}
