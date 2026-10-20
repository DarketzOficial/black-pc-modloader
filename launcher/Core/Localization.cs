using System;

namespace BlackLauncher
{
    /// <summary>Every string the launcher shows, in the two languages it offers.</summary>
    public static class Loc
    {
        public static bool Pt = true;

        public static string T(string pt, string en) => Pt ? pt : en;

        // ---------------------------------------------------------------- chrome ---
        /// <summary>The modloader's own name and version (the version comes from the build's).</summary>
        public static string ProductName => "ModLoader";
        public static string Version
        {
            get
            {
                var v = typeof(Loc).Assembly.GetName().Version;
                // The build number is what an update compares and what the release tag carries (1.4.9), so it
                // belongs in the title too — "1.4" alone would look the same before and after an update.
                return v.Major + "." + v.Minor + (v.Build > 0 ? "." + v.Build : "");
            }
        }
        /// <summary>The product's title, small line under the logo and the window's own title.</summary>
        public static string Product => ProductName + " " + Version;
        public static string WindowTitle => Product + " for Black PC Port";
        public static string Credits => "By Darketz";
        public static string CreditsUrl => "https://www.youtube.com/Darketz";
        public static string DiscordUrl => "https://discord.gg/HKpgA9ghVE";
        public static string YouTubeHint => Pt ? "YouTube do Darketz" : "Darketz on YouTube";
        public static string DiscordHint => Pt ? "Entre no Discord da comunidade" : "Join the community Discord";
        public static string Subtitle => Pt ? "PORT PARA PC — CONFIGURAÇÃO" : "PC PORT — CONFIGURATION";
        public static string LanguageLabel => Pt ? "IDIOMA" : "LANGUAGE";
        public static string Save => Pt ? "SALVAR" : "SAVE";
        public static string Play => Pt ? "JOGAR" : "PLAY";
        public static string Restore => Pt ? "RESTAURAR" : "RESTORE";
        public static string SoundOn => Pt ? "Desligar o som (música e efeitos)" : "Turn the sound off (music and effects)";
        public static string SoundOff => Pt ? "Ligar o som (música e efeitos)" : "Turn the sound back on";
        public static string CaptionMinimize => Pt ? "Minimizar" : "Minimize";
        public static string CaptionMaximize => Pt ? "Maximizar" : "Maximize";
        public static string CaptionRestore => Pt ? "Restaurar a janela" : "Restore the window";
        public static string CaptionClose => Pt ? "Fechar" : "Close";

        // ---------------------------------------------------------------- status ---
        public static string Saved => Pt ? "Salvado com sucesso" : "Saved successfully";
        public static string LoadedFrom => Pt ? "Lendo" : "Reading";
        public static string NoIni => Pt ? "black.ini novo — usando os padrões." : "New black.ini — using the defaults.";
        public static string MissingGameTitle => Pt ? "Jogo não encontrado" : "Game not found";
        public static string MissingGameHint(string why) => Pt
            ? why + "\n\nColoque o launcher na pasta do port (a que tem black.exe e a pasta game)."
            : why + "\n\nPut the launcher in the port's folder (the one with black.exe and the game folder).";
        public static string PlayFailedTitle => Pt ? "Não foi possível abrir o jogo" : "Could not start the game";
        public static string RestoreConfirmTitle => Pt ? "Restaurar os padrões?" : "Restore the defaults?";
        public static string RestoreConfirmBody => Pt
            ? "Todos os ajustes voltam ao padrão de uma instalação nova. Suas partidas (savedata) não são tocadas."
            : "Every setting goes back to a fresh install's default. Your saves (savedata) are not touched.";

        // ------------------------------------------------------------------ mods ---
        public static string ModsTitle => Pt ? "MODS INSTALADOS" : "INSTALLED MODS";
        public static string ModsEmpty => Pt
            ? "Nenhum mod instalado. Extraia a pasta de um mod dentro de mods\\ — o launcher acha os arquivos do jogo sozinho."
            : "No mods installed. Extract a mod's folder inside mods\\ — the launcher finds the game's files by itself.";
        public static string ModsHint => Pt
            ? "Cada pasta em mods\\ é lida como o autor do mod a montou (game\\, ou as pastas do próprio jogo). O estado liga/desliga fica no próprio mod."
            : "Each folder in mods\\ is read the way its author packed it (game\\, or the game's own folders). The on/off state lives in the mod itself.";
        public static string ModsBlocked => Pt ? "OUTRO IDIOMA" : "OTHER LANGUAGE";
        public static string ModsBlockedTip => Pt
            ? "Este mod é de outro idioma e é deixado de fora enquanto o idioma atual estiver em uso — mesmo que apareça ligado."
            : "This mod is for another language and is skipped while the current language is played — even though it shows as on.";
        public static string On => Loc.Pt ? "ON" : "ON";
        public static string Off => "OFF";
        public static string HdReady(int matched, int total) => total > 0
            ? (Pt ? $"{(int)Math.Round(100.0 * matched / total)}% do pacote pronto" : $"{(int)Math.Round(100.0 * matched / total)}% of the pack ready")
            : "";

