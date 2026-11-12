# ModLoader 1.4.26

## BLACK Reloaded Mod (kROM3r) no launcher

- **AVANÇADO > INFORMAÇÕES** ganhou o botão **BLACK RELOADED MOD (kROM3r)**: abre o
  repositório do add-on
  ([kROM3r/BLACKReloadedMod](https://github.com/kROM3r/BLACKReloadedMod)) — 29 mods
  numa `winmm.dll`: pulo, corrida com fôlego, noclip, câmera livre, terceira pessoa
  com corpo, mira moderna, armas de ouro, multiplicador de inimigos, modo esquadrão,
  speedrun, o modo desafio que foi cortado do jogo e a dificuldade **Seventh Wave**.
- O launcher **detecta** a `winmm.dll` na pasta do port e mostra a versão instalada
  ("BLACK Reloaded Mod 1.9 detectado"), ou instrui a instalar.
- Créditos do **kROM3r** no botão (tooltip), no `README`, no `docs/MODS.md` e nos
  dois `LEIA-ME`.

## O que funciona junto com o ModLoader (conferido num run real)

- Os **mods de jogo** dele rodam: barra de fôlego da corrida no HUD, terceira pessoa
  com corpo (modelo criado, animações), câmera livre, tecla do HUD, noclip. As opções
  ficam no `black_mods.ini`, criado ao lado da `winmm.dll`.
- O ModLoader continua funcionando (aba MODS, CONF. PC, áudio dublado, mods de
  arquivo).

## O que fica de fora (e por quê)

- As **telas** dele — a página MODS da pausa e o menu do modo desafio/Seventh Wave —
  foram montadas sobre as páginas do port **0.2.0**. O menu do ModLoader é outro
  (página CONF. PC com as linhas do port, mouse nos menus), então o construtor de
  telas dele não reconhece a página e desliga só essa tela; o `black_mods.log` dele
  registra qual página não bateu.
- O pacote dele **não** é distribuído aqui: o código é fechado e a licença do autor
  só permite compartilhar o pacote original, sem alterações. Baixe no repositório
  dele.

## Instalar o add-on

1. Baixe o pacote `BLACK_Reloaded_Mod_..._Port_Spark198_PTBR.zip` na release do
   repositório do kROM3r.
2. Copie só a `winmm.dll` para a pasta do `black.exe` (a mesma do `blackrt.dll`).
   **Não** troque o `blackrt.dll` do ModLoader.
3. Abra o jogo pelo launcher. O `black_mods.ini` e o progresso dele ficam ao lado.

## Pacotes

- `BLACK-PC-Modloader-1.4.26.zip` — atualizador do launcher (Windows).
- `BLACK-PC-Modloader-1.4.26-com-mods.zip` — instalação completa com os mods oficiais.
- `BLACK-PC-Modloader-1.4.26-linux.zip` e `...-linux-com-mods.zip` — launcher Linux.
- `SHA256SUMS-1.4.26.txt` e `SHA256SUMS-1.4.26-linux.txt`.

A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`,
`log\` nem na `winmm.dll` de add-ons.

## Compatibilidade

- ModLoader 1.4.26 é compatível com o **BLACK PC port 0.3.0** (e com o 0.2.1).
- Só para o BLACK PC port do Spark198rus; o `default.xbe` precisa ser o build NTSC-U
  (o launcher avisa quando não é).
