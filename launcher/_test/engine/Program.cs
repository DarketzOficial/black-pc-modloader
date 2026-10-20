using System;
using System.IO;
using BlackLauncher;

internal static class Program
{
    private static int Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        var cfg = Config.Load(dir);
        Console.WriteLine("port: " + cfg.PortDir);
        Console.WriteLine("ini exists: " + cfg.Ini.Exists);

        // read-back of the interesting rows
        Console.WriteLine("reverb   = " + cfg.GetReverb() + "   (reverb=" + cfg.Get("reverb") + " gain=" + cfg.Get("reverb_gain") + ")");
        Console.WriteLine("scale    = " + cfg.Get("scale") + "  render=" + cfg.Get("render"));
        Console.WriteLine("window   = '" + cfg.GetWindow() + "'");
        Console.WriteLine("hd       = " + cfg.GetHd() + "  '" + cfg.Get("hd_textures") + "'");
        Console.WriteLine("fov      = " + cfg.Get("fov") + "  viewmodel=" + cfg.Get("viewmodel_fov"));
        Console.WriteLine("fire key = " + string.Join(",", KeyBindings.ReadSlots(cfg.Ini, KeyBindings.Actions[4], cfg.Get).Length > 0
            ? Array.ConvertAll(KeyBindings.ReadSlots(cfg.Ini, KeyBindings.Actions[4], cfg.Get), v => KeyBindings.IniName(v))
            : new string[0]));

        if (args.Length < 2 || args[1] != "write") return 0;

        // exercise the write path
        cfg.SetBool("aa", true);
        cfg.SetNumber("scale", 2, "0.000");
        cfg.Set("render", "scale");
        cfg.SetReverb("1.5");
        cfg.SetHd(false);
        cfg.SetWindow("0x1080");
        cfg.SetBool("fpscounter", false);
        cfg.SetNumber("gain", 1.5, "0.00");
        // move Fire off Mouse1 and rebind it to Space, clearing Space from objectives like the in-game capture does
        var table = new System.Collections.Generic.Dictionary<KeyBindings.Action, ushort[]>();
        foreach (var a in KeyBindings.Actions) table[a] = KeyBindings.ReadSlots(cfg.Ini, a, cfg.Get);
        ushort vk = 0x20;
        foreach (var a in KeyBindings.Actions)
            for (int s = 0; s < 2; s++)
                if (table[a][s] == vk) table[a][s] = 0;
        table[KeyBindings.Actions[4]][0] = vk;
        foreach (var a in KeyBindings.Actions) cfg.SetBinding(a.Ini, table[a]);

        cfg.Save();
        Console.WriteLine("saved.");
        return 0;
    }
}
