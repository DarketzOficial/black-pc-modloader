using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BlackLauncher
{
    /// <summary>
    /// The crash report, in the launcher's own frame: same caption, banner, colours and buttons.
    /// ENVIAR posts the log and the PC information to the Discord webhook; NAO ENVIAR (or closing)
    /// leaves the file on disk and sends nothing.
    /// </summary>
    public partial class CrashWindow : Window
    {
        private readonly CrashInfo _info;
        private readonly Config _cfg;
        private bool _sending;
        private bool _sent;

        public CrashWindow(CrashInfo info, Config cfg)
        {
            InitializeComponent();
            _info = info;
            _cfg = cfg;
            Title = Loc.CrashCaption;
            PreviewKeyDown += OnKeyDown;
            Closing += OnClosing;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CaptionText.Text = Loc.CrashCaption;
            VersionText.Text = Loc.Product;
            SubtitleText.Text = Loc.CrashSubtitle;
            CreditLink.Text = Loc.Credits;
            CreditLink.ToolTip = Loc.CreditsUrl;
            YouTubeButton.ToolTip = Loc.YouTubeHint;
            DiscordButton.ToolTip = Loc.DiscordHint;

            TitleText.Text = _info.Kind == "Start failure" ? Loc.CrashTitleStart : Loc.CrashTitle;
            IntroText.Text = _info.Kind == "Start failure" ? Loc.CrashIntroStart : Loc.CrashIntro;
            SubText.Text = Loc.CrashOptional;

            string size = FormatSize(_info.LogSize);
            LogLabel.Text = "LOG";
            LogValue.Text = string.IsNullOrEmpty(_info.LogName)
                ? Loc.CrashNoLog
                : _info.LogName + (size.Length == 0 ? "" : "  (" + size + ")");

            VersionLabel.Text = Loc.CrashVersionLabel;
            VersionValue.Text = "ModLoader " + Loc.Version + "  \u2022  Port " + _info.System.PortVersion;

            SystemLabel.Text = Loc.CrashSystemLabel;
            SystemValue.Text = _info.System.Os + "\n" +
                               _info.System.Cpu + "\n" +
                               _info.System.Gpu + "\n" +
                               _info.System.Ram + "  \u2022  " + _info.System.Screen;

            SessionLabel.Text = Loc.CrashSessionLabel;
            string session = (string.IsNullOrEmpty(_info.Menu) ? Loc.CrashMenuUnknown : _info.Menu) +
                             "  \u2022  " + _info.StartedLocal.ToString("HH:mm:ss") + " - " +
                             _info.EndedLocal.ToString("HH:mm:ss");
            if (_info.ExitCode.HasValue) session += "  \u2022  " + Loc.CrashExitCode(_info.ExitCode.Value);
            if (!string.IsNullOrEmpty(_info.System.Mods) && _info.System.Mods != "nenhum")
                session += "\n" + Loc.CrashMods + ": " + _info.System.Mods;
            SessionValue.Text = session;

            WhatText.Text = Loc.CrashPrivacy(_info.LogName);

            OpenFolderButton.Content = Loc.CrashOpenFolder;
            NoButton.Content = Loc.CrashNo;
            SendButton.Content = Loc.CrashSend;
            SendButton.Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || _sending) return;
            e.Handled = true;
            Close();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Do not close while the request is in flight; the player gets a success/error answer.
            if (_sending) e.Cancel = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (!_sending) Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            if (_sending) return;
            LogStore.AppendReport(_info.PortDir, "nao enviado  " + _info.LogName);
            Close();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (_sent) { Close(); return; }
            if (_sending) return;

            _sending = true;
            ErrorText.Visibility = Visibility.Collapsed;
            StatusText.Text = Loc.CrashSending;
            StatusText.Foreground = (Brush)Application.Current.Resources["B.Accent"];
            StatusText.Visibility = Visibility.Visible;
            SendButton.IsEnabled = false;
            NoButton.IsEnabled = false;
            CloseButton.IsEnabled = false;

            SendResult result = await DiscordReporter.SendAsync(_info, _cfg, System.Threading.CancellationToken.None);

            _sending = false;
            CloseButton.IsEnabled = true;
            if (result.Ok)
            {
                _sent = true;
                StatusText.Text = Loc.CrashSent;
                StatusText.Foreground = (Brush)Application.Current.Resources["B.AccentHi"];
                NoButton.Visibility = Visibility.Collapsed;
                SendButton.Content = Loc.CrashClose;
                SendButton.IsEnabled = true;
                SendButton.Focus();
            }
            else
            {
                StatusText.Visibility = Visibility.Collapsed;
                ErrorText.Text = Loc.CrashFailed(result.Error);
                ErrorText.Visibility = Visibility.Visible;
                SendButton.Content = Loc.CrashRetry;
                SendButton.IsEnabled = true;
                NoButton.IsEnabled = true;
                SendButton.Focus();
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = _info.LogPath;
            string folder = null;
            try { folder = string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path); } catch { }
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                folder = LogStore.CrashDir(_info.PortDir);
            GameLauncher.OpenInShell(folder);
        }

        private void Credit_Click(object sender, MouseButtonEventArgs e) => GameLauncher.OpenUrl(Loc.CreditsUrl);
        private void YouTube_Click(object sender, RoutedEventArgs e) => GameLauncher.OpenUrl(Loc.CreditsUrl);
        private void Discord_Click(object sender, RoutedEventArgs e) => GameLauncher.OpenUrl(Loc.DiscordUrl);

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024)
                return (bytes / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }
    }
}
