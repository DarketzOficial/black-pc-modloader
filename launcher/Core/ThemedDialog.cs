using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlackLauncher
{
    /// <summary>
    /// A small dialog in the launcher's own theme (dark panel, gold buttons, no system chrome) used in place of the
    /// Windows message box: the save-on-close question, the restore-defaults confirmation and the play error.
    /// </summary>
    public static class ThemedDialog
    {
        public enum Result { Yes, No, Cancel, Ok }

        private static readonly Brush Panel = new SolidColorBrush(Color.FromRgb(0x1E, 0x1F, 0x22));
        private static readonly Brush Edge = new SolidColorBrush(Color.FromRgb(0x4A, 0x4C, 0x52));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xC8, 0xA2, 0x4A));
        private static readonly Brush Text = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEE));

        public static Result Show(Window owner, string message, Result buttons, string yes = "SIM", string no = "NÃO", string cancel = "CANCELAR",
                                  bool showCancel = true)
        {
            var win = new Window
            {
                Owner = owner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Title = owner != null ? owner.Title : "BLACK",
            };
            var border = new Border
            {
                Background = Panel,
                BorderBrush = Edge,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(22, 18, 22, 16),
            };
            var stack = new StackPanel { MaxWidth = 520 };
            stack.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Text,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18),
            });
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var answer = Result.Cancel;
            Action<string, Result, bool> add = (label, value, primary) =>
            {
                var button = new Button
                {
                    Content = label,
                    MinWidth = 96,
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(12, 6, 12, 6),
                    Background = primary ? Gold : Brushes.Transparent,
                    Foreground = primary ? Brushes.Black : Text,
                    BorderBrush = primary ? Gold : Edge,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                };
                button.Click += (s, e) => { answer = value; win.DialogResult = true; };
                row.Children.Add(button);
            };
            switch (buttons)
            {
                case Result.Yes: add(yes, Result.Yes, true); add(no, Result.No, false); add(cancel, Result.Cancel, false); break;
                case Result.Ok:
                    add(yes == "SIM" ? "OK" : yes, Result.Ok, true);
                    if (showCancel) add(cancel, Result.Cancel, false);
                    break;
            }
            stack.Children.Add(row);
            border.Child = stack;
            win.Content = border;
            // drag by the panel and close with Esc
            border.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) win.DragMove(); };
            win.KeyDown += (s, e) => { if (e.Key == Key.Escape) { answer = Result.Cancel; win.DialogResult = true; } };
            win.ShowDialog();
            return answer;
        }
    }
}
