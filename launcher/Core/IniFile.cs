using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackLauncher
{
    /// <summary>
    /// The port's black.ini: "key = value" lines, '#' comments, values without spaces (settings.cpp
    /// reads them with sscanf "%63[^= ] = %127s"). Only the keys the launcher owns are ever rewritten;
    /// every other line (comments, unknown keys, the port's own ordering) is preserved as it was.
    /// </summary>
    public sealed class IniFile
    {
        private readonly List<string> _lines = new List<string>();

        public string Path { get; private set; }
        public bool Exists { get; private set; }

        private IniFile(string path, IEnumerable<string> lines)
        {
            Path = path;
            _lines.AddRange(lines);
        }

        public static IniFile Load(string path)
        {
            if (!File.Exists(path)) return new IniFile(path, new string[0]);
            string text = File.ReadAllText(path);
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int n = lines.Length;
            // a trailing newline leaves one empty entry; Save writes it back
            if (n > 0 && lines[n - 1].Length == 0) n--;
            var list = new List<string>(n);
            for (int i = 0; i < n; i++) list.Add(lines[i]);
            return new IniFile(path, list) { Exists = true };
        }

        /// <summary>The port's own split: '#' starts a comment, the key runs up to '=' or a space.</summary>
        private static bool Split(string line, out string key, out string value)
        {
            key = null;
            value = null;
            if (line == null) return false;
            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length || line[i] == '#') return false;
            int ks = i;
            while (i < line.Length && line[i] != '=' && !char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length) return false;
            key = line.Substring(ks, i - ks);
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length || line[i] != '=') return false;
            i++;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            value = line.Substring(i).Trim();
            return true;
        }

        public bool TryGet(string key, out string value)
        {
            // later lines win, like the port's sequential load()
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (!Split(_lines[i], out string k, out string v)) continue;
                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) { value = v; return true; }
            }
            value = null;
            return false;
        }

        /// <summary>Replace the key in place (or append it), dropping any duplicate lines of it.</summary>
        public void Set(string key, string value)
        {
            value = value ?? "";
            int first = -1;
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (!Split(_lines[i], out string k, out _)) continue;
                if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;
                if (first >= 0) _lines.RemoveAt(first); // an earlier duplicate
                first = i;
            }
            if (first >= 0) _lines[first] = key + " = " + value;
            else _lines.Add(key + " = " + value);
        }

        public void Remove(string key)
        {
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (!Split(_lines[i], out string k, out _)) continue;
                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) _lines.RemoveAt(i);
            }
        }

        public void Save(string path = null)
        {
            path = path ?? Path;
            var sb = new StringBuilder();
            foreach (string l in _lines) { sb.Append(l); sb.Append("\r\n"); }
            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
        }
    }
}
