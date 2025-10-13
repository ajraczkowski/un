namespace Un
{
    public enum CardColor { Red, Blue, Green, Yellow, None }

    public enum CardType { Number, Skip, Reverse, DrawTwo, Wild, DrawFour }

    public class Card
    {
        public CardType Type { get; }
        public CardColor Color { get; }
        public int? Number { get; }

        public Card(CardType type, CardColor color = CardColor.None, int? number = null)
        {
            Type = type;
            Color = color;
            Number = number;
        }

        public override string ToString()
        {
            return Type switch
            {
                CardType.Number => $"{Color} {Number ?? -1}",
                CardType.Skip => $"{Color} Skip",
                CardType.Reverse => $"{Color} Reverse",
                CardType.DrawTwo => $"{Color} DrawTwo",
                CardType.Wild => "Wild",
                CardType.DrawFour => "Wild DrawFour",
                _ => string.Empty
            };
        }
    }
}
