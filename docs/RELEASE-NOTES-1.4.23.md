# ModLoader 1.4.23

## A atualização que ficava repetindo (1.4.21 → 1.4.22 → 1.4.21...)

- A 1.4.22 saiu com o launcher ainda marcado como **1.4.21** (número interno do
  executável). O launcher se via como 1.4.21, oferecia a 1.4.22, instalava o
  próprio pacote 1.4.22 — que trazia o launcher 1.4.21 — e voltava a oferecer a
  mesma atualização para sempre: abria, pedia para atualizar, fechava e continuava
  na mesma versão.
- Agora o launcher se identifica como **1.4.23** e o pacote 1.4.23 traz o launcher
  1.4.23. Quem está no ciclo: deixe o launcher instalar a 1.4.23 uma última vez
  (ou extraia o pacote desta release por cima da pasta do port) e o aviso some.
- Junto com isso, o launcher escolhe o pacote do sistema certo na atualização: no
  Windows ignora os pacotes `-linux`; no Linux ignora o pacote do Windows.

## Launcher Linux nativo

- O launcher Linux agora é o **mesmo launcher do Windows**, compilado para Linux
  (Avalonia/.NET 8): mesmas abas, mesmos ajustes, mesmos avisos e novidades.
- Não precisa instalar Python nem .NET: a pasta `launcher-linux/` já vem completa
  no pacote (self-contained).
- Rode com `bash run-blacklauncher.sh` (o script passa a pasta do port ao launcher).
- O jogo e o `blackrt.dll` continuam Win32: o launcher abre o `black.exe` pelo Wine.
- Som do launcher por `mpv` ou `ffplay`, quando existirem. O relatório de erro
  aparece igual ao do Windows e a atualização baixa o pacote `...-linux.zip`,
  aplicado no próximo início do launcher.
- Wine, prefixo e DXVK ficam em `launcher-linux/launcher.ini` (`wine`,
  `wineprefix`, `dxvk`). O `LEIA-ME-LINUX.txt` do pacote explica cada um,
  incluindo criar um prefixo 32 bits com `WINEARCH=win32`.

## Launcher (Windows)

- Relatório de erro: os textos em português estavam com `?` no lugar dos acentos
  ("Relat?rio", "VERS?O", "SESS?O", "N?O ENVIAR"); voltaram ao normal.
- Os botões das janelas de diálogo voltaram a mostrar o texto (o tema do
  launcher desenhava a moldura e deixava o conteúdo de fora).

## Compatibilidade

- ModLoader 1.4.23 é compatível com o **BLACK PC port 0.3.0** (e com o 0.2.1): as
  opções novas do port 0.3.0 aparecem no launcher e as opções antigas continuam
  exatamente iguais.
- Continua sendo **só** para o BLACK PC port do Spark198rus
  (https://github.com/Spark198rus/black-pc), não para o BLACK-XBERecomp.

## Pacotes

- `BLACK-PC-Modloader-1.4.23.zip` — atualizador do launcher (Windows): launcher,
  `blackrt.dll`, `LEIA-ME.txt` e os pacotes de idioma.
- `BLACK-PC-Modloader-1.4.23-com-mods.zip` — instalação completa no Windows, com
  os mods oficiais (texturas HD e ícones de botão).
- `BLACK-PC-Modloader-1.4.23-linux.zip` — launcher Linux nativo + `blackrt.dll` +
  `languages` + `launcher.sounds`.
- `BLACK-PC-Modloader-1.4.23-linux-com-mods.zip` — o mesmo com os mods oficiais.
- `SHA256SUMS-1.4.23.txt` e `SHA256SUMS-1.4.23-linux.txt` — conferência dos
  arquivos.

A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`,
`mods\` nem `log\`.
