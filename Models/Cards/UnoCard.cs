namespace UnoGame.Models.Cards
{
    /// <summary>
    /// UNO kartını temsil eden sınıf
    /// SOLID Prensibi: Single Responsibility - Sadece kart verisi tutar
    /// </summary>
    public class UnoCard
    {
        // Kart renkleri - Wild (Joker) dahil
        public enum CardColor 
        { 
            Red,      // Kırmızı
            Blue,     // Mavi
            Green,    // Yeşil
            Yellow,   // Sarı
            Wild      // Joker (renksiz)
        }

        // Kart tipleri - Özel kartlar dahil
        public enum CardType 
        { 
            Number,         // Sayı kartı (0-9)
            Skip,           // Sıra atlama
            Reverse,        // Yön değiştirme (DÜZELTME: "Reverse" doğru yazıldı)
            DrawTwo,        // +2 kart çektirme
            Wild,           // Joker
            WildDrawFour    // Joker +4
        }

        // Properties (özellikleri)
        public CardColor Color { get; set; }
        public CardType Type { get; set; }
        public int? Number { get; set; }  // Sadece Number tipinde kullanılır (0-9)

        /// <summary>
        /// Wild kart atıldığında seçilen renk
        /// Bir sonraki oyuncu bu renge uygun kart atmalı
        /// </summary>
        public CardColor? ChosenColor { get; set; }
        

        /// <summary>
        /// Constructor - Kart oluştururken çağrılır
        /// </summary>
        public UnoCard(CardColor color, CardType type, int? number = null)
        {
            Color = color;
            Type = type;
            Number = number;
        }

        /// <summary>
        /// Kartın okunabilir halini döndürür
        /// Örnek: "Red 5" veya "Blue Skip"
        /// </summary>
        public override string ToString()
        {
            if (Type == CardType.Number && Number.HasValue)
                return $"{Color} {Number}";
            
            return $"{Color} {Type}";
        }
    }
}