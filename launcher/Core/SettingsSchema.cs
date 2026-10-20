using System;
using System.Collections.Generic;

namespace BlackLauncher
{
    public enum ItemKind { Bool, Choice, Slider, Text, Resolution, Scale, Reverb, HdTextures }

    public sealed class Choice
    {
        public string Value;
        public string Pt;
        public string En;
        public Choice(string value, string pt, string en) { Value = value; Pt = pt; En = en; }
    }

    public sealed class SettingItem
    {
        public string Key;              // black.ini key (special items read more than one)
        public ItemKind Kind;
        public string Pt, En;           // label
        public string HelpPt, HelpEn;   // tooltip
        public string Default = "";     // reset value
        public bool Pref;               // launcher.ini instead of black.ini
        public bool Browse;             // text path with a native "change folder" button

        public Choice[] Choices;        // Choice / Resolution / Scale / Reverb
        public double Min, Max, Step;   // Slider
        public string Format = "0.0";   // Slider value format
        public string Suffix = "";      // Slider suffix (e.g. "%")

        public string Label(bool pt) => pt ? Pt : En;
        public string Help(bool pt) => pt ? HelpPt : HelpEn;
    }

    public sealed class Section
    {
        public string Id;
        public string Pt, En;
        public bool HasBindings;   // the key-bindings panel goes here
        public bool HasMods;       // the mods list panel goes here
        public bool HasAdvanced;   // the file/folder buttons panel
        public bool HasPad;        // the Xbox pad panel goes here
        public string PadPt, PadEn;    // the pad panel's header
        public List<SettingItem> Items = new List<SettingItem>();
        public List<SettingItem> PadItems = new List<SettingItem>();
    }

    /// <summary>One row of the pad's button reference (CONTROLS > XBOX CONTROLLER BUTTONS): the button itself
    /// and what the game does with it, by default.</summary>
    public sealed class PadButton
    {
        public string BtnPt, BtnEn;   // the button, as it is written on the controller
        public string Pt, En;         // the game's action
        public PadButton(string btnPt, string btnEn, string pt, string en)
        {
            BtnPt = btnPt; BtnEn = btnEn; Pt = pt; En = en;
        }
    }

    /// <summary>
    /// The launcher's settings, mirroring the port's PC SETTINGS rows (fe/pc_settings.cpp) and the
    /// black.ini keys settings.cpp reads. Labels are the port's own, in both launcher languages.
    ///
    /// The tabs split the rows by what the player is changing: VIDEO is the window and the camera,
    /// GRAPHICS is everything that changes the picture, PERFORMANCE is frame pacing, AUDIO AND GAME is
    /// the rest of the port's own options. Moving a row between tabs does not move its black.ini key.
    /// </summary>
    public static class Schema
    {
        /// <summary>The Xbox pad's buttons and what the game does with them by default (the game's own
        /// controller-setup values: FE_ACTION_* in the front end, input/bindings.cpp on the port's side).
        /// Shown in CONTROLS so a player who wants to personalise the pad knows what it does today.</summary>
        public static readonly PadButton[] PadButtons =
        {
            new PadButton("A", "A", "Recarregar (e confirmar nos menus)", "Reload (and confirm in the menus)"),
            new PadButton("B", "B", "Corpo a corpo (e voltar nos menus)", "Melee (and go back in the menus)"),
            new PadButton("X", "X", "Ação: usar, pegar, abrir", "Action: use, pick up, open"),
            new PadButton("Y", "Y", "Ligar/desligar o silenciador", "Toggle the suppressor"),
            new PadButton("Gatilho direito (RT)", "Right trigger (RT)", "Atirar", "Fire"),
            new PadButton("Gatilho esquerdo (LT)", "Left trigger (LT)", "Granada", "Grenade"),
            new PadButton("Analógico esquerdo", "Left stick", "Correr, andar para trás e para os lados", "Run forward, back and sideways"),
            new PadButton("Analógico direito", "Right stick", "Olhar e girar a visão", "Look around and turn the view"),
            new PadButton("Clique do analógico esquerdo (L3)", "Left stick click (L3)", "Agachar", "Crouch"),
            new PadButton("Clique do analógico direito (R3)", "Right stick click (R3)", "Mirar (zoom de precisão)", "Aim (scope zoom)"),
            new PadButton("Direcional para cima", "D-pad up", "Modo de disparo", "Change firing mode"),
            new PadButton("Direcional para baixo", "D-pad down", "Pacote de saúde", "Health pack"),
            new PadButton("Direcional para a esquerda", "D-pad left", "Arma anterior", "Previous weapon"),
            new PadButton("Direcional para a direita", "D-pad right", "Próxima arma", "Next weapon"),
            new PadButton("START", "START", "Pausar", "Pause"),
            new PadButton("BACK", "BACK", "Ver os objetivos", "Show the objectives"),
            new PadButton("LB (botão branco)", "LB (white button)", "Próxima música", "Next music track"),
            new PadButton("RB (botão preto)", "RB (black button)", "— (o jogo não usa)", "— (not used by the game)"),
        };

