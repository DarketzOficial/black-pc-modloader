# ModLoader 1.4.19

Data: 23/10/2026
Tag: `1.4.19`
Nome da release: **ModLoader 1.4.19**

Pacote para o port **0.2.1**: nova opcao **Textos e objetivos 16:9**, separada do HUD 16:9,
para as dicas, os avisos e os textos da gameplay. Sem mudanca no port.

## Textos e objetivos 16:9 (novo)

- A opcao fica na aba **MODS** do launcher, logo **abaixo de HUD 16:9**, e vem **desligada**.
  Cada uma tem o seu proprio ON/OFF: ligar os textos nao mexe nos paineis de vida/municao e
  vice-versa.
- Chave no `black.ini`: `text169` (`0` = desligado, padrao). A linha de comando aceita
  `--text169` / `--no-text169`.
- As dicas de tutorial, os avisos e os textos da gameplay passam a ficar na proporcao certa,
  sem a fonte esticada.
- **Metodo:** um unico mapa centrado para a camada de texto, sem a tabela de ancoras por
  componente (que marcava texto centrado como esquerda/direita):

  ```text
  x' = 320 + (x - 320) * (4/3) / aspect      (0,750 em 16:9)
  ```

  Letras, sombras/zoom e a linha do objetivo passam pelo mesmo mapa, entao nada e recortado,
  deslocado ou duplicado. O `hud169` roda primeiro; o `text169` so age no que o HUD nao pegou.
- Ficam de fora: passes 2D de tela cheia (fades/filmes), as caixas dos paineis de vida/municao
  e a caixa da mira. Texto nao e forcado para filtro linear (fonte nitida).
- Menus, pause e o briefing da missao continuam no layout largo do proprio jogo.
- Vale a partir do proximo inicio do jogo (o `black.ini` e lido no boot).

Testado no port 0.2.1 (1280x720, `level_04`, dicas de tutorial 12 e 13 pelo comando de teste
`hint:N`): desligado, a dica "PRESSIONE ... KITS DE PRIMEIROS SOCORROS" ocupa x 236..1048
(esticada); ligado, x 337..944 (largura 0,748, centro 640) com os icones entre as palavras.
Com `--no-hud169 --text169` os paineis ficam como o jogo desenha (largos) e a dica continua
estreitada, confirmando que as opcoes sao independentes. O save real nao foi alterado.

## Linux

- A opcao **Textos e objetivos 16:9** tambem esta no launcher Linux, abaixo de HUD 16:9.
- Launcher nativo em Python/Tkinter; o jogo e o `blackrt.dll` (Win32) rodam pelo Wine, com
  suporte 32 bits.
- Requisitos: `python3`, `python3-tk`, `wine`/`wine32`.

## Arquivos e hashes (SHA-256)

- `blackrt.dll` do port **0.2.1** (mesmo hash na raiz e em `dist\modloader`):
  `C366C9A75327E887DF039B74B2D092A3093D1B1F9A9AEC5F309F5424DB863219`
- `blacklauncher.exe` (1.4.19):
  `16198D7DD26777FF3027892908292F7FF542F5EA4309A63A94B2419061C388C8`
- `BLACK-PC-Modloader-1.4.19.zip` (atualizador do launcher):
  `a4f2e499647a5fc6fa953cf87bc261482dca694d7179fa7c0eb58ef04e1581d6`
- `BLACK-PC-Modloader-1.4.19-com-mods.zip` (pacote completo com mods):
  `adaf169e6b469e07f179209d845cd182fadafc3e8e227264d3c50679ea09c954`
- `BLACK-PC-Modloader-1.4.19-linux.zip`:
  `bab5814615779bd8af5e538856e9dc2f859187f3f60f09c28f6e051fef49706b`
- `BLACK-PC-Modloader-1.4.19-linux-com-mods.zip`:
  `048f2da1374f6efee1d237f8ad1175a699b7455125017d8c3f2251f82b15f097`
- `SHA256SUMS-1.4.19.txt` e `SHA256SUMS-1.4.19-linux.txt`
- `BLACK-PC-ModKit-1.0.0.zip` (sem mudanca):
  `b1800a453d3d2e4c8face4f42154e6d926a8e5d1514fdfbe396b5c24f0bbaf8f`

## Atualizacao

A atualizacao nao toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`
nem `log\`. O launcher 1.4.18 encontra a 1.4.19 pelo proprio atualizador (PROCURAR
ATUALIZACAO) ou pelos assets da release.
