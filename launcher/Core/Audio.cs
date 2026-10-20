using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace BlackLauncher
{
    /// <summary>
    /// The launcher's own sound: a looping background track and three short interface effects (hover,
    /// select and saved). The clips live encrypted inside <see cref="SoundBank.FileName"/> next to
    /// blacklauncher.exe; a loose mp3 of the same name in the port folder is used instead when the bank
    /// has no entry for it (which is also how the sounds are made: drop the mp3, run the packer).
    /// A missing clip is not an error — that sound simply stays silent — and the footer's speaker button
    /// (<see cref="Muted"/>) silences music and effects together.
    ///
    /// The player is WPF's MediaPlayer, which reads MP3 through the same media stack as the rest of
    /// Windows but only opens files by path, never from memory, so the decrypted bytes are written to a
    /// temporary file that exists for exactly as long as the launcher runs (the handle is held open with
    /// delete-on-close, and anything left behind by a killed run is swept on the next start). It has no
    /// loop switch either, so the track restarts from its start whenever it ends, and each effect keeps a
    /// few players of its own so that quick clicks do not cut each other off.
    /// </summary>
    public static class Audio
    {
        /// <summary>The music plays at half volume (the effects play at full volume).</summary>
        public const double MusicVolume = 1.0;

        /// <summary>Copies of one effect that may overlap (a fast run down the list of settings).</summary>
        private const int Voices = 3;

        /// <summary>How many folders up from the launcher the bank and the loose mp3s are looked for.</summary>
        private const int LookUp = 3;

        /// <summary>The temporary files the decrypted clips are written to.</summary>
        private const string TempPrefix = "black-launcher-";

        private static readonly Dictionary<string, MediaPlayer[]> _pool = new Dictionary<string, MediaPlayer[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _next = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Kept open for the life of the launcher: closing them is what erases the temporary files.</summary>
        private static readonly List<Stream> _temp = new List<Stream>();

        private static MediaPlayer _music;
        private static string _root;
        private static bool _muted;

        public static bool Muted
        {
            get { return _muted; }
            set { _muted = value; ApplyVolume(); }
        }

        /// <summary>Load the sounds that are there and start the music. Safe to call with nothing to play.</summary>
        public static void Init(string root)
        {
            _root = root ?? "";
            SweepTemporary();

            SoundBank.Bank bank = null;
            string bankPath = Find(SoundBank.FileName);
            if (bankPath != null)
            {
                try { bank = SoundBank.Load(bankPath); }
                catch (Exception) { bank = null; }      // damaged bank: play whatever loose mp3s are there
            }

            string music = ClipPath(bank, SoundBank.Music);
            if (music != null)
            {
                try
                {
                    _music = new MediaPlayer { Volume = MusicVolume };
                    _music.MediaFailed += (s, e) => _music = null;
                    _music.MediaEnded += (s, e) =>
                    {
                        try { _music.Position = TimeSpan.Zero; _music.Play(); }
                        catch (Exception) { }
                    };
                    _music.Open(new Uri(music));
                    _music.Play();
                }
                catch (Exception) { _music = null; }
            }

            foreach (string clip in SoundBank.Clips)
            {
                if (clip == SoundBank.Music) continue;
                string path = ClipPath(bank, clip);
                if (path == null) continue;
                var voices = new MediaPlayer[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    try { voices[i] = new MediaPlayer { Volume = 1.0 }; voices[i].Open(new Uri(path)); }
                    catch (Exception) { voices[i] = null; }
                }
                _pool[clip] = voices;
                _next[clip] = 0;
            }

            ApplyVolume();
        }

        public static void Hover() { Play("hover.mp3"); }
        public static void Select() { Play("select.mp3"); }
        public static void Saved() { Play("saved.mp3"); }

        private static void Play(string file)
        {
            if (_muted) return;
            MediaPlayer[] voices;
            if (!_pool.TryGetValue(file, out voices)) return;
            int i = _next[file];
            _next[file] = (i + 1) % Voices;
            var p = voices[i];
            if (p == null) return;
            try { p.Position = TimeSpan.Zero; p.Play(); }
            catch (Exception) { }
        }

        private static void ApplyVolume()
        {
            if (_music == null) return;
            try { _music.Volume = _muted ? 0 : MusicVolume; }
            catch (Exception) { }
        }

        /// <summary>Where the media player is to read one clip from: the decrypted bytes in a temporary
        /// file, else the loose mp3 next to the launcher. Null when the clip is nowhere to be found.</summary>
        private static string ClipPath(SoundBank.Bank bank, string clip)
        {
            if (bank != null)
            {
                byte[] data = bank.Get(clip);
                if (data != null && data.Length > 0)
                {
                    string path = Temporary(data, clip);
                    if (path != null) return path;
                }
            }
            return Find(clip);
        }

        /// <summary>Write one clip's bytes to a temporary file that erases itself when the launcher closes.
        /// The extension is kept: the media stack picks its parser by file name.</summary>
        private static string Temporary(byte[] data, string clip)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(),
                    TempPrefix + Guid.NewGuid().ToString("N") + Path.GetExtension(clip));
                var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
                file.Write(data, 0, data.Length);
                file.Flush();
                _temp.Add(file);
                return path;
            }
            catch (Exception) { return null; }
        }

        /// <summary>Delete temporary clips left behind by a launcher that was killed rather than closed.
        /// Only yesterday's are touched, so a second launcher running right now keeps its own.</summary>
        private static void SweepTemporary()
        {
            try
            {
                foreach (string path in Directory.GetFiles(Path.GetTempPath(), TempPrefix + "*"))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-1)) File.Delete(path);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        /// <summary>The file's full path: the launcher's own folder, then up to a couple of parents
        /// (so a launcher started from a sub-folder still finds the port's sounds). Null when it is not there.</summary>
        private static string Find(string file)
        {
            if (string.IsNullOrEmpty(_root)) return null;
            var dir = new DirectoryInfo(_root);
            for (int up = 0; up < LookUp && dir != null; up++, dir = dir.Parent)
            {
                string path = Path.Combine(dir.FullName, file);
                if (File.Exists(path)) return path;
            }
            return null;
        }
    }
}
