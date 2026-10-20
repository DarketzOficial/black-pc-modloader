using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackLauncher
{
    /// <summary>
    /// The port writes black.log and black-crash-*.log in the port's root. The launcher moves them
    /// into log\ after a game session, so the root stays readable and an old crash is never confused
    /// with the one that just happened.
    /// </summary>
    public static class LogStore
    {
        public const string CrashPattern = "black-crash-*.log";

        public static string Dir(string portDir) => Path.Combine(portDir, "log");
        public static string GameDir(string portDir) => Path.Combine(Dir(portDir), "game");
        public static string CrashDir(string portDir) => Path.Combine(Dir(portDir), "crash");
        public static string LauncherDir(string portDir) => Path.Combine(Dir(portDir), "launcher");

        public static void Ensure(string portDir)
        {
            try
            {
                Directory.CreateDirectory(GameDir(portDir));
                Directory.CreateDirectory(CrashDir(portDir));
                Directory.CreateDirectory(LauncherDir(portDir));
            }
            catch { }
        }

        /// <summary>The crash logs that already exist before a game starts (their names, not paths).</summary>
        public static HashSet<string> SnapshotCrashLogs(string portDir)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string f in Directory.GetFiles(portDir, CrashPattern))
                    set.Add(Path.GetFileName(f));
            }
            catch { }
            return set;
        }

        /// <summary>
        /// Move every root log into log\, returning the ones that appeared during this session.
        /// Files are retried briefly: the port can still be releasing the log handle when this runs.
        /// </summary>
        public static LogArchiveResult ArchiveAfterGame(string portDir, HashSet<string> before, DateTime startedUtc)
        {
            Ensure(portDir);
            var result = new LogArchiveResult();

            var after = new List<string>();
            try { after.AddRange(Directory.GetFiles(portDir, CrashPattern)); } catch { }
            foreach (string source in after.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(source);
                string moved = MoveRetry(source, CrashDir(portDir), name);
                if (moved != null && before != null && !before.Contains(name))
                    result.NewCrashLogs.Add(moved);
            }

            string gameLog = Path.Combine(portDir, "black.log");
            if (File.Exists(gameLog))
            {
                string name = "black-" + SafeTimestamp(File.GetLastWriteTimeUtc(gameLog).ToLocalTime()) + ".log";
                result.GameLog = MoveRetry(gameLog, GameDir(portDir), name);
            }

            result.NewCrashLogs.Sort((a, b) => string.CompareOrdinal(a, b));
            return result;
        }

        /// <summary>
        /// Fold logs left by an older run/direct launch into log\. Called when the launcher opens,
        /// never while the game is running. Returns the crash logs that were in the root, so the
        /// launcher can offer a report for a player who started black.exe without the watchdog.
        /// </summary>
        public static List<string> MigratePortRoot(string portDir)
        {
            var movedCrashes = new List<string>();
            try
            {
                if (Process.GetProcessesByName("black").Length > 0) return movedCrashes;
            }
            catch { }
            Ensure(portDir);
            try
            {
                foreach (string source in Directory.GetFiles(portDir, CrashPattern))
                {
                    string moved = MoveRetry(source, CrashDir(portDir), Path.GetFileName(source));
                    if (moved != null) movedCrashes.Add(moved);
                }
                string gameLog = Path.Combine(portDir, "black.log");
                if (File.Exists(gameLog) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(gameLog)).TotalSeconds > 3)
                {
                    string name = "black-" + SafeTimestamp(File.GetLastWriteTimeUtc(gameLog).ToLocalTime()) + ".log";
                    MoveRetry(gameLog, GameDir(portDir), name);
                }
            }
            catch { }
            return movedCrashes;
        }

        public static string LatestGameLog(string portDir)
        {
            try
            {
                var files = Directory.GetFiles(GameDir(portDir), "*.log");
                if (files.Length == 0) return null;
                return files.OrderByDescending(File.GetLastWriteTimeUtc).First();
            }
            catch { return null; }
        }

        public static string LatestCrashLog(string portDir)
        {
            try
            {
                var files = Directory.GetFiles(CrashDir(portDir), CrashPattern);
                if (files.Length == 0) return null;
                return files.OrderByDescending(File.GetLastWriteTimeUtc).First();
            }
            catch { return null; }
        }

        /// <summary>A crash report for a failure before the port had a chance to write its own log.</summary>
        public static string CreateSyntheticLog(string portDir, string title, string details)
        {
            Ensure(portDir);
            string name = "start-failure-" + SafeTimestamp(DateTime.Now) + ".log";
            string path = UniquePath(CrashDir(portDir), name);
            try
            {
                string text = "BLACK PC - relatorio do launcher\r\n" +
                              "Gerado em " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" +
                              title + "\r\n\r\n" + (details ?? "") + "\r\n";
                File.WriteAllText(path, text, new UTF8Encoding(false));
                return path;
            }
            catch { return null; }
        }

        public static void AppendReport(string portDir, string line)
        {
            try
            {
                Ensure(portDir);
                string path = Path.Combine(LauncherDir(portDir), "reports.log");
                File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n",
                                   new UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>The release notes saved by the updater for this version, read by the changelog window.</summary>
        public static string ChangelogPath(string portDir, string version)
        {
            string safe = string.IsNullOrEmpty(version) ? "current" : version;
            foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            return Path.Combine(LauncherDir(portDir), "changelog-" + safe + ".txt");
        }

        public static void SaveChangelog(string portDir, string version, string notes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notes)) return;
                Ensure(portDir);
                File.WriteAllText(ChangelogPath(portDir, version), notes, new UTF8Encoding(false));
            }
            catch { }
        }

        public static string ReadChangelog(string portDir, string version)
        {
            try
            {
                string path = ChangelogPath(portDir, version);
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "";
            }
            catch { return ""; }
        }

        private static string MoveRetry(string source, string destinationDir, string name)
        {
            try { Directory.CreateDirectory(destinationDir); } catch { }
            string target = UniquePath(destinationDir, name);
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    File.Move(source, target);
                    return target;
                }
                catch (IOException) { Thread.Sleep(120); }
                catch (UnauthorizedAccessException) { Thread.Sleep(120); }
                catch { return null; }
                target = UniquePath(destinationDir, name);
            }
            return null;
        }

        private static string UniquePath(string dir, string name)
        {
            string path = Path.Combine(dir, name);
            if (!File.Exists(path)) return path;
            string stem = Path.GetFileNameWithoutExtension(name);
            string ext = Path.GetExtension(name);
            for (int i = 2; ; i++)
            {
                path = Path.Combine(dir, stem + "-" + i + ext);
                if (!File.Exists(path)) return path;
            }
        }

        private static string SafeTimestamp(DateTime value) => value.ToString("yyyyMMdd-HHmmss");
    }

    public sealed class LogArchiveResult
    {
        public string GameLog;
        public readonly List<string> NewCrashLogs = new List<string>();
    }
}
