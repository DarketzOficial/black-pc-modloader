# ModLoader 1.4.18

Data: 23/10/2026
Tag: `1.4.18`
Nome da release: **ModLoader 1.4.18**

Pacote para o port **0.2.1**: mais um crash enviado por jogador (helper `0x13AE0`),
HUD 16:9 so em vida/municao (textos de gameplay no 4:3 original) e atualizacao
automatica ligada na primeira instalacao. Os guards de efeito e de stream da 1.4.17
continuam.

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

- **Helper do HUD sem o sub-objeto da arma** (`black-crash-20261022-233350.log`):
  depois de dar a arma 9 pelo trainer, `FUN_00013AE0` lia `[ecx+0x18]` e em seguida
  `[eax+0x54]` com `eax=0` (crash em `0x0013AE3`). O guard em `0x0013AE0` devolve
  `0` quando o ponteiro e nulo; com a arma completa o teste de bit continua igual.

## HUD

- **So vida e municao em 16:9**: os paineis, os pacotes de vida e o icone do
  soldado continuam ancorados nos cantos. As mensagens de gameplay (objetivo,
  dicas e prompts) voltaram ao 4:3 original do jogo, sem recorte nem fantasma.
- **Tela de tutorial**: o texto do corpo da dica tambem fica visivel nos testes
  (`hint:N`); o jogo normal nao muda.

## Launcher

- **Atualizacao automatica ligada na primeira instalacao**: quando `update_auto`
  ainda nao existe no `launcher.ini`, o launcher grava `update_auto = 1` na
  primeira abertura. Quem desligar explicitamente continua desligado.

## Build do jogo

- O log `black-20261022-025153.log` nao e um crash do port: o jogador usou outro
  `default.xbe` (SHA-256 `67f35121...30f0`, tamanho `0x577A60`). O port 0.2.1 continua
  exigindo o dump NTSC-U `df2739c3...68f6` (`0x577A40`). O aviso de build desconhecida
  ja mostra os dois hashes para o jogador conferir o dump.

## Arquivos

- `blackrt.dll` do port **0.2.1** (mesmo hash na raiz e em `dist\modloader`):
  `06D3937128C07A975A8710A3587C503D7BBF331B0CC67DA15875FB89D92402B5`
- `blacklauncher.exe`: `C8FB4DE680B5CC9A6C4375AE7B6B71445B88A4E9CD4512EACF5516A6503446C7`
- `BLACK-PC-Modloader-1.4.18.zip` (atualizador do launcher):
  `0ae64ddb0970eee47847cd6494d07a238d29a27f159318ca6a2ffbfc2068db08`
- `BLACK-PC-Modloader-1.4.18-com-mods.zip` (pacote completo com mods):
  `cee3191f78fdde3052aceffc239cc2090961e03c12eaee758124d7bddbb08c11`
