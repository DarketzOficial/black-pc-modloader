using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BlackLauncher
{
    /// <summary>Everything shown in a crash report: the log, the versions, the PC and the game context.</summary>
    public sealed class CrashInfo
    {
        public string PortDir;
        public string LogPath;
        public string GameLogPath;
        public string Menu;
        public string Kind;              // "Crash", "Start failure", ...
        public int? ExitCode;
        public DateTime StartedLocal;
        public DateTime EndedLocal;
        public string Error;
        public SystemInfo System;

        public string LogName
        {
            get
            {
                try { return string.IsNullOrEmpty(LogPath) ? "" : Path.GetFileName(LogPath); }
                catch { return ""; }
            }
        }

        public long LogSize
        {
            get
            {
                try { return string.IsNullOrEmpty(LogPath) || !File.Exists(LogPath) ? 0 : new FileInfo(LogPath).Length; }
                catch { return 0; }
            }
        }
    }

    /// <summary>
    /// PC and game information collected for the report. WMI is used for the hardware; every query is
    /// optional, so a machine with a broken WMI still sends the log and the rest of the fields.
    /// </summary>
    public sealed class SystemInfo
    {
        public string Os = "Windows";
        public string Cpu = "desconhecido";
        public string Gpu = "desconhecido";
        public string Ram = "desconhecida";
        public string Screen = "desconhecida";
        public string User = "";
        public string Machine = "";
        public string DotNet = "";
        public string LauncherVersion = "";
        public string PortVersion = "desconhecida";
        public string GameVersion = "BLACK";
        public string Config = "";
        public string Mods = "nenhum";

        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);

        public static SystemInfo Collect(string portDir, string logPath)
        {
            var info = new SystemInfo();
            info.LauncherVersion = Loc.Version;
            info.User = Safe(() => Environment.UserName);
            info.Machine = Safe(() => Environment.MachineName);
            info.DotNet = Safe(() => typeof(object).Assembly.GetName().Version.ToString());
            info.Screen = ScreenSize();
            info.Os = OperatingSystem();
            info.Cpu = CpuInfo();
            info.Gpu = GpuInfo();
            info.Ram = RamInfo();
            info.Config = ConfigInfo(portDir);
            info.Mods = ModInfo(portDir);

            string log = ReadText(logPath, 512 * 1024);
            if (!string.IsNullOrEmpty(log))
            {
                var m = Regex.Match(log, @"BLACK loader\s+([0-9][0-9A-Za-z.\-]*)");
                if (m.Success) info.PortVersion = m.Groups[1].Value;
                var x = Regex.Match(log, @"xbe:\s*'([^']+)'\s+title id\s+([0-9A-Fa-f]+)");
                if (x.Success) info.GameVersion = x.Groups[1].Value + " (" + x.Groups[2].Value + ")";
            }
            if (info.PortVersion == "desconhecida")
                info.PortVersion = FileVersion(Path.Combine(portDir, "blackrt.dll")) ?? "desconhecida";
            return info;
        }

        public static string LastMenu(string logPath)
        {
            string log = ReadText(logPath, 512 * 1024);
            if (string.IsNullOrEmpty(log)) return "";
            var matches = Regex.Matches(log, @"fe:\s*the pointer is on the page\s+([A-Za-z0-9_]+)");
            return matches.Count == 0 ? "" : matches[matches.Count - 1].Groups[1].Value;
        }

        private static string OperatingSystem()
        {
            string caption = WmiValue("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem",
                                      "Caption");
            string version = WmiValue("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem",
                                      "Version");
            string build = WmiValue("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem",
                                    "BuildNumber");
            string arch = WmiValue("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem",
                                   "OSArchitecture");
            if (string.IsNullOrEmpty(caption))
                return Safe(() => Environment.OSVersion.VersionString) +
                       (Environment.Is64BitOperatingSystem ? " (64-bit)" : " (32-bit)");
            return (caption + " " + version + " build " + build + " " + arch).Trim();
        }

        private static string CpuInfo()
        {
            string name = WmiValue("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor", "Name");
            if (string.IsNullOrEmpty(name)) name = Safe(() => Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));
            string cores = WmiValue("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
                                    "NumberOfCores");
            string threads = WmiValue("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
                                      "NumberOfLogicalProcessors");
            if (!string.IsNullOrEmpty(cores) || !string.IsNullOrEmpty(threads))
                name += " (" + cores + "C/" + threads + "T)";
            return string.IsNullOrEmpty(name) ? "desconhecido" : name;
        }

        private static string GpuInfo()
        {
            var names = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DriverVersion FROM Win32_VideoController"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject o in results)
                    {
                        string name = Convert.ToString(o["Name"]);
                        string driver = Convert.ToString(o["DriverVersion"]);
                        if (string.IsNullOrEmpty(name)) continue;
                        names.Add(driver == null ? name : name + " (driver " + driver + ")");
                    }
                }
            }
            catch { }
            return names.Count == 0 ? "desconhecida" : string.Join(" / ", names.Distinct().ToArray());
        }

        private static string RamInfo()
        {
            string bytes = WmiValue("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");
            double value;
            if (double.TryParse(bytes, out value) && value > 0)
                return Math.Round(value / 1073741824.0, 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " GB";
            return "desconhecida";
        }

        private static string ScreenSize()
        {
            try
            {
                int w = GetSystemMetrics(0);
                int h = GetSystemMetrics(1);
                if (w > 0 && h > 0) return w + "x" + h;
            }
            catch { }
            try
            {
                return (int)SystemParameters.PrimaryScreenWidth + "x" + (int)SystemParameters.PrimaryScreenHeight;
            }
            catch { return "desconhecida"; }
        }

        private static string ConfigInfo(string portDir)
        {
            try
            {
                var cfg = BlackLauncher.Config.Load(portDir);
                return "FOV " + cfg.Get("fov") + " | escala " + cfg.Get("scale") +
                       " | VSync " + cfg.Get("vsync") + " | FPS " + cfg.Get("fps_cap") +
                       " | HD " + (cfg.GetHd() ? "ON" : "OFF") +
                       " | HUD169 " + cfg.Get("hud169") + " | effectsnative " + cfg.Get("effectsnative");
            }
            catch { return ""; }
        }

        private static string ModInfo(string portDir)
        {
            try
            {
                var on = BlackLauncher.Mods.Scan(portDir).Where(m => m.On && !m.Blocked).Select(m => m.Title).ToArray();
                return on.Length == 0 ? "nenhum" : string.Join(", ", on);
            }
            catch { return "desconhecido"; }
        }

        private static string WmiValue(string query, string property)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject o in results)
                        return Convert.ToString(o[property]);
                }
            }
            catch { }
            return "";
        }

        public static string FileVersion(string path)
        {
            try
            {
                var v = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrWhiteSpace(v.FileVersion)) return v.FileVersion;
                if (!string.IsNullOrWhiteSpace(v.ProductVersion)) return v.ProductVersion;
            }
            catch { }
            return null;
        }

        private static string Safe(Func<string> read)
        {
            try { return read() ?? ""; } catch { return ""; }
        }

        public static string ReadText(string path, int maxBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "";
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0, fs.Length - maxBytes);
                    fs.Seek(start, SeekOrigin.Begin);
                    var buffer = new byte[fs.Length - start];
                    int read = 0;
                    while (read < buffer.Length)
                    {
                        int n = fs.Read(buffer, read, buffer.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    string text = new UTF8Encoding(false, false).GetString(buffer, 0, read);
                    return text.Replace("\0", "");
                }
            }
            catch { return ""; }
        }
    }

    /// <summary>
    /// Sends the report to a Discord webhook with the log attached. The webhook is a hidden preference
    /// (crash_webhook) so it can be changed without a rebuild; the default points at #logs-black.
    /// No request is made until the player presses ENVIAR.
    /// </summary>
    public static class DiscordReporter
    {
        public const string DefaultWebhook =
            "https://discord.com/api/webhooks/1562155749482831902/gZy5zJFcFLOvb9AShKQtM7LJtIfFhycj5Am-FYV6bwIgUcpaNGiXl5y6JFiraR-QZmEk";

        private const long MaxAttachment = 7_500_000;
        private static readonly HttpClient Http = CreateClient();

        public static string WebhookFor(Config cfg)
        {
            try
            {
                string value = cfg == null ? null : cfg.GetPref("crash_webhook", DefaultWebhook);
                return string.IsNullOrWhiteSpace(value) ? DefaultWebhook : value.Trim();
            }
            catch { return DefaultWebhook; }
        }

        public static async Task<SendResult> SendAsync(CrashInfo info, Config cfg, CancellationToken token)
        {
            if (info == null || string.IsNullOrEmpty(info.LogPath) || !File.Exists(info.LogPath))
                return new SendResult { Error = "O log do jogo nao foi encontrado." };

            string payload = BuildPayload(info);
            string fileName = SafeName(info.LogName);
            Stream stream = null;
            string temporary = null;
            bool truncated = false;
            try
            {
                var file = new FileInfo(info.LogPath);
                if (file.Length <= MaxAttachment)
                {
                    stream = new FileStream(info.LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }
                else
                {
                    truncated = true;
                    temporary = Path.Combine(Path.GetTempPath(), "black-report-" + Guid.NewGuid().ToString("N") + ".log");
                    using (var source = new FileStream(info.LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var target = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        source.Seek(Math.Max(0, source.Length - MaxAttachment), SeekOrigin.Begin);
                        source.CopyTo(target);
                    }
                    stream = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

                using (var form = new MultipartFormDataContent())
                using (stream)
                {
                    form.Add(new StringContent(payload, Encoding.UTF8, "application/json"), "payload_json");
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    form.Add(fileContent, "files[0]", fileName);

                    string url = WebhookFor(cfg) + (WebhookFor(cfg).Contains("?") ? "&" : "?") + "wait=true";
                    using (var response = await Http.PostAsync(url, form, token).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            string shortBody = body.Length > 500 ? body.Substring(0, 500) : body;
                            var failed = new SendResult
                            {
                                Error = "Discord respondeu " + (int)response.StatusCode + " " + response.ReasonPhrase + ". " + shortBody
                            };
                            LogStore.AppendReport(info.PortDir, "falhou  " + fileName + "  HTTP " + (int)response.StatusCode);
                            return failed;
                        }
                        var result = new SendResult { Ok = true, MessageId = MessageId(body), Truncated = truncated };
                        LogStore.AppendReport(info.PortDir, "enviado  " + fileName + "  " + FormatSize(info.LogSize) +
                            (truncated ? " (anexo truncado)" : "") + (string.IsNullOrEmpty(result.MessageId) ? "" : "  msg " + result.MessageId));
                        return result;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new SendResult { Error = "Envio cancelado." };
            }
            catch (Exception ex)
            {
                LogStore.AppendReport(info.PortDir, "erro    " + fileName + "  " + ex.Message);
                return new SendResult { Error = ex.Message };
            }
            finally
            {
                if (stream != null) stream.Dispose();
                if (temporary != null)
                {
                    try { File.Delete(temporary); } catch { }
                }
            }
        }

        private static string BuildPayload(CrashInfo info)
        {
            var s = info.System ?? new SystemInfo();
            string reason = info.Kind == "Start failure"
                ? "O jogo nao conseguiu iniciar."
                : (info.ExitCode.HasValue
                    ? "O jogo terminou de forma inesperada (codigo " + info.ExitCode.Value + ")."
                    : "O jogo terminou de forma inesperada.");
            if (!string.IsNullOrEmpty(info.Error)) reason += " " + info.Error;

            string tail = Tail(info.LogPath, 1500);
            string description = reason;
            if (!string.IsNullOrEmpty(tail))
                description += "\n\n**Ultimo trecho do log**\n" + tail;
            description = Limit(description, 3900);

            var fields = new List<KeyValuePair<string, string>>
            {
                Field("ModLoader", s.LauncherVersion + " (launcher)"),
                Field("Port", s.PortVersion),
                Field("Jogo", s.GameVersion),
                Field("Windows", s.Os),
                Field("CPU", s.Cpu),
                Field("GPU", s.Gpu),
                Field("RAM", s.Ram),
                Field("Tela", s.Screen),
                Field("Jogador", (string.IsNullOrEmpty(s.User) ? "?" : s.User) + "  (Windows)\n" +
                                  (string.IsNullOrEmpty(s.Machine) ? "" : s.Machine)),
                Field("Sessao", "Menu: " + (string.IsNullOrEmpty(info.Menu) ? "?" : info.Menu) +
                                "\nInicio: " + info.StartedLocal.ToString("yyyy-MM-dd HH:mm:ss") +
                                "\nFim: " + info.EndedLocal.ToString("yyyy-MM-dd HH:mm:ss") +
                                "\nConfig: " + s.Config),
                Field("Mods", s.Mods),
                Field("Log", info.LogName + "  (" + FormatSize(info.LogSize) + ")")
            };

            var sb = new StringBuilder();
            sb.Append("{\"username\":\"BLACK PC - Relatorio de erro\",\"allowed_mentions\":{\"parse\":[]},");
            sb.Append("\"embeds\":[{");
            sb.Append("\"title\":").Append(Json(info.Kind == "Start failure" ? "Falha ao iniciar o BLACK PC" : "Crash no BLACK PC"));
            sb.Append(",\"description\":").Append(Json(description));
            sb.Append(",\"color\":12597547,\"timestamp\":").Append(Json(DateTime.UtcNow.ToString("o")));
            sb.Append(",\"footer\":{\"text\":").Append(Json("BLACK PC - ModLoader " + Loc.Version)).Append("}");
            sb.Append(",\"fields\":[");
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"name\":").Append(Json(Limit(fields[i].Key, 250)));
                sb.Append(",\"value\":").Append(Json(Limit(fields[i].Value ?? "", 1000)));
                sb.Append(",\"inline\":").Append(i < 3 || i == 6 || i == 7 ? "true" : "false").Append("}");
            }
            sb.Append("]}]");
            sb.Append(",\"attachments\":[{\"id\":0,\"filename\":").Append(Json(SafeName(info.LogName)));
            sb.Append(",\"description\":").Append(Json("black.log / black-crash log")).Append("}]}");
            return sb.ToString();
        }

        private static KeyValuePair<string, string> Field(string name, string value)
        {
            return new KeyValuePair<string, string>(name, value ?? "");
        }

        private static string Tail(string path, int maxChars)
        {
            string text = SystemInfo.ReadText(path, 256 * 1024);
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var tail = new StringBuilder();
            for (int i = Math.Max(0, lines.Length - 18); i < lines.Length; i++)
            {
                if (tail.Length + lines[i].Length + 1 > maxChars) break;
                if (tail.Length > 0) tail.Append('\n');
                tail.Append(lines[i].TrimEnd());
            }
            string result = tail.ToString();
            if (result.Length > maxChars) result = result.Substring(result.Length - maxChars);
            return result;
        }

        private static string MessageId(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            var m = Regex.Match(body, "\"id\"\\s*:\\s*\"([0-9]+)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string SafeName(string name)
        {
            name = string.IsNullOrEmpty(name) ? "black.log" : Path.GetFileName(name);
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrEmpty(name) ? "black.log" : name;
        }

        private static string Limit(string value, int max)
        {
            value = value ?? "";
            if (value.Length <= max) return value;
            return value.Substring(0, Math.Max(0, max - 1)) + "\u2026";
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }

        private static string Json(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            try { client.DefaultRequestHeaders.UserAgent.ParseAdd("BlackPC-ModLoader/" + Loc.Version); } catch { }
            return client;
        }
    }

    public sealed class SendResult
    {
        public bool Ok;
        public string Error;
        public string MessageId;
        public bool Truncated;
    }
}
