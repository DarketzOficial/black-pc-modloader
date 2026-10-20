using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackLauncher
{
#pragma warning disable 0649 // filled by the JSON serializer, not by the C# code
    // Only the fields the launcher reads; GitHub's release JSON is much larger than this.
    [DataContract]
    internal sealed class GhRelease
    {
        [DataMember(Name = "tag_name")] public string TagName;
        [DataMember(Name = "body")] public string Body;
        [DataMember(Name = "assets")] public GhAsset[] Assets;
    }

    [DataContract]
    internal sealed class GhAsset
    {
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "browser_download_url")] public string Url;
        [DataMember(Name = "size")] public long Size;
    }
#pragma warning restore 0649

    /// <summary>One release read from the project's GitHub, with the asset the launcher would install.</summary>
    public sealed class UpdateRelease
    {
        public string Version;      // "1.4.9" (the tag without the leading v)
        public string Tag;          // "v1.4.9"
        public string Notes;        // the release body, shown in the update window
        public string AssetName;    // the .zip attached to the release
        public string AssetUrl;
        public long AssetSize;
    }

    /// <summary>
    /// The launcher's updater: reads the newest release from GitHub, downloads its zip to a folder inside the
    /// port, extracts it, and hands the last step to a small PowerShell helper that waits for the launcher to
    /// close, copies the files, starts the launcher again and removes the temporary folder.
    ///
    /// The zip is expected to carry the files to overlay on the port at its root (blacklauncher.exe,
    /// blackrt.dll, LEIA-ME.txt). The helper never touches black.ini, launcher.ini, the saves, game\ or mods\.
    /// </summary>
    public static class Updater
    {
        public const string Repo = "DarketzOficial/black-pc-modloader";
        public static string DefaultApi => "https://api.github.com/repos/" + Repo + "/releases/latest";

        /// <summary>This launcher's own version (the updater compares the release tag against it).</summary>
        public static Version Current => typeof(Updater).Assembly.GetName().Version;
        public static string CurrentText => VersionText(Current);

        public static string VersionText(Version v) => v.Major + "." + v.Minor + (v.Build > 0 ? "." + v.Build : "");

        // ------------------------------------------------------------------ check ---
        /// <summary>The newest published release, or null when the repository has no releases yet (GitHub answers
        /// 404). Network and rate-limit errors are thrown and shown by the caller.</summary>
        public static async Task<UpdateRelease> CheckAsync(string apiUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiUrl)) apiUrl = DefaultApi;
            using (var http = NewHttp(TimeSpan.FromSeconds(30)))
            using (var resp = await http.GetAsync(apiUrl, ct).ConfigureAwait(false))
            {
                if (resp.StatusCode == HttpStatusCode.NotFound) return null;
                if (resp.StatusCode == (HttpStatusCode)403)
                    throw new InvalidOperationException(Loc.T(
                        "o GitHub recusou a consulta (limite de verificações). Tente de novo mais tarde.",
                        "GitHub refused the request (rate limit). Try again later."));
                resp.EnsureSuccessStatusCode();

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                GhRelease root;
                try
                {
                    var ser = new DataContractJsonSerializer(typeof(GhRelease));
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                        root = (GhRelease)ser.ReadObject(ms);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("invalid release JSON: " + ex.Message);
                }
                if (root == null) throw new InvalidDataException("invalid release JSON");

                string tag = root.TagName ?? "";
                Version version;
                string text;
                if (!TryVersion(tag, out text, out version))
                {
                    // The repository's older releases (the full 1.0 package, tag "modloader-1.0") are not
                    // launcher updates. Treat anything without a version tag as "no launcher update yet",
                    // instead of turning the start-up check into an error.
                    return null;
                }

                var rel = new UpdateRelease { Tag = tag, Version = text, Notes = root.Body ?? "" };
                GhAsset chosen = null;
                if (root.Assets != null)
                {
                    foreach (var a in root.Assets)
                    {
                        if (a == null || string.IsNullOrEmpty(a.Name) ||
                            !a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                        // A release can carry the small launcher update and the full first-install package.
                        // Prefer the update zip by name; the full package is only the fallback.
                        if (chosen == null || AssetRank(a) > AssetRank(chosen))
                            chosen = a;
                    }
                }
                if (chosen == null)
                    throw new InvalidDataException(Loc.T(
                        "a release não tem um pacote .zip para instalar.",
                        "the release has no .zip package to install."));
                rel.AssetName = chosen.Name;
                rel.AssetUrl = chosen.Url ?? "";
                rel.AssetSize = chosen.Size;
                if (string.IsNullOrEmpty(rel.AssetUrl))
                    throw new InvalidDataException(Loc.T("o pacote não tem endereço de download.", "the package has no download URL."));

                return rel;
            }
        }

        /// <summary>True when the release tag is newer than this launcher. Accepts "1.4.9", "v1.4.9" and
        /// suffixes such as "v1.4.9-beta" (the numeric part is what counts).</summary>
        public static bool IsNewer(string versionText)
        {
            Version remote;
            string normalized;
            if (!TryVersion(versionText, out normalized, out remote)) return false;
            return remote > new Version(Current.Major, Current.Minor, Math.Max(0, Current.Build));
        }

        public static bool TryVersion(string tag, out string text, out Version version)
        {
            text = (tag ?? "").Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text.Substring(1);
            var m = System.Text.RegularExpressions.Regex.Match(text, @"^(\d+)\.(\d+)(?:\.(\d+))?");
            if (!m.Success)
            {
                version = null;
                return false;
            }
            int a = int.Parse(m.Groups[1].Value);
            int b = int.Parse(m.Groups[2].Value);
            int c = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
            version = new Version(a, b, c);
            text = a + "." + b + (c > 0 ? "." + c : "");
            return true;
        }

        // --------------------------------------------------------------- download ---
        public static async Task DownloadAsync(UpdateRelease rel, string zipPath, IProgress<double> progress,
                                               CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(zipPath));
            using (var http = NewHttp(TimeSpan.FromMinutes(15)))
            using (var resp = await http.GetAsync(rel.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? rel.AssetSize;
                using (var input = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    long done = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                        done += read;
                        if (total > 0) progress?.Report(Math.Min(1.0, (double)done / total));
                    }
                }
            }
            progress?.Report(1.0);
        }

        /// <summary>
        /// The update zip is named black-pc-modloader.zip. The full package (BLACK-PC-Modloader-1.4.10.zip)
        /// is also a zip, but the updater must not pick it when both are attached to the release.
        /// </summary>
        private static int AssetRank(GhAsset a)
        {
            string name = a == null || a.Name == null ? "" : a.Name;
            if (name.Equals("black-pc-modloader.zip", StringComparison.OrdinalIgnoreCase)) return 100;
            if (name.IndexOf("BLACK-PC-Modloader", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
            if (name.IndexOf("modloader", StringComparison.OrdinalIgnoreCase) >= 0) return 50;
            return 1;
        }

        /// <summary>Extract the package, refusing entries that would escape the destination (zip-slip).</summary>
        public static void ExtractSafe(string zipPath, string destDir)
        {
            string root = Path.GetFullPath(destDir);
            Directory.CreateDirectory(root);
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;   // directory entry
                    string rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    string target = Path.GetFullPath(Path.Combine(root, rel));
                    if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("invalid path in the update package: " + entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }
        }

        /// <summary>The folder inside the extraction that holds blacklauncher.exe (the zip may wrap its files
        /// in one top-level folder). Null when the package is not a launcher update.</summary>
        public static string FindLauncherRoot(string filesDir)
        {
            if (File.Exists(Path.Combine(filesDir, "blacklauncher.exe"))) return filesDir;
            foreach (var file in Directory.GetFiles(filesDir, "blacklauncher.exe", SearchOption.AllDirectories))
            {
                string rel = file.Substring(filesDir.Length).TrimStart(Path.DirectorySeparatorChar);
                if (rel.Split(Path.DirectorySeparatorChar).Length <= 3) return Path.GetDirectoryName(file);
            }
            return null;
        }

        // ---------------------------------------------------------------- install ---
        /// <summary>Start the PowerShell helper that replaces the files after this process exits. The caller
        /// shuts the launcher down; the helper waits for its PID, copies, restarts and cleans up.</summary>
        public static void StartInstall(string portDir, string filesDir)
        {
            string target = Path.GetFullPath(portDir);
            string source = Path.GetFullPath(filesDir);
            string exe = Path.Combine(target, "blacklauncher.exe");
            if (!File.Exists(exe)) exe = Process.GetCurrentProcess().MainModule.FileName;
            string tempDir = Path.GetFullPath(Path.Combine(target, "update"));
            string script = Path.Combine(Path.GetTempPath(), "black-pc-update-" + Process.GetCurrentProcess().Id + ".ps1");
            File.WriteAllText(script, HelperScript, new UTF8Encoding(false));

            string args = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\"" +
                          " -LauncherPid " + Process.GetCurrentProcess().Id +
                          " -Source \"" + source + "\"" +
                          " -Target \"" + target + "\"" +
                          " -Exe \"" + exe + "\"" +
                          " -TempDir \"" + tempDir + "\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetTempPath()
            });
        }

        /// <summary>Remove a leftover update folder at startup (a failed/cancelled install). Best effort.</summary>
        public static void CleanTemp(string portDir)
        {
            try
            {
                string dir = Path.Combine(portDir, "update");
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            catch { }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "?";
            if (bytes >= 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("0") + " KB";
            return bytes + " B";
        }

        private static HttpClient NewHttp(TimeSpan timeout)
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BLACK-PC-Launcher/" + CurrentText);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return http;
        }

        // The helper lives in %TEMP%, not in the folder it deletes. It waits for the launcher to exit (the exe
        // is locked while it runs), copies everything the package brought except the player's own files, starts
        // the new launcher and removes <port>\update. Any failure is written to %TEMP%\black-pc-update.log and
        // the launcher is started anyway, so a bad package cannot leave the player without a launcher.
        private const string HelperScript = @"
param(
    [int]$LauncherPid,
    [string]$Source,
    [string]$Target,
    [string]$Exe,
    [string]$TempDir
)
$ErrorActionPreference = 'Continue'
$log = Join-Path $env:TEMP 'black-pc-update.log'
try { Wait-Process -Id $LauncherPid -Timeout 90 -ErrorAction SilentlyContinue } catch { }
Start-Sleep -Milliseconds 500
$skip = @('black.ini','launcher.ini','black.log','savedata','game','mods','update')
$failed = $null
try {
    Get-ChildItem -LiteralPath $Source -Force | Where-Object { $skip -notcontains $_.Name } | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Target -Recurse -Force -ErrorAction Stop
    }
    ""ok: $((Get-Date).ToString('o'))"" | Out-File -LiteralPath $log -Encoding utf8
} catch {
    $failed = $_.Exception.Message
    ""error: $failed"" | Out-File -LiteralPath $log -Encoding utf8
}
try {
    Start-Process -FilePath $Exe -WorkingDirectory $Target
} catch {
    ""start error: $($_.Exception.Message)"" | Out-File -LiteralPath $log -Encoding utf8 -Append
}
Start-Sleep -Seconds 2
if (-not $failed) {
    try { Remove-Item -LiteralPath $TempDir -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}
try { Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue } catch { }
";
    }
}
