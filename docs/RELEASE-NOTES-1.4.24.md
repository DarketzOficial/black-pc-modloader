# ModLoader 1.4.24

## Botões do HUD (keycaps)

- As células do atlas de fonte estavam fora do lugar: o retângulo do D-pad invadia a
  fileira de letras (pintava "W-" em cima da palavra SWAP) e o do X começava no meio do
  ícone, deixando o círculo azul do jogo aparecendo e o keycap deslocado. As nove
  células agora são as caixas reais de cada arte, medidas no atlas do jogo (B, A, Y, X,
  START e os quatro do D-pad).
- Conferido no jogo: os avisos saem limpos ("PULL [R] TO FIRE WEAPON"), sem círculo
  sobrando e sem lixo em cima das letras.

## Pausa, CONF. PC e menus em 21:9

- As páginas da pausa (`pausemenu`, **CONF. PC** e as opções da pausa) entram na mesma
  caixa 16:9 das telas antes do jogo; o mundo atrás continua no quadro 21:9.
- O **CONF. PC** da pausa vinha sem as linhas (VIDEO / DISPLAY / AUDIO AND GAME /
  CONTROLS / MODS): a tabela de valores do port enchia antes de chegar nelas.
- A pausa monta as linhas em ~10 quadros (antes eram ~30).
- A caixa 21:9 só vale fora do gameplay: em jogo ela encolhia o quadro inteiro (barras
  laterais erradas) e derrubava o fps pela metade.
- O contador de FPS fica no canto superior esquerdo do quadro, inclusive na pausa.

## Áudio do jogo (dublagem PT-BR)

- Nova linha **ÁUDIO DO JOGO (MRGEEH)** em AUDIO E JOGO, logo abaixo do idioma:
  **ORIGINAL (inglês)** ou **PORTUGUÊS (Brasil)**, a dublagem por I.A. do **MrGeeH**
  (https://github.com/MrGeeH).
- A dublagem vem no pacote com mods, em `mods\dublagem-ptbr`, só o áudio (as falas do
  jogo e das cutscenes no codec do Xbox). A troca vale na hora: o jogo reabre os
  streams de áudio. O texto e as fontes continuam vindo do pacote de idioma.
- **LEGENDAS** fica logo abaixo.

## Launcher

- **Confere o `game\default.xbe` antes de abrir o jogo**: compara o SHA-256 do arquivo
  e, quando não bate, o da seção executável (outro dump do mesmo build continua
  passando). Outro build aparece como **JOGO INCOMPATÍVEL**, com o motivo na barra de
  status, e o **JOGAR fica desabilitado** — em vez de abrir o jogo, ver o aviso dele e
  depois um relatório de crash.
- **Linux/Steam Deck**: procura o comando do Wine (`launcher-linux/launcher.ini`,
  `LAUNCHER_WINE`, PATH, `/usr/bin`, `/usr/local/bin`, `/usr/games`, `/snap/bin`) e,
  quando não existe, mostra **"Wine não encontrado…"** com o que instalar e o JOGAR
  desabilitado. Também confere se a pasta do port existe (cartão SD desmontado).
- O launcher se identifica como **1.4.24**.

## Port 0.2.1

- XBE de outro build: mensagem clara com o código encontrado e o esperado (e a região e
  a versão do XBE no `black.log`); o port sai com **código 3**, então o launcher não
  abre relatório de crash em cima do aviso.
- Guards novos a partir dos logs dos jogadores:
  - divisor zero no tamanho do stream (banco de fala vazio/ilegível: 3 relatos em
    `level_00`, logo depois de abrir `spch_en.bks`) — o stream fica mudo em vez de
    derrubar o jogo;
  - modelo de arma nulo na checagem de linha de tiro — responde "sem linha de tiro";
  - ponteiro de saída nulo no getter de variável do Apt/Flash — a escrita é pulada.

## Pacotes

- `BLACK-PC-Modloader-1.4.24.zip` — atualizador do launcher (Windows): launcher,
  `blackrt.dll`, `LEIA-ME.txt` e os pacotes de idioma.
- `BLACK-PC-Modloader-1.4.24-com-mods.zip` — instalação completa no Windows, com os
  mods oficiais (texturas HD, ícones de botão e a dublagem PT-BR).
- `BLACK-PC-Modloader-1.4.24-linux.zip` — launcher Linux nativo + `blackrt.dll` +
  `languages` + `launcher.sounds`.
- `BLACK-PC-Modloader-1.4.24-linux-com-mods.zip` — o mesmo com os mods oficiais.
- `SHA256SUMS-1.4.24.txt` e `SHA256SUMS-1.4.24-linux.txt` — conferência dos arquivos.

A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`
nem `log\`.

## Compatibilidade

- ModLoader 1.4.24 é compatível com o **BLACK PC port 0.3.0** (e com o 0.2.1).
- Continua sendo **só** para o BLACK PC port do Spark198rus
  (https://github.com/Spark198rus/black-pc), não para o BLACK-XBERecomp.
- O `default.xbe` precisa ser o build NTSC-U do jogo (o launcher avisa se não for).
