# ModLoader 1.4.17

Data: 22/10/2026
Tag: `1.4.17`
Nome da release: **ModLoader 1.4.17**

Pacote de correcao para o port **0.2.1**. O foco sao os dois crashes enviados por
jogadores (pasta `logplayers`) e o aviso de build do jogo nao suportada.

## Correcoes de crash

- **Efeito com entrada invalida na tabela** (`black-crash-20261022-144210.log`):
  o jogo lia `[entrada + 0x39C]` com a entrada valendo `6` (arma 8 dada pelo trainer)
  e caia em `0x001A7992`. O guard em `0x001A7992` agora so aceita ponteiros na
  imagem do XBE (`0x10000..0x600000`) ou na RAM do jogo (`0x80000000..0x84000000`);
  fora dessas faixas, devolve `-1` (sem efeito), sem desreferenciar. Efeitos com
  entrada valida continuam iguais.

- **Stream de audio com formato liberado** (`black-crash-20261022-002452.log`):
  depois de 764 s em `level_05`, o mixer comparava 16 bytes de um formato cujo
  ponteiro era `0x38415C30` e caia em `0x00085A8A`. O guard em `0x0008FC76` valida
  o descritor do stream e o ponteiro do formato; se um deles estiver fora da memoria
  do jogo, a comparacao e pulada e o fluxo normal continua.

## Build do jogo

- O log `black-20261022-025153.log` nao e um crash do port: o jogador usou outro
  `default.xbe` (SHA-256 `67f35121...30f0`, tamanho `0x577A60`). O port 0.2.1 continua
  exigindo o dump NTSC-U `df2739c3...68f6` (`0x577A40`). O aviso de build desconhecida
  ja mostra os dois hashes para o jogador conferir o dump.

## Arquivos

- `blackrt.dll` do port **0.2.1** (mesmo hash na raiz e em `dist\modloader`).
- `BLACK-PC-Modloader-1.4.17.zip` (atualizador do launcher).
- `BLACK-PC-Modloader-1.4.17-com-mods.zip` (pacote completo com mods).
