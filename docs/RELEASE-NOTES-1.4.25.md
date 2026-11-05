# ModLoader 1.4.25

## Ferramentas de modding no launcher (pedrolZasx)

- **AVANÇADO > INFORMAÇÕES** ganhou o botão **FERRAMENTAS DE MODDING**: abre o
  repositório do **pedrolZasx** (Black 2006 Modding tools), com substituição de armas
  e animações (Maya), editor de textura (o fork do SubCraft e o
  `black_texture_tool.py`) e efeitos de clima.
  https://github.com/pedrolZasx/Black_2006_Modding_tools
- O tooltip do botão explica o que tem lá. Créditos também no `README`, no
  `docs/MODS.md` e nos dois `LEIA-ME`.

## Mods de arma/textura feitos com essas ferramentas

- Os `.db` exportados por elas entram no modloader como **qualquer outro mod**: a
  pasta do mod com os arquivos **no caminho do jogo**:
  ```
  mods\minha-arma\
    mod.ini
    game\
      levels\level_00\fpguns\bg1_ak1.db
  ```
- Conferido no teste: o arquivo do mod é servido no lugar do original
  (`files: \Levels\Level_00\fpguns\bg1_pst.db from ...\mods\teste-arma\...`).
- Armas com modelo/textura grande precisam de mais memória para os `.db`: em
  **AUDIO E JOGO > Buffers de arma** escolha **4 MB** ou **8 MB** — é o mesmo
  `black.exe --buf-mb 8` que as ferramentas pedem (o port aceita o mesmo nome).
  Sem isso o jogo pode mostrar "disco sujo" ao carregar a arma.
- O **loader** que vem no pacote das ferramentas **não** é usado junto com o
  ModLoader: os dois são o mesmo `blackrt.dll` e só um pode ficar na pasta do port.
  Fique com o do ModLoader; os mods de arquivo continuam valendo.

## Pacotes

- `BLACK-PC-Modloader-1.4.25.zip` — atualizador do launcher (Windows): launcher,
  `blackrt.dll`, `LEIA-ME.txt` e os pacotes de idioma.
- `BLACK-PC-Modloader-1.4.25-com-mods.zip` — instalação completa no Windows, com os
  mods oficiais (texturas HD, ícones de botão e a dublagem PT-BR).
- `BLACK-PC-Modloader-1.4.25-linux.zip` — launcher Linux nativo + `blackrt.dll` +
  `languages` + `launcher.sounds`.
- `BLACK-PC-Modloader-1.4.25-linux-com-mods.zip` — o mesmo com os mods oficiais.
- `SHA256SUMS-1.4.25.txt` e `SHA256SUMS-1.4.25-linux.txt`.

A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`
nem `log\`.

## Compatibilidade

- ModLoader 1.4.25 é compatível com o **BLACK PC port 0.3.0** (e com o 0.2.1).
- Só para o BLACK PC port do Spark198rus; o `default.xbe` precisa ser o build NTSC-U
  (o launcher avisa quando não é).
