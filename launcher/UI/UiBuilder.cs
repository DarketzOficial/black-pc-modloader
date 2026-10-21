using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlackLauncher
{
    /// <summary>Shared state the generated controls read and write.</summary>
    public sealed class UiContext
    {
        public Config Cfg;
        public ISettingsStore Settings;
        public bool Pt;
        public List<Mod> Mods;
        public string ModsRoot;
        public Func<string> HdDefaultDir;
        public Action Dirty = () => { };
        public Action Rebuild = () => { };
        public Action RefreshBindings = () => { };   // the key-bindings panel redraws itself
        public Action ShowChangelog = () => { };

        private readonly List<Action> _refreshers = new List<Action>();
        public void RegisterRefresh(Action a) { _refreshers.Add(a); }
        public void Refresh() { foreach (var a in _refreshers) a(); }
    }

    public static class UiBuilder
    {
        private static readonly Brush FgDim = (Brush)Application.Current.Resources["B.FgDim"];
        private static readonly Brush Fg = (Brush)Application.Current.Resources["B.Fg"];
        private static readonly Brush Row = (Brush)Application.Current.Resources["B.Row"];
        private static readonly Brush RowAlt = (Brush)Application.Current.Resources["B.RowAlt"];
        private static readonly Brush Hover = (Brush)Application.Current.Resources["B.Hover"];
        private static readonly Brush Border = (Brush)Application.Current.Resources["B.Border"];
        private static readonly Brush Accent = (Brush)Application.Current.Resources["B.Accent"];

        public static FrameworkElement BuildSection(Section s, UiContext ctx)
        {
            var panel = new StackPanel { Margin = new Thickness(0) };
            bool alt = false;
            foreach (var item in s.Items)
            {
                panel.Children.Add(BuildItem(item, ctx, alt));
                alt = !alt;
            }
            if (s.HasPad) panel.Children.Add(BuildPad(s, ctx));
            if (s.HasBindings) panel.Children.Add(BuildBindings(ctx));
            if (s.HasMods) panel.Children.Add(BuildMods(ctx));
            if (s.HasAdvanced) panel.Children.Add(BuildAdvanced(ctx));

            // The margin lives on the content, not on the ScrollViewer: the rows then keep a gap from the
            // window edge and from the scroll bar (and the render tool draws the same inset as the window).
            var host = new Border { Padding = new Thickness(16, 14, 18, 18), Child = panel };
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Focusable = false,   // not a tab stop: Tab should move between the controls, not land here
                Content = host
            };
        }

        // --------------------------------------------------------------- shell ---
        private static Border ItemRow(string label, string help, FrameworkElement control, bool alt)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new TextBlock
            {
                Text = label, Foreground = Fg, FontSize = 13.5, VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrEmpty(help))
            {
                // The explicit "?" is the discoverable help: the row's own tool tip still answers anywhere else
                // on it, but a player looking for "what does this do?" should not have to guess where to hover.
                var bubble = new Border
                {
                    Style = (Style)Application.Current.Resources["HelpBubble"],
                    ToolTip = new TextBlock { Text = help, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 },
                    Child = new TextBlock
                    {
                        Text = "?", Foreground = Accent, FontSize = 10.5, FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 1)
                    }
                };
                ToolTipService.SetInitialShowDelay(bubble, 250);
                ToolTipService.SetShowDuration(bubble, 30000);
                Hoverable(bubble);
                left.Children.Add(bubble);
            }
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            control.VerticalAlignment = VerticalAlignment.Center;
            control.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);

            var background = alt ? RowAlt : Row;
            var border = new Border
            {
                Background = background,
                Padding = new Thickness(18, 12, 18, 12),
                Margin = new Thickness(0, 0, 0, 6),
                CornerRadius = new CornerRadius(8),
                BorderBrush = Border, BorderThickness = new Thickness(1),
                Child = grid
            };
            // The rows are cards, not a table: a quiet lift under the pointer makes it clear where the control is.
            border.MouseEnter += (s, e) => border.Background = Hover;
            border.MouseLeave += (s, e) => border.Background = background;
            if (!string.IsNullOrEmpty(help))
            {
                // WPF's default tool tip is a single line as wide as the text: the longer help texts ran past the
                // screen edge and were cut. A wrapping TextBlock with a width of its own keeps the whole text on screen.
                border.ToolTip = new TextBlock { Text = help, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };
            }
            Hoverable(control);
            return border;
        }

        /// <summary>The interface's hover sound: on the option itself (the switch, the list, the slider, the
        /// field), never on the row around it, and on the buttons (the tabs, the actions, the key fields).</summary>
        private static void Hoverable(UIElement e) { e.MouseEnter += (s, a) => Audio.Hover(); }

        private static FrameworkElement BuildItem(SettingItem item, UiContext ctx, bool alt)
        {
            FrameworkElement control;
            switch (item.Kind)
            {
                case ItemKind.Bool: control = MakeBool(item, ctx); break;
                case ItemKind.Slider: control = MakeSlider(item, ctx); break;
                case ItemKind.Choice: control = MakeChoice(item, ctx); break;
                case ItemKind.Resolution: control = MakeResolution(item, ctx); break;
                case ItemKind.Scale: control = MakeScale(item, ctx); break;
                case ItemKind.Reverb: control = MakeReverb(item, ctx); break;
                case ItemKind.HdTextures: control = MakeHd(item, ctx); break;
                default: control = MakeText(item, ctx); break;
            }
            return ItemRow(item.Label(ctx.Pt), item.Help(ctx.Pt), control, alt);
        }

        // ---------------------------------------------------------------- kinds ---
        private static FrameworkElement MakeBool(SettingItem item, UiContext ctx)
        {
            bool on = item.Pref ? ctx.Cfg.GetPref(item.Key, item.Default) != "0" : ctx.Settings.GetBool(item.Key);
            var cb = new CheckBox { Style = (Style)Application.Current.Resources["ToggleSwitch"], IsChecked = on };
            cb.Checked += (s, e) =>
            {
                // A launcher preference is not a black.ini setting: it saves itself and never marks the tab dirty.
                if (item.Pref) { ctx.Cfg.SetPref(item.Key, "1"); ctx.Cfg.SavePrefs(); }
                else { ctx.Settings.SetBool(item.Key, true); ctx.Dirty(); }
                ctx.Refresh();
            };
            cb.Unchecked += (s, e) =>
            {
                if (item.Pref) { ctx.Cfg.SetPref(item.Key, "0"); ctx.Cfg.SavePrefs(); }
                else { ctx.Settings.SetBool(item.Key, false); ctx.Dirty(); }
                ctx.Refresh();
            };
            return cb;
        }

        private static FrameworkElement MakeSlider(SettingItem item, UiContext ctx)
        {
            double value = ctx.Settings.GetNumber(item.Key);
            if (value < item.Min) value = item.Min;
            if (value > item.Max) value = item.Max;

            var slider = new Slider
            {
                Style = (Style)Application.Current.Resources["DarkSlider"],
                Minimum = item.Min, Maximum = item.Max, TickFrequency = item.Step,
                IsSnapToTickEnabled = item.Step > 0, Width = 260, Value = value
            };
            var text = new TextBlock
            {
                Width = 62, Margin = new Thickness(14, 0, 0, 0), Foreground = Fg, FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right,
                Text = Format(item, value)
            };
            slider.ValueChanged += (s, e) =>
            {
                text.Text = Format(item, e.NewValue);
                ctx.Settings.SetNumber(item.Key, e.NewValue, item.Format);
                ctx.Dirty();
                ctx.Refresh();
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(slider);
            sp.Children.Add(text);
            return sp;
        }

        private static string Format(SettingItem item, double v)
        {
            return v.ToString(item.Format, CultureInfo.InvariantCulture) + item.Suffix;
        }

        // The ini keeps numbers in its own formatting ("1.00", "1", "0.2"); a choice is the same option when the
        // numbers are equal, not when the strings look alike.
        private static bool SameChoice(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            double x, y;
            return double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                   double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out y) && Math.Abs(x - y) < 1e-6;
        }

        private static FrameworkElement MakeChoice(SettingItem item, UiContext ctx)
        {
            var combo = new ComboBox { Style = (Style)Application.Current.Resources["DarkComboBox"], MinWidth = 190 };
            int sel = 0;
            string current = ctx.Settings.Get(item.Key);
            for (int i = 0; i < item.Choices.Length; i++)
            {
                combo.Items.Add(item.Choices[i].Pt == item.Choices[i].En ? item.Choices[i].En : (ctx.Pt ? item.Choices[i].Pt : item.Choices[i].En));
                if (SameChoice(item.Choices[i].Value, current)) sel = i;
            }
            combo.SelectedIndex = sel;
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedIndex < 0) return;
                ctx.Settings.Set(item.Key, item.Choices[combo.SelectedIndex].Value);
                ctx.Dirty();
                ctx.Refresh();
            };
            return combo;
        }

        private static FrameworkElement MakeResolution(SettingItem item, UiContext ctx)
        {
            var combo = new ComboBox { Style = (Style)Application.Current.Resources["DarkComboBox"], MinWidth = 190 };
            string cur = ctx.Settings.Get(item.Key);
            var values = new List<string>();
            int sel = 0;
            foreach (var c in item.Choices) { values.Add(c.Value); combo.Items.Add(ctx.Pt ? c.Pt : c.En); }
            if (!string.IsNullOrEmpty(cur) && !values.Contains(cur))
            {
                combo.Items.Add(ctx.Pt ? "PERSONALIZADO (" + cur + ")" : "CUSTOM (" + cur + ")");
                values.Add(cur);
                sel = values.Count - 1;
            }
            else
            {
                sel = Math.Max(0, values.IndexOf(cur ?? ""));
            }
            combo.SelectedIndex = sel;
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedIndex < 0) return;
                ctx.Settings.Set(item.Key, values[combo.SelectedIndex]);
                ctx.Dirty();
            };
            return combo;
        }

        private static FrameworkElement MakeScale(SettingItem item, UiContext ctx)
        {
            var combo = new ComboBox { Style = (Style)Application.Current.Resources["DarkComboBox"], MinWidth = 190 };
            var values = new List<string>();
            foreach (var c in item.Choices) { values.Add(c.Value); combo.Items.Add(ctx.Pt ? c.Pt : c.En); }
            string cur = ctx.Cfg.Get("scale");
            string render = ctx.Cfg.Get("render");
            int sel;
            if (render != "scale" || !values.Contains(TrimNum(cur)))
            {
                string shown = render == "native" ? (ctx.Pt ? "NATIVO" : "NATIVE")
                            : render == "scale" ? cur : render;
                combo.Items.Add(ctx.Pt ? "PERSONALIZADO (" + shown + ")" : "CUSTOM (" + shown + ")");
                values.Add(null); // never selected by a real value
                sel = values.Count - 1;
            }
            else sel = values.IndexOf(TrimNum(cur));
            combo.SelectedIndex = sel;
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedIndex < 0) return;
                string v = values[combo.SelectedIndex];
                if (v == null) return;
                double d;
                if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                {
                    ctx.Cfg.SetScale(d);
                    ctx.Dirty();
                    ctx.Rebuild(); // the custom entry may need to go
                }
            };
            return combo;
        }

        private static string TrimNum(string v)
        {
            double d;
            if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                return d.ToString("0.###", CultureInfo.InvariantCulture);
            return v;
        }

        private static FrameworkElement MakeReverb(SettingItem item, UiContext ctx)
        {
            var combo = new ComboBox { Style = (Style)Application.Current.Resources["DarkComboBox"], MinWidth = 190 };
            string cur = ctx.Cfg.GetReverb();
            bool off = cur == "off";
            double curNum = 0;
            if (!off) double.TryParse(cur, NumberStyles.Float, CultureInfo.InvariantCulture, out curNum);

            var values = new List<string>();
            foreach (var c in item.Choices)
            {
                values.Add(c.Value);
                combo.Items.Add(ctx.Pt ? c.Pt : c.En);
            }
            // the stop nearest the gain being used, as the port's own row does (nearest(kReverb, gain))
            int sel = 0;
            double best = double.MaxValue;
            for (int i = 0; i < values.Count; i++)
            {
                if (off) { if (values[i] == "off") { sel = i; break; } continue; }
                if (values[i] == "off") continue;
                double v;
                if (!double.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) continue;
                double d = Math.Abs(v - curNum);
                if (d < best) { best = d; sel = i; }
            }
            combo.SelectedIndex = sel;
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedIndex < 0) return;
                ctx.Cfg.SetReverb(values[combo.SelectedIndex]);
                ctx.Dirty();
            };
            return combo;
        }

        private static FrameworkElement MakeHd(SettingItem item, UiContext ctx)
        {
            var cb = new CheckBox { Style = (Style)Application.Current.Resources["ToggleSwitch"], IsChecked = !string.IsNullOrEmpty(ctx.Settings.Get(item.Key)) };
            var note = new TextBlock { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Foreground = FgDim };
            Action refresh = () =>
            {
                string path = ctx.Settings.Get(item.Key);
                cb.IsChecked = !string.IsNullOrEmpty(path);
                if (string.IsNullOrEmpty(path)) { note.Text = ""; return; }
                int matched, total;
                HdPack.Progress(path, out matched, out total);
                note.Text = total > 0 ? Loc.HdReady(matched, total) : "";
            };
            ctx.RegisterRefresh(refresh);
            cb.Checked += (s, e) => { if (string.IsNullOrEmpty(ctx.Settings.Get(item.Key))) ctx.Settings.Set(item.Key, ctx.HdDefaultDir()); ctx.Dirty(); ctx.Refresh(); };
            cb.Unchecked += (s, e) => { ctx.Settings.Set(item.Key, ""); ctx.Dirty(); ctx.Refresh(); };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(cb);
            sp.Children.Add(note);
            refresh();
            return sp;
        }

        private static FrameworkElement MakeText(SettingItem item, UiContext ctx)
        {
            var tb = new TextBox
            {
                Style = (Style)Application.Current.Resources["DarkTextBox"], Width = item.Browse ? 300 : 420,
                Text = ctx.Settings.Get(item.Key)
            };
            tb.LostFocus += (s, e) => { ctx.Settings.Set(item.Key, tb.Text.Trim()); ctx.Dirty(); ctx.Refresh(); };
            tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) { ctx.Settings.Set(item.Key, tb.Text.Trim()); ctx.Dirty(); ctx.Refresh(); Keyboard.ClearFocus(); } };
            if (!item.Browse) return tb;

            var button = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = Loc.AdvBrowseHd,
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 11,
                Margin = new Thickness(10, 0, 0, 0)
            };
            button.Click += (s, e) => BrowseFolder(tb, ctx, item.Key);
            Hoverable(button);

            var clear = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = "X",
                ToolTip = Loc.AdvHdClearHint,
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(6, 0, 0, 0)
            };
            clear.Click += (s, e) =>
            {
                tb.Text = "";
                ctx.Settings.Set(item.Key, "");
                ctx.Dirty();
            };
            Hoverable(clear);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(tb);
            row.Children.Add(button);
            row.Children.Add(clear);

            var stack = new StackPanel();
            stack.Children.Add(row);
            stack.Children.Add(new TextBlock
            {
                Text = Loc.AdvHdDefault,
                FontSize = 10.5,
                Foreground = FgDim,
                Margin = new Thickness(0, 4, 0, 0)
            });
            return stack;
        }

        /// <summary>
        /// Pick the pack's map.csv with the native Windows dialog (Explorer). The folder that holds
        /// the chosen map.csv is what black.ini stores; empty still means "HD textures off".
        /// </summary>
        private static void BrowseFolder(TextBox tb, UiContext ctx, string key)
        {
            string current = ctx.Settings.Get(key);
            string initial = Directory.Exists(current) ? current : ctx.HdDefaultDir();
            if (!Directory.Exists(initial)) initial = ctx.Settings == ctx.Cfg ? ctx.Cfg.PortDir : ctx.ModsRoot;
            var dialog = new OpenFileDialog
            {
                Title = Loc.AdvBrowseHdTitle,
                Filter = Loc.AdvBrowseHdFilter,
                FileName = "map.csv",
                InitialDirectory = initial,
                CheckFileExists = true,
                Multiselect = false,
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() != true) return;
            string folder = Path.GetDirectoryName(dialog.FileName);
            if (string.IsNullOrEmpty(folder)) return;
            tb.Text = folder;
            ctx.Settings.Set(key, folder);
            ctx.Dirty();
            ctx.Refresh();
        }

        // ------------------------------------------------------------- bindings ---
        private static FrameworkElement BuildBindings(UiContext ctx)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
            outer.Children.Add(Header(Loc.BindingsTitle));
            outer.Children.Add(Sub(Loc.BindingsHint));

            var grid = new Grid { Margin = new Thickness(18, 8, 18, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            outer.Children.Add(grid);

            // redrawn on its own after a capture, so the tab does not jump back to the top
            Action fill = () =>
            {
                grid.Children.Clear();
                grid.RowDefinitions.Clear();
                int r = 0;
                foreach (var action in KeyBindings.Actions)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    ushort[] slots = KeyBindings.ReadSlots(ctx.Cfg.Ini, action, ctx.Cfg.Get);

                    var name = new TextBlock
                    {
                        Text = ctx.Pt ? action.NamePt : action.NameEn, Foreground = Fg, FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 9, 0, 9)
                    };
                    Grid.SetRow(name, r); Grid.SetColumn(name, 0); grid.Children.Add(name);

                    for (int slot = 0; slot < 2; slot++)
                    {
                        int si = slot;
                        var btn = new Button
                        {
                            Style = (Style)Application.Current.Resources["MiniButton"],
                            Content = action.Fixed && si == 0 ? KeyBindings.DisplayName(slots[si], ctx.Pt) : KeyDisplay(slots[si], ctx.Pt),
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        if (action.Fixed && si == 0) { btn.IsEnabled = false; btn.ToolTip = Loc.BindFixed; }
                        btn.Click += (s, e) => MainWindow.Current.BeginCapture(action, si, btn);
                        Hoverable(btn);
                        Grid.SetRow(btn, r); Grid.SetColumn(btn, slot + 1); grid.Children.Add(btn);
                    }
                    r++;
                }
            };
            ctx.RefreshBindings = fill;
            fill();

            var reset = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"], Content = Loc.BindReset,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(18, 14, 0, 20)
            };
            reset.Click += (s, e) =>
            {
                foreach (var a in KeyBindings.Actions) ctx.Cfg.SetBinding(a.Ini, a.Defaults);
                ctx.Dirty(); fill();
            };
            Hoverable(reset);
            outer.Children.Add(reset);
            return outer;
        }

        private static string KeyDisplay(ushort key, bool pt)
        {
            return key == 0 ? (pt ? "—" : "—") : KeyBindings.DisplayName(key, pt);
        }

        // ------------------------------------------------------------------- pad ---
        // The Xbox pad's own settings (CONTROLES > JOYSTICK DE XBOX): the port's PC-side rows, which the game's own
        // menus have no room for, so the launcher is where they are set (black.ini: pad_*).
        private static FrameworkElement BuildPad(Section s, UiContext ctx)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
            outer.Children.Add(Header(ctx.Pt ? s.PadPt : s.PadEn));
            bool alt = false;
            foreach (var item in s.PadItems)
            {
                outer.Children.Add(BuildItem(item, ctx, alt));
                alt = !alt;
            }
            outer.Children.Add(BuildPadButtons(ctx));
            return outer;
        }

        // The pad's buttons and what the game does with each one, as its own controller setup has them: the
        // reference a player needs before changing anything there. Nothing here is a setting.
        private static FrameworkElement BuildPadButtons(UiContext ctx)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 22, 0, 0) };
            outer.Children.Add(Header(Loc.PadButtonsTitle));
            outer.Children.Add(Sub(Loc.PadButtonsHint));

            var grid = new Grid { Margin = new Thickness(18, 4, 18, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int r = 0;
            foreach (var b in Schema.PadButtons)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var btn = new TextBlock
                {
                    Text = ctx.Pt ? b.BtnPt : b.BtnEn, Foreground = Accent, FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 6, 14, 6), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
                };
                var act = new TextBlock
                {
                    Text = ctx.Pt ? b.Pt : b.En, Foreground = Fg, FontSize = 13,
                    Margin = new Thickness(0, 6, 0, 6), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(btn, r); Grid.SetColumn(btn, 0); grid.Children.Add(btn);
                Grid.SetRow(act, r); Grid.SetColumn(act, 1); grid.Children.Add(act);
                r++;
            }
            outer.Children.Add(grid);
            return outer;
        }

        // ----------------------------------------------------------------- mods ---
        private static FrameworkElement BuildMods(UiContext ctx)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
            outer.Children.Add(Header(Loc.ModsTitle));
            outer.Children.Add(Sub(Loc.ModsHint));

            var host = new StackPanel();
            outer.Children.Add(host);

            // re-scanned in place on every toggle, so the list does not jump back to the top
            Action fill = null;
            fill = () =>
            {
                host.Children.Clear();
                var mods = string.IsNullOrEmpty(ctx.ModsRoot) ? new List<Mod>() : Mods.Scan(ctx.ModsRoot);
                ctx.Mods = mods;

                if (mods.Count == 0)
                {
                    host.Children.Add(new TextBlock
                    {
                        Text = Loc.ModsEmpty, Foreground = FgDim, FontSize = 13,
                        Margin = new Thickness(4, 12, 20, 20), TextWrapping = TextWrapping.Wrap
                    });
                    return;
                }

                foreach (var m in mods)
                {
                    var grid = new Grid { Margin = new Thickness(0) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
                    titleRow.Children.Add(new TextBlock { Text = m.Title, Foreground = Fg, FontSize = 13.5, FontWeight = FontWeights.SemiBold });
                    if (!string.IsNullOrEmpty(m.Lang))
                    {
                        titleRow.Children.Add(new Border
                        {
                            Margin = new Thickness(9, 0, 0, 0), CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 1, 6, 1),
                            Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)),
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock { Text = m.Lang.ToUpperInvariant(), FontSize = 9.5, Foreground = FgDim }
                        });
                    }
                    if (m.Blocked)
                    {
                        titleRow.Children.Add(new Border
                        {
                            Margin = new Thickness(9, 0, 0, 0), CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 1, 6, 1),
                            Background = new SolidColorBrush(Color.FromArgb(0x33, 0xE0, 0x8A, 0x6A)),
                            VerticalAlignment = VerticalAlignment.Center, ToolTip = Loc.ModsBlockedTip,
                            Child = new TextBlock { Text = Loc.ModsBlocked, FontSize = 9.5, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xA8)) }
                        });
                    }
                    info.Children.Add(titleRow);
                    info.Children.Add(new TextBlock
                    {
                        Text = m.Folder + "  ·  " + (ctx.Pt ? "prioridade" : "priority") + " " + m.Priority,
                        Foreground = FgDim, FontSize = 11, Margin = new Thickness(0, 3, 0, 0)
                    });
                    Grid.SetColumn(info, 0); grid.Children.Add(info);

                    var cb = new CheckBox { Style = (Style)Application.Current.Resources["ToggleSwitch"], IsChecked = m.On };
                    var mod = m;
                    cb.Checked += (s, e) => { Mods.SetOn(mod, true); ctx.Dirty(); fill(); };
                    cb.Unchecked += (s, e) => { Mods.SetOn(mod, false); ctx.Dirty(); fill(); };
                    Grid.SetColumn(cb, 1); grid.Children.Add(cb);

                    var row = new Border
                    {
                        Padding = new Thickness(18, 12, 18, 12), Margin = new Thickness(0, 0, 0, 6),
                        CornerRadius = new CornerRadius(8), BorderBrush = Border, BorderThickness = new Thickness(1),
                        Background = Row, Child = grid
                    };
                    Hoverable(cb);
                    host.Children.Add(row);
                }
                host.Children.Add(new TextBlock { Text = "", Height = 14 });
            };
            fill();
            return outer;
        }

        // ------------------------------------------------------------- advanced ---
        private static FrameworkElement BuildAdvanced(UiContext ctx)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
            outer.Children.Add(Header(Loc.AdvFiles));

            var wrap = new WrapPanel { Margin = new Thickness(18, 10, 18, 24) };
            string port = ctx.Cfg.PortDir;
            wrap.Children.Add(AdvButton(Loc.AdvOpenGame, () => GameLauncher.OpenInShell(Path.Combine(port, "game"))));
            wrap.Children.Add(AdvButton(Loc.AdvOpenMods, () => GameLauncher.OpenInShell(Mods.ModsDir(port))));
            wrap.Children.Add(AdvButton(Loc.AdvOpenIni, () => GameLauncher.SelectInShell(ctx.Cfg.IniPath)));
            wrap.Children.Add(AdvButton(Loc.AdvOpenLog, () => GameLauncher.OpenInShell(LogStore.Dir(port))));
            outer.Children.Add(wrap);

            outer.Children.Add(Header(Loc.AdvInfo));
            var infoWrap = new WrapPanel { Margin = new Thickness(18, 10, 18, 24) };
            infoWrap.Children.Add(AdvButton(Loc.AdvChangelog, () => ctx.ShowChangelog()));
            outer.Children.Add(infoWrap);
            return outer;
        }

        private static Button AdvButton(string text, Action onClick)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["GhostButton"], Content = text, Margin = new Thickness(0, 0, 10, 10) };
            b.Click += (s, e) => onClick();
            Hoverable(b);
            return b;
        }

        // ---------------------------------------------------------------- header ---
        private static FrameworkElement Header(string text)
        {
            return new Border
            {
                Background = (Brush)Application.Current.Resources["B.Panel"],
                BorderBrush = Border, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 12, 18, 12), Margin = new Thickness(0, 0, 0, 6),
                Child = new TextBlock { Text = text, Foreground = Accent, FontSize = 12, FontWeight = FontWeights.Bold }
            };
        }

        private static FrameworkElement Sub(string text)
        {
            return new TextBlock
            {
                Text = text, Foreground = FgDim, FontSize = 12, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(18, 10, 18, 6)
            };
        }
    }
}
