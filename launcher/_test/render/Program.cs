using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackLauncher;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("use: render.exe <portDir> <tab id> <out.png> [pt|en]");
            return 2;
        }

        string portDir = args[0];
        string id = args[1];
        string outFile = args[2];
        bool pt = args.Length < 4 || !args[3].Equals("en", StringComparison.OrdinalIgnoreCase);

        // The builder reads its colours from the application's resources, so the theme has to be there
        // before the first section is built.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/blacklauncher;component/Theme.xaml", UriKind.Absolute)
        });
        Loc.Pt = pt;

        var cfg = Config.Load(portDir);
        var ctx = new UiContext { Cfg = cfg, Pt = pt };

        Section section = Schema.Build(pt).FirstOrDefault(s => s.Id == id);
        if (section == null)
        {
            Console.Error.WriteLine("no tab with id '" + id + "'");
            return 3;
        }

        // The section is a scroll viewer; draw what is inside it, at its full height (the whole page,
        // including the part that only shows up when the player scrolls).
        var view = (ScrollViewer)UiBuilder.BuildSection(section, ctx);
        var content = (FrameworkElement)view.Content;
        double width = 1060;
        content.Measure(new Size(width, double.PositiveInfinity));
        double height = content.DesiredSize.Height;
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();

        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(outFile)) encoder.Save(stream);

        Console.WriteLine("wrote " + outFile + "  " + (int)width + "x" + (int)height + "  (" + (pt ? "pt" : "en") + ")");
        return 0;
    }
}
