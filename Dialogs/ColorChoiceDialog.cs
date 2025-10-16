using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Un.Dialogs
{
    /// <summary>
    /// Dialog for choosing a wild card color.
    /// </summary>
    public static class ColorChoiceDialog
    {
        /// <summary>
        /// Shows a dialog for the player to choose a wild card color.
        /// </summary>
        /// <param name="owner">The owner window</param>
        /// <returns>The chosen color, or Red if the dialog is closed without selection</returns>
        public static CardColor Show(Window owner)
        {
            var dialog = new Window
            {
                Title = "Choose Color",
                Width = 300,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var label = new TextBlock
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
                var button = new Button
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
    }
}
