// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace BlackModKit
{
    /// <summary>What a community package declares. It lives in mod.json and is ignored by the port.</summary>
    [DataContract]
    public sealed class ModManifest
    {
        [DataMember(Name = "format", Order = 0)]
        public int Format { get; set; } = 1;

        [DataMember(Name = "id", Order = 1)]
        public string Id { get; set; } = "";

        [DataMember(Name = "name", Order = 2)]
        public string Name { get; set; } = "";

        [DataMember(Name = "version", Order = 3)]
        public string Version { get; set; } = "1.0.0";

        [DataMember(Name = "author", Order = 4)]
        public string Author { get; set; } = "";

        [DataMember(Name = "description", Order = 5)]
        public string Description { get; set; } = "";

        [DataMember(Name = "lang", Order = 6)]
        public string Lang { get; set; } = "";

        [DataMember(Name = "priority", Order = 7)]
        public int Priority { get; set; }

        [DataMember(Name = "requires", Order = 8)]
        public List<string> Requires { get; set; } = new List<string>();

        [DataMember(Name = "conflicts", Order = 9)]
        public List<string> Conflicts { get; set; } = new List<string>();

        [DataMember(Name = "files", Order = 10)]
        public List<ModFile> Files { get; set; } = new List<ModFile>();
    }

    [DataContract]
    public sealed class ModFile
    {
        [DataMember(Name = "path", Order = 0)]
        public string Path { get; set; } = "";

        [DataMember(Name = "size", Order = 1)]
        public long Size { get; set; }

        [DataMember(Name = "sha256", Order = 2)]
        public string Sha256 { get; set; } = "";
    }

    /// <summary>A folder under mods\ as the port sees it, plus the kit's metadata.</summary>
    public sealed class ModInfo
    {
        public string Folder = "";
        public string BasePath = "";
        public string Root = "";
        public string IniPath = "";
        public string ManifestPath = "";
        public string Title = "";
        public string Id = "";
        public string Version = "1.0.0";
        public string Author = "";
        public string Description = "";
        public string Lang = "";
        public int Priority;
        public bool On = true;
        public bool Blocked;
        public ModManifest Manifest;
        public List<string> Files = new List<string>();
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();

        public string StateText
        {
            get
            {
                if (Blocked) return "OFF (IDIOMA)";
                return On ? "ON" : "OFF";
            }
        }

        public string PriorityText => Priority.ToString();
        public string FileCountText => Files.Count.ToString();
    }

    public sealed class ModConflict
    {
        public string Path = "";
        public List<ModInfo> Mods = new List<ModInfo>();
        public ModInfo Winner;
        public bool SameContent;
    }

    public sealed class ValidationResult
    {
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public List<string> Info = new List<string>();

        public bool Ok => Errors.Count == 0;

        public string Text
        {
            get
            {
                var lines = new List<string>();
                foreach (string line in Errors) lines.Add("[ERRO] " + line);
                foreach (string line in Warnings) lines.Add("[AVISO] " + line);
                foreach (string line in Info) lines.Add("[INFO] " + line);
                if (lines.Count == 0) lines.Add("Nada encontrado.");
                return string.Join(Environment.NewLine, lines);
            }
        }
    }

    public sealed class ImportResult
    {
        public bool Ok;
        public string Error = "";
        public string Target = "";
        public ModInfo Mod;
        public ModManifest Manifest;
        public List<string> Warnings = new List<string>();
        public List<ModConflict> Conflicts = new List<ModConflict>();
    }

    public sealed class PackageInfo
    {
        public string Path = "";
        public ModManifest Manifest;
        public string Error = "";
        public bool Ok => string.IsNullOrEmpty(Error) && Manifest != null;
    }

    /// <summary>Thrown for a package that breaks the format; the message is shown to the user.</summary>
    public sealed class PackageException : Exception
    {
        public PackageException(string message) : base(message) { }
    }
}
