using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlackLauncher
{
    /// <summary>
    /// The update notes: shown automatically once after a new version opens, and again from
    /// ADVANCED > Update information. The notes are grouped into section cards and the body scrolls
    /// when the release has more text than fits on screen.
    /// </summary>
    public partial class ChangelogWindow : Window
    {
        private readonly Config _cfg;
        private readonly Brush _panel;
        private readonly Brush _border;
        private readonly Brush _accent;
        private readonly Brush _fg;
        private readonly Brush _fgDim;

        private sealed class NoteBlock
        {
            public bool Heading;
            public bool Bullet;
            public int Level;
            public string Text;
        }

        public ChangelogWindow(Config cfg)
        {
            InitializeComponent();
            _cfg = cfg;
            _panel = (Brush)Application.Current.Resources["B.Panel"];
            _border = (Brush)Application.Current.Resources["B.Border"];
            _accent = (Brush)Application.Current.Resources["B.Accent"];
            _fg = (Brush)Application.Current.Resources["B.Fg"];
            _fgDim = (Brush)Application.Current.Resources["B.FgDim"];
            Title = Loc.ChangelogCaption;
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CaptionText.Text = Loc.ChangelogCaption;
            VersionText.Text = Loc.Product;
            SubtitleText.Text = Loc.ChangelogSubtitle;
            CreditLink.Text = Loc.Credits;
            CreditLink.ToolTip = Loc.CreditsUrl;
            YouTubeButton.ToolTip = Loc.YouTubeHint;
            DiscordButton.ToolTip = Loc.DiscordHint;

            TitleText.Text = Loc.ChangelogTitle;
            IntroText.Text = Loc.ChangelogIntro;
            HintText.Text = Loc.ChangelogHint;
            ContinueButton.Content = Loc.ChangelogContinue;

            BuildNotes(ReleaseNotes.Current(_cfg));
            ContinueButton.Focus();
        }

        private void BuildNotes(string raw)
        {
            NotesPanel.Children.Clear();
            List<NoteBlock> blocks = ParseNotes(raw);
            StackPanel section = null;
            bool anyBody = false;

            foreach (NoteBlock block in blocks)
            {
                if (block.Heading)
                {
                    if (IsVersionHeading(block.Text)) continue;
                    section = new StackPanel();
                    var card = Card(section);
                    var heading = new TextBlock
                    {
                        Text = block.Text,
                        FontSize = block.Level <= 1 ? 15.5 : block.Level == 2 ? 14 : 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = _accent,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 7)
                    };
                    section.Children.Add(heading);
                    NotesPanel.Children.Add(card);
                    continue;
                }

                if (section == null)
                {
                    section = new StackPanel();
                    section.Children.Add(new TextBlock
                    {
                        Text = Loc.ChangelogSummary,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = _accent,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 7)
                    });
                    NotesPanel.Children.Add(Card(section));
                }

                if (block.Bullet)
                {
                    section.Children.Add(Bullet(block.Text));
                    anyBody = true;
                }
                else if (!string.IsNullOrWhiteSpace(block.Text))
                {
                    section.Children.Add(Paragraph(block.Text));
                    anyBody = true;
                }
            }

            if (NotesPanel.Children.Count == 0 || !anyBody)
            {
                NotesPanel.Children.Clear();
                NotesPanel.Children.Add(Paragraph(Loc.ChangelogEmpty));
            }
        }

        private Border Card(StackPanel content)
        {
            return new Border
            {
                Background = _panel,
                BorderBrush = _border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(14, 13, 14, 13),
                Margin = new Thickness(0, 0, 0, 10),
                Child = content
            };
        }

        private FrameworkElement Bullet(string text)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dot = new TextBlock
            {
                Text = "\u2022",
                Foreground = _accent,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top
            };
            var body = new TextBlock
            {
                Text = text,
                Foreground = _fg,
                FontSize = 12.8,
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(dot, 0);
            Grid.SetColumn(body, 1);
            row.Children.Add(dot);
            row.Children.Add(body);
            return row;
        }

        private FrameworkElement Paragraph(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = _fgDim,
                FontSize = 12.8,
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private static bool IsVersionHeading(string text)
        {
            string value = (text ?? "").Trim();
            return value.Equals(Loc.Product, StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("ModLoader " + Loc.Version, StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(value, @"^ModLoader\s+" + Regex.Escape(Loc.Version) + @"$", RegexOptions.IgnoreCase);
        }

        private static List<NoteBlock> ParseNotes(string raw)
        {
            var blocks = new List<NoteBlock>();
            if (string.IsNullOrWhiteSpace(raw)) return blocks;
            NoteBlock current = null;

            foreach (string rawLine in raw.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.TrimEnd();
                if (line.TrimStart().StartsWith("```")) { current = null; continue; }

                var heading = Regex.Match(line, @"^\s{0,3}(#{1,6})\s*(.*?)\s*#*\s*$");
                if (heading.Success)
                {
                    blocks.Add(new NoteBlock
                    {
                        Heading = true,
                        Level = heading.Groups[1].Value.Length,
                        Text = CleanInline(heading.Groups[2].Value)
                    });
                    current = null;
                    continue;
                }

                var bullet = Regex.Match(line, @"^\s{0,3}[-*+]\s+(.*)$");
                if (bullet.Success)
                {
                    current = new NoteBlock { Bullet = true, Text = CleanInline(bullet.Groups[1].Value) };
                    blocks.Add(current);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    current = null;
                    continue;
                }

                // Indented lines are a continuation of the previous bullet/paragraph, not a new one.
                if (current != null && char.IsWhiteSpace(line[0]))
                {
                    current.Text = (current.Text + " " + CleanInline(line.Trim())).Trim();
                    continue;
                }

                current = new NoteBlock { Text = CleanInline(line.Trim()) };
                blocks.Add(current);
            }
            return blocks;
        }

        private static string CleanInline(string text)
        {
            string value = (text ?? "").Trim();
            value = value.Replace("**", "").Replace("__", "");
            value = Regex.Replace(value, @"`([^`]*)`", "$1");
            value = Regex.Replace(value, @"!\[([^\]]*)\]\([^)]*\)", "$1");
            value = Regex.Replace(value, @"\[([^\]]+)\]\(([^)]+)\)", "$1 ($2)");
            return value.Trim();
        }

        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        private void Credit_Click(object sender, MouseButtonEventArgs e) => GameLauncher.OpenUrl(Loc.CreditsUrl);
        private void YouTube_Click(object sender, RoutedEventArgs e) => GameLauncher.OpenUrl(Loc.CreditsUrl);
        private void Discord_Click(object sender, RoutedEventArgs e) => GameLauncher.OpenUrl(Loc.DiscordUrl);
    }
}
