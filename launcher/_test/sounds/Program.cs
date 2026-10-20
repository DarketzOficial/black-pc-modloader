using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackLauncher
{
    /// <summary>
    /// Dev tool around <see cref="SoundBank"/> (the launcher ships without it):
    ///
    ///   sounds.exe pack  &lt;portDir&gt; [sourceDir]   pack the mp3s into &lt;portDir&gt;\launcher.sounds
    ///   sounds.exe check &lt;portDir&gt;                load that file and open every clip with the media stack
    ///
    /// "pack" takes each clip from sourceDir when one is given, else from the port folder itself, else
    /// from this repository's Assets\sounds (where the originals are kept). "check" is the honest test:
    /// it writes each decrypted clip out as the launcher does and asks MediaPlayer to open it, reporting
    /// the length and whether there is audio, so a bank that merely parses but plays as noise is caught.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("use: sounds.exe pack <portDir> [sourceDir] | sounds.exe check <portDir>");
                return 2;
            }

            string mode = args[0].ToLowerInvariant();
            string portDir = Path.GetFullPath(args[1]);
            if (!Directory.Exists(portDir))
            {
                Console.WriteLine("no such folder: " + portDir);
                return 2;
            }

            switch (mode)
            {
                case "pack": return Pack(portDir, args.Length > 2 ? Path.GetFullPath(args[2]) : null);
                case "check": return Check(portDir);
                default:
                    Console.WriteLine("unknown command: " + mode);
                    return 2;
            }
        }

        // ---------------------------------------------------------------- pack ---
        private static int Pack(string portDir, string sourceDir)
        {
            var clips = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (string clip in SoundBank.Clips)
            {
                string path = Locate(sourceDir ?? portDir, clip) ?? Locate(Assets(), clip);
                if (path == null)
                {
                    // A clip the launcher can do without, but say so loudly.
                    Console.WriteLine("  MISSING  " + clip);
                    continue;
                }
                clips[clip] = File.ReadAllBytes(path);
                Console.WriteLine("  packed   {0,-20} {1,9:n0} bytes  key {2}  <- {3}",
                    clip, new FileInfo(path).Length, SoundBank.KeyOf(clip), path);
            }

            if (clips.Count == 0)
            {
                Console.WriteLine("nothing to pack");
                return 1;
            }

            string bank = Path.Combine(portDir, SoundBank.FileName);
            SoundBank.Write(bank, clips);
            Console.WriteLine("wrote {0} ({1:n0} bytes, {2} clips)", bank, new FileInfo(bank).Length, clips.Count);
            return 0;
        }

        // --------------------------------------------------------------- check ---
        private static int Check(string portDir)
        {
            string bank = Path.Combine(portDir, SoundBank.FileName);
            if (!File.Exists(bank))
            {
                Console.WriteLine("no bank: " + bank);
                return 1;
            }

            SoundBank.Bank opened;
            try { opened = SoundBank.Load(bank); }
            catch (Exception e)
            {
                Console.WriteLine("cannot read " + bank + ": " + e.Message);
                return 1;
            }

            Console.WriteLine("{0} ({1:n0} bytes, {2} clips)", bank, new FileInfo(bank).Length, opened.Count);
            int bad = 0;
            foreach (string clip in SoundBank.Clips)
            {
                byte[] data = opened.Get(clip);
                if (data == null)
                {
                    Console.WriteLine("  {0,-20} not in the bank", clip);
                    continue;
                }

                string temp = Path.Combine(Path.GetTempPath(), "sounds-check-" + Guid.NewGuid().ToString("N") + Path.GetExtension(clip));
                File.WriteAllBytes(temp, data);
                string detail;
                bool ok;
                try { ok = TryOpen(temp, out detail); }
                finally { try { File.Delete(temp); } catch (Exception) { } }

                Console.WriteLine("  {0,-20} {1,9:n0} bytes  {2}  {3}", clip, data.Length, ok ? "plays" : "FAILED", detail);
                if (!ok) bad++;
            }
            return bad == 0 ? 0 : 1;
        }

        /// <summary>Open one clip with the same player the launcher uses and report what it made of it.</summary>
        private static bool TryOpen(string path, out string detail)
        {
            detail = "";
            var player = new MediaPlayer();
            bool opened = false, failed = false;
            string error = null;
            var frame = new DispatcherFrame();
            player.MediaOpened += (s, e) => { opened = true; frame.Continue = false; };
            player.MediaFailed += (s, e) =>
            {
                failed = true;
                error = e.ErrorException != null ? e.ErrorException.Message : "media failed";
                frame.Continue = false;
            };

            player.Open(new Uri(path));
            var guard = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Normal,
                (s, e) => frame.Continue = false, Dispatcher.CurrentDispatcher);
            guard.Start();
            Dispatcher.PushFrame(frame);
            guard.Stop();

            if (opened && !failed)
            {
                detail = player.HasAudio ? "audio" : "no audio track";
                if (player.HasAudio && player.NaturalDuration.HasTimeSpan)
                    detail += ", " + player.NaturalDuration.TimeSpan.TotalSeconds.ToString("0.0") + "s";
            }
            else if (error != null) detail = error;

            player.Close();
            return opened && !failed;
        }

        // -------------------------------------------------------------- files ---
        private static string Locate(string dir, string clip)
        {
            if (string.IsNullOrEmpty(dir)) return null;
            string path = Path.Combine(dir, clip);
            return File.Exists(path) ? path : null;
        }

        /// <summary>This repository's Assets\sounds, found by walking up from the built tool.</summary>
        private static string Assets()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int up = 0; up < 8 && dir != null; up++, dir = dir.Parent)
            {
                string path = Path.Combine(dir.FullName, "Assets", "sounds");
                if (Directory.Exists(path)) return path;
                if (File.Exists(Path.Combine(dir.FullName, "BlackLauncher.csproj"))) return null;
            }
            return null;
        }
    }
}