        // -------------------------------------------------------------- pad ------
        public static string PadButtonsTitle => Pt ? "BOTÕES DO CONTROLE XBOX" : "XBOX CONTROLLER BUTTONS";
        public static string PadButtonsHint => Pt
            ? "O que cada botão faz no jogo, do jeito que ele vem de fábrica. Para trocar, use a tela do próprio jogo: "
              + "OPÇÕES > OPÇÕES DO CONTROLE > PERSONALIZAR. As opções acima são só os ajustes do analógico no PC."
            : "What each button does in the game, as it comes by default. To change it, use the game's own screen: "
              + "OPTIONS > CONTROL SETUP > CUSTOMISE. The options above are only the PC-side stick adjustments.";

        // -------------------------------------------------------------- bindings ---
        public static string BindingsTitle => Pt ? "TECLAS" : "KEY BINDINGS";
        public static string BindingsHint => Pt
            ? "Clique num campo e pressione a tecla (ou botão do mouse). Delete apaga; Esc cancela."
            : "Click a field and press a key (or mouse button). Delete clears it; Esc cancels.";
        public static string BindPress => Pt ? "pressione..." : "press...";
        public static string BindFixed => Pt ? "fixo" : "fixed";
        public static string BindPrimary => Pt ? "Principal" : "Primary";
        public static string BindReset => Pt ? "Padrões das teclas" : "Reset keys";

        // -------------------------------------------------------------- advanced ---
        public static string AdvFiles => Pt ? "ARQUIVOS DO PORT" : "PORT FILES";
        public static string AdvOpenGame => Pt ? "Abrir pasta do jogo" : "Open game folder";
        public static string AdvOpenMods => Pt ? "Abrir pasta de mods" : "Open mods folder";
        public static string AdvOpenIni => Pt ? "Abrir black.ini" : "Open black.ini";
        public static string AdvOpenLog => Pt ? "Abrir pasta de logs" : "Open log folder";
        public static string AdvBrowseHd => Pt ? "TROCAR PASTA" : "CHANGE FOLDER";
        public static string AdvBrowseHdTitle => Pt ? "Escolher o map.csv das texturas HD" : "Choose the HD textures map.csv";
        public static string AdvBrowseHdFilter => Pt ? "map.csv|map.csv|Todos os arquivos (*.*)|*.*" : "map.csv|map.csv|All files (*.*)|*.*";
        public static string AdvHdDefault => Pt ? "Padrão: mods\\hdtextures\\hd" : "Default: mods\\hdtextures\\hd";
        public static string AdvHdClearHint => Pt ? "Limpar o caminho" : "Clear the path";

        public static string GameMissingBadge => Pt ? "JOGO NÃO ENCONTRADO" : "GAME NOT FOUND";
        public static string Ready => Pt ? "PRONTO" : "READY";

