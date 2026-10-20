using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BlackLauncher
{
    /// <summary>
    /// The release notes shown after an update. The updater saves the GitHub release body in
    /// log\launcher\changelog-<version>.txt before it restarts; a fresh install has the built-in text.
    /// </summary>
    public static class ReleaseNotes
    {
        public static string Current(Config cfg)
        {
            string saved = LogStore.ReadChangelog(cfg.PortDir, Loc.Version);
            return string.IsNullOrWhiteSpace(saved) ? Changelog.DefaultNotes : saved;
        }

        /// <summary>Release notes are written in GitHub Markdown; the window is a plain TextBlock, so strip
        /// the markers that would otherwise show up literally (**bold**, # headings, `code`, [link](url)).</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.TrimStart().StartsWith("```")) continue;
                line = Regex.Replace(line, @"^\s{0,3}#{1,6}\s*", "");
                line = Regex.Replace(line, @"^\s*>\s?", "");
                line = Regex.Replace(line, @"^\s*[-*+]\s+", "\u2022 ");
                line = line.Replace("**", "").Replace("__", "");
                line = Regex.Replace(line, @"`([^`]*)`", "$1");
                line = Regex.Replace(line, @"!\[([^\]]*)\]\([^)]*\)", "$1");
                line = Regex.Replace(line, @"\[([^\]]+)\]\(([^)]+)\)", "$1 ($2)");
                if (line.Trim() == "---" || line.Trim() == "***") { sb.AppendLine(); continue; }
                sb.AppendLine(line);
            }
            return sb.ToString().Trim();
        }
    }

    /// <summary>The built-in notes for the version this launcher was compiled as.</summary>
    public static class Changelog
    {
        public const string DefaultNotes = @"# ModLoader 1.4.10

## Novidades

- Relatorio de erro: quando o jogo falha, a janela do launcher abre com ENVIAR e NAO ENVIAR.
  O ENVIAR manda o log e as informacoes do PC para o canal #logs-black; nada e enviado sem o clique.
- O relatorio inclui o black-crash-*.log, as versoes do ModLoader e do port, Windows, CPU, GPU,
  RAM, tela, usuario/maquina do Windows, o menu, os mods ativos e os ajustes do black.ini.
- Logs organizados: black.log e black-crash-*.log saem da raiz para log\game\ e log\crash\.
  As tentativas de envio ficam em log\launcher\reports.log.
- Se o jogo for aberto direto pelo black.exe e falhar, o proximo inicio do launcher encontra o
  crash log na raiz e mostra a mesma janela uma vez.

## Arquivos

- O pacote de atualizacao e o black-pc-modloader.zip; o pacote base do loader e o
  BLACK-PC-Modloader-1.4.10.zip.
- A atualizacao nao toca em black.ini, launcher.ini, savedata\, game\, mods\ nem log\.";
    }
}
