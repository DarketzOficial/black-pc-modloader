using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlackLauncher
{
    /// <summary>
    /// The hidden instance of the launcher that starts the game and waits for it. If the game exits
    /// with an error (or writes a black-crash-*.log), it moves the logs into log\ and shows the report
    /// window. A normal exit archives the log and closes silently.
    /// </summary>
    public sealed class CrashWatcher
    {
        private readonly string _portDir;

        public CrashWatcher(string portDir) { _portDir = portDir; }

        public async Task RunAsync()
        {
            var cfg = Config.Load(_portDir);
            string defLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt" : "en";
            Loc.Pt = cfg.GetPref("lang", defLang) != "en";
            LogStore.Ensure(_portDir);

            string missing = GameLauncher.MissingReason(_portDir);
            if (missing != null)
            {
                ShowReport(cfg, "Start failure", null, null, DateTime.Now, DateTime.Now, null, missing);
                return;
            }

            var before = LogStore.SnapshotCrashLogs(_portDir);
            DateTime started = DateTime.Now;
            Process game = null;
            try
            {
                game = GameLauncher.StartGame(_portDir);
                if (game == null)
                    throw new InvalidOperationException("O Windows nao devolveu o processo do jogo.");
            }
            catch (Exception ex)
            {
                string log = LogStore.CreateSyntheticLog(_portDir, "Nao foi possivel iniciar o black.exe", ex.ToString());
                ShowReport(cfg, "Start failure", log, null, started, DateTime.Now, null, ex.Message);
                return;
            }

            try
            {
                await Task.Run(() => game.WaitForExit()).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // The process handle can disappear on a hard crash; the log is still the useful part.
                LogStore.AppendReport(_portDir, "watcher erro ao esperar o jogo: " + ex.Message);
            }

            // The port's crash handler writes its stack log just before the process ends.
            await Task.Delay(1200).ConfigureAwait(true);

            LogArchiveResult archive = null;
            try { archive = LogStore.ArchiveAfterGame(_portDir, before, started); }
            catch (Exception ex) { LogStore.AppendReport(_portDir, "erro ao arquivar logs: " + ex.Message); }

            int? exitCode = null;
            try { if (game != null && game.HasExited) exitCode = game.ExitCode; } catch { }

            string crashLog = archive != null && archive.NewCrashLogs.Count > 0
                ? archive.NewCrashLogs.OrderByDescending(File.GetLastWriteTimeUtc).First()
                : null;
            bool crashed = (exitCode.HasValue && exitCode.Value != 0) || crashLog != null;
            if (!crashed) return;

            string logPath = crashLog ?? (archive == null ? null : archive.GameLog);
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
            {
                string reason = exitCode.HasValue
                    ? "O jogo terminou com o codigo " + exitCode.Value + " e nao deixou um log."
                    : "O jogo terminou de forma inesperada e nao deixou um log.";
                logPath = LogStore.CreateSyntheticLog(_portDir, reason, "Port: " + _portDir);
            }

            ShowReport(cfg, "Crash", logPath, archive == null ? null : archive.GameLog,
                       started, DateTime.Now, exitCode, null);
        }

        private void ShowReport(Config cfg, string kind, string logPath, string gameLog,
                                DateTime started, DateTime ended, int? exitCode, string error)
        {
            // The report must always have something to attach, even when the port died before it
            // could create its own file. Create the synthetic log first so the port version and the
            // last menu are also read from a real path when possible.
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
            {
                string reason = kind == "Start failure"
                    ? (error ?? "O jogo nao conseguiu iniciar.")
                    : (exitCode.HasValue
                        ? "O jogo terminou com o codigo " + exitCode.Value + " e nao deixou um log."
                        : "O jogo terminou de forma inesperada e nao deixou um log.");
                logPath = LogStore.CreateSyntheticLog(_portDir, reason, "Port: " + _portDir);
            }

            var info = new CrashInfo
            {
                PortDir = _portDir,
                LogPath = logPath,
                GameLogPath = gameLog,
                Kind = kind,
                ExitCode = exitCode,
                StartedLocal = started,
                EndedLocal = ended,
                Error = error,
                Menu = SystemInfo.LastMenu(logPath),
                System = SystemInfo.Collect(_portDir, logPath)
            };
            var window = new CrashWindow(info, cfg);
            window.ShowDialog();
        }
    }
}