        // --------------------------------------------------------------- updates ---
        public static string CheckUpdates => Pt ? "PROCURAR ATUALIZAÇÃO" : "CHECK FOR UPDATES";
        public static string CheckUpdatesHint => Pt
            ? "Procura no GitHub uma versão nova do launcher."
            : "Checks GitHub for a newer launcher version.";
        public static string Checking => Pt ? "PROCURANDO..." : "CHECKING...";
        public static string UpdateWindowTitle(string v) => Pt ? "Atualização — ModLoader " + v : "Update — ModLoader " + v;
        public static string UpdateOverline => Pt ? "ATUALIZAÇÃO DO MODLOADER" : "MODLOADER UPDATE";
        public static string UpdateTitle(string v) => Pt ? $"Nova atualização ({v}):" : $"New update ({v}):";
        public static string UpdateIntro(string current, string size) => Pt
            ? $"Você está na versão {current}. A versão nova está disponível ({size})."
            : $"You are on version {current}. The new version is available ({size}).";
        public static string UpdateNotes => Pt ? "NOVIDADES" : "WHAT'S NEW";
        public static string UpdateNotesEmpty => Pt
            ? "Uma versão nova do launcher está disponível."
            : "A newer launcher version is available.";
        public static string UpdateNow => Pt ? "ATUALIZAR" : "UPDATE";
        public static string UpdateLater => Pt ? "DEIXAR PRA DEPOIS" : "LATER";
        public static string UpdateRetry => Pt ? "TENTAR DE NOVO" : "TRY AGAIN";
        public static string Cancel => Pt ? "CANCELAR" : "CANCEL";
        public static string UpdateStarting => Pt ? "Preparando o download..." : "Preparing the download...";
        public static string UpdateDownloading(int percent) => Pt
            ? $"Baixando... {percent}%"
            : $"Downloading... {percent}%";
        public static string UpdateExtracting => Pt ? "Extraindo os arquivos..." : "Extracting the files...";
        public static string UpdateInstalling => Pt ? "Instalando... o launcher vai reiniciar." : "Installing... the launcher will restart.";
        public static string UpdateNoLauncher => Pt
            ? "O pacote baixado não tem o blacklauncher.exe."
            : "The downloaded package has no blacklauncher.exe.";
        public static string UpdateFailed(string why) => Pt
            ? "Não foi possível atualizar:\n" + why
            : "Could not update:\n" + why;
        public static string UpdateLatest => Pt
            ? "Você já está na versão mais recente (" + Version + ")."
            : "You are already on the latest version (" + Version + ").";
        public static string UpdateNoRelease => Pt
            ? "Ainda não há uma atualização do launcher publicada no GitHub do projeto."
            : "There is no launcher update published on the project's GitHub yet.";
        public static string UpdateCheckFailed(string why) => Pt
            ? "Não foi possível procurar atualizações:\n" + why
            : "Could not check for updates:\n" + why;

        // ------------------------------------------------------------- donation ---
        public static string DonationButton => Pt ? "DOAR" : "DONATE";
        public static string DonationHint => Pt ? "Apoiar o projeto" : "Support the project";
        public static string DonationCaption => Pt ? "Apoie o projeto — " + Product : "Support the project — " + Product;
        public static string DonationSubtitle => Pt ? "APOIE O PROJETO" : "SUPPORT THE PROJECT";
        public static string DonationTitle => Pt ? "Obrigado por apoiar o BLACK PC" : "Thank you for supporting BLACK PC";
        public static string DonationIntro => Pt
            ? "O projeto é gratuito. Se ele te ajudou, você pode apoiar com PIX ou uma das opções ao lado. Os recursos usados no desenvolvimento são pagos, incluindo o uso de IA como o Claude Code. Obrigado!"
            : "The project is free. If it helped you, you can support it with PIX or one of the options beside it. The resources used in development are paid for, including the use of AI such as Claude Code. Thank you!";
        public static string DonationPix => "PIX";
        public static string DonationPixHint => Pt
            ? "Escaneie o QR code no app do seu banco para pagar direto, sem sair da tela."
            : "Scan the QR code in your banking app to pay directly, without leaving this screen.";
        public static string DonationOther => Pt ? "OUTRAS FORMAS DE APOIAR" : "OTHER WAYS TO SUPPORT";
        public static string DonationLivePix => "LivePix";
        public static string DonationLivePixHint => Pt ? "Abrir a página de doação no navegador" : "Open the donation page in your browser";
        public static string DonationCopy => Pt ? "COPIAR" : "COPY";
        public static string DonationCopied => Pt ? "Copiado!" : "Copied!";
        public static string DonationCopyFailed => Pt ? "Não foi possível copiar." : "Could not copy.";
        public static string DonationClose => Pt ? "FECHAR" : "CLOSE";
        public static string DonationThanks => Pt
            ? "Obrigado por fazer parte do projeto."
            : "Thank you for being part of the project.";

