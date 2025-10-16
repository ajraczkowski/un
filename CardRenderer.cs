using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Svg.Skia;
using SkiaSharp;

namespace Un
{
    public static class CardRenderer
    {
        private static readonly string AssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        /// <summary>
        /// Creates a Border element displaying the card using SVG graphics
        /// </summary>
        public static Border CreateCardVisual(Card card, bool showBack = false, double width = 100, double height = 140)
        {
            var border = new Border
            {
                Width = width,
                Height = height
            };

            try
            {
                string svgPath = GetSvgPath(card, showBack);
                
                if (!File.Exists(svgPath))
                {
                    throw new FileNotFoundException($"SVG card file not found: {svgPath}");
                }

                // Read SVG file
                string svgContent = File.ReadAllText(svgPath);
                
                // For number cards, update the number in the SVG
                if (!showBack && card.Type == CardType.Number && card.Number.HasValue)
                {
                    svgContent = svgContent.Replace(">0<", $">{card.Number.Value}<");
                }
                
                // Load and render SVG using Svg.Skia
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent)))
                {
                    var svg = new SKSvg();
                    svg.Load(stream);
                    
                    if (svg.Picture == null)
                    {
                        throw new InvalidOperationException($"Failed to load SVG: {svgPath}");
                    }

                    // Render at higher resolution for better quality (3x)
                    const int scale = 3;
                    var info = new SKImageInfo((int)(width * scale), (int)(height * scale));
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.Transparent);
                        
                        // Scale to fit the higher resolution
                        float scaleX = (float)(width * scale) / svg.Picture.CullRect.Width;
                        float scaleY = (float)(height * scale) / svg.Picture.CullRect.Height;
                        var matrix = SKMatrix.CreateScale(scaleX, scaleY);
                        
                        canvas.DrawPicture(svg.Picture, ref matrix);
                        canvas.Flush();
                        
                        // Convert to WPF BitmapSource
                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = data.AsStream();
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                            
                            var wpfImage = new Image
                            {
                                Source = bitmapImage,
                                Stretch = Stretch.Fill
                            };
                            
                            border.Child = wpfImage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error and re-throw - missing SVGs should be fatal
                System.Diagnostics.Debug.WriteLine($"FATAL: SVG rendering failed for card {card}: {ex.Message}");
                throw new InvalidOperationException($"Failed to render SVG for card {card}. SVG files must be present in Assets folder.", ex);
            }

            return border;
        }

        /// <summary>
        /// Gets the SVG file path for a given card
        /// </summary>
        private static string GetSvgPath(Card card, bool showBack)
        {
            if (showBack)
            {
                return Path.Combine(AssetsPath, "CardBack.svg");
            }

            string fileName = card.Type switch
            {
                CardType.Number => $"CardFace_{card.Color}.svg",
                CardType.Skip => $"CardFace_{card.Color}_Skip.svg",
                CardType.Reverse => $"CardFace_{card.Color}_Reverse.svg",
                CardType.DrawTwo => $"CardFace_{card.Color}_DrawTwo.svg",
                CardType.Wild => "CardFace_Wild.svg",
                CardType.DrawFour => "CardFace_WildDrawFour.svg",
                _ => "CardBack.svg"
            };

            return Path.Combine(AssetsPath, fileName);
        }
    }
}
