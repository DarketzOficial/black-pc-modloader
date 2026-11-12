# ModLoader 1.4.27

## Menus antes do jogo de volta (correção)

O **CONF. PC do menu principal** sumia desde a 1.4.24: a página **CONTROLS** ficou com
sete linhas (MOUSE SENSITIVITY, INVERT MOUSE, KEY BINDINGS, AIM MODE, WALK SPEED,
LEAN ANGLE, LEAN DISTANCE) e o front end do jogo aceita **no máximo seis por página**.
O port recusava a página inteira e o log dizia:

```
fe: FEMain.bin is not laid out as expected (a page has at most six rows)
fe: PC SETTINGS pages: main menu off, pause menu on
```

Com isso iam embora o CONF. PC do menu principal e o MODS de dentro dele.

- **WALK SPEED** agora fica só no launcher (`black.ini walk_speed`); a página CONTROLS
  voltou a ter seis linhas e as duas linhas do **TACTICAL LEAN** continuam no jogo.
- Conferido no port real: `fe: PC SETTINGS pages: main menu on, pause menu on`.
- O mesmo erro aparecia com o BLACK Reloaded Mod instalado — não era o mod.

## BLACK Reloaded Mod (kROM3r)

- A linha **MODS** dele continua fora do menu da pausa: as telas dele são montadas
  sobre o menu do port 0.2.0 e o menu do ModLoader é outro (`black_mods.log` mostra
  qual página não bateu).
- Os **mods de jogo** dele seguem funcionando (fôlego da corrida, terceira pessoa com
  corpo, câmera livre, tecla do HUD, noclip) e as opções ficam no `black_mods.ini`.
- O launcher detecta a `winmm.dll` e mostra a versão instalada (AVANÇADO >
  INFORMAÇÕES > BLACK RELOADED MOD).

## Pacotes

- `BLACK-PC-Modloader-1.4.27.zip` — atualizador do launcher (Windows).
- `BLACK-PC-Modloader-1.4.27-com-mods.zip` — instalação completa com os mods oficiais.
- `BLACK-PC-Modloader-1.4.27-linux.zip` e `...-linux-com-mods.zip` — launcher Linux.
- `SHA256SUMS-1.4.27.txt` e `SHA256SUMS-1.4.27-linux.txt`.

A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\`,
`log\` nem na `winmm.dll` de add-ons.

## Compatibilidade

- ModLoader 1.4.27 é compatível com o **BLACK PC port 0.3.0** (e com o 0.2.1).
- Só para o BLACK PC port do Spark198rus; o `default.xbe` precisa ser o build NTSC-U
  (o launcher avisa quando não é).
