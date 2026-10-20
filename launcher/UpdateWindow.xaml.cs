using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace BlackLauncher
{
    /// <summary>
    /// "Nova atualização (x):" — the same visual language as the launcher, with the release notes and the two
    /// answers the player asked for: UPDATE (download, install, restart) and LATER. The download happens now,
    /// into &lt;port&gt;\update; the file swap is done by the updater helper after the launcher closes.
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private readonly UpdateRelease _rel;
        private readonly string _portDir;
        private CancellationTokenSource _cts;
        private bool _busy;
        private bool _installing;

        public UpdateWindow(UpdateRelease rel, string portDir)
        {
            InitializeComponent();
            _rel = rel;
            _portDir = portDir;
            Title = Loc.UpdateWindowTitle(rel.Version);
            PreviewKeyDown += OnKeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OverlineText.Text = Loc.UpdateOverline;
            TitleText.Text = Loc.UpdateTitle(_rel.Version);
            IntroText.Text = Loc.UpdateIntro(Loc.Version, Updater.FormatSize(_rel.AssetSize));
            NotesLabel.Text = Loc.UpdateNotes;
            NotesText.Text = string.IsNullOrWhiteSpace(_rel.Notes) ? Loc.UpdateNotesEmpty : ReleaseNotes.Clean(_rel.Notes);
            LaterButton.Content = Loc.UpdateLater;
            UpdateButton.Content = Loc.UpdateNow;
            UpdateButton.Focus();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (_busy) { _cts?.Cancel(); return; }
            Close();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) { _cts?.Cancel(); return; }
            Close();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            _busy = true;
            _cts = new CancellationTokenSource();
            ErrorText.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            Progress.Value = 0;
            ProgressText.Text = Loc.UpdateStarting;
            UpdateButton.Content = Loc.UpdateNow;
            UpdateButton.IsEnabled = false;
            LaterButton.Content = Loc.Cancel;

            string work = Path.Combine(_portDir, "update");
            string zip = Path.Combine(work, "download", SafeName(_rel.AssetName));
            string files = Path.Combine(work, "files");
            try
            {
                if (Directory.Exists(files)) Directory.Delete(files, true);
                Directory.CreateDirectory(Path.GetDirectoryName(zip));

                var progress = new Progress<double>(p =>
                {
                    Progress.Value = p;
                    ProgressText.Text = Loc.UpdateDownloading((int)Math.Round(p * 100));
                });
                await Updater.DownloadAsync(_rel, zip, progress, _cts.Token);

                ProgressText.Text = Loc.UpdateExtracting;
                Updater.ExtractSafe(zip, files);
                string source = Updater.FindLauncherRoot(files);
                if (source == null) throw new InvalidDataException(Loc.UpdateNoLauncher);

                ProgressText.Text = Loc.UpdateInstalling;
                _installing = true;
                // Keep the release notes for the changelog window after the helper restarts us.
                LogStore.SaveChangelog(_portDir, _rel.Version, _rel.Notes);
                MainWindow.Current?.PrepareForUpdate();
                Updater.StartInstall(_portDir, source);
                Application.Current.Shutdown();
            }
            catch (OperationCanceledException)
            {
                ResetAfterCancel();
            }
            catch (Exception ex)
            {
                _busy = false;
                _installing = false;
                ErrorText.Text = Loc.UpdateFailed(ex.Message);
                ErrorText.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
                UpdateButton.Content = Loc.UpdateRetry;
                UpdateButton.IsEnabled = true;
                LaterButton.Content = Loc.UpdateLater;
            }
            finally
            {
                if (_cts != null) { _cts.Dispose(); _cts = null; }
            }
        }

        private void ResetAfterCancel()
        {
            _busy = false;
            _installing = false;
            ErrorText.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Collapsed;
            UpdateButton.Content = Loc.UpdateNow;
            UpdateButton.IsEnabled = true;
            LaterButton.Content = Loc.UpdateLater;
        }

        private static string SafeName(string name)
        {
            string safe = Path.GetFileName(name ?? "");
            return string.IsNullOrEmpty(safe) ? "black-pc-modloader.zip" : safe;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Closing the window during the download cancels it; once the helper is started the launcher is
            // shutting down anyway, so there is nothing left to cancel.
            if (_busy && !_installing) _cts?.Cancel();
            base.OnClosing(e);
        }
    }
}
