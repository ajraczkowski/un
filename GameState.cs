using System;
using System.Collections.Generic;

namespace Un
{
    /// <summary>
    /// Manages all game state including player hands, current player, game rules state, etc.
    /// </summary>
    public class GameState
    {
        // Player hands
        public List<Card> Player1Hand { get; set; } = new List<Card>();
        public List<Card> Player2Hand { get; set; } = new List<Card>();
        public List<Card> Player3Hand { get; set; } = new List<Card>();
        public List<Card> Player4Hand { get; set; } = new List<Card>();
        
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
        /// Gets the hand for the specified player number (1-4)
        /// </summary>
        public List<Card> GetPlayerHand(int playerNumber)
        {
            return playerNumber switch
            {
                1 => Player1Hand,
                2 => Player2Hand,
                3 => Player3Hand,
                4 => Player4Hand,
                _ => throw new ArgumentOutOfRangeException(nameof(playerNumber), "Player number must be 1-4")
            };
        }
        
        /// <summary>
        /// Gets the name for the specified player number (1-4)
        /// </summary>
        public string GetPlayerName(int playerNumber)
        {
            return playerNumber switch
            {
                1 => "You",
                2 => "Alice",
                3 => "Bob",
                4 => "Charlie",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Resets all game state to initial values for a new game
        /// </summary>
        public void Reset()
        {
            Player1Hand.Clear();
            Player2Hand.Clear();
            Player3Hand.Clear();
            Player4Hand.Clear();
            DiscardPile.Clear();
            
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
            if (Player1Hand.Count == 0) return 1;
            if (Player2Hand.Count == 0) return 2;
            if (Player3Hand.Count == 0) return 3;
            if (Player4Hand.Count == 0) return 4;
            return -1;
        }
        
        /// <summary>
        /// Checks if a game is currently in progress
        /// </summary>
        /// <returns>True if any player has cards or discard pile has cards</returns>
        public bool IsGameInProgress()
        {
            return Player1Hand.Count > 0 || Player2Hand.Count > 0 || 
                   Player3Hand.Count > 0 || Player4Hand.Count > 0 || 
                   DiscardPile.Count > 0;
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