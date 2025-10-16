using System.Windows;

namespace Un.Dialogs
{
    /// <summary>
    /// Dialog shown when the game is over.
    /// </summary>
    public static class GameOverDialog
    {
        /// <summary>
        /// Shows a game over dialog with the winner's name.
        /// </summary>
        /// <param name="winnerName">The name of the winning player</param>
        /// <returns>True if the user wants to play again, false otherwise</returns>
        public static bool Show(string winnerName)
        {
            var result = MessageBox.Show(
                $"{winnerName} won the game!\n\nWould you like to play again?",
                "Game Over",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            return result == MessageBoxResult.Yes;
        }
    }
}
