// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BlackModKit
{
    /// <summary>
    /// The mod rules the port uses (a folder under mods\, a game\ folder, priority and language),
    /// plus the kit's package format. The port never reads mod.json; mod.ini remains the runtime file.
    /// </summary>
    public sealed class ModManager
    {
        private static readonly string[] GameDirs = { "chars", "data", "export", "language", "levels", "sound", "videos" };
        private static readonly string[] GameFiles = { "default.xbe", "GlobData.bin" };
        private static readonly string[] PlaceholderPrefixes = { "coloque-", "leia-me", "readme", "read-me" };

        public string PortDir { get; private set; }
        public string ModsDir => Path.Combine(PortDir, "mods");

        public ModManager(string portDir)
        {
            PortDir = Path.GetFullPath(portDir);
        }

        public static bool LooksLikePort(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return false;
            return File.Exists(Path.Combine(dir, "black.exe")) &&
                   File.Exists(Path.Combine(dir, "game", "default.xbe"));
        }

        // ------------------------------------------------------------------ discovery ---

        public List<ModInfo> Scan()
        {
            var mods = new List<ModInfo>();
            if (!Directory.Exists(ModsDir)) return mods;
            string[] directories;
            try { directories = Directory.GetDirectories(ModsDir); }
            catch { return mods; }

            foreach (string dir in directories)
            {
                try
                {
                    ModInfo mod = Load(dir);
                    if (mod != null) mods.Add(mod);
                }
                catch (Exception)
                {
                    // A broken folder must not stop the rest of the list; Load records its own errors.
                }
            }
            mods.Sort((a, b) => string.CompareOrdinal(a.Folder, b.Folder));
            ApplyLanguage(mods);
            return mods;
        }

        private ModInfo Load(string basePath)
        {
            string folder = Path.GetFileName(basePath);
            if (folder.StartsWith(".", StringComparison.Ordinal)) return null;

            List<string> chain;
            string root = FindRoot(basePath, out chain);
            if (root == null) return null;

            var mod = new ModInfo
            {
                Folder = folder,
                BasePath = basePath,
                Root = root,
                Title = Pretty(folder),
                Id = folder,
                Priority = 0,
                On = !File.Exists(Path.Combine(basePath, ".off"))
            };

            // mod.ini: the port reads the first one found while walking the wrapper folder down.
            foreach (string at in chain)
            {
                string ini = Path.Combine(at, "mod.ini");
                if (!File.Exists(ini)) continue;
                mod.IniPath = ini;
                ReadIni(ini, mod);
                break;
            }

            string manifest = Path.Combine(basePath, "mod.json");
            if (File.Exists(manifest))
            {
                mod.ManifestPath = manifest;
                try
                {
                    mod.Manifest = Json.ReadFile(manifest);
                    if (mod.Manifest != null)
                    {
                        // mod.ini is authoritative for what the port does; the manifest fills the kit fields.
                        if (string.IsNullOrEmpty(mod.Title) || mod.IniPath.Length == 0) mod.Title = mod.Manifest.Name;
                        if (mod.IniPath.Length == 0) mod.Priority = mod.Manifest.Priority;
                        if (mod.IniPath.Length == 0) mod.Lang = mod.Manifest.Lang ?? "";
                        mod.Id = string.IsNullOrWhiteSpace(mod.Manifest.Id) ? folder : mod.Manifest.Id;
                        mod.Version = string.IsNullOrWhiteSpace(mod.Manifest.Version) ? "1.0.0" : mod.Manifest.Version;
                        mod.Author = mod.Manifest.Author ?? "";
                        mod.Description = mod.Manifest.Description ?? "";
                    }
                }
                catch (Exception ex)
                {
                    mod.Errors.Add("mod.json inválido: " + ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(mod.Title)) mod.Title = Pretty(folder);
            mod.Files = EnumerateGameFiles(root);
            return mod;
        }

        private static string FindRoot(string basePath, out List<string> chain)
        {
            chain = new List<string>();
            string current = basePath;
            for (int depth = 0; depth < 3; depth++)
            {
                chain.Add(current);
                string here = RootHere(current);
                if (here != null) return here;

                var candidates = new List<string>();
                try
                {
                    foreach (string sub in Directory.GetDirectories(current))
                        if (RootHere(sub) != null) candidates.Add(sub);
                }
                catch { return null; }

                if (candidates.Count != 1) return null;
                current = candidates[0];
            }
            return null;
        }

        private static string RootHere(string dir)
        {
            string game = Path.Combine(dir, "game");
            if (Directory.Exists(game)) return game;
            foreach (string name in GameDirs)
                if (Directory.Exists(Path.Combine(dir, name))) return dir;
            foreach (string name in GameFiles)
                if (File.Exists(Path.Combine(dir, name))) return dir;
            return null;
        }

        private static List<string> EnumerateGameFiles(string root)
        {
            var result = new List<string>();
            try
            {
                foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                    if (relative.Length > 0) result.Add(relative);
                }
            }
            catch { }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static void ReadIni(string path, ModInfo mod)
        {
            try
            {
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int equal = line.IndexOf('=');
                    if (equal < 0) continue;
                    string key = line.Substring(0, equal).Trim().ToLowerInvariant();
                    string value = line.Substring(equal + 1).Trim();
                    if (key == "name") mod.Title = value;
                    else if (key == "lang") mod.Lang = value.ToLowerInvariant();
                    else if (key == "priority")
                    {
                        int priority;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out priority))
                            mod.Priority = priority;
                        else
                            mod.Errors.Add("mod.ini: priority não é um número inteiro: " + value);
                    }
                }
            }
            catch (Exception ex)
            {
                mod.Errors.Add("não foi possível ler mod.ini: " + ex.Message);
            }
        }

        public static void ApplyLanguage(List<ModInfo> mods)
        {
            string active = "en";
            foreach (ModInfo mod in mods
                .Where(m => m.On)
                .OrderByDescending(m => m.Priority)
                .ThenBy(m => m.Folder, StringComparer.Ordinal))
            {
                if (!string.IsNullOrEmpty(mod.Lang))
                {
                    active = mod.Lang;
                    break;
                }
            }
            foreach (ModInfo mod in mods)
                mod.Blocked = !string.IsNullOrEmpty(mod.Lang) && mod.Lang != active;
        }

        // ------------------------------------------------------------------ conflicts ---

        public List<ModConflict> Conflicts(List<ModInfo> mods)
        {
            var byPath = new Dictionary<string, List<ModInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (ModInfo mod in mods)
            {
                if (!mod.On || mod.Blocked) continue;
                foreach (string file in mod.Files)
                {
                    List<ModInfo> list;
                    if (!byPath.TryGetValue(file, out list))
                    {
                        list = new List<ModInfo>();
                        byPath[file] = list;
                    }
                    list.Add(mod);
                }
            }

            var result = new List<ModConflict>();
            foreach (var pair in byPath.Where(p => p.Value.Count > 1).OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                var conflict = new ModConflict { Path = pair.Key };
                conflict.Mods = pair.Value
                    .OrderByDescending(m => m.Priority)
                    .ThenBy(m => m.Folder, StringComparer.Ordinal)
                    .ToList();
                conflict.Winner = conflict.Mods[0];

                string firstHash = null;
                bool same = true;
                foreach (ModInfo mod in conflict.Mods)
                {
                    string path = Path.Combine(mod.Root, pair.Key.Replace('/', Path.DirectorySeparatorChar));
                    string hash = HashFile(path);
                    if (firstHash == null) firstHash = hash;
                    else if (!string.Equals(firstHash, hash, StringComparison.OrdinalIgnoreCase)) same = false;
                }
                conflict.SameContent = same && firstHash != null;
                result.Add(conflict);
            }
            return result;
        }

        // ------------------------------------------------------------------ editing ---

        public ModInfo CreateMod(string name, string folder, int priority, string lang, bool startOff)
        {
            name = (name ?? "").Trim();
            folder = Slug(folder);
            if (name.Length == 0) throw new InvalidOperationException("Digite um nome para o mod.");
            if (folder.Length == 0 || folder.StartsWith(".", StringComparison.Ordinal))
                throw new InvalidOperationException("Digite uma pasta para o mod.");
            if (!Directory.Exists(ModsDir)) Directory.CreateDirectory(ModsDir);

            string basePath = Path.Combine(ModsDir, folder);
            if (Directory.Exists(basePath) || File.Exists(basePath))
                throw new InvalidOperationException("Já existe uma pasta chamada " + folder + " em mods.");

            Directory.CreateDirectory(Path.Combine(basePath, "game"));
            Directory.CreateDirectory(Path.Combine(basePath, "subtitles"));

            var manifest = new ModManifest
            {
                Id = folder,
                Name = name,
                Version = "1.0.0",
                Lang = lang ?? "",
                Priority = priority,
                Files = new List<ModFile>()
            };
            WriteIni(basePath, name, priority, lang);
            Json.WriteFile(Path.Combine(basePath, "mod.json"), manifest);
            if (startOff) File.WriteAllText(Path.Combine(basePath, ".off"), "");

            ApplyLanguage(Scan());
            return Scan().FirstOrDefault(m => string.Equals(m.Folder, folder, StringComparison.OrdinalIgnoreCase));
        }

        public ModInfo UpdateMod(ModInfo mod, string name, int priority, string lang, string version,
                                 string author, string description, bool on)
        {
            name = (name ?? "").Trim();
            version = (version ?? "").Trim();
            if (name.Length == 0) throw new InvalidOperationException("O nome não pode ficar vazio.");
            if (version.Length == 0) version = "1.0.0";
            if (lang == null) lang = "";
            lang = lang.Trim().ToLowerInvariant();

            WriteIni(mod.BasePath, name, priority, lang);
            string manifestPath = Path.Combine(mod.BasePath, "mod.json");
            ModManifest manifest = mod.Manifest ?? new ModManifest();
            manifest.Format = 1;
            manifest.Id = Slug(mod.Folder);
            manifest.Name = name;
            manifest.Version = version;
            manifest.Author = author ?? "";
            manifest.Description = description ?? "";
            manifest.Lang = lang;
            manifest.Priority = priority;
            manifest.Files = BuildManifestFiles(mod.BasePath);
            Json.WriteFile(manifestPath, manifest);
            SetOn(mod, on);

            ModInfo updated = Scan().FirstOrDefault(m => string.Equals(m.Folder, mod.Folder, StringComparison.OrdinalIgnoreCase));
            return updated ?? mod;
        }

        public void SetOn(ModInfo mod, bool on)
        {
            string marker = Path.Combine(mod.BasePath, ".off");
            try
            {
                if (on)
                {
                    if (File.Exists(marker)) File.Delete(marker);
                }
                else
                {
                    File.WriteAllText(marker, "");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Não foi possível mudar o estado do mod: " + ex.Message, ex);
            }
            mod.On = on;
            mod.Blocked = false;
        }

        public void AddFolder(ModInfo mod, string sourceFolder, bool overwrite)
        {
            if (!Directory.Exists(sourceFolder))
                throw new InvalidOperationException("A pasta escolhida não existe.");
            string source = Directory.Exists(Path.Combine(sourceFolder, "game"))
                ? Path.Combine(sourceFolder, "game")
                : sourceFolder;

            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart('\\', '/');
                if (relative.Length == 0) continue;
                if (relative.Equals("mod.ini", StringComparison.OrdinalIgnoreCase) ||
                    relative.Equals("mod.json", StringComparison.OrdinalIgnoreCase) ||
                    relative.Equals(".off", StringComparison.OrdinalIgnoreCase)) continue;

                string target = Path.Combine(mod.Root, relative);
                if (!IsUnder(mod.Root, target)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (File.Exists(target) && !overwrite)
                    throw new InvalidOperationException("O arquivo já existe no mod: " + relative);
                File.Copy(file, target, true);
            }
        }

        public ValidationResult Validate(ModInfo mod, List<ModInfo> all)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(mod.Errors);
            result.Warnings.AddRange(mod.Warnings);

            if (!Directory.Exists(mod.BasePath)) result.Errors.Add("A pasta do mod não existe.");
            if (mod.Files.Count == 0)
                result.Errors.Add("Nenhum arquivo de jogo encontrado (a pasta game\\ está vazia?).");
            if (mod.Blocked)
                result.Warnings.Add("Este mod está bloqueado porque o idioma ativo é outro. Veja o campo lang.");
            if (mod.IniPath.Length == 0)
                result.Warnings.Add("Sem mod.ini; o port usa o nome da pasta e a prioridade 0.");

            var placeholders = new List<string>();
            try
            {
                foreach (string file in Directory.EnumerateFiles(mod.BasePath, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(mod.BasePath.Length).TrimStart('\\', '/').Replace('\\', '/');
                    string leaf = Path.GetFileName(file);
                    if (IsPlaceholder(leaf)) placeholders.Add(relative);
                }
            }
            catch { }
            if (placeholders.Count > 0)
                result.Warnings.Add("Há arquivos de aviso/modelo no mod: " + string.Join(", ", placeholders.Take(6)));

            if (mod.Manifest != null)
            {
                ModManifest manifest = mod.Manifest;
                if (manifest.Format != 1) result.Errors.Add("mod.json: format diferente de 1.");
                if (string.IsNullOrWhiteSpace(manifest.Id)) result.Errors.Add("mod.json: campo id vazio.");
                if (string.IsNullOrWhiteSpace(manifest.Name)) result.Errors.Add("mod.json: campo name vazio.");
                if (!string.IsNullOrEmpty(manifest.Id) &&
                    !string.Equals(Slug(manifest.Id), Slug(mod.Folder), StringComparison.OrdinalIgnoreCase))
                    result.Warnings.Add("mod.json: id \"" + manifest.Id + "\" é diferente da pasta \"" + mod.Folder + "\".");
                if (!string.IsNullOrEmpty(manifest.Lang) && manifest.Lang != mod.Lang)
                    result.Warnings.Add("mod.json: lang difere do mod.ini.");
                if (manifest.Priority != mod.Priority)
                    result.Warnings.Add("mod.json: priority difere do mod.ini.");

                var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ModFile file in manifest.Files ?? new List<ModFile>())
                {
                    string relative = NormalizeRelative(file.Path);
                    if (!IsSafeRelative(relative))
                    {
                        result.Errors.Add("mod.json: caminho inválido: " + file.Path);
                        continue;
                    }
                    if (!listed.Add(relative)) result.Errors.Add("mod.json: caminho repetido: " + relative);
                    string full = Path.Combine(mod.BasePath, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full))
                    {
                        result.Errors.Add("mod.json aponta para um arquivo que não existe: " + relative);
                        continue;
                    }
                    var info = new FileInfo(full);
                    if (file.Size > 0 && info.Length != file.Size)
                        result.Warnings.Add("Tamanho diferente do mod.json: " + relative);
                    if (!string.IsNullOrEmpty(file.Sha256) &&
                        !string.Equals(HashFile(full), file.Sha256, StringComparison.OrdinalIgnoreCase))
                        result.Warnings.Add("Hash diferente do mod.json: " + relative);
                }
            }
            else
            {
                result.Info.Add("Sem mod.json. O launcher cria um ao aplicar ou exportar.");
            }

            // Subtitles are optional, but a .srt without a matching video is usually a mistake.
            string subtitles = Path.Combine(mod.BasePath, "subtitles");
            if (Directory.Exists(subtitles))
            {
                var videos = new HashSet<string>(
                    mod.Files.Where(f => f.EndsWith(".xmv", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".bik", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".mpg", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".mpeg", StringComparison.OrdinalIgnoreCase))
                        .Select(f => Path.GetFileNameWithoutExtension(f)),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string srt in Directory.GetFiles(subtitles, "*.srt", SearchOption.AllDirectories))
                {
                    if (!videos.Contains(Path.GetFileNameWithoutExtension(srt)))
                        result.Warnings.Add("Legenda sem vídeo correspondente no mod: " + Path.GetFileName(srt));
                }
            }

            result.Info.Add(mod.On ? "Estado: ligado." : "Estado: desligado (.off).");
            result.Info.Add("Arquivos de jogo: " + mod.Files.Count + ".");
            return result;
        }

        // ------------------------------------------------------------------ package ---

        public PackageInfo ReadPackage(string path)
        {
            var info = new PackageInfo { Path = path };
            try
            {
                using (var zip = ZipFile.OpenRead(path))
                {
                    ZipArchiveEntry entry = FindManifestEntry(zip);
                    if (entry == null) throw new PackageException("O pacote não tem mod.json na raiz.");
                    using (Stream stream = entry.Open())
                        info.Manifest = Json.Read(stream);
                    ValidateManifest(info.Manifest);
                }
            }
            catch (Exception ex)
            {
                info.Manifest = null;
                info.Error = ex.Message;
            }
            return info;
        }

        public ImportResult ImportPackage(string packagePath, bool overwrite, bool forceDisabled)
        {
            var result = new ImportResult();
            string temporary = Path.Combine(Path.GetTempPath(), "blackmodkit-" + Guid.NewGuid().ToString("N"));
            string target = null;
            string backup = null;
            try
            {
                PackageInfo info = ReadPackage(packagePath);
                if (!info.Ok) throw new PackageException(info.Error);
                ModManifest manifest = info.Manifest;
                result.Manifest = manifest;
                string id = Slug(manifest.Id);
                if (id.Length == 0 || id.StartsWith(".", StringComparison.Ordinal))
                    throw new PackageException("mod.json: id inválido.");
                Directory.CreateDirectory(ModsDir);
                target = Path.Combine(ModsDir, id);
                result.Target = target;

                if (Directory.Exists(target) && !overwrite)
                    throw new PackageException("Já existe um mod instalado com o id \"" + id + "\".");

                Directory.CreateDirectory(temporary);
                ExtractSafely(packagePath, temporary);
                VerifyExtracted(temporary, manifest);
                VerifyIni(temporary, manifest);

                if (Directory.Exists(target))
                {
                    string backupRoot = Path.Combine(ModsDir, ".backup");
                    Directory.CreateDirectory(backupRoot);
                    backup = Path.Combine(backupRoot, id + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                    while (Directory.Exists(backup)) backup += "-b";
                    Directory.Move(target, backup);
                    result.Warnings.Add("Mod anterior guardado em mods\\.backup\\" + Path.GetFileName(backup));
                }

                CopyDirectory(temporary, target);
                string ini = Path.Combine(target, "mod.ini");
                if (!File.Exists(ini))
                    WriteIni(target, manifest.Name, manifest.Priority, manifest.Lang);
                if (forceDisabled)
                    File.WriteAllText(Path.Combine(target, ".off"), "");
                else
                {
                    string marker = Path.Combine(target, ".off");
                    if (File.Exists(marker)) File.Delete(marker);
                }

                List<ModInfo> mods = Scan();
                result.Mod = mods.FirstOrDefault(m => string.Equals(m.Folder, id, StringComparison.OrdinalIgnoreCase));
                if (result.Mod != null)
                {
                    result.Conflicts = Conflicts(mods)
                        .Where(c => c.Mods.Any(m => string.Equals(m.Folder, id, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
                result.Ok = true;
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Error = ex.Message;
                // Do not leave the user without the mod that was replaced.
                try
                {
                    if (backup != null && target != null && Directory.Exists(backup))
                    {
                        if (Directory.Exists(target)) Directory.Delete(target, true);
                        Directory.Move(backup, target);
                    }
                }
                catch { }
            }
            finally
            {
                try { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); } catch { }
            }
            return result;
        }

        public string ExportPackage(ModInfo mod, string outputPath)
        {
            if (mod == null) throw new InvalidOperationException("Escolha um mod.");
            if (mod.Files.Count == 0) throw new InvalidOperationException("O mod não tem arquivos de jogo.");

            ModManifest manifest = mod.Manifest ?? new ModManifest();
            manifest.Format = 1;
            manifest.Id = Slug(mod.Folder);
            manifest.Name = (mod.Title ?? "").Trim();
            manifest.Version = string.IsNullOrWhiteSpace(mod.Version) ? "1.0.0" : mod.Version;
            manifest.Author = mod.Author ?? "";
            manifest.Description = mod.Description ?? "";
            manifest.Lang = mod.Lang ?? "";
            manifest.Priority = mod.Priority;
            if (manifest.Name.Length == 0) throw new InvalidOperationException("O mod não tem nome.");

            // mod.ini is the file the port reads; keep it in the package and in the manifest hash list.
            WriteIni(mod.BasePath, manifest.Name, manifest.Priority, manifest.Lang);
            manifest.Files = BuildManifestFiles(mod.BasePath, new[] { "mod.json", ".off" });
            Json.WriteFile(Path.Combine(mod.BasePath, "mod.json"), manifest);

            if (File.Exists(outputPath)) File.Delete(outputPath);
            using (var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                foreach (ModFile file in manifest.Files)
                {
                    string full = Path.Combine(mod.BasePath, file.Path.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full)) continue;
                    ZipArchiveEntry entry = zip.CreateEntry(file.Path, CompressionLevel.Optimal);
                    entry.LastWriteTime = File.GetLastWriteTime(full);
                    using (Stream input = File.OpenRead(full))
                    using (Stream output = entry.Open())
                        input.CopyTo(output);
                }
                ZipArchiveEntry manifestEntry = zip.CreateEntry("mod.json", CompressionLevel.Optimal);
                using (Stream output = manifestEntry.Open())
                    Json.Write(output, manifest);
            }
            return outputPath;
        }

        private static void ValidateManifest(ModManifest manifest)
        {
            if (manifest == null) throw new PackageException("mod.json não foi lido.");
            if (manifest.Format != 1) throw new PackageException("mod.json: versão de formato não suportada.");
            if (string.IsNullOrWhiteSpace(manifest.Id) || Slug(manifest.Id).StartsWith(".", StringComparison.Ordinal))
                throw new PackageException("mod.json: id vazio.");
            if (string.IsNullOrWhiteSpace(manifest.Name)) throw new PackageException("mod.json: nome vazio.");
            if (manifest.Files == null || manifest.Files.Count == 0)
                throw new PackageException("mod.json: a lista de arquivos está vazia.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModFile file in manifest.Files)
            {
                string relative = NormalizeRelative(file.Path);
                if (!IsSafeRelative(relative)) throw new PackageException("mod.json: caminho inválido: " + file.Path);
                if (relative.Equals("mod.json", StringComparison.OrdinalIgnoreCase) ||
                    relative.Equals(".off", StringComparison.OrdinalIgnoreCase))
                    throw new PackageException("mod.json: caminho reservado: " + relative);
                if (!seen.Add(relative)) throw new PackageException("mod.json: caminho repetido: " + relative);
            }
        }

        private static ZipArchiveEntry FindManifestEntry(ZipArchive zip)
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                string name = entry.FullName.Replace('\\', '/').TrimStart('/');
                if (name.Equals("mod.json", StringComparison.OrdinalIgnoreCase)) return entry;
            }
            return null;
        }

        private static void ExtractSafely(string zipPath, string destination)
        {
            const long maxBytes = 2L * 1024 * 1024 * 1024;
            long total = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                if (zip.Entries.Count > 20000) throw new PackageException("Pacote grande demais (mais de 20.000 arquivos).");
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string raw = NormalizeRelative(entry.FullName);
                    if (raw.Length == 0) continue;
                    bool isDirectory = raw.EndsWith("/", StringComparison.Ordinal);
                    string relative = isDirectory ? raw.TrimEnd('/') : raw;
                    if (relative.Length == 0) continue;
                    if (!IsSafeRelative(relative)) throw new PackageException("Pacote com caminho inválido: " + entry.FullName);
                    if (!seen.Add(relative)) throw new PackageException("Pacote com caminho repetido: " + relative);
                    string target = Path.Combine(destination, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (!IsUnder(destination, target)) throw new PackageException("Pacote tentando sair da pasta: " + entry.FullName);
                    if (isDirectory)
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (Stream input = entry.Open())
                    using (Stream output = File.Create(target))
                    {
                        var buffer = new byte[81920];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            total += read;
                            if (total > maxBytes) throw new PackageException("Pacote grande demais.");
                            output.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        private static void VerifyIni(string folder, ModManifest manifest)
        {
            string ini = Path.Combine(folder, "mod.ini");
            if (!File.Exists(ini)) return;
            var parsed = new ModInfo { Title = "", Lang = "", Priority = 0 };
            ReadIni(ini, parsed);
            if (parsed.Errors.Count > 0)
                throw new PackageException("mod.ini inválido: " + parsed.Errors[0]);
            if (string.IsNullOrWhiteSpace(parsed.Title))
                throw new PackageException("mod.ini: falta name.");
            if (!string.Equals(parsed.Title.Trim(), (manifest.Name ?? "").Trim(), StringComparison.Ordinal))
                throw new PackageException("mod.ini name difere do mod.json.");
            if (parsed.Priority != manifest.Priority)
                throw new PackageException("mod.ini priority difere do mod.json.");
            if (!string.Equals((parsed.Lang ?? "").Trim().ToLowerInvariant(),
                (manifest.Lang ?? "").Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new PackageException("mod.ini lang difere do mod.json.");
        }

        private static void VerifyExtracted(string folder, ModManifest manifest)
        {
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModFile file in manifest.Files)
            {
                string relative = NormalizeRelative(file.Path);
                string full = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full)) throw new PackageException("Falta o arquivo declarado: " + relative);
                var info = new FileInfo(full);
                if (file.Size > 0 && info.Length != file.Size)
                    throw new PackageException("Tamanho diferente do mod.json: " + relative);
                if (!string.IsNullOrEmpty(file.Sha256) &&
                    !string.Equals(HashFile(full), file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new PackageException("Hash diferente do mod.json: " + relative);
                listed.Add(relative);
            }

            // Anything not in the manifest is a hidden payload; reject it instead of installing it.
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(folder.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (relative.Equals("mod.json", StringComparison.OrdinalIgnoreCase)) continue;
                if (!listed.Contains(relative))
                    throw new PackageException("Arquivo não declarado no mod.json: " + relative);
            }
        }

        public static List<ModFile> BuildManifestFiles(string basePath, string[] exclude = null)
        {
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mod.json", ".off" };
            if (exclude != null) foreach (string name in exclude) excluded.Add(name);

            var files = new List<ModFile>();
            foreach (string full in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
            {
                string relative = full.Substring(basePath.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (excluded.Contains(relative)) continue;
                if (relative.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                files.Add(new ModFile
                {
                    Path = relative,
                    Size = new FileInfo(full).Length,
                    Sha256 = HashFile(full)
                });
            }
            files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return files;
        }

        // ------------------------------------------------------------------ helpers ---

        public static string HashFile(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToUpperInvariant();
            }
            catch { return ""; }
        }

        public static string NormalizeRelative(string path)
        {
            if (path == null) return "";
            string value = path.Trim().Replace('\\', '/');
            while (value.StartsWith("./", StringComparison.Ordinal)) value = value.Substring(2);
            return value;
        }

        public static bool IsSafeRelative(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative)) return false;
            if (relative.IndexOf('\0') >= 0) return false;
            if (Path.IsPathRooted(relative)) return false;
            if (relative.Length >= 2 && relative[1] == ':') return false;
            string[] parts = relative.Split('/');
            foreach (string part in parts)
            {
                if (part.Length == 0 || part == "." || part == "..") return false;
                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            }
            return true;
        }

        public static string Slug(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            var builder = new StringBuilder();
            bool dash = false;
            foreach (char c in value)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.')
                {
                    builder.Append(c);
                    dash = false;
                }
                else if (c == '-' || char.IsWhiteSpace(c))
                {
                    if (!dash && builder.Length > 0) builder.Append('-');
                    dash = true;
                }
            }
            string result = builder.ToString().Trim('-', '.');
            if (result.Length > 64) result = result.Substring(0, 64).Trim('-', '.');
            return result;
        }

        public static string Pretty(string folder)
        {
            var builder = new StringBuilder();
            foreach (char c in folder ?? "")
                builder.Append(c == '_' || c == '-' || c == '.' ? ' ' : char.ToUpperInvariant(c));
            return builder.ToString();
        }

        private static bool IsPlaceholder(string name)
        {
            name = (name ?? "").ToLowerInvariant();
            if (name == ".gitkeep" || name == "thumbs.db" || name == "desktop.ini") return true;
            foreach (string prefix in PlaceholderPrefixes)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsUnder(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteIni(string basePath, string name, int priority, string lang)
        {
            var lines = new List<string>
            {
                "name = " + OneLine(name),
                "priority = " + priority.ToString(CultureInfo.InvariantCulture)
            };
            if (!string.IsNullOrWhiteSpace(lang)) lines.Add("lang = " + OneLine(lang).ToLowerInvariant());
            File.WriteAllText(Path.Combine(basePath, "mod.ini"), string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));
        }

        private static string OneLine(string value)
        {
            return (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(source.Length).TrimStart('\\', '/');
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart('\\', '/');
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }
    }
}
