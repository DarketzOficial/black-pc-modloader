<!-- Texto pronto para colar no corpo do Release do GitHub (tag: modloader-1.0). -->

# BLACK PC — Modloader 1.0

**Modloader para o BLACK PC port** (de [Spark198rus](https://github.com/Spark198rus/black-pc)). O download abaixo é o
**único arquivo** do release: `BLACK-PC-Modloader-1.0.zip`, com o **modloader + todos os mods** já inclusos.

> **Compatibilidade**
> - ✅ **Só funciona** no **BLACK PC port** do **Spark198rus**: https://github.com/Spark198rus/black-pc
> - ❌ **Não é compatível** com o **BLACK-XBERecomp** do **TGP482**: https://github.com/TGP482/BLACK-XBERecomp
> - **Modloader 1.0** funciona no **BLACK PC port 0.2.0**.

## Instalação (só extrair)

1. Tenha o **BLACK PC port do Spark198rus** (link acima), com o **jogo já com dump dentro da pasta `game`** do port.
   O modloader **não inclui o jogo**.
2. **Feche o jogo.**
3. Extraia o `BLACK-PC-Modloader-1.0.zip` **na pasta do port** (a que tem o `black.exe`), confirmando a substituição do
   `blackrt.dll`.
4. Abra o `black.exe`. Os mods já vêm ligados.

Para ligar/desligar qualquer mod: **`OPÇÕES > CONF. PC > MODS`** (menu) ou **`pausa (Esc) > CONF. PC > MODS`** (jogo).

> O ZIP também traz o `uninstaller-modloader.exe`: dois cliques e ele apaga a pasta `mods` e volta o `blackrt.dll`
> anterior. Suas partidas (`savedata`) e o `black.ini` não são tocados.

## O que vem no pacote

| Mod / opção | O que faz |
|---|---|
| **HD TEXTURES** | Texturas HD do pack *BLACK HD Reimagined*, com a porcentagem pronta na linha: **`ON 34%`** |
| **HUD 16:9** | Barras de vida/munição proporcionais nos cantos, sem o esticamento do 4:3 |
| **FADING HUD** | Os painéis de vida/munição somem com fade quando você fica um tempo sem ação |
| **PT-BR TEXT + KEYBOARD** | Tradução PT-BR + textos com as teclas do teclado + vídeos legendados |
| **XBOX ICONS AS KEYS** | Ícones A/B/seta dos menus viram teclas do PC (Enter/Bksp/seta) |
| **KEYBOARD PROMPTS (EN)** | Botões do Xbox viram teclas do teclado, em inglês (só com o jogo em inglês) |

Também fazem parte do modloader: **mouse nos menus** (passar o mouse destaca a linha, o botão esquerdo confirma e em
barras o valor acompanha o ponto clicado, o direito volta, a roda anda como as setas), **contador de FPS**, aviso
**"Pular video"** nos filmes (com fade), legendas `.srt` por mod, e as opções do port (campo de visão, resolução, brilho,
gama, reverb, mouse look, HUD…).

## Padrões da primeira instalação

Na primeira execução (sem `black.ini`), o modloader já vem com: **pular vídeos**, **HUD 16:9**, **FADING HUD**,
**HD TEXTURES**, **XBOX ICONS AS KEYS** e **PT-BR TEXT + KEYBOARD** ligados; **andar a 25%**; **campo de visão 120**;
**campo de visão da arma 92**. Depois da primeira mudança, o `black.ini` guarda as suas escolhas.

## Requisitos

| | |
|---|---|
| **Sistema** | Windows 10 **32 ou 64 bits** |
| **Memória** | **4 GB** de RAM |
| **Vídeo** | **1 GB** de VRAM (Direct3D 9) |
| **Armazenamento** | **5 GB** livres |
| **Port** | BLACK PC port do Spark198rus (0.2.0) |
| **Jogo** | BLACK (Xbox NTSC-U) com **dump em `game\`** — o modloader não inclui o jogo |

Feito para rodar em **computadores de configuração mais fraca**: o port leva o jogo do Xbox ao PC sem exigir hardware
atual, e o modloader não acrescenta custo de desempenho.

## Fazendo seus mods

Qualquer pessoa pode fazer mods: é só criar uma pasta em `mods\` com os arquivos que substituem os do jogo, um
`mod.ini` opcional (`name`, `priority`, `lang`) e, se quiser, legendas em `subtitles\`. O formato completo está em
**`docs/MODS.md`**, no repositório. O **código-fonte do modloader** também está no repositório
(`https://github.com/DarketzOficial/black-pc-modloader`, GPL-2.0-or-later).

## Créditos

- **Darketz** — modloader, tela MODS, mouse nos menus, HUD 16:9, FADING HUD, texturas HD, legendas, FPS, aviso de
  pular vídeo, mods e adaptação PT-BR da versão PC. ▶️ https://www.youtube.com/c/Darketz
- **Spark198rus** — port original do BLACK para PC (base do modloader). https://github.com/Spark198rus/black-pc
- **Bruno Fernando, Rayne Games, André Romhacker e HNNEWGAMES** — tradução PT-BR original (2018/2019).
- **BLACK HD Reimagined** — pack de texturas HD do BLACK para PCSX2 (SLUS-21376).
- Desenvolvido com **IA (Claude Code)** como assistente, **Visual Studio 2026** (MSVC, C++20, CMake), em **C++20**
  (loader/modloader), **ActionScript 2** (páginas do front end) e **Python** (ferramentas de casamento/patch).

**BLACK** é marca da **Electronic Arts**. O pacote não inclui o jogo nem arquivos do jogo — é preciso ter o BLACK
(Xbox NTSC-U) com dump e o port do Spark198rus.

## Acompanhe

- ▶️ YouTube: https://www.youtube.com/c/Darketz
- 💬 Discord: https://discord.gg/HKpgA9ghVE
- 🎮 Port: https://github.com/Spark198rus/black-pc
