# ModLoader 1.4.20

Data: 23/10/2026
Tag: `1.4.20`
Nome da release: **ModLoader 1.4.20**

Pacote para o port **0.2.1**: correcao das legendas dos filmes (o numero do bloco do
`.srt` aparecia embaixo da fala). Sem mudanca no jogo nem nas opcoes da 1.4.19.

## Correcao

- O arquivo de legenda (`.srt`) tem um numero de bloco antes de cada fala. O leitor
  do port pulava apenas o numero do primeiro bloco; os seguintes entravam como texto
  e apareciam como uma linha extra embaixo da fala anterior (`2` depois da fala 1,
  `4` depois da fala 3 -- reportado pelo jogador em `03_N`).
- O leitor agora ignora qualquer linha sem `-->` antes do proximo bloco e encerra o
  texto no branco ou no numero seguinte (funciona tambem em `.srt` sem linha em
  branco separadora). O BOM UTF-8 e descartado.
- Nada muda no texto das falas nem na contagem: os arquivos do pacote pt-BR carregam
  com as mesmas falas (35/18/26/22/22/17/13/4/23).

## Teste

Filme `11_N` a 1280x720, com `--no-movie-skip` (com o skip padrao uma tecla enfileirada
corta o filme antes das legendas): antes, a fala 1 saia com um `2` na linha de baixo e
a fala 3 saia com um `4`; depois, as duas saem limpas ("Sargento Kellar: Voce e um
mentiroso!"). Save real sem alteracao.

## Mantido da 1.4.19

- **Textos e objetivos 16:9** na aba MODS, abaixo de HUD 16:9 (independente, desligado
  por padrao).
- HUD 16:9 so em vida/municao; menus, pause e briefing no layout do proprio jogo.

## Linux

- Launcher **nativo** em Python/Tkinter; o jogo e o `blackrt.dll` (Win32) rodam pelo
  Wine, com suporte 32 bits.
- Requisitos: `python3`, `python3-tk`, `wine`/`wine32`.
- `BLACK-PC-Modloader-1.4.20-linux.zip`: launcher nativo + `blackrt.dll` + `languages`.
- `BLACK-PC-Modloader-1.4.20-linux-com-mods.zip`: o mesmo + `mods` oficiais.
- `SHA256SUMS-1.4.20-linux.txt`.

## Arquivos e hashes (SHA-256)

- `blackrt.dll` do port **0.2.1** (mesmo hash na raiz e em `dist\modloader`):
  `779C0E8E9AD3A5964105B8D02D199BE8F1ABF14A1CC89797C1A451E857E1C644`
- `blacklauncher.exe` (1.4.20):
  `7A2F2CADA9E4E96CCC6DA382B7B4EA56F7E3915CF786A78DBA6D9ED8F70D2F0C`
- `BLACK-PC-Modloader-1.4.20.zip` (atualizador do launcher):
  `b8aa9b80b27a2a1ae296f8760476b244b53624a3b2a5d3d191a1f2789b8b03c2`
- `BLACK-PC-Modloader-1.4.20-com-mods.zip` (pacote completo com mods):
  `68359e84b1835164fb539914cabb9121e80f42f21ed56d2d0743c33542dbb98f`
- `BLACK-PC-Modloader-1.4.20-linux.zip`:
  `72189ecb7bba493ad8df2da463d490d3a3d9d4b77eef4954e8d47b928eb0815d`
- `BLACK-PC-Modloader-1.4.20-linux-com-mods.zip`:
  `08fea7fd6840be09bcd9c13577e08244fd1ea82f0a8e367b6b9697d08552437f`
- `SHA256SUMS-1.4.20.txt` e `SHA256SUMS-1.4.20-linux.txt`
- `BLACK-PC-ModKit-1.0.0.zip` (sem mudanca):
  `b1800a453d3d2e4c8face4f42154e6d926a8e5d1514fdfbe396b5c24f0bbaf8f`

## Atualizacao

A atualizacao nao toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`
nem `log\`.
