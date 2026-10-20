# BLACK PC — ModLoader

**ModLoader 1.4.10** para o **[BLACK PC port](https://github.com/Spark198rus/black-pc)** de
[Spark198rus](https://github.com/Spark198rus/black-pc).

O projeto é o port do **BLACK** (Xbox NTSC-U) para Windows com uma camada de mods e um
launcher próprio. O jogo **não** vem no pacote: você precisa do seu dump em `game\`.

---

## Compatibilidade

| | |
|---|---|
| **Funciona** | BLACK PC port do Spark198rus — https://github.com/Spark198rus/black-pc |
| **Não funciona** | BLACK-XBERecomp do TGP482 (projeto diferente, sem `blackrt.dll`) |
| **Versão do port** | 0.2.0 |
| **Jogo** | BLACK Xbox NTSC-U com dump em `game\` (o pacote não inclui o jogo) |
| **Sistema** | Windows 10/11, 32 ou 64 bits |
| **Hardware mínimo** | 4 GB de RAM, 1 GB de VRAM (Direct3D 9), 5 GB livres |

O `blackrt.dll` deste projeto substitui o DLL do port. Não misture os arquivos com o
BLACK-XBERecomp.

---

## Download e instalação

1. Baixe o [BLACK PC port](https://github.com/Spark198rus/black-pc) e deixe o jogo em
   `game\`, ao lado de `black.exe`:

   ```
   BLACK\
     black.exe
     blackrt.dll
     game\           default.xbe + arquivos do jogo
     savedata\
   ```

2. Baixe na aba [Releases](../../releases):
   - **`BLACK-PC-Modloader-1.4.10.zip`** — loader/base para instalar;
   - os pacotes de mods desejados (`mod-ptbr`, `mod-hdtextures`, etc.);
   - **`black-pc-modloader.zip`** é o pacote pequeno usado pelo **atualizador** depois
     que o launcher já está instalado.

   O pacote `BLACK-PC-Modloader-1.4.10-com-mods.zip` é só para testes: ele já contém o
   loader e todos os mods, sem o jogo.

3. Feche o jogo. Extraia o conteúdo **dentro da pasta do port** (a que tem `black.exe`),
   confirmando a substituição de `blackrt.dll`.

4. Abra **`blacklauncher.exe`** (ou o atalho `BLACK`). Na primeira abertura, o launcher
   mostra as novidades desta versão; depois use **AVANÇADO > Informações da atualização**
   para rever.

5. Escolha os ajustes/mods e clique em **JOGAR**. O launcher inicia o jogo por um
   observador escondido que cuida dos logs e do relatório em caso de erro.

> Guarde uma cópia do seu `blackrt.dll` antigo. O pacote também traz o
> `uninstaller-modloader.exe`, que apaga a pasta `mods` e restaura o DLL anterior
> (`blackrt.dll.antes-do-modloader` ou `blackrt.dll.original-do-port`).
> `savedata\` e `black.ini` não são tocados.

---

## Novidades da 1.4.10

### Relatório de erro e logs

- Quando o jogo falha, o launcher mostra uma janela no mesmo visual do launcher, com
  **ENVIAR** e **NÃO ENVIAR**.
- **ENVIAR** manda para o canal `#logs-black` no Discord:
  - o `black-crash-*.log` como anexo;
  - versão do ModLoader e do port;
  - Windows, CPU, GPU, RAM e tela;
  - usuário e máquina do Windows;
  - menu onde o jogo estava, mods ativos e ajustes do `black.ini`.
- Nada é enviado sem o clique. O histórico de tentativas fica em
  `log\launcher\reports.log`.
- `black.log` e `black-crash-*.log` saem da raiz para `log\game\` e `log\crash\`; a raiz
  fica limpa.
- Se o jogador abrir `black.exe` direto e o jogo falhar, o próximo início do launcher
  encontra o crash log na raiz e mostra a mesma janela uma vez.

### Tela de novidades (changelog)

- Depois de uma atualização, o launcher mostra as notas da versão **uma vez** no primeiro
  início.
- O controle fica em `launcher.ini` na preferência `changelog_seen`.
- Para rever: **AVANÇADO > Informações da atualização**.
- O texto é organizado em cartões e tem rolagem quando necessário.

### Pasta das texturas HD

- O caminho padrão é `mods\hdtextures\hd`.
- **TROCAR PASTA** abre o seletor nativo do Windows na pasta atual e procura `map.csv`.
- O **X** ao lado limpa o caminho; campo vazio desliga as texturas HD.
- O caminho é salvo no `black.ini` ao clicar em **SALVAR**.

### Apoio ao projeto

Depois dos ícones do YouTube e Discord, o botão **DOAR/DONATE** abre a tela de apoio:

- QR code do **PIX**;
- **LivePix**: https://livepix.gg/darketz;
- Bitcoin, Dogecoin, Ethereum e USDT, com botão de copiar.

O projeto continua gratuito. A tela também explica que os recursos usados no
desenvolvimento são pagos, incluindo o uso de IA como o Claude Code.

### Correções

- Corrigido o crash ao descarregar uma fase: o código FVF `0x444` podia ser tratado como
  um ponteiro de vertex shader; agora handles FVF/inválidos são ignorados.
- A tela do port no jogo voltou a se chamar **MODS** (não mais `IMAGE`). Com o mod PT-BR,
  o título continua **CONF. PC**.
- `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\` e `log\` não são alterados
  pelas atualizações.

### Recursos anteriores mantidos

- HUD 16:9 e FADING HUD.
- Filtros de imagem: SHARPENING, DETAILS, CLARITY e VIBRANCE (0–100).
- Página de imagem no jogo e aba **GRÁFICOS** no launcher.
- FOV da arma limitado a 80.
- Mouse nos menus, FPS, legendas, pular vídeos e trainer (F5).
- Atualização automática pelo GitHub.
- Preservação das configurações e dos mods durante a atualização.

---

## Launcher

O launcher organiza as opções em **VÍDEO, GRÁFICOS, DESEMPENHO, ÁUDIO E JOGO,
CONTROLES, MODS e AVANÇADO**.

- Todos os ajustes têm um **?** com explicação.
- **SALVAR** grava o `black.ini`; mudanças não salvas são avisadas.
- **RESTAURAR** volta aos padrões de uma instalação nova.
- **JOGAR** inicia o jogo e o observador de logs.
- **PROCURAR ATUALIZAÇÃO** consulta as releases do GitHub.
- **DOAR** abre a tela de apoio.
- O launcher tem idioma PT/EN em `launcher.ini`.

### Mods

Os mods ficam em `mods\<nome>` e aparecem na aba **MODS** do launcher, com ON/OFF.
O port entrega o arquivo do mod no lugar do arquivo do jogo, sem alterar o jogo.

Os mods de arquivo distribuídos nos Releases são:

- **PT-BR TEXT + KEYBOARD** — tradução, teclas e vídeos legendados;
- **XBOX ICONS AS KEYS** — ícones dos botões como teclas;
- **KEYBOARD PROMPTS (EN)** — prompts em inglês;
- **HD TEXTURES** — pacote de texturas em alta resolução.

O mod de idioma de maior prioridade define o idioma da sessão; mods de outro idioma ficam
bloqueados (`OFF (IDIOMA)`). Por isso, para jogar em PT-BR, o **PT-BR TEXT + KEYBOARD**
precisa estar ligado; o mod de prompts em inglês não atrapalha.

Mais detalhes: [`docs/MODS.md`](docs/MODS.md).

---

## Atualizações

O launcher consulta o GitHub em busca da release mais nova e baixa o asset
`black-pc-modloader.zip`. O helper:

- copia o launcher, o `blackrt.dll` e o `LEIA-ME.txt`;
- **não copia** `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\` nem `log\`;
- reinicia o launcher ao terminar.

A release atual é **tag `1.4.10`**, nome **`ModLoader 1.4.10`**. As notas estão em
[`docs/RELEASE-NOTES-1.4.10.md`](docs/RELEASE-NOTES-1.4.10.md).

---

## Logs e suporte

```
log\
  game\       black-AAAAMMDD-HHMMSS.log (uma partida por arquivo)
  crash\      black-crash-*.log e relatórios de falha de início
  launcher\   reports.log e, depois de uma atualização, changelog-<versao>.txt
```

O log completo pode ser anexado manualmente no Discord se o envio automático falhar.
Nunca envie sua pasta `savedata\` ou o jogo.

---

## Compilando

### Port (C++ / Windows x86)

Requisitos: Visual Studio 2026 (ou 2022) Build Tools com C++ x86 e CMake 3.24+.

```bat
cmake --preset win32
cmake --build build/win32 --config RelWithDebInfo
```

Saída: `build\win32\RelWithDebInfo\black.exe` e `blackrt.dll`. Detalhes em
[`docs/BUILDING.md`](docs/BUILDING.md).

### Launcher (WPF / .NET Framework 4.8)

```powershell
cd launcher
.\build.ps1 -Deploy
```

O projeto está em `launcher\` e usa `Assets\logo.png`, `Assets\qrcode.png` e o tema do
launcher. O `build.ps1 -Deploy` copia o `blacklauncher.exe` para a pasta do port.

---

## Estrutura do repositório

| Pasta/arquivo | O que é |
|---|---|
| `src\` | código do port (HLE do Xbox, D3D, áudio, input, front end, mods) |
| `stub\` | o `black.exe` mínimo que carrega o XBE e chama `blackrt.dll` |
| `launcher\` | código-fonte do launcher WPF e seus assets |
| `docs\` | documentação, notas de versão e guia de mods |
| `CMakeLists.txt` / `CMakePresets.json` | build do port |
| `LICENSE` | GPL-2.0-or-later |

O jogo, as texturas, os vídeos traduzidos e os binários não ficam neste repositório; os
pacotes prontos ficam nos Releases.

---

## Solução de problemas

- **A tela do jogo está em inglês:** ligue **PT-BR TEXT + KEYBOARD** na aba MODS do
  launcher. Se o mod de prompts em inglês estiver ligado e o PT-BR desligado, a sessão
  fica em inglês.
- **O jogo fechou com erro:** veja `log\crash\` ou use o relatório do launcher.
- **HD TEXTURES não aparece:** confira `mods\hdtextures\hd\map.csv` e o caminho em
  **AVANÇADO > Pasta das texturas HD**.
- **O launcher não inicia o jogo:** confirme `black.exe`, `blackrt.dll` e
  `game\default.xbe` na mesma pasta.
- **Atualização não aparece:** a release precisa ter tag de versão (ex.: `1.4.10`) e o
  asset `black-pc-modloader.zip`; releases marcadas como draft/prerelease são ignoradas.

---

## Créditos

- **Darketz** — modloader, launcher, tela MODS, mouse nos menus, HUD 16:9, FADING HUD,
  filtros de imagem, trainer, texturas HD, tradução/adaptação PT-BR da versão PC e
  empacotamento.
  YouTube: https://www.youtube.com/c/Darketz
  Discord: https://discord.gg/HKpgA9ghVE
- **Spark198rus** — port original do BLACK para PC.
- **Bruno Fernando, Rayne Games, André Romhacker e HNNEWGAMES** — tradução PT-BR original
  (2018/2019).
- **BLACK HD Reimagined** — pack de texturas HD para PCSX2 (SLUS-21376).
- Desenvolvido com **IA (Claude Code)** como assistente, **Visual Studio 2026**
  (MSVC, C++20, CMake) e WPF/.NET.

**BLACK** é marca da Electronic Arts. O projeto não inclui o jogo nem arquivos do jogo;
você precisa ter o BLACK original (Xbox NTSC-U).

Licença: **GPL-2.0-or-later** (veja [`LICENSE`](LICENSE)).
