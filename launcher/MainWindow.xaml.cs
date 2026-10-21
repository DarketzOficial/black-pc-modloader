using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BlackLauncher
{
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }

        private Config _cfg;
        private RecompConfig _recompCfg;
        private UiContext _ctx;
        private List<Mod> _mods;
        private string _recompRoot;
        private GameLauncher.TargetKind _targetKind = GameLauncher.TargetKind.PcPort;
        private List<string> _recoveredCrashes;
        private bool _pt = true;
        private bool _ready;
        private bool _dirty;
        private bool _portDirty;
        private bool _recompDirty;
        private bool _updatingTargetSelector;

        // key capture: the button waiting for a key, plus what it showed before
        private KeyBindings.Action _capAction;
        private int _capSlot;
        private Button _capButton;
        private object _capOriginal;

        private Brush _accent;
        private Brush _normal;

        // the "saved" notice: shown for a moment, then faded out
        private readonly DispatcherTimer _toast = new DispatcherTimer();

        // the updater: one check at a time, one window at a time, and nothing after the launcher starts closing
        private bool _checkingUpdate;
        private bool _updateWindowOpen;
        private bool _closing;

        public MainWindow()
        {
            InitializeComponent();
            Current = this;
            _accent = (Brush)Application.Current.Resources["B.AccentHi"];
            _normal = (Brush)Application.Current.Resources["B.Fg"];

            string portDir = PortDir();
            _cfg = Config.Load(portDir);
            if (_cfg.GetPref("target", "pc") == "recomp") _targetKind = GameLauncher.TargetKind.Recomp;
            _recoveredCrashes = _targetKind == GameLauncher.TargetKind.PcPort
                ? LogStore.MigratePortRoot(portDir) : new List<string>();
            if (_targetKind == GameLauncher.TargetKind.PcPort) Updater.CleanTemp(portDir);
            _mods = Mods.Scan(portDir);
            _recompRoot = FindRecompRoot(portDir, _cfg.GetPref("recomp_dir", ""));
            if (!string.IsNullOrEmpty(_recompRoot))
            {
                try { _recompCfg = RecompConfig.Load(_recompRoot); }
                catch (Exception) { _recompCfg = null; }
            }

            Title = Loc.WindowTitle;
            VersionText.Text = Loc.Product;
            CreditLink.Text = Loc.Credits;
            CreditLink.ToolTip = Loc.CreditsUrl;
            _toast.Interval = TimeSpan.FromSeconds(2.2);
            _toast.Tick += (s, e) => HideToast();

            // the launcher's own sound: the music loops, the effects follow the mouse (see WireSounds)
            Audio.Init(portDir);
            Audio.Muted = _cfg.GetPref("mute", "0") == "1";

            // the launcher's own language: the player's choice, else the Windows one
            string defLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt" : "en";
            _pt = _cfg.GetPref("lang", defLang) != "en";
            Loc.Pt = _pt;

            _ctx = new UiContext { Cfg = _cfg, Settings = _cfg, Pt = _pt, Dirty = MarkDirty, Rebuild = Rebuild,
                                   HdDefaultDir = () => _targetKind == GameLauncher.TargetKind.Recomp ? _recompCfg.HdDefaultDir() : _cfg.HdDefaultDir() };
            _ctx.Mods = _mods;
            _ctx.ShowChangelog = ShowChangelog;

            InitLanguageCombo();
            InitTargetSelector();
            BuildFooter();
            BuildTabs();
            RestoreGeometry();
            RestoreTab();
            UpdateBadge();
            WireChromeSounds();

            _ready = true;
            SetStatus(_targetKind == GameLauncher.TargetKind.Recomp
                ? Loc.T("Configuração da recompilação: ", "Recomp settings: ") + Path.Combine(_recompRoot ?? "", "black.toml")
                : (_cfg.Ini.Exists ? Loc.LoadedFrom + " " + Path.GetFileName(_cfg.IniPath) : Loc.NoIni));

            PreviewKeyDown += OnCaptureKey;
            PreviewMouseDown += OnCaptureMouse;
            PreviewMouseLeftButtonDown += OnAnyClick;
            PreviewMouseWheel += OnCaptureWheel;
            Loaded += Window_Loaded;
        }

        private static string PortDir()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(dir))
                dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return dir.TrimEnd('\\', '/');
        }

        // ------------------------------------------------------------------ build ---
        private void InitLanguageCombo()
        {
            LangCombo.Items.Add("Português (BR)");
            LangCombo.Items.Add("English");
            LangCombo.SelectedIndex = _pt ? 0 : 1;
        }

        private void InitTargetSelector()
        {
            UpdateTargetSelectorLabels();
            TargetCombo.SelectedIndex = _targetKind == GameLauncher.TargetKind.Recomp ? 1 : 0;
            TargetBrowseButton.MouseEnter += (s, e) => Audio.Hover();
            TargetCombo.MouseEnter += (s, e) => Audio.Hover();
            SwitchTarget(_targetKind, false);
        }

        private void UpdateTargetSelectorLabels()
        {
            if (TargetCombo == null) return;
            int selected = TargetCombo.SelectedIndex;
            _updatingTargetSelector = true;
            TargetLabel.Text = Loc.T("JOGO", "GAME");
            TargetBrowseButton.Content = Loc.T("LOCALIZAR RECOMPILAÇÃO", "BROWSE FOR RECOMP");
            TargetCombo.Items.Clear();
            TargetCombo.Items.Add(Loc.T("BLACK PC — ModLoader", "BLACK PC — ModLoader"));
            TargetCombo.Items.Add(Loc.T("BLACK — XBE Recomp", "BLACK — XBE Recomp"));
            if (selected >= 0) TargetCombo.SelectedIndex = selected;
            _updatingTargetSelector = false;
        }

        private static string FindRecompRoot(string portDir, string saved)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(saved) && GameLauncher.IsRecomp(saved)) return Path.GetFullPath(saved);
                string parent = Directory.GetParent(Path.GetFullPath(portDir))?.FullName;
                if (string.IsNullOrEmpty(parent)) return null;
                var found = Directory.GetDirectories(parent).Where(GameLauncher.IsRecomp).Take(2).ToArray();
                return found.Length == 1 ? found[0] : null;
            }
            catch (Exception) { return null; }
        }

        private void Target_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _updatingTargetSelector) return;
            if (TargetCombo.SelectedIndex == 0)
            {
                _cfg.SetPref("target", "pc");
                _cfg.SavePrefs();
                SwitchTarget(GameLauncher.TargetKind.PcPort, true);
                return;
            }
            if (TargetCombo.SelectedIndex == 1)
            {
                if (string.IsNullOrEmpty(_recompRoot) || !GameLauncher.IsRecomp(_recompRoot))
                {
                    if (!BrowseRecomp()) { TargetCombo.SelectedIndex = 0; return; }
                }
                _cfg.SetPref("target", "recomp");
                _cfg.SetPref("recomp_dir", _recompRoot);
                _cfg.SavePrefs();
                SwitchTarget(GameLauncher.TargetKind.Recomp, true);
            }
        }

        private void BrowseRecomp_Click(object sender, RoutedEventArgs e)
        {
            if (BrowseRecomp())
            {
                TargetCombo.SelectedIndex = 1;
                _cfg.SetPref("target", "recomp");
                _cfg.SetPref("recomp_dir", _recompRoot);
                _cfg.SavePrefs();
                SwitchTarget(GameLauncher.TargetKind.Recomp, true);
            }
        }

        private bool BrowseRecomp()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.T("Selecione BLACK.exe da recompilação", "Select the Recomp BLACK.exe"),
                Filter = "BLACK.exe|BLACK.exe",
                CheckFileExists = true,
                Multiselect = false,
                InitialDirectory = Directory.Exists(_recompRoot) ? _recompRoot : _cfg.PortDir
            };
            if (dialog.ShowDialog(this) != true) return false;
            string root = Path.GetDirectoryName(dialog.FileName);
            if (!GameLauncher.IsRecomp(root))
            {
                ThemedDialog.Show(this,
                    Loc.T("Essa pasta não parece ser uma instalação do XBE Recomp (BLACK.exe e arquivos do jogo são necessários).",
                          "This folder does not look like an XBE Recomp installation (BLACK.exe and game files are required)."),
                    ThemedDialog.Result.Ok, "OK", showCancel: false);
                return false;
            }
            _recompRoot = Path.GetFullPath(root);
            try { _recompCfg = RecompConfig.Load(_recompRoot); }
            catch (Exception ex)
            {
                _recompCfg = null;
                ThemedDialog.Show(this, Loc.T("Não foi possível ler black.toml: ", "Could not read black.toml: ") + ex.Message,
                                  ThemedDialog.Result.Ok, "OK", showCancel: false);
                return false;
            }
            return true;
        }

        private void SwitchTarget(GameLauncher.TargetKind kind, bool rebuild)
        {
            _targetKind = kind;
            bool recomp = kind == GameLauncher.TargetKind.Recomp;
            if (recomp && _recompCfg == null && !string.IsNullOrEmpty(_recompRoot))
            {
                try { _recompCfg = RecompConfig.Load(_recompRoot); }
                catch (Exception ex) { SetStatus(Loc.T("Não foi possível ler black.toml: ", "Could not read black.toml: ") + ex.Message); }
            }
            _ctx.Settings = recomp && _recompCfg != null ? (ISettingsStore)_recompCfg : _cfg;
            try
            {
                _mods = recomp && string.IsNullOrEmpty(_recompRoot) ? new List<Mod>()
                    : Mods.Scan(recomp ? _recompRoot : _cfg.PortDir);
            }
            catch (Exception) { _mods = new List<Mod>(); }
            _ctx.Mods = _mods;
            _ctx.ModsRoot = recomp ? _recompRoot : _cfg.PortDir;
            UpdateTargetDisplay();
            UpdateBadge();
            if (UpdateButton != null) UpdateButton.Visibility = recomp ? Visibility.Collapsed : Visibility.Visible;
            if (rebuild && _ready) BuildTabs();
        }

        private void UpdateTargetDisplay()
        {
            if (TargetVersion == null) return;
            if (_targetKind == GameLauncher.TargetKind.Recomp)
                TargetVersion.Text = string.IsNullOrEmpty(_recompRoot) ? Loc.T("Localize a pasta da recompilação", "Browse for the Recomp folder")
                    : Loc.T("XBE Recomp · ", "XBE Recomp · ") + GameLauncher.RecompVersion(_recompRoot) + " · " +
                      GameLauncher.RecompImageIdentity(_recompRoot) + " · " + _recompRoot;
            else
            {
                string version = "";
                try { version = FileVersionInfo.GetVersionInfo(GameLauncher.BlackExe(_cfg.PortDir)).ProductVersion; } catch (Exception) { }
                TargetVersion.Text = Loc.T("PC ModLoader · ", "PC ModLoader · ") +
                    (string.IsNullOrWhiteSpace(version) ? Loc.T("versão desconhecida", "version unknown") : version) + " · " + _cfg.PortDir;
            }
        }

        private void BuildFooter()
        {
            LangLabel.Text = Loc.LanguageLabel;
            RestoreButton.Content = Loc.Restore;
            SaveButton.Content = Loc.Save;
            PlayButton.Content = Loc.Play;
            UpdateButtonText.Text = Loc.CheckUpdates;
            UpdateButton.ToolTip = Loc.CheckUpdatesHint;
            YouTubeButton.ToolTip = Loc.YouTubeHint;
            DiscordButton.ToolTip = Loc.DiscordHint;
            DonateButton.ToolTip = Loc.DonationHint;
            DonateButtonText.Text = Loc.DonationButton;
            CaptionText.Text = Loc.WindowTitle;
            MinButton.ToolTip = Loc.CaptionMinimize;
            CloseButton.ToolTip = Loc.CaptionClose;
            UpdateChrome();
            UpdateMuteButton();
        }

        /// <summary>The launcher's own hover sound, once per element (a language change keeps the chrome).</summary>
        private void WireChromeSounds()
        {
            foreach (var e in new FrameworkElement[] { MinButton, MaxButton, CloseButton, UpdateButton, MuteButton,
                                                       RestoreButton, SaveButton, PlayButton, LangCombo, CreditLink,
                                                       YouTubeButton, DiscordButton, DonateButton, TargetBrowseButton, TargetCombo })
                if (e != null) e.MouseEnter += (s, a) => Audio.Hover();
        }

        // --------------------------------------------------------- window buttons ---
        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateChrome();
        }

        /// <summary>The caption's middle button follows the window: maximise when it is floating, restore
        /// when it fills the screen (its tool tip says which), and the frame line goes away maximised.</summary>
        private void UpdateChrome()
        {
            if (MaxButton == null) return;
            bool max = WindowState == WindowState.Maximized;
            MaxButton.Content = max ? "\uE923" : "\uE922";   // Segoe MDL2 Assets: restore / maximise
            MaxButton.ToolTip = max ? Loc.CaptionRestore : Loc.CaptionMaximize;
            if (Frame != null) Frame.BorderThickness = new Thickness(max ? 0 : 1);
        }

        private void BuildTabs()
        {
            int keep = Tabs.SelectedIndex;
            Tabs.Items.Clear();
            if (_targetKind == GameLauncher.TargetKind.Recomp && _recompCfg == null)
            {
                Tabs.Items.Add(new TabItem
                {
                    Header = Loc.T("RECOMP", "RECOMP"),
                    Content = new Border
                    {
                        Padding = new Thickness(24),
                        Child = new TextBlock
                        {
                            Text = Loc.T("Localize BLACK.exe e os arquivos do jogo para editar as opções da recompilação.",
                                         "Locate BLACK.exe and the game files to edit Recomp settings."),
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)Application.Current.Resources["B.FgDim"],
                            FontSize = 14
                        }
                    }
                });
                Tabs.SelectedIndex = 0;
                return;
            }
            foreach (var s in (_targetKind == GameLauncher.TargetKind.Recomp ? Schema.BuildRecomp() : Schema.Build(_pt)))
            {
                var tab = new TabItem { Header = _pt ? s.Pt : s.En, Content = UiBuilder.BuildSection(s, _ctx) };
                tab.MouseEnter += (s2, a) => Audio.Hover();
                Tabs.Items.Add(tab);
            }
            Tabs.SelectedIndex = keep >= 0 && keep < Tabs.Items.Count ? keep : 0;
        }

        private void Rebuild()
        {
            _ctx.Pt = _pt;
            BuildTabs();
            UpdateBadge();
        }

        private void UpdateBadge()
        {
            string missing = _targetKind == GameLauncher.TargetKind.Recomp
                ? GameLauncher.RecompMissingReason(_recompRoot)
                : GameLauncher.MissingReason(_cfg.PortDir);
            GameBadge.Visibility = missing != null ? Visibility.Visible : Visibility.Collapsed;
            GameBadgeText.Text = _targetKind == GameLauncher.TargetKind.Recomp
                ? Loc.T("RECOMPILAÇÃO NÃO LOCALIZADA", "RECOMP NOT FOUND") : Loc.GameMissingBadge;
            PlayButton.IsEnabled = missing == null;
            PlayButton.ToolTip = missing;
            UpdateTargetDisplay();
        }

        // -------------------------------------------------------------- geometry ---
        private void RestoreGeometry()
        {
            double w = ParseD(_cfg.GetPref("win_w", "")), h = ParseD(_cfg.GetPref("win_h", ""));
            if (!double.IsNaN(w) && !double.IsNaN(h) && w >= MinWidth && h >= MinHeight)
            {
                Width = w;
                Height = h;
            }
            double l = ParseD(_cfg.GetPref("win_left", "")), t = ParseD(_cfg.GetPref("win_top", ""));
            if (!double.IsNaN(l) && !double.IsNaN(t) &&
                l > SystemParameters.VirtualScreenLeft - 60 &&
                l < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 120 &&
                t > SystemParameters.VirtualScreenTop - 10 &&
                t < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = l;
                Top = t;
            }
            if (_cfg.GetPref("win_max", "0") == "1") WindowState = WindowState.Maximized;
        }

        private void RestoreTab()
        {
            int tab;
            if (int.TryParse(_cfg.GetPref("tab", "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out tab) &&
                tab >= 0 && tab < Tabs.Items.Count)
                Tabs.SelectedIndex = tab;
        }

        private static double ParseD(string s)
        {
            double d;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : double.NaN;
        }

        private void MarkDirty()
        {
            _dirty = true;
            if (_targetKind == GameLauncher.TargetKind.Recomp) _recompDirty = true;
            else _portDirty = true;
            if (SaveButton != null) SaveButton.Foreground = _accent;
            if (_ready) SetStatus(Loc.T("Há alterações não salvas.", "You have unsaved changes."));
        }

        private void SetStatus(string text) { StatusText.Text = text; }

        // ---------------------------------------------------------------- actions ---
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string error;
            if (!SaveDirty(out error))
            {
                ThemedDialog.Show(this, error, ThemedDialog.Result.Ok, "OK", showCancel: false);
                return;
            }
            SaveButton.Foreground = _normal;
            string savedPath = _targetKind == GameLauncher.TargetKind.Recomp
                ? (!string.IsNullOrEmpty(_recompRoot) ? Path.Combine(_recompRoot, "black.toml") : Loc.T("Recomp não localizado", "Recomp not found"))
                : _cfg.IniPath;
            SetStatus(Loc.Saved + "  ·  " + savedPath);
            Audio.Saved();
            ShowToast(Loc.Saved);
        }

        private bool SaveDirty(out string error)
        {
            error = null;
            try
            {
                if (_portDirty) _cfg.Save();
                if (_recompDirty)
                {
                    if (_recompCfg == null) { error = Loc.T("Selecione a pasta da recompilação antes de salvar.", "Select the Recomp folder before saving."); return false; }
                    _recompCfg.Save();
                }
                _portDirty = _recompDirty = _dirty = false;
                return true;
            }
            catch (Exception ex)
            {
                error = Loc.T("Não foi possível salvar as configurações: ", "Could not save settings: ") + ex.Message;
                return false;
            }
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            Audio.Muted = !Audio.Muted;
            _cfg.SetPref("mute", Audio.Muted ? "1" : "0");
            _cfg.SavePrefs();
            UpdateMuteButton();
            if (!Audio.Muted) Audio.Select();
        }

        private void UpdateMuteButton()
        {
            if (MuteButton == null) return;
            MuteButton.Content = Audio.Muted ? "\uE74F" : "\uE767";   // Segoe MDL2 Assets: mute / volume
            MuteButton.ToolTip = Audio.Muted ? Loc.SoundOff : Loc.SoundOn;
            MuteButton.Foreground = Audio.Muted ? (Brush)Application.Current.Resources["B.FgDim"]
                                                : (Brush)Application.Current.Resources["B.Fg"];
        }

        private void Credit_Click(object sender, MouseButtonEventArgs e)
        {
            GameLauncher.OpenUrl(Loc.CreditsUrl);
        }

        private void YouTube_Click(object sender, RoutedEventArgs e) { GameLauncher.OpenUrl(Loc.CreditsUrl); }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            new DonationWindow { Owner = this }.ShowDialog();
        }

        private void Discord_Click(object sender, RoutedEventArgs e) { GameLauncher.OpenUrl(Loc.DiscordUrl); }

        private void Update_Click(object sender, RoutedEventArgs e) { CheckForUpdates(true); }

        // --------------------------------------------------------------- updates ---
        /// <summary>The GitHub API the launcher reads. A launcher.ini key can point it at a mirror (or a local
        /// test server); unset, it is the project's own releases page.</summary>
        private string UpdateApi
        {
            get
            {
                string url = _cfg.GetPref("update_api", Updater.DefaultApi);
                return string.IsNullOrWhiteSpace(url) ? Updater.DefaultApi : url;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_targetKind == GameLauncher.TargetKind.Recomp) return;
            // A crash log found in the port root means black.exe was started without the hidden
            // watcher (a direct launch, or the watcher was killed). Offer the same report once.
            ShowRecoveredCrash();

            // The updater saved the release notes; show them once for the new version that just opened.
            ShowChangelogIfNew();

            if (_targetKind == GameLauncher.TargetKind.Recomp || _cfg.GetPref("update_auto", "1") == "0") return;
            await Task.Delay(1000);
            if (_closing || !IsLoaded) return;
            CheckForUpdates(false);
        }

        private void ShowChangelog()
        {
            var window = new ChangelogWindow(_cfg) { Owner = this };
            window.ShowDialog();
        }

        private void ShowChangelogIfNew()
        {
            string seen = _cfg.GetPref("changelog_seen", "");
            if (string.Equals(seen, Loc.Version, StringComparison.OrdinalIgnoreCase)) return;
            ShowChangelog();
            _cfg.SetPref("changelog_seen", Loc.Version);
            _cfg.SavePrefs();
        }

        private void ShowRecoveredCrash()
        {
            if (_recoveredCrashes == null || _recoveredCrashes.Count == 0) return;
            string logPath;
            try
            {
                logPath = _recoveredCrashes.OrderByDescending(File.GetLastWriteTimeUtc).First();
            }
            catch { logPath = _recoveredCrashes[0]; }
            if (!File.Exists(logPath)) return;

            DateTime ended;
            try { ended = File.GetLastWriteTime(logPath); } catch { ended = DateTime.Now; }
            var info = new CrashInfo
            {
                PortDir = _cfg.PortDir,
                LogPath = logPath,
                GameLogPath = LogStore.LatestGameLog(_cfg.PortDir),
                Kind = "Crash (direct launch)",
                StartedLocal = ended,
                EndedLocal = ended,
                Error = "O jogo foi iniciado sem o launcher (ou o observador foi fechado).",
                Menu = SystemInfo.LastMenu(logPath),
                System = SystemInfo.Collect(_cfg.PortDir, logPath)
            };
            new CrashWindow(info, _cfg).ShowDialog();
        }

        /// <summary>Checks GitHub once. The automatic check is silent when there is nothing new (or no network);
        /// the footer button reports what happened.</summary>
        private async void CheckForUpdates(bool manual)
        {
            if (_checkingUpdate || _updateWindowOpen || _closing) return;
            _checkingUpdate = true;
            string previousStatus = StatusText.Text;
            string previousButton = UpdateButtonText.Text;
            if (manual)
            {
                UpdateButton.IsEnabled = false;
                UpdateButtonText.Text = Loc.Checking;
                SetStatus(Loc.Checking);
            }
            try
            {
                var rel = await Updater.CheckAsync(UpdateApi, CancellationToken.None);
                if (_closing || !IsLoaded) return;

                if (rel == null)
                {
                    if (manual)
                    {
                        UpdateButtonText.Text = previousButton;
                        SetStatus(previousStatus);
                        ThemedDialog.Show(this, Loc.UpdateNoRelease, ThemedDialog.Result.Ok, "OK", showCancel: false);
                    }
                }
                else if (!Updater.IsNewer(rel.Version))
                {
                    if (manual)
                    {
                        UpdateButtonText.Text = previousButton;
                        SetStatus(previousStatus);
                        ThemedDialog.Show(this, Loc.UpdateLatest, ThemedDialog.Result.Ok, "OK", showCancel: false);
                    }
                }
                else
                {
                    // The check is done: put the footer back to its normal label before the modal opens,
                    // otherwise it would sit behind the dialog reading "PROCURANDO..." until it closes.
                    if (manual)
                    {
                        UpdateButtonText.Text = previousButton;
                        SetStatus(previousStatus);
                    }
                    _updateWindowOpen = true;
                    try
                    {
                        var win = new UpdateWindow(rel, _cfg.PortDir) { Owner = this };
                        win.ShowDialog();
                    }
                    finally { _updateWindowOpen = false; }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (manual)
                {
                    UpdateButtonText.Text = previousButton;
                    SetStatus(previousStatus);
                    ThemedDialog.Show(this, Loc.UpdateCheckFailed(ex.Message), ThemedDialog.Result.Ok, "OK", showCancel: false);
                }
            }
            finally
            {
                _checkingUpdate = false;
                if (UpdateButton != null) UpdateButton.IsEnabled = true;
                if (UpdateButtonText != null) UpdateButtonText.Text = previousButton;
                if (manual && !_closing) SetStatus(previousStatus);
            }
        }

        /// <summary>Called by the update window before the helper starts: whatever was edited is written, and
        /// the close that follows does not ask about it (the launcher is restarting, not quitting).</summary>
        public void PrepareForUpdate()
        {
            // Only write black.ini when there really is something unsaved: an update should not rewrite the
            // player's settings file just because the launcher is about to restart.
            if (!_dirty) return;
            string error;
            if (!SaveDirty(out error)) throw new IOException(error);
            if (SaveButton != null) SaveButton.Foreground = _normal;
        }

        private void ShowToast(string text)
        {
            ToastText.Text = text;
            Toast.Visibility = Visibility.Visible;
            Toast.BeginAnimation(OpacityProperty, null);
            Toast.Opacity = 1;
            _toast.Stop();
            _toast.Start();
        }

        private void HideToast()
        {
            _toast.Stop();
            var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(350)));
            fade.Completed += (s, e) => Toast.Visibility = Visibility.Collapsed;
            Toast.BeginAnimation(OpacityProperty, fade);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            string saveError;
            if (!SaveDirty(out saveError))
            {
                ThemedDialog.Show(this, saveError, ThemedDialog.Result.Ok, "OK", showCancel: false);
                return;
            }

            string missing = _targetKind == GameLauncher.TargetKind.Recomp
                ? GameLauncher.RecompMissingReason(_recompRoot)
                : GameLauncher.MissingReason(_cfg.PortDir);
            if (missing != null)
            {
                ThemedDialog.Show(this, Loc.MissingGameHint(missing), ThemedDialog.Result.Ok, "OK", showCancel: false);
                return;
            }

            string error;
            if (_targetKind == GameLauncher.TargetKind.Recomp)
            {
                if (GameLauncher.StartRecomp(_recompRoot, out error)) { Application.Current.Shutdown(); return; }
                ThemedDialog.Show(this, Loc.MissingGameHint(error ?? ""), ThemedDialog.Result.Ok, "OK", showCancel: false);
                return;
            }

            // The visible launcher closes now; the hidden watcher starts the PC port and handles its logs.
            if (GameLauncher.StartWatchdog(_cfg.PortDir, out error))
            {
                Application.Current.Shutdown();
                return;
            }
            ThemedDialog.Show(this, Loc.MissingGameHint(error ?? ""), ThemedDialog.Result.Ok, "OK", showCancel: false);
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var r = ThemedDialog.Show(this, Loc.RestoreConfirmBody, ThemedDialog.Result.Ok, Loc.T("RESTAURAR", "RESTORE"));
            if (r != ThemedDialog.Result.Ok) return;
            if (_targetKind == GameLauncher.TargetKind.Recomp)
            {
                if (_recompCfg == null) { SetStatus(Loc.T("Localize a pasta da recompilação primeiro.", "Browse for the Recomp folder first.")); return; }
                _recompCfg.ResetToDefaults();
                _recompDirty = _dirty = true;
            }
            else
            {
                _cfg.ResetToDefaults();
                _portDirty = _dirty = true;
            }
            foreach (var m in _mods) Mods.SetOn(m, true);
            SaveButton.Foreground = _accent;
            Rebuild();
            SetStatus(Loc.T("Padrões restaurados (ainda não salvos).", "Defaults restored (not saved yet)."));
        }

        private void LangCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;
            bool pt = LangCombo.SelectedIndex == 0;
            if (pt == _pt) return;
            _pt = pt;
            Loc.Pt = _pt;
            _cfg.SetPref("lang", _pt ? "pt" : "en");
            _cfg.SavePrefs();
            BuildFooter();
            UpdateTargetSelectorLabels();
            Rebuild();
        }

        // ---------------------------------------------------------------- capture ---
        public void BeginCapture(KeyBindings.Action action, int slot, Button owner)
        {
            if (_capButton != null && _capOriginal != null) _capButton.Content = _capOriginal; // drop a previous capture
            _capAction = action;
            _capSlot = slot;
            _capButton = owner;
            _capOriginal = owner.Content;
            owner.Content = Loc.BindPress;
            SetStatus(Loc.BindingsHint);
            Focus();
        }

        private void EndCapture()
        {
            if (_capButton != null && _capOriginal != null) _capButton.Content = _capOriginal;
            _capAction = null;
            _capButton = null;
            _capOriginal = null;
        }

        private void Assign(ushort vk)
        {
            var action = _capAction;
            int slot = _capSlot;
            _capAction = null;
            _capButton = null;
            _capOriginal = null;
            if (action == null) return;

            // every action's slots, de-duplicated like the port's in-game capture
            var table = new Dictionary<KeyBindings.Action, ushort[]>();
            foreach (var a in KeyBindings.Actions) table[a] = KeyBindings.ReadSlots(_cfg.Ini, a, _cfg.Get);
            if (vk != 0)
                foreach (var a in KeyBindings.Actions)
                    for (int s = 0; s < 2; s++)
                        if ((a != action || s != slot) && table[a][s] == vk) table[a][s] = 0;
            table[action][slot] = vk;
            foreach (var a in KeyBindings.Actions) _cfg.SetBinding(a.Ini, table[a]);

            MarkDirty();
            _ctx.RefreshBindings(); // just the list, so the tab keeps its place
        }

        private void OnCaptureKey(object sender, KeyEventArgs e)
        {
            if (_capAction == null) return;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            e.Handled = true;
            if (key == Key.Escape) { EndCapture(); return; }
            if (key == Key.Delete) { Assign(0); return; }
            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (vk <= 0 || vk > 0xFE) return;
            Assign((ushort)vk);
        }

        private void OnCaptureMouse(object sender, MouseButtonEventArgs e)
        {
            if (_capAction == null) return;
            ushort vk;
            switch (e.ChangedButton)
            {
                case MouseButton.Left: vk = 0x01; break;
                case MouseButton.Right: vk = 0x02; break;
                case MouseButton.Middle: vk = 0x04; break;
                case MouseButton.XButton1: vk = 0x05; break;
                case MouseButton.XButton2: vk = 0x06; break;
                default: return;
            }
            e.Handled = true;
            Assign(vk);
        }

        private void OnCaptureWheel(object sender, MouseWheelEventArgs e)
        {
            if (_capAction == null) return;
            e.Handled = true;
            Assign(e.Delta > 0 ? KeyBindings.WheelUp : KeyBindings.WheelDown);
        }

        /// <summary>The interface's click sound on any left click (a setting's row, a tab, a button), except
        /// on the speaker button: that one only sounds when it turns the sound back on.</summary>
        private void OnAnyClick(object sender, MouseButtonEventArgs e)
        {
            if (_capAction != null) return;   // capturing a key: the click is the binding, not the interface
            if (Inside(e.OriginalSource as DependencyObject, MuteButton)) return;
            Audio.Select();
        }

        private static bool Inside(DependencyObject node, DependencyObject ancestor)
        {
            while (node != null)
            {
                if (ReferenceEquals(node, ancestor)) return true;
                node = node is Visual || node is System.Windows.Media.Media3D.Visual3D
                     ? VisualTreeHelper.GetParent(node)
                     : LogicalTreeHelper.GetParent(node);
            }
            return false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _closing = true;
            base.OnClosing(e);
            if (_dirty)
            {
                var r = ThemedDialog.Show(this, Loc.T("Salvar as alterações antes de fechar?", "Save your changes before closing?"),
                                          ThemedDialog.Result.Yes, Loc.T("SALVAR", "SAVE"), Loc.T("NÃO SALVAR", "DON'T SAVE"),
                                          Loc.T("CANCELAR", "CANCEL"));
                if (r == ThemedDialog.Result.Cancel) { e.Cancel = true; _closing = false; return; }
                if (r == ThemedDialog.Result.Yes)
                {
                    string error;
                    if (!SaveDirty(out error))
                    {
                        e.Cancel = true;
                        _closing = false;
                        ThemedDialog.Show(this, error, ThemedDialog.Result.Ok, "OK", showCancel: false);
                        return;
                    }
                }
            }

            // remember where and how the window was, for the next time
            var b = RestoreBounds;
            _cfg.SetPref("win_w", ((int)b.Width).ToString(CultureInfo.InvariantCulture));
            _cfg.SetPref("win_h", ((int)b.Height).ToString(CultureInfo.InvariantCulture));
            _cfg.SetPref("win_left", ((int)b.Left).ToString(CultureInfo.InvariantCulture));
            _cfg.SetPref("win_top", ((int)b.Top).ToString(CultureInfo.InvariantCulture));
            _cfg.SetPref("win_max", WindowState == WindowState.Maximized ? "1" : "0");
            _cfg.SetPref("tab", Tabs.SelectedIndex.ToString(CultureInfo.InvariantCulture));
            _cfg.SavePrefs();
        }
    }
}
