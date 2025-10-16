using System.Windows;

namespace Un.Dialogs
{
    /// <summary>
    /// Dialog for confirming a new game when a game is in progress.
    /// </summary>
    public static class NewGameConfirmationDialog
    {
        /// <summary>
        /// Shows a confirmation dialog asking if the user wants to start a new game.
        /// </summary>
        /// <returns>True if the user confirms, false otherwise</returns>
        public static bool Show()
        {
            var result = MessageBox.Show(
                "A game is currently in progress. Are you sure you want to end the current game and start a new one?",
                "Confirm New Game",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }
    }
}
