using System.Windows;
using System.Windows.Media;
using Un.Dialogs;

namespace Un
{
    public partial class MainWindow : Window, IDisposable
    {
        private GameEngine _engine = new GameEngine();
        private System.Windows.Controls.Border? _pressedCardBorder;
        private SoundManager _soundManager;
        private bool _disposed = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Subscribe to Closed event to dispose resources
            Closed += (s, e) => Dispose();
            
            // Set log to be visible by default
            ShowLogMenuItem.IsChecked = true;
            
            // Initialize sound manager
            _soundManager = new SoundManager();
            
            // Setup game engine and subscribe to events
            SetupEngine();
            
            // Start the first game
            StartNewGame();
        }

        private void SetupEngine()
        {
            // Subscribe to game log events for UI updates
            _engine.Log.LogEntryAdded += GameLog_LogEntryAdded;
            GameLogDisplay.ItemsSource = _engine.Log.Entries;
        }

        private void GameLog_LogEntryAdded(object? sender, EventArgs e)
        {
            // Use Dispatcher to ensure this runs after the UI has updated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Only auto-scroll if the user is currently at the bottom
                // This prevents interrupting manual scrolling
                double tolerance = 5.0; // Small tolerance for floating point comparison
                bool isAtBottom = LogScrollViewer.VerticalOffset >= 
                                  LogScrollViewer.ScrollableHeight - tolerance;
                
                if (isAtBottom || LogScrollViewer.ScrollableHeight == 0)
                {
                    LogScrollViewer.ScrollToBottom();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void StartNewGame()
        {
            // Reset all game state
            _engine = new GameEngine();
            SetupEngine(); // Resubscribe to new engine's log events
            _pressedCardBorder = null;
            _engine.Log.Clear();

            // Deal cards to all players and get the first discard card
            var firstCard = _engine.DealNewGame();
            
            if (firstCard != null)
            {
                _engine.State.DiscardPile.Add(firstCard);
                
                // If the first card is a wild card, the first player chooses the color
                if (firstCard.Type == CardType.Wild || firstCard.Type == CardType.DrawFour)
                {
                    _engine.State.ChosenWildColor = ChooseWildColor(true); // Human player chooses
                    _engine.Log.LogColorChoice(1, _engine.State.ChosenWildColor.Value, " for the starting card");
                }
            }
            
            // Refresh the UI
            UpdateDeckCount();
            RenderAllPlayers();
            RenderStockPile();
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
            bool gameInProgress = _engine.State.IsGameInProgress();

            if (gameInProgress)
            {
                // Show confirmation dialog
                if (!NewGameConfirmationDialog.Show())
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
                return ColorChoiceDialog.Show(this);
            }
            else
            {
                // AI chooses a color based on what they have most of
                var currentHand = _engine.State.GetPlayerHand(_engine.State.CurrentPlayer);

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
            int winner = _engine.State.CheckForWinner();
            
            if (winner != -1)
            {
                string winnerName = _engine.State.GetPlayerName(winner);
                
                // Log the win
                _engine.Log.LogWin(winner);
                
                if (GameOverDialog.Show(winnerName))
                {
                    StartNewGame();
                }
                else
                {
                    // Game is over, disable further moves
                    _engine.State.GameIsOver = true;
                }
            }
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
            Player1Label.Text = $"{_engine.State.GetPlayerName(1)} ({_engine.State.Player1Hand.Count} cards)";
            Player2Label.Text = $"{_engine.State.GetPlayerName(2)} ({_engine.State.Player2Hand.Count} cards)";
            Player3Label.Text = $"{_engine.State.GetPlayerName(3)} ({_engine.State.Player3Hand.Count} cards)";
            Player4Label.Text = $"{_engine.State.GetPlayerName(4)} ({_engine.State.Player4Hand.Count} cards)";
            
            // Change fill color instead of visibility to prevent layout shift
            var activeColor = System.Windows.Media.Brushes.Red;
            var inactiveColor = System.Windows.Media.Brushes.Transparent;
            
            Player1TurnIndicator.Fill = _engine.State.CurrentPlayer == 1 ? activeColor : inactiveColor;
            Player2TurnIndicator.Fill = _engine.State.CurrentPlayer == 2 ? activeColor : inactiveColor;
            Player3TurnIndicator.Fill = _engine.State.CurrentPlayer == 3 ? activeColor : inactiveColor;
            Player4TurnIndicator.Fill = _engine.State.CurrentPlayer == 4 ? activeColor : inactiveColor;
        }

        private void NextTurn(CardType? actionCardType = null)
        {
            // Don't allow new turns if game is over
            if (_engine.State.GameIsOver) return;
            
            // Clear any drawn card from previous turn
            _engine.State.JustDrawnCard = null;
            
            // Advance to next player
            _engine.State.AdvanceToNextPlayer();
            
            // Handle Draw Four Wild card - next player draws 4 cards and is skipped
            if (actionCardType == CardType.DrawFour)
            {
                var targetPlayer = _engine.State.CurrentPlayer;
                var targetHand = _engine.State.GetPlayerHand(targetPlayer);
                
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
                _engine.Log.LogDrawMultiple(targetPlayer, drawnCards.Count, targetPlayer == 1 ? drawnCards : null, " and was skipped!");
                
                // Update displays
                RenderAllPlayers();
                UpdateDeckCount();
                
                // Advance to the next player after the one who drew 4
                _engine.State.AdvanceToNextPlayer();
            }
            // Handle Draw Two card - next player draws 2 cards and is skipped
            else if (actionCardType == CardType.DrawTwo)
            {
                var targetPlayer = _engine.State.CurrentPlayer;
                var targetHand = _engine.State.GetPlayerHand(targetPlayer);
                
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
                _engine.Log.LogDrawMultiple(targetPlayer, drawnCards.Count, targetPlayer == 1 ? drawnCards : null, " and was skipped!");
                
                // Update displays
                RenderAllPlayers();
                UpdateDeckCount();
                
                // Advance to the next player after the one who drew 2
                _engine.State.AdvanceToNextPlayer();
            }
            // Handle Skip card - next player is skipped
            else if (actionCardType == CardType.Skip)
            {
                var skippedPlayer = _engine.State.CurrentPlayer;
                _engine.Log.LogSkip(skippedPlayer, "!");
                _ = _soundManager.PlaySkipSoundAsync();
                
                // Advance to the next player after the skipped one
                _engine.State.AdvanceToNextPlayer();
            }
            
            UpdateTurnIndicator();
            
            // If it's an AI player's turn, let them play
            if (_engine.State.CurrentPlayer != 1)
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
            
            var currentHand = _engine.State.GetPlayerHand(_engine.State.CurrentPlayer);

            if (currentHand.Count == 0)
            {
                NextTurn();
                return;
            }

            var topCard = _engine.State.GetTopDiscardCard();
            Card? cardToPlay = null;

            // Try to find a playable card
            if (topCard != null)
            {
                cardToPlay = currentHand.FirstOrDefault(c => _engine.CanPlayCard(c, topCard));
            }

            if (cardToPlay != null)
            {
                // Try to play the card using GameEngine
                if (_engine.TryPlayCard(_engine.State.CurrentPlayer, cardToPlay, () => ChooseWildColor(false)))
                {
                    // Update visuals immediately
                    RenderAllPlayers();
                    RenderDiscardPile();
                    UpdateDeckCount();
                    
                    // Check if this player won
                    if (_engine.State.Players[_engine.State.CurrentPlayer - 1].Hand.Count == 0)
                    {
                        CheckForWinner();
                        return; // Don't continue if game is over
                    }
                    
                    // Play sound in background without blocking
                    _ = _soundManager.PlayCardSoundAsync();
                    
                    await System.Threading.Tasks.Task.Delay(300);
                    
                    // Pass the card type if it's Skip, DrawTwo, or DrawFour
                    var actionCard = (cardToPlay.Type == CardType.Skip || cardToPlay.Type == CardType.DrawTwo || cardToPlay.Type == CardType.DrawFour) 
                        ? cardToPlay.Type 
                        : (CardType?)null;
                    NextTurn(actionCard);
                }
            }
            else
            {
                // Draw a card
                var drawnCard = _engine.DrawCard();
                if (drawnCard != null)
                {
                    currentHand.Add(drawnCard);
                    
                    // Log the draw
                    _engine.Log.LogDraw(_engine.State.CurrentPlayer);
                    
                    UpdateDeckCount();
                    RenderAllPlayers();
                    
                    // Check if the drawn card can be played
                    if (topCard != null && _engine.CanPlayCard(drawnCard, topCard))
                    {
                        // AI decides to play it (50% chance for variety)
                        if (new System.Random().Next(2) == 0)
                        {
                            await System.Threading.Tasks.Task.Delay(500);
                            
                            // Try to play the drawn card using GameEngine
                            if (_engine.TryPlayCard(_engine.State.CurrentPlayer, drawnCard, () => ChooseWildColor(false)))
                            {
                                // Update visuals immediately
                                RenderAllPlayers();
                                RenderDiscardPile();
                                UpdateDeckCount();
                                
                                // Check if this player won
                                if (_engine.State.Players[_engine.State.CurrentPlayer - 1].Hand.Count == 0)
                                {
                                    CheckForWinner();
                                    return; // Don't continue if game is over
                                }
                                
                                // Play sound in background without blocking
                                _ = _soundManager.PlayCardSoundAsync();
                                
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
                }
                
                // Play sound in background without blocking
                _ = _soundManager.PlayCardSoundAsync();
                
                await System.Threading.Tasks.Task.Delay(300);
                
                // Only get here if card was not played - no action card effect
                NextTurn();
            }
        }

        private void StockPile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Don't allow moves if game is over
            if (_engine.State.GameIsOver) return;
            
            // Only allow human player to draw on their turn
            if (_engine.State.CurrentPlayer != 1) return;
            
            // If player already drew a card this turn, clicking again passes the turn
            if (_engine.State.JustDrawnCard != null)
            {
                _engine.State.JustDrawnCard = null; // Clear the drawn card
                _ = _soundManager.PlayCardSoundAsync();
                NextTurn(); // Pass turn
                return;
            }
            
            var card = _engine.DrawCard();
            if (card != null)
            {
                _engine.State.Player1Hand.Add(card);
                _engine.State.JustDrawnCard = card; // Mark this as the only playable card
                
                // Log the draw with the card info (for human player)
                _engine.Log.LogDraw(1, card);
                
                RenderPlayer1Hand();
                UpdateDeckCount();
                
                // Play sound
                _ = _soundManager.PlayCardSoundAsync();
                
                // Check if the drawn card is playable
                var topCard = _engine.State.GetTopDiscardCard();
                if (topCard != null)
                {
                    if (!_engine.CanPlayCard(card, topCard))
                    {
                        // Card is not playable, automatically pass turn
                        _engine.State.JustDrawnCard = null;
                        NextTurn();
                    }
                    // Otherwise, player can play the drawn card
                }
            }
        }

        private void PlayerCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only allow interaction on player 1's turn
            if (_engine.State.CurrentPlayer != 1) return;
            
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
            if (_engine.State.GameIsOver) return;
            
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
                        if (_engine.State.JustDrawnCard != null && card != _engine.State.JustDrawnCard)
                        {
                            // Can't play any other card after drawing
                            _ = _soundManager.PlayErrorSoundAsync();
                            _pressedCardBorder = null;
                            return;
                        }
                        
                        // Try to play the card using GameEngine
                        if (_engine.TryPlayCard(1, card, () => ChooseWildColor(true)))
                        {
                            // Play success sound (async, doesn't block player)
                            _ = _soundManager.PlayCardSoundAsync();
                            
                            // Clear the just drawn card since we played it
                            _engine.State.JustDrawnCard = null;
                            
                            // Update displays
                            RenderPlayer1Hand();
                            RenderDiscardPile();
                            UpdateDeckCount();
                            
                            // Check if player won
                            if (_engine.State.Player1Hand.Count == 0)
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
                            _engine.State.Player1Hand.Remove(card);
                            _engine.State.Player1Hand.Add(card);
                            
                            // Re-render the hand to show new position
                            RenderPlayer1Hand();
                            
                            // Play error sound
                            _ = _soundManager.PlayErrorSoundAsync();
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
            Player1Label.Text = $"{_engine.State.GetPlayerName(1)} ({_engine.State.Player1Hand.Count} cards)";
            Player2Label.Text = $"{_engine.State.GetPlayerName(2)} ({_engine.State.Player2Hand.Count} cards)";
            Player3Label.Text = $"{_engine.State.GetPlayerName(3)} ({_engine.State.Player3Hand.Count} cards)";
            Player4Label.Text = $"{_engine.State.GetPlayerName(4)} ({_engine.State.Player4Hand.Count} cards)";
        }

        private void RenderAllPlayers()
        {
            RenderPlayer1Hand();
            RenderPlayer2Hand();
            RenderPlayer3Hand();
            RenderPlayer4Hand();
        }

        private void RenderPlayerHand(int playerNumber)
        {
            var player = _engine.State.Players[playerNumber - 1];
            var hand = player.Hand;
            var isHuman = player.IsHuman;
            
            // Get the appropriate display control
            System.Windows.Controls.ItemsControl display = playerNumber switch
            {
                1 => Player1HandDisplay,
                2 => Player2HandDisplay,
                3 => Player3HandDisplay,
                4 => Player4HandDisplay,
                _ => throw new ArgumentException($"Invalid player number: {playerNumber}")
            };
            
            display.Items.Clear();
            
            // Calculate dynamic overlap based on number of cards and orientation
            int cardCount = hand.Count;
            bool isVertical = playerNumber == 3 || playerNumber == 4;
            int baseOverlap = isVertical ? -105 : -75;
            int overlap = cardCount <= 7 ? baseOverlap : Math.Max(isVertical ? -135 : -95, baseOverlap - (cardCount - 7) * (isVertical ? 5 : 4));
            
            // Determine rotation for side players
            RotateTransform? rotation = playerNumber switch
            {
                3 => new RotateTransform(90),   // Left player rotates clockwise
                4 => new RotateTransform(-90),  // Right player rotates counter-clockwise
                _ => null
            };
            
            foreach (var card in hand)
            {
                // Create card visual using SVG renderer
                var cardVisual = CardRenderer.CreateCardVisual(card, showBack: !isHuman);
                
                // Wrap in outer border for consistent sizing and interaction
                var outerBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    Margin = isVertical ? new Thickness(0, 0, 0, overlap) : new Thickness(0, 0, overlap, 0),
                    Cursor = isHuman ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow,
                    Tag = isHuman ? card : null,
                    Child = cardVisual
                };
                
                // Apply rotation for side players
                if (rotation != null)
                {
                    outerBorder.RenderTransform = rotation;
                    outerBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                // Add mouse handlers only for human player
                if (isHuman)
                {
                    outerBorder.MouseLeftButtonDown += PlayerCard_MouseDown;
                    outerBorder.MouseLeftButtonUp += PlayerCard_MouseUp;
                    outerBorder.MouseLeave += PlayerCard_MouseLeave;
                }

                display.Items.Add(outerBorder);
            }
        }

        private void RenderPlayer1Hand()
        {
            RenderPlayerHand(1);
        }

        private void RenderPlayer2Hand()
        {
            RenderPlayerHand(2);
        }

        private void RenderPlayer3Hand()
        {
            RenderPlayerHand(3);
        }

        private void RenderPlayer4Hand()
        {
            RenderPlayerHand(4);
        }

        private void RenderDiscardPile()
        {
            DiscardPileContainer.Child = null;
            
            if (_engine.State.DiscardPile.Count == 0)
            {
                // Show empty placeholder
                var emptyBorder = new System.Windows.Controls.Border
                {
                    Width = 100,
                    Height = 140,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Background = System.Windows.Media.Brushes.LightGray
                };
                
                var emptyText = new System.Windows.Controls.TextBlock
                {
                    Text = "Empty",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                emptyBorder.Child = emptyText;
                DiscardPileContainer.Child = emptyBorder;
                return;
            }

            // Show the top card of the discard pile using SVG
            var topCard = _engine.State.GetTopDiscardCard();
            if (topCard == null) return;

            var cardVisual = CardRenderer.CreateCardVisual(topCard, showBack: false);
            DiscardPileContainer.Child = cardVisual;
        }

        private void RenderStockPile()
        {
            // Always show the card back for stock pile
            StockPileContainer.Child = null;
            
            // Create a dummy card just to get the back SVG
            var dummyCard = new Card(CardType.Number, CardColor.Red, 0);
            var cardBack = CardRenderer.CreateCardVisual(dummyCard, showBack: true);
            
            StockPileContainer.Child = cardBack;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _soundManager?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