        public static List<Section> Build(bool pt)
        {
            var s = new List<Section>();

            // ------------------------------------------------------------------ VIDEO ---
            var video = new Section { Id = "video" };
            video.Pt = "VÍDEO"; video.En = "VIDEO";
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Resolution, Key = "window", Default = "",
                Pt = "Resolução", En = "Resolution",
                HelpPt = "Tamanho da janela do jogo (a largura sai da proporção). AUTOMÁTICO usa o maior tamanho que couber na tela — em tela cheia, a resolução do monitor.",
                HelpEn = "The game window's size (the width follows the aspect). AUTOMATIC uses the largest size that fits the screen — in fullscreen, the monitor's resolution.",
                Choices = new[]
                {
                    new Choice("", "AUTOMÁTICO", "AUTOMATIC"),
                    new Choice("0x480", "854 X 480", "854 X 480"),
                    new Choice("0x720", "1280 X 720", "1280 X 720"),
                    new Choice("0x768", "1366 X 768", "1366 X 768"),
                    new Choice("0x900", "1600 X 900", "1600 X 900"),
                    new Choice("0x1080", "1920 X 1080", "1920 X 1080"),
                    new Choice("0x1440", "2560 X 1440", "2560 X 1440"),
                    new Choice("0x2160", "3840 X 2160", "3840 X 2160"),
                }
            });
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "fullscreen", Default = "0",
                Pt = "Tela cheia", En = "Fullscreen",
                HelpPt = "Começa em tela cheia sem bordas. Também dá para alternar com Alt+Enter no jogo.",
                HelpEn = "Starts borderless fullscreen. Alt+Enter toggles it in the game too.",
            });
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "widescreen", Default = "1",
                Pt = "Proporção da tela", En = "Aspect ratio",
                HelpPt = "16:9 é o padrão. 4:3 é o formato original de 2006.",
                HelpEn = "16:9 is the default. 4:3 is the original 2006 format.",
                Choices = new[] { new Choice("1", "16:9", "16:9"), new Choice("0", "4:3", "4:3") },
            });
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "fov", Default = "120.0", Min = 90, Max = 120, Step = 2, Format = "0",
                Pt = "Campo de visão", En = "Field of view",
                HelpPt = "Quanto se vê na horizontal enquanto joga. O mínimo é 90; 120 é o padrão do port.",
                HelpEn = "How much is seen horizontally in play. The minimum is 90; 120 is the port's default.",
            });
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "viewmodel_fov", Default = "80.0", Min = 70, Max = 80, Step = 2, Format = "0",
                Pt = "Campo de visão da arma", En = "Weapon FOV",
                HelpPt = "O campo de visão da arma e das mãos. O máximo é 80 (acima de 70 as pontas cortadas dos braços começam a aparecer); 70 é o original.",
                HelpEn = "The field of view of the gun and hands. The maximum is 80 (past 70 the arms' cut ends start to show); 70 is the original.",
            });
            video.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "fmv_scale", Default = "0",
                Pt = "Vídeos (FMV)", En = "Movies (FMV)",
                HelpPt = "Como os vídeos aparecem numa tela widescreen: FILL estica para preencher, 4:3 mantém a proporção com tarjas.",
                HelpEn = "How movies appear on a widescreen display: FILL stretches to fill, 4:3 keeps the aspect with side bars.",
                Choices = new[] { new Choice("0", "FILL", "FILL"), new Choice("1", "4:3", "4:3") },
            });
            s.Add(video);

            // --------------------------------------------------------------- GRAPHICS ---
            var graphics = new Section { Id = "graphics" };
            graphics.Pt = "GRÁFICOS"; graphics.En = "GRAPHICS";
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Scale, Key = "scale", Default = "3.000",
                Pt = "Escala de renderização", En = "Render scale",
                HelpPt = "Resolução interna do 3D. 3X (original) é o padrão; mais alto deixa a imagem mais nítida e pesa mais na placa de vídeo.",
                HelpEn = "The internal 3D resolution. 3X (original) is the default; higher is sharper and heavier on the GPU.",
                Choices = new[]
                {
                    new Choice("0.5", "0.5X", "0.5X"),
                    new Choice("1", "1X", "1X"),
                    new Choice("1.5", "1.5X", "1.5X"),
                    new Choice("2", "2X", "2X"),
                    new Choice("3", "3X (ORIGINAL)", "3X (ORIGINAL)"),
                    new Choice("4", "4X", "4X"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "aa", Default = "1",
                Pt = "Anti-serrilhado", En = "Anti-aliasing",
                HelpPt = "Suaviza as bordas serrilhadas. FXAA é o filtro leve do port; SMAA 1x suaviza mais e mantém melhor os detalhes finos. OFF desliga.",
                HelpEn = "Smooths jagged edges. FXAA is the port's light filter; SMAA 1x smooths more and keeps fine detail better. OFF turns it off.",
                Choices = new[]
                {
                    new Choice("0", "OFF", "OFF"), new Choice("1", "FXAA", "FXAA"), new Choice("2", "SMAA", "SMAA"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "aniso", Default = "0",
                Pt = "Filtro anisotrópico", En = "Anisotropic filtering",
                HelpPt = "Deixa as texturas nítidas quando vistas de lado. OFF usa os filtros do próprio jogo.",
                HelpEn = "Keeps textures sharp when seen at an angle. OFF uses the game's own filters.",
                Choices = new[]
                {
                    new Choice("0", "OFF", "OFF"), new Choice("2", "2X", "2X"), new Choice("4", "4X", "4X"),
                    new Choice("8", "8X", "8X"), new Choice("16", "16X", "16X"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "ao", Default = "0",
                Pt = "Oclusão ambiental", En = "Ambient occlusion",
                HelpPt = "Escurece as dobras e os cantos onde a geometria se fecha (o SSAO do recomp). LIGADO roda em metade da resolução; ALTO na resolução cheia — mais nítido nas arestas finas e bem mais pesado na placa de vídeo.",
                HelpEn = "Darkens the creases and corners where geometry folds into itself (the recomp's SSAO). ON runs at half the render size; HIGH at the full size — sharper in fine edges and much heavier on the GPU.",
                Choices = new[]
                {
                    new Choice("0", "OFF", "OFF"), new Choice("1", "LIGADO", "ON"), new Choice("2", "ALTO", "HIGH"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "hdr", Default = "0",
                Pt = "HDR", En = "HDR",
                HelpPt = "Luz de verdade nos pontos claros: brilho em volta das janelas, luzes e fogo, altas luzes que rolam em vez de estourar, e cor mais viva. A força abaixo dosa o efeito.",
                HelpEn = "Real light in the bright spots: glow around windows, lights and fire, highlights that roll off instead of clipping, and fuller colour. The strength below doses it.",
                Choices = new[]
                {
                    new Choice("0", "OFF", "OFF"), new Choice("1", "LIGADO", "ON"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "hdr_strength", Default = "70", Min = 0, Max = 100, Step = 5, Format = "0",
                Suffix = "%",
                Pt = "Força do HDR", En = "HDR strength",
                HelpPt = "Quanto do efeito aparece. 0 deixa a imagem como sem HDR; 70 é o padrão.",
                HelpEn = "How much of the effect shows. 0 leaves the picture as it is without HDR; 70 is the default.",
            });
            // Image filters (the port's pass after the gamma ramp, before the anti-aliasing). 0 = off; the game's
            // PC SETTINGS > IMAGE page has the same four rows and the same range (0-100 in steps of 5).
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "sharpen", Default = "0", Min = 0, Max = 100, Step = 5, Format = "0",
                Suffix = "%",
                Pt = "Nitidez", En = "Sharpening",
                HelpPt = "Acentua as bordas e os detalhes finos. Valores altos criam halos em volta das bordas; 0 desliga.",
                HelpEn = "Sharpens edges and fine detail. High values leave halos around edges; 0 turns it off.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "details", Default = "0", Min = 0, Max = 100, Step = 5, Format = "0",
                Suffix = "%",
                Pt = "Detalhes", En = "Details",
                HelpPt = "Realça só os detalhes pequenos, sem endurecer as bordas fortes (sem halos). 0 desliga.",
                HelpEn = "Boosts only the small details, without hardening strong edges (no halos). 0 turns it off.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "clarity", Default = "0", Min = 0, Max = 100, Step = 5, Format = "0",
                Suffix = "%",
                Pt = "Clareza", En = "Clarity",
                HelpPt = "Contraste local nos tons médios: dá volume à imagem sem mudar o brilho geral. 0 desliga.",
                HelpEn = "Local contrast in the midtones: gives the picture body without changing the overall brightness. 0 turns it off.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "vibrance", Default = "0", Min = 0, Max = 100, Step = 5, Format = "0",
                Suffix = "%",
                Pt = "Vibrance", En = "Vibrance",
                HelpPt = "Cores mais vivas, principalmente as apagadas; as que já estão saturadas mudam pouco. 0 desliga.",
                HelpEn = "Livelier colours, mainly the dull ones; the already saturated ones change little. 0 turns it off.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "gain", Default = "1.00", Min = 0.5, Max = 3.0, Step = 0.1, Format = "0.0",
                Pt = "Brilho", En = "Brightness",
                HelpPt = "Brilho geral da imagem. No jogo, prefira o menu OPÇÕES DE IMAGEM.",
                HelpEn = "Overall picture brightness. In the game, prefer the PICTURE SETUP menu.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "gamma", Default = "1.00", Min = 0.5, Max = 3.0, Step = 0.1, Format = "0.0",
                Pt = "Gama", En = "Gamma",
                HelpPt = "Correção de gama da imagem final.",
                HelpEn = "Gamma correction of the final picture.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "draw_distance", Default = "1",
                Pt = "Distância de visão", En = "Draw distance",
                HelpPt = "Quão longe personagens, objetos e vegetação continuam desenhados. 1x é o original; acima disso " +
                         "tudo fica desenhado mais longe (até 16x). Abaixo de 1x (até 0.2x) é para PC fraco: a névoa do " +
                         "cenário fecha junto (em 0.2x some tudo além de ~20 m), então o que é cortado some dentro da " +
                         "névoa em vez de aparecer de repente.",
                HelpEn = "How far characters, props and ground cover stay drawn. 1x is the original; above it everything " +
                         "stays drawn farther away (up to 16x). Below 1x (down to 0.2x) is for slower PCs: the level's " +
                         "fog closes in with it (at 0.2x everything past ~20 m is gone), so what is culled fades into " +
                         "the fog instead of popping in.",
                Choices = new[]
                {
                    new Choice("0.2", "0.2x", "0.2x"), new Choice("0.3", "0.3x", "0.3x"),
                    new Choice("0.5", "0.5x", "0.5x"), new Choice("0.75", "0.75x", "0.75x"),
                    new Choice("1", "1x", "1x"), new Choice("1.5", "1.5x", "1.5x"),
                    new Choice("2", "2x", "2x"), new Choice("3", "3x", "3x"),
                    new Choice("4", "4x", "4x"), new Choice("6", "6x", "6x"),
                    new Choice("8", "8x", "8x"), new Choice("12", "12x", "12x"),
                    new Choice("16", "16x", "16x"),
                }
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "effectsnative", Default = "0",
                Pt = "Efeitos no tamanho Xbox", En = "Effects at Xbox size",
                HelpPt = "Mantém os efeitos pequenos (brilhos, ofuscamento) no tamanho do Xbox. Desligado, eles acompanham a escala e ficam mais suaves.",
                HelpEn = "Keeps the small effect buffers (glow, bloom) at the Xbox size. Off, they follow the render scale and look smoother.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "weaponblur", Default = "1",
                Pt = "Blur das armas", En = "Weapon blur",
                HelpPt = "O borrão que cobre a tela enquanto a arma recarrega. Desligado, a recarga continua igual — só o borrão sai.",
                HelpEn = "The smear across the picture while the weapon reloads. Off, the reload is unchanged and only the blur goes.",
            });
            graphics.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "seamfill", Default = "1",
                Pt = "Preencher rachaduras", En = "Fill geometry seams",
                HelpPt = "Preenche as linhas finas que aparecem entre pedaços de geometria.",
                HelpEn = "Fills the thin cracks that show between geometry pieces.",
            });
            s.Add(graphics);

            // ------------------------------------------------------------ PERFORMANCE ---
            var perf = new Section { Id = "performance" };
            perf.Pt = "DESEMPENHO"; perf.En = "PERFORMANCE";
            perf.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "fps_cap", Default = "0",
                Pt = "Limite máximo de quadros", En = "Max frame rate",
                HelpPt = "O jogo roda destravado e o mundo acompanha o tempo real. SEM LIMITE usa o máximo do PC; " +
                         "TAXA DO MONITOR fica um pouco abaixo da atualização da tela (sem rasgos e com resposta baixa); " +
                         "os valores fixos limitam só os quadros mostrados, nunca a velocidade do jogo.",
                HelpEn = "The game runs unlocked and the world follows real time. UNLIMITED uses the PC's maximum; " +
                         "REFRESH RATE stays just under the display's refresh (no tearing, low latency); " +
                         "the fixed values only cap the pictures shown, never the game's speed.",
                Choices = new[]
                {
                    new Choice("0", "SEM LIMITE", "UNLIMITED"),
                    new Choice("-1", "TAXA DO MONITOR", "REFRESH RATE"),
                    new Choice("30", "30 FPS", "30 FPS"),
                    new Choice("60", "60 FPS", "60 FPS"),
                    new Choice("75", "75 FPS", "75 FPS"),
                    new Choice("90", "90 FPS", "90 FPS"),
                    new Choice("120", "120 FPS", "120 FPS"),
                    new Choice("144", "144 FPS", "144 FPS"),
                    new Choice("165", "165 FPS", "165 FPS"),
                    new Choice("240", "240 FPS", "240 FPS"),
                    new Choice("360", "360 FPS", "360 FPS"),
                }
            });
            perf.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "vsync", Default = "0",
                Pt = "Sincronia vertical", En = "Vertical sync",
                HelpPt = "A imagem espera a atualização do monitor a cada quadro: sem rasgos, com a resposta do monitor. " +
                         "Deixe OFF para usar o LIMITE MÁXIMO DE QUADROS (ou SEM LIMITE).",
                HelpEn = "The picture waits for the monitor's refresh each frame: no tearing, monitor-level latency. " +
                         "Leave it OFF to use MAX FRAME RATE instead (or UNLIMITED).",
            });
            perf.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "fpscounter", Default = "0",
                Pt = "Mostrar FPS", En = "Show FPS",
                HelpPt = "Um contador de quadros pequeno no canto superior esquerdo, no jogo.",
                HelpEn = "A small frame-rate counter in the top-left, in the game.",
            });
            s.Add(perf);

            // ---------------------------------------------------------- AUDIO E JOGO ---
            var game = new Section { Id = "game" };
            game.Pt = "ÁUDIO E JOGO"; game.En = "AUDIO AND GAME";
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Reverb, Key = "reverb", Default = "1",
                Pt = "Reverb do ambiente", En = "Room reverb",
                HelpPt = "O eco dos ambientes (I3DL2). OFF desliga; os outros valores mudam a intensidade.",
                HelpEn = "The rooms' echo (I3DL2). OFF turns it off; the other values change its strength.",
                Choices = new[]
                {
                    new Choice("off", "OFF", "OFF"),
                    new Choice("0.5", "50%", "50%"),
                    new Choice("1", "100%", "100%"),
                    new Choice("1.5", "150%", "150%"),
                    new Choice("2", "200%", "200%"),
                }
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "autoaim", Default = "0",
                Pt = "Mira automática", En = "Auto-aim",
                HelpPt = "A visão é puxada em direção aos inimigos, como nos consoles. Desligado por padrão.",
                HelpEn = "The view pulls toward targets, as on consoles. Off by default.",
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "skipanymovie", Default = "1",
                Pt = "Pular vídeos", En = "Skip any movie",
                HelpPt = "A ou START pula qualquer vídeo, inclusive os que você ainda não viu.",
                HelpEn = "A or START skips any movie, including ones not seen yet.",
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "subtitles", Default = "1",
                Pt = "Legendas", En = "Subtitles",
                HelpPt = "Mostra as legendas (.srt) sobre os vídeos, quando o mod trouxer.",
                HelpEn = "Draws the (.srt) subtitles over movies, when the mod brings them.",
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "pause_focus", Default = "0",
                Pt = "Pausar sem foco", En = "Pause without focus",
                HelpPt = "Congela e silencia o jogo enquanto a janela não estiver em primeiro plano.",
                HelpEn = "Freezes and mutes the game while its window is not in front.",
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "walk_speed", Default = "25",
                Pt = "Velocidade ao andar", En = "Walk speed",
                HelpPt = "Quão devagar o personagem anda com a tecla de andar (Shift).",
                HelpEn = "How slow the character moves while the walk key (Shift) is held.",
                Choices = new[]
                {
                    new Choice("25", "25%", "25%"), new Choice("50", "50%", "50%"),
                    new Choice("75", "75%", "75%"), new Choice("100", "100%", "100%"),
                }
            });
            game.Items.Add(new SettingItem
            {
                Kind = ItemKind.Choice, Key = "aim_toggle", Default = "0",
                Pt = "Modo de mira", En = "Aim mode",
                HelpPt = "SEGURAR mira enquanto o botão estiver pressionado; ALTERNAR liga e desliga a mira a cada toque.",
                HelpEn = "HOLD aims while the button is held; TOGGLE flips aiming on each press.",
                Choices = new[] { new Choice("0", "SEGURAR", "HOLD"), new Choice("1", "ALTERNAR", "TOGGLE") },
            });
            s.Add(game);

            // -------------------------------------------------------------- CONTROLES ---
            var controls = new Section { Id = "controls", HasBindings = true, HasPad = true };
            controls.Pt = "CONTROLES"; controls.En = "CONTROLS";
            controls.PadPt = "JOYSTICK DE XBOX"; controls.PadEn = "XBOX CONTROLLER";
            controls.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "mouse_sens", Default = "2.000", Min = 0.25, Max = 6.5, Step = 0.25, Format = "0.00",
                Pt = "Sensibilidade do mouse", En = "Mouse sensitivity",
                HelpPt = "Quanto a visão gira com o mouse. 2.00 é o padrão do port.",
                HelpEn = "How far the view turns with the mouse. 2.00 is the port's default.",
            });
            controls.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "mouse_invert", Default = "0",
                Pt = "Inverter mouse", En = "Invert mouse",
                HelpPt = "Inverte o eixo vertical do mouse.",
                HelpEn = "Flips the mouse's vertical axis.",
            });
            // The Xbox pad (the port's own PC-side settings, black.ini only: the game's menus have no room for them)
            controls.PadItems.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "pad_deadzone", Default = "0", Min = 0, Max = 50, Step = 5, Format = "0", Suffix = "%",
                Pt = "Zona morta dos analógicos", En = "Stick dead zone",
                HelpPt = "Ignora o tremor dos analógicos perto do centro. 0% deixa como está; suba se o personagem andar ou mirar sozinho.",
                HelpEn = "Ignores the sticks' jitter near the centre. 0% leaves them as they are; raise it if the character walks or aims by itself.",
            });
            controls.PadItems.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "pad_sens", Default = "100", Min = 25, Max = 300, Step = 25, Format = "0", Suffix = "%",
                Pt = "Sensibilidade do joystick", En = "Stick sensitivity",
                HelpPt = "Quanto a visão gira com o analógico direito. 100% é a velocidade do próprio jogo.",
                HelpEn = "How far the view turns with the right stick. 100% is the game's own speed.",
            });
            controls.PadItems.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "pad_vibration", Default = "1",
                Pt = "Vibração", En = "Vibration",
                HelpPt = "Deixa o joystick vibrar quando o jogo pede. Desligado, o jogo não treme o controle.",
                HelpEn = "Lets the pad rumble when the game asks. Off, the controller never shakes.",
            });
            controls.PadItems.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "pad_invert", Default = "0",
                Pt = "Inverter a visão do joystick", En = "Invert the stick's look",
                HelpPt = "Inverte o eixo vertical do analógico direito (o jogo tem a mesma opção: INVERTER VISÃO).",
                HelpEn = "Flips the right stick's vertical axis (the game's own INVERT LOOK option does the same).",
            });
            s.Add(controls);

            // ------------------------------------------------------------------ MODS ---
            var mods = new Section { Id = "mods", HasMods = true };
            mods.Pt = "MODS"; mods.En = "MODS";
            mods.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "hud169", Default = "1",
                Pt = "HUD 16:9", En = "HUD 16:9",
                HelpPt = "Ajusta a barra de vida e a munição para a imagem widescreen, sem esticar.",
                HelpEn = "Fits the health bar and ammo to the widescreen picture, unstretched.",
            });
            mods.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "hudfade", Default = "1",
                Pt = "HUD que some", En = "Fading HUD",
                HelpPt = "Esconde o HUD quando o jogador não está fazendo nada, e o traz de volta na ação.",
                HelpEn = "Hides the HUD while nothing is happening, and brings it back on action.",
            });
            mods.Items.Add(new SettingItem
            {
                Kind = ItemKind.HdTextures, Key = "hd_textures", Default = "",
                Pt = "Texturas HD", En = "HD textures",
                HelpPt = "Usa o pacote de texturas em alta resolução que vem no modloader (mods\\hdtextures\\hd).",
                HelpEn = "Uses the high-resolution texture pack that ships with the modloader (mods\\hdtextures\\hd).",
            });
            mods.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "trainer", Default = "0",
                Pt = "Trapaças (F5)", En = "Cheats (F5)",
                HelpPt = "Liga o menu de trapaças dentro do jogo: F5 abre e fecha, o mouse fica livre para clicar e as " +
                         "trapaças valem só nesta sessão. São elas: munição infinita, granada infinita, imortalidade e " +
                         "a troca de arma na mão.",
                HelpEn = "Turns on the in-game cheat menu: F5 opens and closes it, the mouse is freed to click, and " +
                         "the cheats last for this session only. They are: infinite ammo, infinite grenades, god mode " +
                         "and swapping the weapon in hand.",
            });
            s.Add(mods);

            // -------------------------------------------------------------- AVANÇADO ---
            var adv = new Section { Id = "advanced", HasAdvanced = true };
            adv.Pt = "AVANÇADO"; adv.En = "ADVANCED";
            // A launcher preference (launcher.ini), not a black.ini key: it is saved as soon as it is toggled.
            adv.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "update_auto", Default = "1", Pref = true,
                Pt = "Procurar atualização automaticamente", En = "Check for updates automatically",
                HelpPt = "Toda vez que o launcher abre, procura uma versão nova no GitHub. Se encontrar, mostra a janela de atualização. " +
                         "Desligado, use o botão PROCURAR ATUALIZAÇÃO no rodapé.",
                HelpEn = "Every time the launcher opens, it checks GitHub for a newer version. If it finds one, it shows the update window. " +
                         "Off, use the CHECK FOR UPDATES button in the footer.",
            });
            adv.Items.Add(new SettingItem
            {
                Kind = ItemKind.Text, Key = "hd_textures", Default = "", Browse = true,
                Pt = "Pasta das texturas HD", En = "HD textures folder",
                HelpPt = "Caminho da pasta do pacote (a que tem map.csv). Padrão: mods\\hdtextures\\hd. " +
                         "Use TROCAR PASTA para escolher outro map.csv. Vazio desliga as texturas HD.",
                HelpEn = "The pack folder's path (the one with map.csv). Default: mods\\hdtextures\\hd. " +
                         "Use CHANGE FOLDER to choose another map.csv. Empty turns HD textures off.",
            });
            adv.Items.Add(new SettingItem
            {
                Kind = ItemKind.Slider, Key = "hudfade_hold", Default = "2.50", Min = 0.0, Max = 30.0, Step = 0.5, Format = "0.0",
                Pt = "Segundos antes de sumir (HUD)", En = "Seconds before the HUD fades",
                HelpPt = "Quanto tempo de calma antes do HUD começar a sumir.",
                HelpEn = "How long the calm lasts before the HUD starts to fade.",
            });
            adv.Items.Add(new SettingItem
            {
                Kind = ItemKind.Bool, Key = "hudfade_crosshair", Default = "0",
                Pt = "Sumir também a mira", En = "Fade the crosshair too",
                HelpPt = "Faz a mira sumir junto com o HUD. Por padrão ela fica.",
                HelpEn = "Fades the crosshair along with the HUD. By default it stays.",
            });
            s.Add(adv);

            return s;
        }
    }
}
