# ModLoader 1.4.21

Data: 25/10/2026
Tag: `1.4.21`
Nome da release: **ModLoader 1.4.21**

Pacote para o port **0.2.1**: menus 21:9 consertados, icones de botao
automaticos, letras com acento, correcao de 3 crashes dos logs dos jogadores e
texturas HD de 53 para 62 pares.

## Menus 21:9

- Title, loading, menu principal, opcoes e CONF. PC: caixa 16:9 centralizada,
  sem moldura duplicada nem ornamento deslocado; as laterais levam a cor da
  borda da pagina (degrade escurecido), sem esticar o menu.
- Pause e briefing dentro da missao continuam sobre o mundo 21:9.

## Icones de botao automaticos

- Com teclado/mouse, os icones do Xbox nos prompts viram keycaps com as teclas
  vinculadas (as duas, ex. `E`, `M1`, `W-`); o cursor do port some da tela
  quando um controle esta em uso.
- Controle Xbox: icones originais. Controle PlayStation (DualSense/DualShock):
  faces × ○ □ △, setas no D-pad e options no START. A troca e automatica,
  pelo dispositivo que se moveu por ultimo, sem reiniciar.

## Letras com acento

- ã õ Ã Õ nao tem glifo nas fontes e sumiam ("estão" virava "esto"). Agora
  renderizam como ä ö Ä Ö (o mesmo estilo do Ä/Ö que a traducao ja usa).

## Crashes

- `wbinvd` no boot, helper de HUD/arma (0x13AE3) e setup de audio (0x8DF7B)
  dos logs enviados pelos jogadores: guardas instaladas, sem fechar o jogo.

## Texturas HD

- Novo casamento sobre o dump atual (216 texturas): 61 pares + 1 resgatado =
  62 no mapa (era 53). O launcher mostra 62/216.

## Teste

- Menus 21:9 contra 16:9 na mesma cena (pagina pixel-identica, sem derrame nas
  laterais); keycaps `X`/`R` no tutorial do kit medico e faces ×/seta do
  PlayStation com o conjunto forcado; instalacao das guardas no log, sem
  regressao no boot, no jogo nem no save real.

## Arquivos e hashes (SHA-256)

- `blackrt.dll` do port **0.2.1** (mesmo hash na raiz e em `dist\modloader`):
  `360C84FEFED9F1A14BC44D964E74F9A793B9EF178864AF194C95D975C5B33059`
- `blacklauncher.exe` (1.4.21):
  `753AF5FC144DAD07F257290548D6FBFAD6695A388B292F4C3D4E238A3978D49F`
- `BLACK-PC-Modloader-1.4.21.zip` (atualizador do launcher):
  `8F808ABD984EF9F0EABBFB89AF144C9159FB6E9C66042A37DD337FCFAE8F7A99`
- `BLACK-PC-Modloader-1.4.21-com-mods.zip` (pacote completo com mods):
  `3DC329A9C3CFECB07EC24A806152E9F50E4C6C38054A31534538DC2AFE8C27A5`
- `BLACK-PC-Modloader-1.4.21-linux.zip`:
  `F6882C111C06A64101E405413DC4B44DAFD99405EA1E877B8A6D6BEE28ACF605`
- `BLACK-PC-Modloader-1.4.21-linux-com-mods.zip`:
  `8581911B034FF72F7F15FD83CD43A787964C89B139A063FF0188997CDA0F97F1`
- `SHA256SUMS-1.4.21.txt` e `SHA256SUMS-1.4.21-linux.txt`
- `BLACK-PC-ModKit-1.0.0.zip` (sem mudanca):
  `b1800a453d3d2e4c8face4f42154e6d926a8e5d1514fdfbe396b5c24f0bbaf8f`

## Atualizacao

A atualizacao nao toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`
nem `log\`.
