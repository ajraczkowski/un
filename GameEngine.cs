using System;
using System.Collections.Generic;

namespace Un
{
    /// <summary>
    /// Central game coordinator that manages game state and provides game logic.
    /// </summary>
    public class GameEngine
    {
        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameState State { get; }

        /// <summary>
        /// Gets the game log that tracks all game events.
        /// </summary>
        public GameLog Log { get; }

        /// <summary>
        /// Gets the current number of cards in the deck.
        /// </summary>
        public int DeckCount => State.DeckCount;

        public GameEngine()
        {
            State = new GameState();
            Log = new GameLog(State);
        }

        /// <summary>
        /// Draws a card from the deck.
        /// </summary>
        /// <returns>The drawn card, or null if deck is empty</returns>
        public Card? DrawCard()
        {
            return State.DrawCard();
        }

        /// <summary>
        /// Deals cards for a new game: 7 cards to each player and one card to start the discard pile.
        /// </summary>
        /// <returns>The first card for the discard pile, or null if deck is empty</returns>
        public Card? DealNewGame()
        {
            // Deal 7 cards to each player
            for (int i = 0; i < 7; i++)
            {
                foreach (var player in State.Players)
                {
                    var card = DrawCard();
                    if (card != null)
                    {
                        player.AddCard(card);
                    }
                }
            }

            // Draw one card for the discard pile
            return DrawCard();
        }

        /// <summary>
        /// Determines if a card can be legally played on top of another card.
        /// </summary>
        /// <param name="card">The card to play</param>
        /// <param name="topCard">The card on top of the discard pile</param>
        /// <returns>True if the card can be played, false otherwise</returns>
        public bool CanPlayCard(Card card, Card topCard)
        {
            // Wild cards can always be played
            if (card.Type == CardType.Wild || card.Type == CardType.DrawFour)
            {
                return true;
            }

            // If the top card is a wild card, check against the chosen color
            if ((topCard.Type == CardType.Wild || topCard.Type == CardType.DrawFour) && State.ChosenWildColor.HasValue)
            {
                // Match against the chosen wild color
                if (card.Color == State.ChosenWildColor.Value)
                {
                    return true;
                }
                
                // Number cards of the chosen color can be played
                if (card.Type == CardType.Number && card.Color == State.ChosenWildColor.Value)
                {
                    return true;
                }
                
                return false;
            }

            // Match by color
            if (card.Color == topCard.Color)
            {
                return true;
            }

            // Number cards match by number
            if (card.Type == CardType.Number && topCard.Type == CardType.Number && 
                card.Number == topCard.Number)
            {
                return true;
            }

            // Action cards match with same action type (Skip with Skip, etc.)
            if (card.Type == topCard.Type && 
                (card.Type == CardType.Skip || card.Type == CardType.Reverse || card.Type == CardType.DrawTwo))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to play a card for a player. Validates the card can be played, removes it from hand,
        /// adds to discard pile, handles special card effects, and logs the action.
        /// </summary>
        /// <param name="playerNumber">The player number (1-4)</param>
        /// <param name="card">The card to play</param>
        /// <param name="chooseWildColor">Function to choose wild card color when needed. Returns the chosen color.</param>
        /// <returns>True if card was successfully played, false if not allowed</returns>
        public bool TryPlayCard(int playerNumber, Card card, Func<CardColor>? chooseWildColor = null)
        {
            var topCard = State.GetTopDiscardCard();
            
            // If there's no discard pile card, allow any card (shouldn't normally happen)
            if (topCard == null)
            {
                PlayCardInternal(playerNumber, card, chooseWildColor);
                return true;
            }
            
            // Check if card can be played
            if (!CanPlayCard(card, topCard))
            {
                return false;
            }
            
            PlayCardInternal(playerNumber, card, chooseWildColor);
            return true;
        }

        /// <summary>
        /// Internal method that performs the actual card playing without validation.
        /// </summary>
        private void PlayCardInternal(int playerNumber, Card card, Func<CardColor>? chooseWildColor)
        {
            var player = State.Players[playerNumber - 1];
            
            // Remove card from player's hand
            player.RemoveCard(card);
            
            // Add card to discard pile
            State.DiscardPile.Add(card);
            
            // Log the play
            Log.LogPlay(playerNumber, card);
            
            // Check if player has one card left (UN!)
            if (player.Hand.Count == 1)
            {
                Log.LogUn(playerNumber);
            }
            
            // Handle wild card color selection
            if (card.Type == CardType.Wild || card.Type == CardType.DrawFour)
            {
                if (chooseWildColor != null)
                {
                    State.ChosenWildColor = chooseWildColor();
                    Log.LogColorChoice(playerNumber, State.ChosenWildColor.Value);
                }
                else
                {
                    // Default to red if no chooser provided (shouldn't happen)
                    State.ChosenWildColor = CardColor.Red;
                    Log.LogColorChoice(playerNumber, CardColor.Red);
                }
            }
            else
            {
                // Clear wild color if not a wild card
                State.ChosenWildColor = null;
            }
            
            // Check if reverse was played
            if (card.Type == CardType.Reverse)
            {
                State.IsClockwise = !State.IsClockwise;
            }
        }
    }
}

