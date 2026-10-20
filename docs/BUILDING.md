# Compilando e entendendo o modloader

O modloader é o **BLACK PC port** do Spark198rus com a camada de mods. É um projeto **C++20 / Windows x86 (32 bits)**
com **MSVC + CMake**. Só Windows: o código usa assembly inline x86 do MSVC e opções do linker, e o processo precisa ser
32 bits (é o espaço de endereço do Xbox).

## Requisitos

- **Visual Studio 2026 (ou 2022) Build Tools**, com a ferramenta C++ para x86.
- **CMake 3.24+** (vem com o Visual Studio, em
  `...\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe`, se não estiver no `PATH`).
- Nada mais: o projeto não usa bibliotecas de terceiros (Direct3D 9, D3DCompiler, XAudio2, GDI+, WIC e as APIs do
  Windows já vêm no sistema).

## Compilar

Na raiz deste repositório:

```bat
cmake --preset win32                       REM Visual Studio 2026 (ou: cmake --preset win32-vs2022)
cmake --build build/win32 --config RelWithDebInfo
```

Saída em `build\win32\RelWithDebInfo\`:

| Arquivo | O que é |
|---|---|
| `black.exe` | o stub mínimo (sem CRT) que carrega o `default.xbe` e chama o DLL |
| `blackrt.dll` | o **modloader** (todo o código: HLE do Xbox, D3D, áudio, input, mods e menus) |

Os dois andam juntos, na pasta do port (a que tem a pasta `game` e o `black.ini`). Para instalar / testar:

```
BLACK\
  black.exe
  blackrt.dll        <- o que você acabou de compilar
  game\              <- o dump do BLACK (Xbox NTSC-U): default.xbe + arquivos
  mods\              <- os mods
  black.ini          (criado na primeira execução)
  savedata\          (criado na primeira execução)
```

O repositório **não inclui o jogo** (nem o `default.xbe` nem os arquivos do dump).

## Rodar (desenvolvimento)

```bat
black.exe                                  REM usa .\game\default.xbe
black.exe --info game\default.xbe          REM mostra a estrutura do XBE e sai
black.exe --timeout 30                     REM fecha sozinho depois de 30 s
```

O `black.log` (ao lado do `black.exe`) tem tudo o que o port fez. `black.exe --help` lista as opções de depuração
(`--log`, `--verbose`, `--trace`, `--strict`, `--screenshot`, `--shot-every`, `--press`, `--press-file`, `--watch`,
`--perf-at`, `--dump-textures`, …). As opções de jogo ficam em `PC SETTINGS`, dentro do jogo.

## Launcher (WPF)

O launcher fica em `launcher\` (projeto .NET Framework 4.8/WPF). Para compilar e copiar o
`blacklauncher.exe` para a pasta do port:

```powershell
cd launcher
.\build.ps1 -Deploy
```

Os assets do launcher (`logo.png`, `qrcode.png`, `favicon.ico`) ficam em `launcher\Assets\`.
O README especifico do launcher, com a arquitetura de cada janela, esta em
`launcher\README.md`.

## Onde fica cada coisa

| Arquivo | O que faz |
|---|---|
| `stub\stub.c` | `black.exe`: programa mínimo sem CRT, ligado em `0x10000` com um BSS grande para a imagem dele ocupar o
  espaço do XBE antes de o Windows pôr heaps pilhas ali |
| `src\main.cpp` | `BlackMain`: argumentos, checagem do build (SHA-256), ordem de inicialização, padrões da primeira
  instalação |
| `src\image.cpp` | mapeia o XBE sobre a imagem do stub, monta um cabeçalho PE para o Windows, `XeLoadSection` |
| `src\xthread.cpp` | estado de thread do Xbox; reescreve `fs:[0x04/0x20/0x24/0x28/0x58]` para slots de TLS; lock de IRQL |
| `src\hooks.cpp` | hooks das bibliotecas (tabelas em `*_functions.inc`), relatos de crash, `--watch`/`--probe` |
| `src\kernel\` | kernel HLE: tabela de ordinais, memória, threads, sincronização, DPC/timers, arquivos, Rtl |
| `src\d3d\` | D3D HLE sobre Direct3D 9: device/estado, recursos, texturas, draws, push buffer, vertex programs NV2A |
| `src\audio\` | DirectSound HLE: mixer (`dsound.cpp`), reverb (`reverb.cpp`), saída XAudio2 (`output.cpp`) |
| `src\input\` | XInput HLE: controles de PC, teclado e mouse; `mouselook.cpp` leva o mouse para os ângulos da visão |
| `src\mods.cpp` | **a camada de mods**: varre `mods\`, lê `mod.ini`, resolve arquivos por prioridade e idioma |
| `src\fe\` | **as páginas e menus do port**: PC SETTINGS + tela MODS (`pc_settings.cpp`, `pages.cpp`), o teclado de
  bind (`bind_list.cpp`), a página de pausa (`pause_ui.cpp`), o reinício automático (`restart.cpp`) e a ponte com o
  front end do jogo (`calls.cpp`, `apt.cpp`, `vars.cpp`) |
| `src\fe\restart.cpp` | reinicia o jogo quando a pasta de um mod muda (aplica no próximo start) |
| `src\settings.cpp` | `black.ini`: lê, grava e guarda quais opções vieram da linha de comando |
| `src\perf.cpp` | gravador de desempenho (`--perf-at`) |

### A camada de mods em detalhe

- `mods::scan()` lê `mods\<pasta>` e monta a lista (nome, prioridade, idioma, `.off`).
- `mods::resolve(path, out)` responde por qualquer leitura do CD do jogo em `kernel/k_file.cpp`: se um mod ligado (e do
  idioma em uso) tiver `game\<path>`, o port entrega esse arquivo; senão segue para `--game-override` e para o cache
  de menus do port, e por fim para o arquivo do jogo.
- `mods::restart_needed()` compara o estado atual com o do começo da sessão; `fe/restart.cpp` reinicia o jogo quando o
  menu principal aparece (o jogo lê textos/ícones só no início).
- O formato de um mod (pastas, `mod.ini`, `.off`, `subtitles\`) está em **[MODS.md](MODS.md)**.

## Licença e créditos

GPL-2.0-or-later (o port original do Spark198rus também é). Se você distribuir um `blackrt.dll` modificado, distribua
também o código-fonte. O port original, o crédito do port e os créditos da tradução PT-BR estão no
**[README](../README.md#créditos)**.
