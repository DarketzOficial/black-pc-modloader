// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BlackModKit
{
    /// <summary>A headless smoke test for the package and conflict engine: blackmodkit.exe --selftest.</summary>
    internal static class SelfTest
    {
        public static int Run()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            string root = Path.Combine(Path.GetTempPath(), "blackmodkit-selftest-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var newMod = new NewModForm())
                    Check(!string.IsNullOrEmpty(newMod.Text), "a janela de novo mod abre");
                using (var main = new MainForm())
                    Check(!string.IsNullOrEmpty(main.Text), "a janela principal abre");

                string portA = Path.Combine(root, "port-a");
                Directory.CreateDirectory(Path.Combine(portA, "game"));
                File.WriteAllText(Path.Combine(portA, "black.exe"), "");
                File.WriteAllText(Path.Combine(portA, "game", "default.xbe"), "x");

                var manager = new ModManager(portA);
                manager.CreateMod("MOD A", "mod-a", 0, "", false);
                WriteGameFile(portA, "mod-a", "language/strings/MainUS.bin", "conteudo A");
                manager.CreateMod("MOD B", "mod-b", 10, "", false);
                WriteGameFile(portA, "mod-b", "language/strings/MainUS.bin", "conteudo B");

                var mods = manager.Scan();
                Check(mods.Count == 2, "os dois mods foram encontrados");
                var conflicts = manager.Conflicts(mods);
                Check(conflicts.Count == 1, "um conflito de arquivo foi detectado");
                Check(conflicts[0].Winner.Folder == "mod-b", "a prioridade maior vence");
                Check(!conflicts[0].SameContent, "conteúdos diferentes não são marcados como iguais");

                ModInfo modA = mods.First(m => m.Folder == "mod-a");
                modA = manager.UpdateMod(modA, "MOD A", 0, "ptbr", "1.2.3", "Autor A", "Descrição A", true);
                Check(modA.Version == "1.2.3", "a versão foi salva");
                Check(modA.Lang == "ptbr", "o idioma foi salvo");

                string package = Path.Combine(root, "mod-a.bmod");
                manager.ExportPackage(manager.Scan().First(m => m.Folder == "mod-a"), package);
                Check(File.Exists(package), "o pacote .bmod foi criado");
                PackageInfo packageInfo = manager.ReadPackage(package);
                Check(packageInfo.Ok, "o pacote lido tem manifesto válido: " + packageInfo.Error);
                Check(packageInfo.Manifest.Id == "mod-a", "o id do pacote foi mantido");

                string portB = Path.Combine(root, "port-b");
                Directory.CreateDirectory(Path.Combine(portB, "game"));
                File.WriteAllText(Path.Combine(portB, "black.exe"), "");
                File.WriteAllText(Path.Combine(portB, "game", "default.xbe"), "x");
                var target = new ModManager(portB);
                ImportResult import = target.ImportPackage(package, false, false);
                Check(import.Ok, "o pacote foi importado: " + import.Error);
                Check(import.Mod != null && import.Mod.Folder == "mod-a", "o mod importado aparece na lista");
                string imported = Path.Combine(portB, "mods", "mod-a", "game", "language", "strings", "MainUS.bin");
                Check(File.Exists(imported) && File.ReadAllText(imported) == "conteudo A", "o arquivo importado está correto");

                ImportResult duplicate = target.ImportPackage(package, false, false);
                Check(!duplicate.Ok && duplicate.Error.Contains("Já existe"), "sem overwrite, a importação duplicada é recusada");

                ImportResult replaced = target.ImportPackage(package, true, false);
                Check(replaced.Ok, "é possível substituir um mod existente: " + replaced.Error);
                string backupRoot = Path.Combine(portB, "mods", ".backup");
                Check(Directory.Exists(backupRoot) && Directory.GetDirectories(backupRoot).Length > 0,
                    "o mod substituído foi guardado em .backup");
                Check(target.Scan().Count == 1, "a pasta .backup não aparece como mod");

                // A package with an extra, undeclared file must not be installed.
                string suspicious = Path.Combine(root, "suspicious.bmod");
                File.Copy(package, suspicious, true);
                using (var zip = ZipFile.Open(suspicious, ZipArchiveMode.Update))
                {
                    var entry = zip.CreateEntry("extra.txt");
                    using (var writer = new StreamWriter(entry.Open())) writer.Write("não declarado");
                }
                ImportResult rejected = target.ImportPackage(suspicious, true, false);
                Check(!rejected.Ok && rejected.Error.Contains("não declarado"), "arquivo extra no pacote é recusado");

                // A package with an extra traversal entry must be rejected before writing outside mods.
                string traversal = Path.Combine(root, "traversal.bmod");
                File.Copy(package, traversal, true);
                using (var zip = ZipFile.Open(traversal, ZipArchiveMode.Update))
                {
                    var entry = zip.CreateEntry("../fora.txt");
                    using (var writer = new StreamWriter(entry.Open())) writer.Write("fora");
                }
                ImportResult blocked = target.ImportPackage(traversal, true, false);
                Check(!blocked.Ok && blocked.Error.Contains("caminho inválido"), "zip-slip é recusado");

                // Language: the highest-priority enabled lang wins, other languages are blocked and do not conflict.
                string portLang = Path.Combine(root, "port-lang");
                Directory.CreateDirectory(Path.Combine(portLang, "game"));
                File.WriteAllText(Path.Combine(portLang, "black.exe"), "");
                File.WriteAllText(Path.Combine(portLang, "game", "default.xbe"), "x");
                var languages = new ModManager(portLang);
                languages.CreateMod("PT", "pt", 0, "ptbr", false);
                WriteGameFile(portLang, "pt", "language/strings/MainUS.bin", "PT");
                languages.CreateMod("EN", "en", 10, "en", false);
                WriteGameFile(portLang, "en", "language/strings/MainUS.bin", "EN");
                var languageMods = languages.Scan();
                Check(languageMods.First(m => m.Folder == "pt").Blocked, "mod de outro idioma fica bloqueado");
                Check(!languageMods.First(m => m.Folder == "en").Blocked, "o idioma de maior prioridade fica ativo");
                Check(languages.Conflicts(languageMods).Count == 0, "mod bloqueado por idioma não gera conflito");

                // Install a conflicting package disabled, so the user can review it.
                ImportResult disabled = languages.ImportPackage(package, false, true);
                Check(disabled.Ok, "pacote conflitante pode ser instalado desligado: " + disabled.Error);
                Check(disabled.Mod != null && !disabled.Mod.On && File.Exists(Path.Combine(disabled.Target, ".off")),
                    "o pacote importado desligado tem .off");

                Check(!ModManager.IsSafeRelative("../fora.txt"), "caminho com .. é recusado");
                Check(!ModManager.IsSafeRelative("C:/fora.txt"), "caminho absoluto é recusado");
                Check(ModManager.IsSafeRelative("game/language/strings/MainUS.bin"), "caminho normal é aceito");

                Console.WriteLine("SELFTEST OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SELFTEST FALHOU: " + ex.Message);
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void WriteGameFile(string port, string folder, string relative, string text)
        {
            string path = Path.Combine(port, "mods", folder, "game", relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Falhou: " + message);
            Console.WriteLine("ok: " + message);
        }
    }
}
