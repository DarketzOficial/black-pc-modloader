using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BlackLauncher
{
    /// <summary>
    /// The launcher's view of black.ini: one string per managed key, the port's own defaults when a
    /// key is missing, and the few settings that are more than one key (resolution, render scale,
    /// reverb, frame generation, HD textures) handled explicitly. The port rewrites black.ini on exit,
    /// so anything the launcher keeps for itself lives in launcher.ini instead.
    /// </summary>
    public sealed class Config : ISettingsStore
    {
        public string PortDir { get; private set; }
        public string IniPath { get; private set; }
        public string PrefsPath { get; private set; }
        public IniFile Ini { get; private set; }
        public IniFile Prefs { get; private set; }

        private readonly Dictionary<string, string> _v = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // keys the port omits when empty (settings_save)
        private static readonly HashSet<string> Optional = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "window", "hd_textures" };

        /// <summary>Defaults a fresh port/modloader install uses (common.h + main.cpp first-install).</summary>
        public static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "scale", "3.000" }, { "render", "scale" },
            { "seamfill", "1" }, { "weaponblur", "1" }, { "hud169", "1" }, { "hudfade", "1" }, { "trainer", "0" },
            { "hudfade_hold", "2.50" }, { "hudfade_crosshair", "0" }, { "subtitles", "1" },
            { "hd_textures", "" }, { "effectsnative", "0" },
            { "widescreen", "1" }, { "fullscreen", "0" },
            { "gain", "1.00" }, { "gamma", "1.00" },
            { "mouse_sens", "2.000" }, { "mouse_invert", "0" },
            { "reverb", "1" }, { "reverb_gain", "1.00" },
            { "autoaim", "0" }, { "fov", "120.0" }, { "skipanymovie", "1" },
            { "aniso", "0" }, { "viewmodel_fov", "80.0" }, { "fmv_scale", "0" }, { "aa", "1" },
            { "ao", "0" },
            { "hdr", "0" }, { "hdr_strength", "70" },
            { "sharpen", "0" }, { "details", "0" }, { "clarity", "0" }, { "vibrance", "0" },
            { "fps_cap", "0" }, { "vsync", "0" }, { "fpscounter", "0" }, { "draw_distance", "1" },
            { "pause_focus", "0" }, { "aim_toggle", "0" }, { "walk_speed", "25" },
            { "pad_deadzone", "0" }, { "pad_sens", "100" }, { "pad_vibration", "1" }, { "pad_invert", "0" },
            { "window", "" },
            // key_<action> added from KeyBindings.Actions below
        };

        static Config()
        {
            foreach (var a in KeyBindings.Actions)
                Defaults["key_" + a.Ini] = KeyBindings.WriteSlots(a.Defaults);
        }

        public static Config Load(string portDir)
        {
            var c = new Config
            {
                PortDir = portDir,
                IniPath = Path.Combine(portDir, "black.ini"),
                PrefsPath = Path.Combine(portDir, "launcher.ini"),
                Ini = IniFile.Load(Path.Combine(portDir, "black.ini")),
                Prefs = IniFile.Load(Path.Combine(portDir, "launcher.ini")),
            };
            foreach (var kv in Defaults)
            {
                string v;
                c._v[kv.Key] = c.Ini.TryGet(kv.Key, out v) ? v : kv.Value;
            }
            return c;
        }

        // ------------------------------------------------------------------ values ---
        public string Get(string key) { string v; return _v.TryGetValue(key, out v) ? v : ""; }
        public void Set(string key, string value) { _v[key] = value ?? ""; }

        public bool GetBool(string key) => Get(key) != "0" && !string.IsNullOrEmpty(Get(key));
        public void SetBool(string key, bool on) => Set(key, on ? "1" : "0");

        public double GetNumber(string key) { double d; return double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0; }
        public void SetNumber(string key, double value, string format) => Set(key, value.ToString(format, CultureInfo.InvariantCulture));

        // ------------------------------------------------------------ special rows ---
        // RESOLUTION: "window = 0xH" (0 width = the desktop fit) or nothing for MONITOR
        public string GetWindow() => Get("window");
        public void SetWindow(string value)
        {
            if (string.IsNullOrEmpty(value)) { Set("window", ""); return; }
            Set("window", value);
        }

        // RESOLUTION SCALE: "scale = K" (render = scale so a leftover native/resolution from the command line steps aside)
        public void SetScale(double scale)
        {
            Set("render", "scale");
            SetNumber("scale", scale, "0.000");
        }

        // ROOM REVERB: reverb on/off + reverb_gain together ("off" or the gain)
        public string GetReverb() { return GetBool("reverb") ? Get("reverb_gain") : "off"; }
        public void SetReverb(string value)
        {
            if (value == "off") { SetBool("reverb", false); return; }
            SetBool("reverb", true);
            if (!string.IsNullOrEmpty(value)) Set("reverb_gain", value);
        }

        // HD TEXTURES: on when the path is set (the port's default location otherwise)
        public bool GetHd() => !string.IsNullOrEmpty(Get("hd_textures"));
        public string HdDefaultDir() => Path.Combine(PortDir, "mods", "hdtextures", "hd");
        public void SetHd(bool on)
        {
            if (on)
            {
                string cur = Get("hd_textures");
                Set("hd_textures", string.IsNullOrEmpty(cur) ? HdDefaultDir() : cur);
            }
            else Set("hd_textures", "");
        }

        public void SetBinding(string iniName, ushort[] slots) => Set("key_" + iniName, KeyBindings.WriteSlots(slots));

        // ------------------------------------------------------------------ persist ---
        public void ResetToDefaults()
        {
            _v.Clear();
            foreach (var kv in Defaults) _v[kv.Key] = kv.Value;
        }

        public void Save()
        {
            foreach (var kv in _v)
            {
                if (Optional.Contains(kv.Key))
                {
                    if (string.IsNullOrEmpty(kv.Value)) Ini.Remove(kv.Key);
                    else Ini.Set(kv.Key, kv.Value);
                }
                else Ini.Set(kv.Key, kv.Value);
            }
            Ini.Save(IniPath);
        }

        // ------------------------------------------------------------- launcher prefs ---
        public string GetPref(string key, string def)
        {
            string v;
            return Prefs.TryGet(key, out v) ? v : def;
        }
        public void SetPref(string key, string value) { Prefs.Set(key, value); }
        public void SavePrefs() { Prefs.Save(PrefsPath); }
    }
}
