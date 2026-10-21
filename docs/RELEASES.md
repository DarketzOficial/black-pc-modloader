# Releases do BLACK PC

As builds oficiais do **ModLoader** são publicadas na aba
[Releases](https://github.com/DarketzOficial/black-pc-modloader/releases) deste
repositório. O código do kit de mods fica no repositório; os pacotes prontos do
ModLoader ficam anexados às releases.

## Pacotes

| Asset | Conteúdo |
|---|---|
| `BLACK-PC-Modloader-<versão>.zip` | Pacote pequeno, com o launcher, `blackrt.dll` e o leia-me. É o arquivo usado pelo atualizador. |
| `BLACK-PC-Modloader-<versão>-com-mods.zip` | Pacote completo para a primeira instalação, com o ModLoader e os mods oficiais. |
| `BLACK-PC-ModKit-<versão>.zip` | Kit de desenvolvimento com o `blackmodkit.exe` pronto, o fonte do ModKit e a documentação. |
| `SHA256SUMS-<versão>.txt` | Hashes SHA-256 dos assets da release. |

O ModLoader não inclui o jogo. Para jogar, você precisa do seu dump de BLACK em
`game\`, ao lado de `black.exe`.

## Atualizações

O launcher consulta a release mais recente do GitHub e baixa o pacote pequeno do
ModLoader. A atualização preserva:

- `black.ini` e `launcher.ini`;
- `savedata\`;
- `game\`;
- `mods\`;
- `log\`.

As notas de cada versão ficam no corpo da release e em `docs/RELEASE-NOTES-*.md`.

## Versionamento

- A versão do ModLoader segue `MAJOR.MINOR.PATCH`.
- Correções e ajustes compatíveis entram em `PATCH`.
- Recursos novos entram em `MINOR`.
- Mudanças que quebram compatibilidade entram em `MAJOR`.

Cada release deve ter:

1. tag e nome da versão;
2. notas em português, com instalação e compatibilidade;
3. os pacotes do ModLoader anexados;
4. o pacote do ModKit quando o kit mudar;
5. o arquivo de hashes SHA-256.

## Mods da comunidade

Os `.bmod` enviados pela comunidade entram pelo fluxo de
[`comunidade/`](../comunidade/README.md). Depois de validados e testados, eles
podem ser incluídos no pacote `-com-mods` da próxima release.
