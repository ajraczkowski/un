using System;
using System.Collections.Generic;

namespace Un
{
    public class GameEngine
    {
        private readonly Stack<Card> _deck = new();

        public int DeckCount => _deck.Count;

        public GameEngine()
        {
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            // Build UN deck per specification (108 cards)
            var cards = new List<Card>();

            var colors = new[] { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

            // For each color: one 0, two of each 1-9
            foreach (var color in colors)
            {
                // one 0
                cards.Add(new Card(CardType.Number, color, 0));

                // two of each 1-9
                for (int n = 1; n <= 9; n++)
                {
                    cards.Add(new Card(CardType.Number, color, n));
                    cards.Add(new Card(CardType.Number, color, n));
                }

                // two Skip, two Reverse, two DrawTwo
                cards.Add(new Card(CardType.Skip, color));
                cards.Add(new Card(CardType.Skip, color));

                cards.Add(new Card(CardType.Reverse, color));
                cards.Add(new Card(CardType.Reverse, color));

                cards.Add(new Card(CardType.DrawTwo, color));
                cards.Add(new Card(CardType.DrawTwo, color));
            }

            // Four Wild and four DrawFour (no color)
            for (int i = 0; i < 4; i++)
            {
                cards.Add(new Card(CardType.Wild, CardColor.None));
                cards.Add(new Card(CardType.DrawFour, CardColor.None));
            }

            var rnd = new Random();
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = cards[i];
                cards[i] = cards[j];
                cards[j] = tmp;
            }

            foreach (var c in cards)
                _deck.Push(c);
        }

        public Card? DrawCard()
        {
            if (_deck.Count == 0) return null;
            return _deck.Pop();
        }
    }
}
