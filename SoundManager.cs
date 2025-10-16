using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace Un
{
    /// <summary>
    /// Manages all game sound effects, including loading and playing sounds.
    /// </summary>
    public class SoundManager : IDisposable
    {
        private SoundPlayer? _cardPlaySound;
        private SoundPlayer? _errorSound;
        private SoundPlayer? _skipSound;
        
        /// <summary>
        /// Initializes the sound manager and loads all sound files.
        /// </summary>
        public SoundManager()
        {
            LoadSounds();
        }
        
        /// <summary>
        /// Loads all sound files from the Sounds directory.
        /// </summary>
        private void LoadSounds()
        {
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var cardPlayPath = Path.Combine(baseDirectory, "Sounds", "card_play.wav");
                var errorPath = Path.Combine(baseDirectory, "Sounds", "error.wav");
                var skipPath = Path.Combine(baseDirectory, "Sounds", "skip.wav");
                
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
                // Silently continue - sounds are not critical to gameplay
            }
        }
        
        /// <summary>
        /// Plays the card play sound effect asynchronously.
        /// </summary>
        public async Task PlayCardSoundAsync()
        {
            if (_cardPlaySound != null)
            {
                // Play sound on background thread to not block
                await Task.Run(() => _cardPlaySound.Play());
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }
        
        /// <summary>
        /// Plays the error sound effect asynchronously.
        /// </summary>
        public async Task PlayErrorSoundAsync()
        {
            if (_errorSound != null)
            {
                // Play sound on background thread to not block
                await Task.Run(() => _errorSound.Play());
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
        
        /// <summary>
        /// Plays the skip sound effect asynchronously.
        /// </summary>
        public async Task PlaySkipSoundAsync()
        {
            if (_skipSound != null)
            {
                // Play sound on background thread to not block
                await Task.Run(() => _skipSound.Play());
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }
        
        /// <summary>
        /// Disposes of sound resources.
        /// </summary>
        public void Dispose()
        {
            _cardPlaySound?.Dispose();
            _errorSound?.Dispose();
            _skipSound?.Dispose();
        }
    }
}
