using System.Collections.Generic;

namespace Un
{
    /// <summary>
    /// Represents a player in the game with their hand and identity.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// The player's number (1-4). Player 1 is the human player.
        /// </summary>
        public int PlayerNumber { get; }
        
        /// <summary>
        /// The player's display name.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The cards currently in this player's hand.
        /// </summary>
        public List<Card> Hand { get; }
        
        /// <summary>
        /// Whether this player is a human player (not AI).
        /// </summary>
        public bool IsHuman { get; }
        
        /// <summary>
        /// Creates a new player with the specified number and name.
        /// </summary>
        /// <param name="playerNumber">The player number (1-4)</param>
        /// <param name="name">The player's display name</param>
        /// <param name="isHuman">Whether this is a human player</param>
        public Player(int playerNumber, string name, bool isHuman = false)
        {
            PlayerNumber = playerNumber;
            Name = name;
            IsHuman = isHuman;
            Hand = new List<Card>();
        }
        
        /// <summary>
        /// Gets the number of cards in the player's hand.
        /// </summary>
        public int CardCount => Hand.Count;
        
        /// <summary>
        /// Clears all cards from the player's hand.
        /// </summary>
        public void ClearHand()
        {
            Hand.Clear();
        }
        
        /// <summary>
        /// Adds a card to the player's hand.
        /// </summary>
        public void AddCard(Card card)
        {
            Hand.Add(card);
        }
        
        /// <summary>
        /// Removes a card from the player's hand.
        /// </summary>
        public bool RemoveCard(Card card)
        {
            return Hand.Remove(card);
        }
        
        /// <summary>
        /// Checks if this player has won (has no cards left).
        /// </summary>
        public bool HasWon => Hand.Count == 0;
    }
}
