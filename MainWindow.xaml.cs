using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;

namespace Un
{
    public partial class MainWindow : Window
    {
        private GameEngine _engine = new GameEngine();
        private GameState _gameState = new GameState();
        private System.Windows.Controls.Border? _pressedCardBorder;
        private GameLog _gameLog;
        private SoundPlayer? _cardPlaySound;
        private SoundPlayer? _errorSound;
        private SoundPlayer? _skipSound;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize game log
            _gameLog = new GameLog(LogScrollViewer, Dispatcher);
            GameLogDisplay.ItemsSource = _gameLog.Entries;
            
            // Set log to be visible by default
            ShowLogMenuItem.IsChecked = true;
            
            // Load sound effects
            try
            {
                var cardPlayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "card_play.wav");
                var errorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "error.wav");
                var skipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "skip.wav");
                
                if (File.Exists(cardPlayPath))
                    _cardPlaySound = new SoundPlayer(cardPlayPath);
                
                if (File.Exists(errorPath))
                    _errorSound = new SoundPlayer(errorPath);
                
                if (File.Exists(skipPath))
                    _skipSound = new SoundPlayer(skipPath);
            }
            catch
            {
                // If sounds fail to load, we'll fall back to system sounds
            }
            
            // Start the first game
            StartNewGame();
        }

        private void PlayCardSound()
        {
            if (_cardPlaySound != null)
            {
                _cardPlaySound.Play();
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }

        private async System.Threading.Tasks.Task PlayCardSoundAsync()
        {
            if (_cardPlaySound != null)
            {
                // Play sound on background thread to not block
                await System.Threading.Tasks.Task.Run(() => _cardPlaySound.PlaySync());
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }

        private void PlayErrorSound()
        {
            if (_errorSound != null)
            {
                _errorSound.Play();
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }

        private void PlaySkipSound()
        {
            if (_skipSound != null)
            {
                _skipSound.Play();
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }

        private void StartNewGame()
        {
            // Reset all game state
            _engine = new GameEngine();
            _gameState.Reset();
            _pressedCardBorder = null;
            _gameLog.Clear();

            // Deal 7 cards to each player
            for (int i = 0; i < 7; i++)
            {
                var card1 = _engine.DrawCard();
                if (card1 != null) _gameState.Player1Hand.Add(card1);
                
                var card2 = _engine.DrawCard();
                if (card2 != null) _gameState.Player2Hand.Add(card2);
                
                var card3 = _engine.DrawCard();
                if (card3 != null) _gameState.Player3Hand.Add(card3);
                
                var card4 = _engine.DrawCard();
                if (card4 != null) _gameState.Player4Hand.Add(card4);
            }
            
            // Start discard pile with one card
            var firstCard = _engine.DrawCard();
            if (firstCard != null)
            {
                _gameState.DiscardPile.Add(firstCard);
                
                // If the first card is a wild card, the first player chooses the color
                if (firstCard.Type == CardType.Wild || firstCard.Type == CardType.DrawFour)
                {
                    _gameState.ChosenWildColor = ChooseWildColor(true); // Human player chooses
                    _gameLog.LogColorChoice(1, _gameState.ChosenWildColor.Value, " for the starting card");
                }
            }
            
            UpdateDeckCount();
            RenderAllPlayers();
            RenderDiscardPile();
            UpdateTurnIndicator();
        }

        private void ShowLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LogPanel.Visibility = ShowLogMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NewGameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Check if a game is in progress
            bool gameInProgress = _gameState.IsGameInProgress();

            if (gameInProgress)
            {
                // Show confirmation dialog
                var result = MessageBox.Show(
                    "A game is currently in progress. Are you sure you want to end the current game and start a new one?",
                    "Confirm New Game",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    return; // User chose not to start a new game
                }
            }

            // Start a new game
            StartNewGame();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.L && 
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                ShowLogMenuItem.IsChecked = !ShowLogMenuItem.IsChecked;
                LogPanel.Visibility = ShowLogMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.N && 
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                NewGameMenuItem_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private CardColor ChooseWildColor(bool isHumanPlayer)
        {
            if (isHumanPlayer)
            {
                // Show dialog for human player
                var dialog = new Window
                {
                    Title = "Choose Color",
                    Width = 300,
                    Height = 280,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var stack = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                var label = new System.Windows.Controls.TextBlock
                {
                    Text = "Choose a color for the Wild card:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 15),
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(label);

                CardColor? selectedColor = null;

                var colors = new[] { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };
                foreach (var color in colors)
                {
                    var button = new System.Windows.Controls.Button
                    {
                        Content = color.ToString(),
                        Height = 30,
                        Margin = new Thickness(0, 5, 0, 0),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold
                    };

                    button.Background = color switch
                    {
                        CardColor.Red => new SolidColorBrush(Color.FromRgb(220, 20, 60)),      // Crimson
                        CardColor.Blue => new SolidColorBrush(Color.FromRgb(30, 144, 255)),    // DodgerBlue
                        CardColor.Green => new SolidColorBrush(Color.FromRgb(50, 205, 50)),    // LimeGreen
                        CardColor.Yellow => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Gold
                        _ => Brushes.Gray
                    };

                    var capturedColor = color;
                    button.Click += (s, e) =>
                    {
                        selectedColor = capturedColor;
                        dialog.DialogResult = true;
                        dialog.Close();
                    };

                    stack.Children.Add(button);
                }

                dialog.Content = stack;
                dialog.ShowDialog();

                return selectedColor ?? CardColor.Red; // Default to Red if dialog is closed
            }
            else
            {
                // AI chooses a color based on what they have most of
                var currentHand = _gameState.GetPlayerHand(_gameState.CurrentPlayer);

                // Count colors in hand
                var colorCounts = new Dictionary<CardColor, int>
                {
                    { CardColor.Red, 0 },
                    { CardColor.Blue, 0 },
                    { CardColor.Green, 0 },
                    { CardColor.Yellow, 0 }
                };

                foreach (var card in currentHand)
                {
                    if (card.Color != CardColor.None)
                    {
                        colorCounts[card.Color]++;
                    }
                }

                // Choose color with most cards
                return colorCounts.OrderByDescending(x => x.Value).First().Key;
            }
        }

        private void CheckForWinner()
        {
            int winner = _gameState.CheckForWinner();
            
            if (winner != -1)
            {
                string winnerName = _gameState.GetPlayerName(winner);
                
                // Log the win
                _gameLog.LogWin(winner);
                
                var result = MessageBox.Show(
                    $"{winnerName} won the game!\n\nWould you like to play again?",
                    "Game Over",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    RestartGame();
                }
                else
                {
                    // Game is over, disable further moves
                    _gameState.GameIsOver = true;
                }
            }
        }

        private void RestartGame()
        {
            // Reset the engine and create new deck
            _engine = new GameEngine();
            
            // Reset all game state
            _gameState.Reset();
            _gameLog.Clear();
            
            // Deal 7 cards to each player
            for (int i = 0; i < 7; i++)
            {
                var card1 = _engine.DrawCard();
                if (card1 != null) _gameState.Player1Hand.Add(card1);
                
                var card2 = _engine.DrawCard();
                if (card2 != null) _gameState.Player2Hand.Add(card2);
                
                var card3 = _engine.DrawCard();
                if (card3 != null) _gameState.Player3Hand.Add(card3);
                
                var card4 = _engine.DrawCard();
                if (card4 != null) _gameState.Player4Hand.Add(card4);
            }
            
            // Start discard pile with one card
            var firstCard = _engine.DrawCard();
            if (firstCard != null)
            {
                _gameState.DiscardPile.Add(firstCard);
                
                // If the first card is a wild card, the first player chooses the color
                if (firstCard.Type == CardType.Wild || firstCard.Type == CardType.DrawFour)
                {
                    _gameState.ChosenWildColor = ChooseWildColor(true); // Human player chooses
                    _gameLog.LogColorChoice(1, _gameState.ChosenWildColor.Value, " for the starting card");
                }
            }
            
            // Refresh the UI
            UpdateDeckCount();
            RenderAllPlayers();
            RenderDiscardPile();
            UpdateTurnIndicator();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void UpdateTurnIndicator()
        {
            // Update labels and turn indicators
            Player1Label.Text = $"You ({_gameState.Player1Hand.Count} cards)";
            Player2Label.Text = $"Alice ({_gameState.Player2Hand.Count} cards)";
            Player3Label.Text = $"Bob ({_gameState.Player3Hand.Count} cards)";
            Player4Label.Text = $"Charlie ({_gameState.Player4Hand.Count} cards)";
            
            // Change fill color instead of visibility to prevent layout shift
            var activeColor = System.Windows.Media.Brushes.Red;
            var inactiveColor = System.Windows.Media.Brushes.Transparent;
            
            Player1TurnIndicator.Fill = _gameState.CurrentPlayer == 1 ? activeColor : inactiveColor;
            Player2TurnIndicator.Fill = _gameState.CurrentPlayer == 2 ? activeColor : inactiveColor;
            Player3TurnIndicator.Fill = _gameState.CurrentPlayer == 3 ? activeColor : inactiveColor;
            Player4TurnIndicator.Fill = _gameState.CurrentPlayer == 4 ? activeColor : inactiveColor;
        }

        private void NextTurn(CardType? actionCardType = null)
        {
            // Don't allow new turns if game is over
            if (_gameState.GameIsOver) return;
            
            // Clear any drawn card from previous turn
            _gameState.JustDrawnCard = null;
            
            // Advance to next player
            _gameState.AdvanceToNextPlayer();
            
            // Handle Draw Four Wild card - next player draws 4 cards and is skipped
            if (actionCardType == CardType.DrawFour)
            {
                var targetPlayer = _gameState.CurrentPlayer;
                var targetHand = _gameState.GetPlayerHand(targetPlayer);
                
                // Draw 4 cards
                var drawnCards = new List<Card>();
                for (int i = 0; i < 4; i++)
                {
                    var card = _engine.DrawCard();
                    if (card != null)
                    {
                        targetHand.Add(card);
                        drawnCards.Add(card);
                    }
                }
                
                // Log the Draw 4 effect with skip
                _gameLog.LogDrawMultiple(targetPlayer, drawnCards.Count, targetPlayer == 1 ? drawnCards : null, " and was skipped!");
                
                // Update displays
                RenderAllPlayers();
                UpdateDeckCount();
                
                // Advance to the next player after the one who drew 4
                _gameState.AdvanceToNextPlayer();
            }
            // Handle Draw Two card - next player draws 2 cards and is skipped
            else if (actionCardType == CardType.DrawTwo)
            {
                var targetPlayer = _gameState.CurrentPlayer;
                var targetHand = _gameState.GetPlayerHand(targetPlayer);
                
                // Draw 2 cards
                var drawnCards = new List<Card>();
                for (int i = 0; i < 2; i++)
                {
                    var card = _engine.DrawCard();
                    if (card != null)
                    {
                        targetHand.Add(card);
                        drawnCards.Add(card);
                    }
                }
                
                // Log the Draw 2 effect with skip
                _gameLog.LogDrawMultiple(targetPlayer, drawnCards.Count, targetPlayer == 1 ? drawnCards : null, " and was skipped!");
                
                // Update displays
                RenderAllPlayers();
                UpdateDeckCount();
                
                // Advance to the next player after the one who drew 2
                _gameState.AdvanceToNextPlayer();
            }
            // Handle Skip card - next player is skipped
            else if (actionCardType == CardType.Skip)
            {
                var skippedPlayer = _gameState.CurrentPlayer;
                _gameLog.LogSkip(skippedPlayer, "!");
                PlaySkipSound();
                
                // Advance to the next player after the skipped one
                _gameState.AdvanceToNextPlayer();
            }
            
            UpdateTurnIndicator();
            
            // If it's an AI player's turn, let them play
            if (_gameState.CurrentPlayer != 1)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    new System.Action(() => AIPlayerTurn()),
                    System.Windows.Threading.DispatcherPriority.Background
                );
            }
        }

        private async void AIPlayerTurn()
        {
            // Small delay so player can see the turn change
            await System.Threading.Tasks.Task.Delay(800);
            
            var currentHand = _gameState.GetPlayerHand(_gameState.CurrentPlayer);

            if (currentHand.Count == 0)
            {
                NextTurn();
                return;
            }

            var topCard = _gameState.GetTopDiscardCard();
            Card? cardToPlay = null;

            // Try to find a playable card
            if (topCard != null)
            {
                cardToPlay = currentHand.FirstOrDefault(c => CanPlayCard(c, topCard));
            }

            if (cardToPlay != null)
            {
                // Play the card
                currentHand.Remove(cardToPlay);
                _gameState.DiscardPile.Add(cardToPlay);
                
                // Log the play
                _gameLog.LogPlay(_gameState.CurrentPlayer, cardToPlay);
                
                // Check if player has one card left (UN!)
                if (currentHand.Count == 1)
                {
                    _gameLog.LogUn(_gameState.CurrentPlayer);
                }
                
                // Handle wild card color selection
                if (cardToPlay.Type == CardType.Wild || cardToPlay.Type == CardType.DrawFour)
                {
                    _gameState.ChosenWildColor = ChooseWildColor(false); // AI chooses
                    _gameLog.LogColorChoice(_gameState.CurrentPlayer, _gameState.ChosenWildColor.Value);
                }
                else
                {
                    // Clear wild color if not a wild card
                    _gameState.ChosenWildColor = null;
                }
                
                // Check if reverse was played
                if (cardToPlay.Type == CardType.Reverse)
                {
                    _gameState.IsClockwise = !_gameState.IsClockwise;
                }
                
                // Update visuals immediately
                RenderAllPlayers();
                RenderDiscardPile();
                UpdateDeckCount();
                
                // Check if this player won
                if (currentHand.Count == 0)
                {
                    CheckForWinner();
                    return; // Don't continue if game is over
                }
                
                // Play sound in background without blocking
                _ = PlayCardSoundAsync();
                
                await System.Threading.Tasks.Task.Delay(300);
                
                // Pass the card type if it's Skip, DrawTwo, or DrawFour
                var actionCard = (cardToPlay.Type == CardType.Skip || cardToPlay.Type == CardType.DrawTwo || cardToPlay.Type == CardType.DrawFour) 
                    ? cardToPlay.Type 
                    : (CardType?)null;
                NextTurn(actionCard);
            }
            else
            {
                // Draw a card
                var drawnCard = _engine.DrawCard();
                if (drawnCard != null)
                {
                    currentHand.Add(drawnCard);
                    
                    // Log the draw
                    _gameLog.LogDraw(_gameState.CurrentPlayer);
                    
                    UpdateDeckCount();
                    RenderAllPlayers();
                    
                    // Check if the drawn card can be played
                    if (topCard != null && CanPlayCard(drawnCard, topCard))
                    {
                        // AI decides to play it (50% chance for variety)
                        if (new System.Random().Next(2) == 0)
                        {
                            await System.Threading.Tasks.Task.Delay(500);
                            currentHand.Remove(drawnCard);
                            _gameState.DiscardPile.Add(drawnCard);
                            
                            // Log the play
                            _gameLog.LogPlay(_gameState.CurrentPlayer, drawnCard);
                            
                            // Check if player has one card left (UN!)
                            if (currentHand.Count == 1)
                            {
                                _gameLog.LogUn(_gameState.CurrentPlayer);
                            }
                            
                            // Handle wild card color selection
                            if (drawnCard.Type == CardType.Wild || drawnCard.Type == CardType.DrawFour)
                            {
                                _gameState.ChosenWildColor = ChooseWildColor(false); // AI chooses
                                _gameLog.LogColorChoice(_gameState.CurrentPlayer, _gameState.ChosenWildColor.Value);
                            }
                            else
                            {
                                // Clear wild color if not a wild card
                                _gameState.ChosenWildColor = null;
                            }
                            
                            // Check if reverse was played
                            if (drawnCard.Type == CardType.Reverse)
                            {
                                _gameState.IsClockwise = !_gameState.IsClockwise;
                            }
                            
                            // Update visuals immediately
                            RenderAllPlayers();
                            RenderDiscardPile();
                            UpdateDeckCount();
                            
                            // Check if this player won
                            if (currentHand.Count == 0)
                            {
                                CheckForWinner();
                                return; // Don't continue if game is over
                            }
                            
                            // Play sound in background without blocking
                            _ = PlayCardSoundAsync();
                            
                            await System.Threading.Tasks.Task.Delay(300);
                            
                            // Pass the card type if it's Skip, DrawTwo, or DrawFour (only when actually played)
                            var actionCard = (drawnCard.Type == CardType.Skip || drawnCard.Type == CardType.DrawTwo || drawnCard.Type == CardType.DrawFour)
                                ? drawnCard.Type
                                : (CardType?)null;
                            NextTurn(actionCard);
                            return; // Exit early since we already called NextTurn
                        }
                    }
                }
                
                // Play sound in background without blocking
                _ = PlayCardSoundAsync();
                
                await System.Threading.Tasks.Task.Delay(300);
                
                // Only get here if card was not played - no action card effect
                NextTurn();
            }
        }

        private void StockPile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Don't allow moves if game is over
            if (_gameState.GameIsOver) return;
            
            // Only allow human player to draw on their turn
            if (_gameState.CurrentPlayer != 1) return;
            
            // If player already drew a card this turn, clicking again passes the turn
            if (_gameState.JustDrawnCard != null)
            {
                _gameState.JustDrawnCard = null; // Clear the drawn card
                PlayCardSound();
                NextTurn(); // Pass turn
                return;
            }
            
            var card = _engine.DrawCard();
            if (card != null)
            {
                _gameState.Player1Hand.Add(card);
                _gameState.JustDrawnCard = card; // Mark this as the only playable card
                
                // Log the draw with the card info (for human player)
                _gameLog.LogDraw(1, card);
                
                RenderPlayer1Hand();
                UpdateDeckCount();
                
                // Play sound
                PlayCardSound();
                
                // Check if the drawn card is playable
                var topCard = _gameState.GetTopDiscardCard();
                if (topCard != null)
                {
                    if (!CanPlayCard(card, topCard))
                    {
                        // Card is not playable, automatically pass turn
                        _gameState.JustDrawnCard = null;
                        NextTurn();
                    }
                    // Otherwise, player can play the drawn card
                }
            }
        }

        private bool CanPlayCard(Card card, Card topCard)
        {
            // Wild cards can always be played
            if (card.Type == CardType.Wild || card.Type == CardType.DrawFour)
            {
                return true;
            }

            // If the top card is a wild card, check against the chosen color
            if ((topCard.Type == CardType.Wild || topCard.Type == CardType.DrawFour) && _gameState.ChosenWildColor.HasValue)
            {
                // Match against the chosen wild color
                if (card.Color == _gameState.ChosenWildColor.Value)
                {
                    return true;
                }
                
                // Number cards of the chosen color can be played
                if (card.Type == CardType.Number && card.Color == _gameState.ChosenWildColor.Value)
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

        private void PlayerCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only allow interaction on player 1's turn
            if (_gameState.CurrentPlayer != 1) return;
            
            if (sender is System.Windows.Controls.Border border)
            {
                _pressedCardBorder = border;
                // Bring to front
                System.Windows.Controls.Panel.SetZIndex(border, 100);
            }
        }

        private void PlayerCard_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Don't allow moves if game is over
            if (_gameState.GameIsOver) return;
            
            if (sender is System.Windows.Controls.Border border && border.Tag is Card card)
            {
                // Check if this is the card we pressed down on
                if (_pressedCardBorder == border)
                {
                    // Check if mouse is still over the card
                    var position = e.GetPosition(border);
                    if (position.X >= 0 && position.X <= border.ActualWidth &&
                        position.Y >= 0 && position.Y <= border.ActualHeight)
                    {
                        // If player drew a card this turn, only allow playing that specific card
                        if (_gameState.JustDrawnCard != null && card != _gameState.JustDrawnCard)
                        {
                            // Can't play any other card after drawing
                            PlayErrorSound();
                            _pressedCardBorder = null;
                            return;
                        }
                        
                        // Check if the card can be played
                        var topCard = _gameState.GetTopDiscardCard();
                        if (topCard != null)
                        {
                            if (CanPlayCard(card, topCard))
                            {
                                // Play success sound (async, doesn't block player)
                                PlayCardSound();
                                
                                // Remove card from player's hand
                                _gameState.Player1Hand.Remove(card);
                                
                                // Add card to discard pile
                                _gameState.DiscardPile.Add(card);
                                
                                // Log the play
                                _gameLog.LogPlay(1, card);
                                
                                // Check if player has one card left (UN!)
                                if (_gameState.Player1Hand.Count == 1)
                                {
                                    _gameLog.LogUn(1);
                                }
                                
                                // Handle wild card color selection
                                if (card.Type == CardType.Wild || card.Type == CardType.DrawFour)
                                {
                                    _gameState.ChosenWildColor = ChooseWildColor(true); // Human chooses with dialog
                                    _gameLog.LogColorChoice(1, _gameState.ChosenWildColor.Value);
                                }
                                else
                                {
                                    // Clear wild color if not a wild card
                                    _gameState.ChosenWildColor = null;
                                }
                                
                                // Check if reverse was played
                                if (card.Type == CardType.Reverse)
                                {
                                    _gameState.IsClockwise = !_gameState.IsClockwise;
                                }
                                
                                // Clear the just drawn card since we played it
                                _gameState.JustDrawnCard = null;
                                
                                // Update displays
                                RenderPlayer1Hand();
                                RenderDiscardPile();
                                UpdateDeckCount();
                                
                                // Check if player won
                                if (_gameState.Player1Hand.Count == 0)
                                {
                                    CheckForWinner();
                                    return; // Don't continue if game is over
                                }
                                
                                // Next player's turn (pass action card type if Skip, DrawTwo, or DrawFour)
                                var actionCard = (card.Type == CardType.Skip || card.Type == CardType.DrawTwo || card.Type == CardType.DrawFour)
                                    ? card.Type
                                    : (CardType?)null;
                                NextTurn(actionCard);
                            }
                            else
                            {
                                // Card can't be played, move it to the end of the hand
                                _gameState.Player1Hand.Remove(card);
                                _gameState.Player1Hand.Add(card);
                                
                                // Re-render the hand to show new position
                                RenderPlayer1Hand();
                                
                                // Play error sound
                                PlayErrorSound();
                            }
                        }
                        else
                        {
                            // No cards in discard pile (shouldn't happen), allow any card
                            PlayCardSound();
                            _gameState.Player1Hand.Remove(card);
                            _gameState.DiscardPile.Add(card);
                            RenderPlayer1Hand();
                            RenderDiscardPile();
                            UpdateDeckCount();
                        }
                    }
                    else
                    {
                        // Mouse left the card, reset it
                        border.Margin = new Thickness(0, 0, -60, 0);
                        System.Windows.Controls.Panel.SetZIndex(border, 0);
                    }
                    
                    _pressedCardBorder = null;
                }
            }
        }

        private void PlayerCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && _pressedCardBorder == border)
            {
                // Reset the Z-index if mouse leaves while pressed
                System.Windows.Controls.Panel.SetZIndex(border, 0);
            }
        }

        private void UpdateDeckCount()
        {
            StockPileLabel.Text = $"Stock Pile ({_engine.DeckCount})";
            Player1Label.Text = $"You ({_gameState.Player1Hand.Count} cards)";
            Player2Label.Text = $"Alice ({_gameState.Player2Hand.Count} cards)";
            Player3Label.Text = $"Bob ({_gameState.Player3Hand.Count} cards)";
            Player4Label.Text = $"Charlie ({_gameState.Player4Hand.Count} cards)";
        }

        private void RenderAllPlayers()
        {
            RenderPlayer1Hand();
            RenderPlayer2Hand();
            RenderPlayer3Hand();
            RenderPlayer4Hand();
        }

        private void RenderPlayer1Hand()
        {
            Player1HandDisplay.Items.Clear();
            
            // Calculate dynamic overlap based on number of cards
            // More aggressive overlap to fit all cards in available space
            int cardCount = _gameState.Player1Hand.Count;
            int baseOverlap = -75;  // Increased from -60
            int overlap = cardCount <= 7 ? baseOverlap : Math.Max(-95, baseOverlap - (cardCount - 7) * 4);
            
            foreach (var card in _gameState.Player1Hand)
            {
                // Outer border (thin black)
                var outerBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, overlap, 0),  // Dynamic overlap
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = card  // Store the card reference
                };

                // Add mouse handlers to outer border
                outerBorder.MouseLeftButtonDown += PlayerCard_MouseDown;
                outerBorder.MouseLeftButtonUp += PlayerCard_MouseUp;
                outerBorder.MouseLeave += PlayerCard_MouseLeave;

                // Inner border (thick white)
                var cardBorder = new System.Windows.Controls.Border
                {
                    BorderBrush = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(4),
                    CornerRadius = new CornerRadius(2)
                };

                // Set background color
                cardBorder.Background = card.Type switch
                {
                    CardType.Wild => System.Windows.Media.Brushes.Black,
                    CardType.DrawFour => System.Windows.Media.Brushes.Black,
                    _ => card.Color switch
                    {
                        CardColor.Red => new SolidColorBrush(Color.FromRgb(220, 20, 60)),      // Crimson
                        CardColor.Blue => new SolidColorBrush(Color.FromRgb(30, 144, 255)),    // DodgerBlue
                        CardColor.Green => new SolidColorBrush(Color.FromRgb(50, 205, 50)),    // LimeGreen
                        CardColor.Yellow => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Gold
                        _ => System.Windows.Media.Brushes.LightGray
                    }
                };

                var grid = new System.Windows.Controls.Grid();

                // Top-left indicator showing card value/type prominently
                var topLeft = new System.Windows.Controls.TextBlock
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(6),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // Set the value text for top-left
                if (card.Type == CardType.Number && card.Number.HasValue)
                {
                    topLeft.Text = card.Number.Value.ToString();
                }
                else
                {
                    topLeft.Text = card.Type switch
                    {
                        CardType.Skip => "S",
                        CardType.Reverse => "R",
                        CardType.DrawTwo => "+2",
                        CardType.Wild => "W",
                        CardType.DrawFour => "+4",
                        _ => string.Empty
                    };
                }

                // Center text (emoji or symbol)
                var center = new System.Windows.Controls.TextBlock
                {
                    FontSize = 32,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (card.Type == CardType.Number && card.Number.HasValue)
                {
                    center.Text = card.Number.Value.ToString();
                }
                else
                {
                    center.Text = card.Type switch
                    {
                        CardType.Skip => "🚫",
                        CardType.Reverse => "🔁",
                        CardType.DrawTwo => "+2",
                        CardType.Wild => "W",
                        CardType.DrawFour => "+4",
                        _ => string.Empty
                    };
                }

                // Set text color
                var fg = (card.Type == CardType.Wild || card.Type == CardType.DrawFour)
                    ? System.Windows.Media.Brushes.White
                    : System.Windows.Media.Brushes.Black;
                topLeft.Foreground = fg;
                center.Foreground = fg;

                grid.Children.Add(topLeft);
                grid.Children.Add(center);
                cardBorder.Child = grid;
                outerBorder.Child = cardBorder;

                Player1HandDisplay.Items.Add(outerBorder);
            }
        }

        private void RenderPlayer2Hand()
        {
            Player2HandDisplay.Items.Clear();
            
            // Calculate dynamic overlap based on number of cards
            // More aggressive overlap to fit all cards in available space
            int cardCount = _gameState.Player2Hand.Count;
            int baseOverlap = -75;  // Increased from -50
            int overlap = cardCount <= 7 ? baseOverlap : Math.Max(-95, baseOverlap - (cardCount - 7) * 4);
            
            foreach (var card in _gameState.Player2Hand)
            {
                // Outer border (thin black)
                var outerBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, overlap, 0),  // Dynamic overlap
                    Background = System.Windows.Media.Brushes.Black
                };

                // Inner border (thick white)
                var cardBorder = new System.Windows.Controls.Border
                {
                    BorderBrush = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(4),
                    CornerRadius = new CornerRadius(2),
                    Background = System.Windows.Media.Brushes.Black
                };

                var text = new System.Windows.Controls.TextBlock
                {
                    Text = "UN",
                    FontSize = 36,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                cardBorder.Child = text;
                outerBorder.Child = cardBorder;
                Player2HandDisplay.Items.Add(outerBorder);
            }
        }

        private void RenderPlayer3Hand()
        {
            Player3HandDisplay.Items.Clear();
            
            // Calculate dynamic overlap based on number of cards
            // Rotated cards need more overlap (140px dimension vs 100px) - multiply by ~1.4
            // More aggressive overlap to fit all cards in available space
            int cardCount = _gameState.Player3Hand.Count;
            int baseOverlap = -105;  // -75 * 1.4 ≈ -105 to match horizontal card overlap visually
            int overlap = cardCount <= 7 ? baseOverlap : Math.Max(-135, baseOverlap - (cardCount - 7) * 5);
            
            foreach (var card in _gameState.Player3Hand)
            {
                // Outer border (thin black)
                var outerBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 0, overlap),  // Dynamic vertical overlap
                    Background = System.Windows.Media.Brushes.Black,
                    RenderTransform = new RotateTransform(90),  // Rotate 90 degrees clockwise
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                // Inner border (thick white)
                var cardBorder = new System.Windows.Controls.Border
                {
                    BorderBrush = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(4),
                    CornerRadius = new CornerRadius(2),
                    Background = System.Windows.Media.Brushes.Black
                };

                var text = new System.Windows.Controls.TextBlock
                {
                    Text = "UN",
                    FontSize = 36,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                cardBorder.Child = text;
                outerBorder.Child = cardBorder;
                Player3HandDisplay.Items.Add(outerBorder);
            }
        }

        private void RenderPlayer4Hand()
        {
            Player4HandDisplay.Items.Clear();
            
            // Calculate dynamic overlap based on number of cards
            // Rotated cards need more overlap (140px dimension vs 100px) - multiply by ~1.4
            // More aggressive overlap to fit all cards in available space
            int cardCount = _gameState.Player4Hand.Count;
            int baseOverlap = -105;  // -75 * 1.4 ≈ -105 to match horizontal card overlap visually
            int overlap = cardCount <= 7 ? baseOverlap : Math.Max(-135, baseOverlap - (cardCount - 7) * 5);
            
            foreach (var card in _gameState.Player4Hand)
            {
                // Outer border (thin black)
                var outerBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 0, overlap),  // Dynamic vertical overlap
                    Background = System.Windows.Media.Brushes.Black,
                    RenderTransform = new RotateTransform(-90),  // Rotate 90 degrees counter-clockwise
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                // Inner border (thick white)
                var cardBorder = new System.Windows.Controls.Border
                {
                    BorderBrush = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(4),
                    CornerRadius = new CornerRadius(2),
                    Background = System.Windows.Media.Brushes.Black
                };

                var text = new System.Windows.Controls.TextBlock
                {
                    Text = "UN",
                    FontSize = 36,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                cardBorder.Child = text;
                outerBorder.Child = cardBorder;
                Player4HandDisplay.Items.Add(outerBorder);
            }
        }

        private void RenderDiscardPile()
        {
            if (_gameState.DiscardPile.Count == 0)
            {
                DiscardPileDisplay.Background = System.Windows.Media.Brushes.LightGray;
                DiscardCenter.Text = "Empty";
                DiscardCenter.Foreground = System.Windows.Media.Brushes.Black;
                WildColorIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            // Show the top card of the discard pile
            var topCard = _gameState.GetTopDiscardCard();
            if (topCard == null) return;

            // Set background color - Wild cards always have black background
            DiscardPileDisplay.Background = topCard.Type switch
            {
                CardType.Wild => System.Windows.Media.Brushes.Black,
                CardType.DrawFour => System.Windows.Media.Brushes.Black,
                _ => topCard.Color switch
                {
                    CardColor.Red => new SolidColorBrush(Color.FromRgb(220, 20, 60)),      // Crimson
                    CardColor.Blue => new SolidColorBrush(Color.FromRgb(30, 144, 255)),    // DodgerBlue
                    CardColor.Green => new SolidColorBrush(Color.FromRgb(50, 205, 50)),    // LimeGreen
                    CardColor.Yellow => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Gold
                    _ => System.Windows.Media.Brushes.LightGray
                }
            };

            // Show color indicator for wild cards with chosen color
            if ((topCard.Type == CardType.Wild || topCard.Type == CardType.DrawFour) && _gameState.ChosenWildColor.HasValue)
            {
                WildColorIndicator.Visibility = Visibility.Visible;
                WildColorIndicator.Background = _gameState.ChosenWildColor.Value switch
                {
                    CardColor.Red => new SolidColorBrush(Color.FromRgb(220, 20, 60)),      // Crimson
                    CardColor.Blue => new SolidColorBrush(Color.FromRgb(30, 144, 255)),    // DodgerBlue
                    CardColor.Green => new SolidColorBrush(Color.FromRgb(50, 205, 50)),    // LimeGreen
                    CardColor.Yellow => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Gold
                    _ => System.Windows.Media.Brushes.Gray
                };
                WildColorText.Text = _gameState.ChosenWildColor.Value.ToString().ToUpper();
            }
            else
            {
                WildColorIndicator.Visibility = Visibility.Collapsed;
            }

            // Center text
            if (topCard.Type == CardType.Number && topCard.Number.HasValue)
            {
                DiscardCenter.Text = topCard.Number.Value.ToString();
            }
            else
            {
                DiscardCenter.Text = topCard.Type switch
                {
                    CardType.Skip => "🚫",
                    CardType.Reverse => "🔁",
                    CardType.DrawTwo => "+2",
                    CardType.Wild => "W",
                    CardType.DrawFour => "+4",
                    _ => string.Empty
                };
            }

            // Set text color - wild cards always use white text on black
            var fg = (topCard.Type == CardType.Wild || topCard.Type == CardType.DrawFour)
                ? System.Windows.Media.Brushes.White
                : System.Windows.Media.Brushes.Black;
            DiscardCenter.Foreground = fg;
        }
    }
}
