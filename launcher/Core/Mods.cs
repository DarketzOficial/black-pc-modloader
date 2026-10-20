using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlackLauncher
{
    /// <summary>One folder under mods\ — the same rules the port uses (mods.cpp scan()).</summary>
    public sealed class Mod
    {
        public string Folder;
        public string Title;
        public int Priority;
        public string Lang;      // "" = any language
        public bool On;          // no ".off" marker in the folder
        public bool Blocked;     // its language is not the one being played
        public string BasePath;
        public string GameRoot;  // where the mod's files start (see Mods.FindRoot)
        public bool IsFileMod;   // brings game files (the port only lists these)

        public string LangDisplay => string.IsNullOrEmpty(Lang) ? "" : Lang;
    }

    public static class Mods
    {
        public static string ModsDir(string portDir) => Path.Combine(portDir, "mods");

        /// <summary>The game's own top-level entries (the disc's layout): a folder that brings any of them holds
        /// the game's files itself.</summary>
        private static readonly string[] GameDirs = { "chars", "data", "export", "language", "levels", "sound", "videos" };
        private static readonly string[] GameFiles = { "default.xbe", "GlobData.bin" };

        /// <summary>Folders that bring game files, read the way their author packed them — a game\ subfolder (the
        /// port's own packaging), the game's own folders at the top (the community's layout) or one folder below
        /// that (a zip unzipped with its wrapper folder), see FindRoot. The rest are not listed.</summary>
        public static List<Mod> Scan(string portDir)
        {
            var list = new List<Mod>();
            string dir = ModsDir(portDir);
            if (!Directory.Exists(dir)) return list;
            foreach (string full in Directory.GetDirectories(dir))
            {
                string folder = Path.GetFileName(full);
                if (folder.StartsWith(".")) continue;
                var chain = new List<string>();
                string root = FindRoot(full, chain);
                if (root == null) continue; // not a file mod (a port update, or a folder that is not a mod at all)

                var m = new Mod { Folder = folder, BasePath = full, GameRoot = root, IsFileMod = true };
                foreach (string at in chain) // the mod's own mod.ini, beside its files
                {
                    string ini = Path.Combine(at, "mod.ini");
                    if (File.Exists(ini)) { ReadIni(ini, m); break; }
                }
                m.Title = string.IsNullOrEmpty(m.Title) ? Pretty(folder) : m.Title;
                m.On = !File.Exists(Path.Combine(full, ".off"));
                list.Add(m);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Folder, b.Folder));
            ApplyLanguage(list);
            return list;
        }

        /// <summary>Where a mod's files start, from its folder in mods\ down: one folder at a time, and only while
        /// exactly one folder below qualifies (a zip unzipped with its own wrapper folder). A folder holding two
        /// candidate mods (a pack) qualifies at neither level. Null when nothing brings game files. The folders
        /// looked at are collected, so the caller can find the mod's mod.ini beside its files.</summary>
        private static string FindRoot(string basePath, List<string> chain)
        {
            string dir = basePath;
            for (int depth = 0; depth < 3; depth++)
            {
                chain.Add(dir);
                string root = RootHere(dir);
                if (root != null) return root;
                string only = null;
                int found = 0;
                foreach (string sub in SafeDirs(dir))
                    if (RootHere(sub) != null) { only = sub; found++; }
                if (found != 1) return null;
                dir = only;
            }
            return null;
        }

        /// <summary>The game files are in this folder (the folder itself, or its game\ subfolder): the root, else
        /// null.</summary>
        private static string RootHere(string dir)
        {
            string game = Path.Combine(dir, "game");
            if (Directory.Exists(game)) return game;
            foreach (string n in GameDirs)
                if (Directory.Exists(Path.Combine(dir, n))) return dir;
            foreach (string n in GameFiles)
                if (File.Exists(Path.Combine(dir, n))) return dir;
            return null;
        }

        private static IEnumerable<string> SafeDirs(string dir)
        {
            try { return Directory.GetDirectories(dir); }
            catch (Exception) { return new string[0]; }
        }

        private static void ReadIni(string ini, Mod m)
        {
            if (!File.Exists(ini)) return;
            foreach (string raw in File.ReadAllLines(ini))
            {
                int eq = raw.IndexOf('=');
                if (eq < 0) continue;
                string k = raw.Substring(0, eq).Trim().ToLowerInvariant();
                string v = raw.Substring(eq + 1).Trim();
                if (k == "priority") { int p; if (int.TryParse(v, out p)) m.Priority = p; }
                else if (k == "lang") m.Lang = v.ToLowerInvariant();
                else if (k == "name") m.Title = v;
            }
        }

        /// <summary>The language being played: the highest-priority enabled mod that declares one, else "en".</summary>
        public static void ApplyLanguage(List<Mod> mods)
        {
            string active = "en";
            foreach (var m in mods.OrderByDescending(x => x.Priority).ThenBy(x => x.Folder, StringComparer.Ordinal))
                if (m.On && !string.IsNullOrEmpty(m.Lang)) { active = m.Lang; break; }
            foreach (var m in mods) m.Blocked = !string.IsNullOrEmpty(m.Lang) && m.Lang != active;
        }

        public static string Pretty(string folder)
        {
            var chars = folder.Select(c => (c == '_' || c == '-' || c == '.') ? ' ' : char.ToUpperInvariant(c));
            return new string(chars.ToArray());
        }

        /// <summary>Turn a mod on/off; the state is the ".off" marker inside its own folder.</summary>
        public static void SetOn(Mod m, bool on)
        {
            string marker = Path.Combine(m.BasePath, ".off");
            try
            {
                if (on) { if (File.Exists(marker)) File.Delete(marker); }
                else { File.WriteAllText(marker, ""); }
            }
            catch (Exception) { }
            m.On = on;
        }
    }

    /// <summary>The HD pack's progress, as the port computes it (d3d/texture.cpp hd_progress).</summary>
    public static class HdPack
    {
        public static void Progress(string dir, out int matched, out int total)
        {
            matched = 0;
            total = 0;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            string map = Path.Combine(dir, "map.csv");
            if (File.Exists(map))
            {
                try
                {
                    foreach (string line in File.ReadLines(map))
                    {
                        if (line.Length == 0 || line[0] == '#') continue;
                        int comma = line.IndexOf(',');
                        if (comma <= 0) continue;
                        string hex = line.Substring(0, comma).Trim();
                        ulong v;
                        if (ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out v) && v != 0) matched++;
                    }
                }
                catch (Exception) { }
            }
            string prog = Path.Combine(dir, "progress.txt");
            if (File.Exists(prog))
            {
                try
                {
                    foreach (string line in File.ReadLines(prog))
                    {
                        if (line.Length == 0 || line[0] == '#') continue;
                        if (line.IndexOf("total", StringComparison.Ordinal) < 0) continue;
                        int eq = line.IndexOf('=');
                        if (eq >= 0) { int t; if (int.TryParse(line.Substring(eq + 1).Trim(), out t)) total = t; }
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
