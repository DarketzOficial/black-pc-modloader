# Como fazer mods para o BLACK PC (modloader)

Um mod é **uma pasta dentro de `mods\`** na raiz do port. Nada no jogo é alterado: quando o jogo abre um arquivo, o
modloader entrega o arquivo do mod no lugar do arquivo original. Por isso dá para ligar e desligar um mod na tela
`MODS` sem reinstalar nada.

## Estrutura

```
BLACK\                         (pasta do port, onde está o black.exe)
  mods\
    meu-mod\
      mod.ini                  (opcional)
      .off                     (opcional: marca o mod como DESLIGADO)
      game\                    (os arquivos que substituem os do jogo)
        language\strings\MainUS.bin
        export\frontend\Core.bin
        videos\02_n.xmv
        ...
      subtitles\               (opcional: legendas .srt desenhadas por cima dos vídeos)
        02_n.srt
```

- O caminho dentro de **`game\`** é o mesmo caminho do arquivo no jogo. `game\language\strings\MainUS.bin` substitui
  `language\strings\MainUS.bin` do jogo.
- O arquivo do mod precisa estar **no formato que o jogo espera** (o modloader só entrega o arquivo; não converte nada).
- O nome da pasta do mod é o identificador dele nos logs.
- Qualquer arquivo do CD do jogo pode ser substituído (textos, ícones, mapas de menu, vídeos, arquivos de nível…).

## `mod.ini`

Arquivo de texto, uma opção por linha, `chave = valor`. Chaves (sem diferenciar maiúsculas/minúsculas):

| Chave | O que é |
|---|---|
| `name` | Nome mostrado na tela `MODS`. Sem ele, o modloader usa o nome da pasta (com espaços). |
| `priority` | Número. Quando dois mods trazem **o mesmo arquivo**, o de **maior prioridade** ganha. O padrão é `0`. |
| `lang` | Idioma do mod (`en`, `ptbr`, …). Opcional. Veja abaixo. |

Exemplo:

```ini
name = MINHA TEXTURA DO MURRO
priority = 5
lang = ptbr
```

### Idiomas (`lang`)

O jogo lê **um** arquivo de texto (`MainUS.bin`), então dois mods de texto de idiomas diferentes não podem valer ao
mesmo tempo. O modloader resolve assim:

1. o idioma em uso é o do **mod ligado de maior prioridade que declara `lang`** (sem nenhum, é `en`, os arquivos do
   próprio jogo);
2. um mod que declara **outro** idioma fica **bloqueado**: os arquivos dele **não são entregues** (na tela `MODS` ele
   aparece como `OFF (IDIOMA)`).

É por isso que o mod `KEYBOARD PROMPTS (EN)` (inglês) não atrapalha o `PT-BR TEXT + KEYBOARD`: os dois podem ficar
ligados, mas só o do idioma em uso entrega arquivo.

## Ligado / desligado

- **Sem** o arquivo `.off` na pasta do mod, ele vem **LIGADO**.
- **Com** `.off`, ele vem **DESLIGADO**.
- A tela `MODS` (no menu e na pausa) mostra e troca esse estado; quem cria o mod não precisa de instalador.
- O estado é por instalação (fica na própria pasta do mod), não no `black.ini`.

## Reiniciar

O jogo lê alguns arquivos uma única vez, ao iniciar (a tabela de textos, por exemplo). Ao ligar ou desligar um mod de
**arquivo**, a tela mostra **`(REINICIAR)`** e o modloader **reinicia o jogo sozinho** quando você volta ao menu
principal (leva uns 2 segundos e volta no menu). Nada do seu progresso é perdido — o reinício só acontece com o menu
principal na tela.

## Legendas

O modloader desenha legendas `.srt` por cima dos vídeos, **se o mod tiver uma pasta `subtitles\`**:

```
mods\meu-mod\subtitles\02_n.srt
```

- Um `.srt` por filme, com o **mesmo nome do vídeo** (`02_n.xmv` → `02_n.srt`).
- Formato SRT normal (o mesmo que qualquer editor de legenda exporta), **UTF-8**.
- O tempo é contado desde o início do filme.
- As legendas ligam/desligam junto com o mod. Se quiser desligar só as legendas, use `subtitles = 0` no `black.ini`.

## Testando

- A tela **`PC SETTINGS > MODS`** lista tudo o que está em `mods\` (e as opções do próprio modloader).
- `--no-mods` inicia com **todos** os mods desligados; `--all-mods` liga todos — útil para descobrir qual mod causa um
  problema.
- O `black.log` (na pasta do port) registra cada mod encontrado, com prioridade e idioma:
  ```
  mods: xboxiconstokeys (XBOX ICONS AS KEYS) priority 0
  mods: xcontroltokeyboard (KEYBOARD PROMPTS (EN)) priority 0 lang en - off (another language is being played)
  ```
- O **conflito de arquivos** é resolvido pela `priority` (maior ganha) e, no empate, pela ordem das pastas: o `black.log`
  mostra qual mod entregou o arquivo quando o jogo o abre (`mods::resolve`).

## Dicas

- Comece com uma pasta só sua (`mods\meu-mod`) e um arquivo de cada vez, testando na tela `MODS`.
- Para texto/ícones, copie o arquivo original do jogo (`game\...`), edite uma cópia e ponha a cópia em
  `mods\meu-mod\game\...` — assim você sempre pode voltar atrás apagando a pasta.
- Para vídeos legendados, o nome do arquivo tem de ser o mesmo do vídeo do jogo (maiúsculas/minúsculas não importam no
  Windows, mas mantenha o padrão).
- Mods **de arquivo** pesados (texturas/vídeos) aumentam o uso de disco/RAM; prefira o formato que o jogo já usa.
