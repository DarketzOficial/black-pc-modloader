using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BlackLauncher
{
    /// <summary>Edits only the Recomp's top-level options in black.toml; all other TOML text is retained.</summary>
    public sealed class RecompConfig : ISettingsStore
    {
        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "display_mode", "borderless" }, { "monitor", "" }, { "width", "0" }, { "height", "0" },
            { "hd_textures", "true" }, { "hd_texture_path", "textures" },
            { "render_percent", "100" }, { "fps_cap", "60" }, { "vsync", "true" }, { "hdr_output", "true" },
            { "fsr_mode", "0" }, { "ambient_occlusion", "0" }, { "msaa", "0" }, { "smaa", "false" },
            { "image_sharpening", "0" }, { "image_details", "0" }, { "image_clarity", "0" }, { "image_vibrance", "0" },
            { "draw_distance", "1" }, { "fov", "70" }, { "viewmodel_fov", "70" }, { "skip_intros", "false" },
            { "fps_counter", "false" }, { "reload_blur", "true" }, { "pause_on_focus_loss", "true" },
            { "hud_fade", "true" }, { "infinite_ammo", "false" }, { "silver_weapons", "false" },
            { "invert_mouse", "false" }, { "hold_aim", "true" }, { "aim_assist", "true" },
            { "controller_hold_aim", "false" }, { "controller_sensitivity", "1.0" }, { "rumble_intensity", "100" },
            { "menu_animations", "true" }, { "mouse_sensitivity", "1.0" }, { "walk_speed", "50" },
            { "fmv_scale", "0" }, { "language", "int" }, { "subtitles_enabled", "true" },
            { "subtitle_size", "120" }, { "subtitle_bold", "false" }, { "subtitle_color", "0" },
            { "subtitle_shadow", "1" }, { "subtitle_background", "true" }, { "audio_output", "0" },
            { "anisotropic_filtering", "0" }
        };

        private static readonly HashSet<string> Booleans = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vsync", "hdr_output", "smaa", "hd_textures", "skip_intros", "fps_counter", "reload_blur", "pause_on_focus_loss",
            "hud_fade", "infinite_ammo", "silver_weapons", "invert_mouse", "hold_aim", "aim_assist",
            "controller_hold_aim", "menu_animations", "subtitles_enabled", "subtitle_bold", "subtitle_background"
        };
        private static readonly HashSet<string> Strings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "display_mode", "monitor", "language", "hd_texture_path"
        };

        private readonly List<string> _lines;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(Defaults, StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string Root { get; private set; }
        public string Path { get; private set; }

        private RecompConfig(string root)
        {
            Root = System.IO.Path.GetFullPath(root);
            Path = System.IO.Path.Combine(Root, "black.toml");
            _lines = File.Exists(Path) ? File.ReadAllLines(Path).ToList() : new List<string>();
            bool rootTable = true;
            foreach (string line in _lines)
            {
                string key, value;
                if (!TryRootPair(line, ref rootTable, out key, out value)) continue;
                _values[key] = Decode(key, value);
            }
        }

        public static RecompConfig Load(string root) => new RecompConfig(root);
        public string HdDefaultDir() => System.IO.Path.Combine(Root, "textures");
        public string Get(string key)
        {
            if (string.Equals(key, "resolution", StringComparison.OrdinalIgnoreCase))
            {
                int width, height;
                return int.TryParse(Get("width"), NumberStyles.None, CultureInfo.InvariantCulture, out width) &&
                       int.TryParse(Get("height"), NumberStyles.None, CultureInfo.InvariantCulture, out height) && width > 0 && height > 0
                    ? width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) : "";
            }
            if (string.Equals(key, "hd_textures_ui", StringComparison.OrdinalIgnoreCase))
            {
                string path = Get("hd_texture_path");
                string fullPath = System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(Root, path);
                try { return GetBool("hd_textures") && Directory.Exists(fullPath) ? path : ""; }
                catch (Exception) { return ""; }
            }
            string value; return _values.TryGetValue(key, out value) ? value : "";
        }
        public void Set(string key, string value)
        {
            value = value ?? "";
            if (string.Equals(key, "resolution", StringComparison.OrdinalIgnoreCase))
            {
                int width = 0, height = 0;
                if (value.Length > 0)
                {
                    string[] parts = value.Split('x');
                    if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out width) ||
                        !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out height) ||
                        width < 1 || width > 16384 || height < 1 || height > 16384) return;
                }
                Set("width", width.ToString(CultureInfo.InvariantCulture));
                Set("height", height.ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (string.Equals(key, "hd_textures_ui", StringComparison.OrdinalIgnoreCase))
            {
                SetBool("hd_textures", !string.IsNullOrWhiteSpace(value));
                if (!string.IsNullOrWhiteSpace(value)) Set("hd_texture_path", value);
                return;
            }
            if (Get(key) == value) return;
            _values[key] = value;
            _changed.Add(key);
        }
        public bool GetBool(string key) => string.Equals(Get(key), "true", StringComparison.OrdinalIgnoreCase) || Get(key) == "1";
        public void SetBool(string key, bool value) => Set(key, value ? "true" : "false");
        public double GetNumber(string key)
        {
            double value;
            return double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0;
        }
        public void SetNumber(string key, double value, string format) => Set(key, value.ToString(format, CultureInfo.InvariantCulture));

        public void ResetToDefaults()
        {
            foreach (var pair in Defaults) Set(pair.Key, pair.Value);
        }

        public void Save()
        {
            if (_changed.Count == 0) return;
            var lines = new List<string>(_lines);
            bool inRoot = true;
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lines.Count; i++)
            {
                string key, raw;
                if (!TryRootPair(lines[i], ref inRoot, out key, out raw) || !_changed.Contains(key)) continue;
                lines[i] = key + " = " + Encode(key, _values[key]) + TrailingComment(lines[i]);
                found.Add(key);
            }
            int insertAt = lines.FindIndex(line => line.TrimStart().StartsWith("[", StringComparison.Ordinal));
            if (insertAt < 0) insertAt = lines.Count;
            foreach (string key in _changed.OrderBy(k => k, StringComparer.Ordinal))
                if (!found.Contains(key)) lines.Insert(insertAt++, key + " = " + Encode(key, _values[key]));

            Directory.CreateDirectory(Root);
            string temp = Path + ".tmp";
            File.WriteAllText(temp, string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));
            try
            {
                if (File.Exists(Path)) File.Replace(temp, Path, null);
                else File.Move(temp, Path);
                _lines.Clear();
                _lines.AddRange(lines);
                _changed.Clear();
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        private static bool TryRootPair(string line, ref bool inRoot, out string key, out string value)
        {
            key = value = null;
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("[", StringComparison.Ordinal)) { inRoot = false; return false; }
            if (!inRoot || trimmed.Length == 0 || trimmed[0] == '#') return false;
            int equals = line.IndexOf('=');
            if (equals <= 0) return false;
            key = line.Substring(0, equals).Trim();
            if (key.Length == 0 || key.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-'))) return false;
            value = line.Substring(equals + 1).Trim();
            bool quoted = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '"' && (i == 0 || value[i - 1] != '\\')) quoted = !quoted;
                else if (value[i] == '#' && !quoted) { value = value.Substring(0, i).TrimEnd(); break; }
            }
            return true;
        }

        private static string Decode(string key, string value)
        {
            if (Booleans.Contains(key)) return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
            if (Strings.Contains(key) && value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
            return value;
        }

        private static string Encode(string key, string value)
        {
            if (Booleans.Contains(key)) return value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
            if (Strings.Contains(key)) return "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            return value;
        }

        private static string TrailingComment(string line)
        {
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"' && (i == 0 || line[i - 1] != '\\')) quoted = !quoted;
                else if (line[i] == '#' && !quoted) return " " + line.Substring(i).TrimEnd();
            }
            return "";
        }
    }
}
