using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Un
{
    public class GameLog
    {
        private readonly ObservableCollection<TextBlock> _logEntries;
        private readonly GameState _gameState;

        public ObservableCollection<TextBlock> Entries => _logEntries;

        /// <summary>
        /// Event raised when a new log entry is added, allowing subscribers to handle UI updates like scrolling.
        /// </summary>
        public event EventHandler? LogEntryAdded;

        public GameLog(GameState gameState)
        {
            _gameState = gameState;
            _logEntries = new ObservableCollection<TextBlock>();
        }

        public void Clear()
        {
            _logEntries.Clear();
        }

        public void LogPlay(int playerNumber, Card card, string action = "played")
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold });
            logEntry.Inlines.Add(new Run($" {action} "));
            logEntry.Inlines.Add(new Run(card.ToString()) { FontWeight = FontWeights.SemiBold });

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogDraw(int playerNumber, Card? drawnCard = null)
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold });
            
            // For human player (player 1), show the card that was drawn
            if (playerNumber == 1 && drawnCard != null)
            {
                logEntry.Inlines.Add(new Run(" drew "));
                logEntry.Inlines.Add(new Run(drawnCard.ToString()) { FontWeight = FontWeights.SemiBold });
            }
            else
            {
                logEntry.Inlines.Add(new Run(" drew a card"));
            }

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogColorChoice(int playerNumber, CardColor chosenColor, string context = "")
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2),
                FontStyle = FontStyles.Italic
            };

            if (playerNumber == 1)
            {
                logEntry.Inlines.Add(new Run("You chose ") { FontWeight = FontWeights.Bold });
            }
            else
            {
                logEntry.Inlines.Add(new Run($"{playerName} chose ") { FontWeight = FontWeights.Bold });
            }
            
            logEntry.Inlines.Add(new Run(chosenColor.ToString()) { FontWeight = FontWeights.SemiBold });
            
            if (!string.IsNullOrEmpty(context))
            {
                logEntry.Inlines.Add(new Run(context));
            }

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogDrawMultiple(int playerNumber, int cardCount, System.Collections.Generic.List<Card>? cards = null, string reason = "")
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold });
            logEntry.Inlines.Add(new Run($" drew {cardCount} card{(cardCount > 1 ? "s" : "")}"));

            if (playerNumber == 1 && cards != null && cards.Count > 0)
            {
                logEntry.Inlines.Add(new Run(" ("));
                for (int i = 0; i < cards.Count; i++)
                {
                    if (i > 0) logEntry.Inlines.Add(new Run(", "));
                    logEntry.Inlines.Add(new Run(cards[i].ToString()) { FontWeight = FontWeights.SemiBold });
                }
                logEntry.Inlines.Add(new Run(")"));
            }

            if (!string.IsNullOrEmpty(reason))
            {
                logEntry.Inlines.Add(new Run(reason));
            }

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogUn(int playerNumber)
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);
            var isSecondPerson = string.Equals(playerName, "You", StringComparison.OrdinalIgnoreCase);

            // Random humorous UN phrases
            var random = new Random();
            string[] phrases;
            
            if (isSecondPerson)
            {
                // Second person verbs for "You"
                phrases = new[]
                {
                    "shout UN at the top of your lungs!",
                    "scream UN like your life depends on it!",
                    "bellow UN with the fury of a thousand suns!",
                    "yell UN so loud the neighbors complain!",
                    "triumphantly declare UN with a fist pump!",
                    "whisper UN... just kidding, SCREAM IT!",
                    "howl UN like a wolf at the moon!",
                    "proclaim UN with dramatic flair!",
                    "shriek UN and do a little victory dance!",
                    "announce UN as if winning an Oscar!"
                };
            }
            else
            {
                // Third person verbs for other players
                phrases = new[]
                {
                    "shouts UN at the top of their lungs!",
                    "screams UN like their life depends on it!",
                    "bellows UN with the fury of a thousand suns!",
                    "yells UN so loud the neighbors complain!",
                    "triumphantly declares UN with a fist pump!",
                    "whispers UN... just kidding, SCREAMS IT!",
                    "howls UN like a wolf at the moon!",
                    "proclaims UN with dramatic flair!",
                    "shrieks UN and does a little victory dance!",
                    "announces UN as if winning an Oscar!"
                };
            }

            var selectedPhrase = phrases[random.Next(phrases.Length)];

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold });
            logEntry.Inlines.Add(new Run(" " + selectedPhrase) { FontStyle = FontStyles.Italic });

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogWin(int playerNumber)
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run("🎉 ") { FontSize = 16 });
            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold, FontSize = 14 });
            logEntry.Inlines.Add(new Run(" won the game! ") { FontWeight = FontWeights.Bold, FontSize = 14 });
            logEntry.Inlines.Add(new Run("🎉") { FontSize = 16 });

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        public void LogSkip(int playerNumber, string reason = "")
        {
            var playerName = GetPlayerName(playerNumber);
            var playerColor = GetPlayerColor(playerNumber);
            var isSecondPerson = string.Equals(playerName, "You", StringComparison.OrdinalIgnoreCase);

            var logEntry = new TextBlock
            {
                Foreground = playerColor,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };

            logEntry.Inlines.Add(new Run(playerName) { FontWeight = FontWeights.Bold });
            // Use "were" for "You", "was" for other players
            var verb = isSecondPerson ? " were skipped" : " was skipped";
            logEntry.Inlines.Add(new Run(verb));
            
            if (!string.IsNullOrEmpty(reason))
            {
                logEntry.Inlines.Add(new Run(reason));
            }
            else
            {
                logEntry.Inlines.Add(new Run("!"));
            }

            _logEntries.Add(logEntry);
            OnLogEntryAdded();
        }

        private void OnLogEntryAdded()
        {
            LogEntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private string GetPlayerName(int playerNumber)
        {
            return _gameState.GetPlayerName(playerNumber);
        }

        private static Brush GetPlayerColor(int playerNumber)
        {
            return playerNumber switch
            {
                1 => Brushes.Blue,
                2 => Brushes.Red,
                3 => Brushes.Green,
                4 => Brushes.Purple,
                _ => Brushes.Black
            };
        }
    }
}
