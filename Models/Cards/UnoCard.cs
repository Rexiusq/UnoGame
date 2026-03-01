namespace UnoGame.Models.Cards
{
    /// <summary>
    /// UNO kartını temsil eden model sınıfı.
    /// </summary>
    public class UnoCard
    {
        public enum CardColor 
        { 
            Red,
            Blue,
            Green,
            Yellow,
            Wild
        }

        public enum CardType 
        { 
            Number,
            Skip,
            Reverse,
            DrawTwo,
            Wild,
            WildDrawFour
        }

        public CardColor Color { get; set; }
        public CardType Type { get; set; }
        public int? Number { get; set; }

        /// <summary>
        /// Wild kart atıldığında oyuncunun seçtiği renk.
        /// </summary>
        public CardColor? ChosenColor { get; set; }

        public UnoCard(CardColor color, CardType type, int? number = null)
        {
            Color = color;
            Type = type;
            Number = number;
        }

        public override string ToString()
        {
            if (Type == CardType.Number && Number.HasValue)
                return $"{Color} {Number}";
            
            return $"{Color} {Type}";
        }
    }
}