        // ------------------------------------------------------------ changelog ---
        public static string AdvInfo => Pt ? "INFORMAÇÕES" : "INFORMATION";
        public static string AdvChangelog => Pt ? "Informações da atualização" : "Update information";
        public static string ChangelogCaption => Pt ? "Novidades — " + Product : "What's new — " + Product;
        public static string ChangelogSubtitle => Pt ? "NOVIDADES DA ATUALIZAÇÃO" : "WHAT'S NEW";
        public static string ChangelogTitle => Pt ? "O que mudou na " + Version : "What changed in " + Version;
        public static string ChangelogIntro => Pt
            ? "Esta tela aparece uma vez depois de cada atualização. Para rever quando quiser, abra AVANÇADO > Informações da atualização."
            : "This screen appears once after each update. To see it again, open ADVANCED > Update information.";
        public static string ChangelogHint => Pt
            ? "Você pode abrir esta tela de novo em AVANÇADO > Informações da atualização."
            : "You can open this screen again in ADVANCED > Update information.";
        public static string ChangelogContinue => Pt ? "CONTINUAR" : "CONTINUE";
        public static string ChangelogSummary => Pt ? "RESUMO" : "SUMMARY";
        public static string ChangelogEmpty => Pt
            ? "As novidades desta versão não estão disponíveis."
            : "The notes for this version are not available.";

        // ---------------------------------------------------------- crash report ---
        public static string CrashCaption => Pt ? "Relatório de erro — " + Product : "Crash report — " + Product;
        public static string CrashSubtitle => Pt ? "RELATÓRIO DE ERRO" : "CRASH REPORT";
        public static string CrashTitle => Pt ? "O jogo encontrou um erro" : "The game hit an error";
        public static string CrashTitleStart => Pt ? "Não foi possível iniciar o jogo" : "The game could not start";
        public static string CrashIntro => Pt
            ? "O jogo foi fechado de forma inesperada. Você pode enviar o log e as informações do PC para o desenvolvedor corrigir o problema."
            : "The game closed unexpectedly. You can send the log and the PC information to the developer so the problem can be fixed.";
        public static string CrashIntroStart => Pt
            ? "O jogo não iniciou. O relatório ajuda a descobrir o que faltou ou o que impediu o black.exe de abrir."
            : "The game did not start. The report helps find what is missing or what prevented black.exe from opening.";
        public static string CrashOptional => Pt
            ? "O envio é opcional. Nada é enviado sem você clicar em ENVIAR."
            : "Sending is optional. Nothing is sent unless you press SEND.";
        public static string CrashNoLog => Pt ? "nenhum log encontrado" : "no log found";
        public static string CrashVersionLabel => Pt ? "VERSÃO" : "VERSION";
        public static string CrashSystemLabel => Pt ? "SISTEMA" : "SYSTEM";
        public static string CrashSessionLabel => Pt ? "SESSÃO" : "SESSION";
        public static string CrashMenuUnknown => Pt ? "menu não identificado" : "menu not identified";
        public static string CrashExitCode(int code) => Pt ? "código " + code : "exit " + code;
        public static string CrashMods => Pt ? "MODS" : "MODS";
        public static string CrashPrivacy(string log) => Pt
            ? "O relatório inclui " + (string.IsNullOrEmpty(log) ? "o log do jogo" : log) +
              ", as versões do ModLoader e do port, Windows, CPU, GPU, RAM, tela, usuário do Windows, " +
              "o menu onde o jogo estava e os mods/ajustes ativos. Nenhum save, senha ou arquivo pessoal é enviado."
            : "The report includes " + (string.IsNullOrEmpty(log) ? "the game log" : log) +
              ", the ModLoader and port versions, Windows, CPU, GPU, RAM, screen, Windows user, " +
              "the menu the game was on, and the active mods/settings. No save, password or personal file is sent.";
        public static string CrashOpenFolder => Pt ? "ABRIR PASTA DO LOG" : "OPEN LOG FOLDER";
        public static string CrashNo => Pt ? "NÃO ENVIAR" : "DON'T SEND";
        public static string CrashSend => Pt ? "ENVIAR" : "SEND";
        public static string CrashSending => Pt ? "ENVIANDO O RELATÓRIO..." : "SENDING THE REPORT...";
        public static string CrashSent => Pt ? "RELATÓRIO ENVIADO. OBRIGADO!" : "REPORT SENT. THANK YOU!";
        public static string CrashRetry => Pt ? "TENTAR DE NOVO" : "TRY AGAIN";
        public static string CrashClose => Pt ? "FECHAR" : "CLOSE";
        public static string CrashFailed(string why) => Pt
            ? "Não foi possível enviar:\n" + why
            : "Could not send:\n" + why;
    }
}
