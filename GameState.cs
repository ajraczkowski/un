using System;
using System.Collections.Generic;
using System.Linq;

namespace Un
{
    /// <summary>
    /// Manages all game state including players, current player, game rules state, etc.
    /// </summary>
    public class GameState
    {
        // Deck
        private readonly Stack<Card> _deck = new();
        
        // Players
        public Player[] Players { get; }
        
        // Discard pile
        public List<Card> DiscardPile { get; set; } = new List<Card>();
        
        // Turn management
        public int CurrentPlayer { get; set; } = 1; // 1-4
        public bool IsClockwise { get; set; } = true; // Direction of play
        
        // Wild card state
        public CardColor? ChosenWildColor { get; set; } // Color chosen for wild cards
        
        // Draw state
        public Card? JustDrawnCard { get; set; } // Card that was just drawn (only this card can be played after drawing)
        
        // Game control
        public bool GameIsOver { get; set; } = false; // Flag to prevent moves after game ends
        
        /// <summary>
        /// Gets the current number of cards in the deck.
        /// </summary>
        public int DeckCount => _deck.Count;
        
        /// <summary>
        /// Initializes the game state with four players and a shuffled deck.
        /// </summary>
        public GameState()
        {
            Players = new Player[]
            {
                new Player(1, "You", isHuman: true),
                new Player(2, "Alice", isHuman: false),
                new Player(3, "Bob", isHuman: false),
                new Player(4, "Charlie", isHuman: false)
            };
            
            InitializeDeck();
        }
        
        /// <summary>
        /// Initializes and shuffles a standard UN deck (108 cards).
        /// </summary>
        private void InitializeDeck()
        {
            _deck.Clear();
            
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

            // Shuffle using Fisher-Yates algorithm
            var rnd = new Random();
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = cards[i];
                cards[i] = cards[j];
                cards[j] = tmp;
            }

            // Add shuffled cards to deck
            foreach (var c in cards)
                _deck.Push(c);
        }
        
        /// <summary>
        /// Draws a card from the deck.
        /// </summary>
        /// <returns>The drawn card, or null if deck is empty</returns>
        public Card? DrawCard()
        {
            if (_deck.Count == 0) return null;
            return _deck.Pop();
        }
        
        /// <summary>
        /// Gets the player object for the specified player number (1-4)
        /// </summary>
        public Player GetPlayer(int playerNumber)
        {
            if (playerNumber < 1 || playerNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(playerNumber), "Player number must be 1-4");
            
            return Players[playerNumber - 1];
        }
        
        /// <summary>
        /// Gets the hand for the specified player number (1-4)
        /// </summary>
        public List<Card> GetPlayerHand(int playerNumber)
        {
            return GetPlayer(playerNumber).Hand;
        }
        
        /// <summary>
        /// Gets the name for the specified player number (1-4)
        /// </summary>
        public string GetPlayerName(int playerNumber)
        {
            return GetPlayer(playerNumber).Name;
        }
        
        // Convenience properties for backward compatibility
        public List<Card> Player1Hand => Players[0].Hand;
        public List<Card> Player2Hand => Players[1].Hand;
        public List<Card> Player3Hand => Players[2].Hand;
        public List<Card> Player4Hand => Players[3].Hand;
        
        /// <summary>
        /// Resets all game state to initial values for a new game
        /// </summary>
        public void Reset()
        {
            foreach (var player in Players)
            {
                player.ClearHand();
            }
            
            DiscardPile.Clear();
            InitializeDeck(); // Reinitialize and shuffle the deck
            
            CurrentPlayer = 1;
            IsClockwise = true;
            ChosenWildColor = null;
            JustDrawnCard = null;
            GameIsOver = false;
        }
        
        /// <summary>
        /// Checks if any player has won the game (has 0 cards)
        /// </summary>
        /// <returns>The winning player number (1-4) or -1 if no winner</returns>
        public int CheckForWinner()
        {
            var winner = Players.FirstOrDefault(p => p.HasWon);
            return winner?.PlayerNumber ?? -1;
        }
        
        /// <summary>
        /// Checks if a game is currently in progress
        /// </summary>
        /// <returns>True if any player has cards or discard pile has cards</returns>
        public bool IsGameInProgress()
        {
            return Players.Any(p => p.CardCount > 0) || DiscardPile.Count > 0;
        }
        
        /// <summary>
        /// Gets the top card from the discard pile
        /// </summary>
        /// <returns>The top card or null if discard pile is empty</returns>
        public Card? GetTopDiscardCard()
        {
            return DiscardPile.Count > 0 ? DiscardPile[DiscardPile.Count - 1] : null;
        }
        
        /// <summary>
        /// Advances to the next player's turn based on the current direction
        /// </summary>
        public void AdvanceToNextPlayer()
        {
            // Turn order around the table:
            // Clockwise: 1 (You, bottom) → 4 (Charlie, right) → 2 (Alice, top) → 3 (Bob, left) → 1
            // Counterclockwise: 1 → 3 (Bob, left) → 2 (Alice, top) → 4 (Charlie, right) → 1
            if (IsClockwise)
            {
                CurrentPlayer = CurrentPlayer switch
                {
                    1 => 4,
                    4 => 2,
                    2 => 3,
                    3 => 1,
                    _ => 1
                };
            }
            else
            {
                CurrentPlayer = CurrentPlayer switch
                {
                    1 => 3,
                    3 => 2,
                    2 => 4,
                    4 => 1,
                    _ => 1
                };
            }
        }
    }
}