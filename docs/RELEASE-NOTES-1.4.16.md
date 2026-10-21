# ModLoader 1.4.16 + Kit de desenvolvimento de mods

Tag: `1.4.16`

Esta versão reúne o **ModLoader 1.4.16** e o **kit aberto para criar mods** do
BLACK PC. O kit é a base para criar, validar e enviar mods de arquivo; o
ModLoader continua sendo distribuído pelos pacotes anexados às releases.

## Kit de desenvolvimento de mods

O repositório agora é o ponto de partida para quem quer criar mods:

- **ModKit** (`blackmodkit.exe`): gerenciador de mods para criar, testar, ligar,
  desligar e empacotar mods;
- **formato `.bmod`**: pacote com `mod.json`, `mod.ini`, arquivos do jogo e
  hashes SHA-256;
- **exemplo pronto**: `exemplo/meu-mod`, com placeholders para testar o fluxo;
- **validadores**: `validar_mod.py` para pastas e `validar_pacote.py` para
  pacotes da comunidade;
- **caixa de entrada**: envio por pull request ou issue em `comunidade/`;
- **CI**: validação automática dos pacotes enviados.

O ModKit é uma ferramenta separada do launcher completo. Ele não tem updater,
crash reporter nem opções do jogo; só mexe em `mods\` e em pacotes `.bmod`.

## ModLoader 1.4.16

### Inicialização e UAC

- Quando o `black.exe` está marcado como **Executar como administrador** (ou tem
  um manifesto que pede elevação), o launcher usa **ShellExecute** e deixa o
  Windows mostrar o aviso do UAC.
- Se o aviso for cancelado, o launcher explica como desmarcar a opção nas
  propriedades do `black.exe`.

### HUD e gráficos

- **HUD 16:9**: os painéis de vida e munição voltam aos cantos; a régua do aviso
  de objetivo continua estreitando com o texto.
- **Oclusão ambiental ALTO**: GTAO do recomp em resolução cheia; não é mais
  experimental.
- **Texturas HD**: caminho completo, inclusive com espaços; **TROCAR PASTA**
  mostra o progresso da pasta escolhida.
- **Modo de mira**: **SEGURAR** não alterna sozinho; **ALTERNAR** continua
  funcionando.

### Relatório de erro e logs

- Quando o jogo falha, o launcher mostra uma janela com **ENVIAR** e
  **NÃO ENVIAR**.
- O relatório inclui o log do crash, versão do ModLoader e do port, informações
  do Windows/PC, tela, menu, mods ativos e ajustes do `black.ini`.
- Nada é enviado sem a confirmação do jogador.
- Os logs ficam organizados em `log\game\`, `log\crash\` e `log\launcher\`.

### Tela de novidades

- Depois de uma atualização, o launcher mostra as notas da versão uma vez no
  primeiro início.
- Para rever: **AVANÇADO > Informações da atualização**.

### Correções

- Corrigido o crash ao descarregar uma fase: handles FVF/inválidos não são mais
  tratados como ponteiros de vertex shader.
- A tela do port no jogo voltou a se chamar **MODS**.
- `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\` e `log\` não são
  alterados pelas atualizações.

### Recursos anteriores mantidos

- HUD 16:9 e FADING HUD.
- Filtros de imagem: SHARPENING, DETAILS, CLARITY e VIBRANCE (0-100).
- Página de imagem no jogo e aba **GRÁFICOS** no launcher.
- FOV da arma limitado a 80.
- Mouse nos menus, FPS, legendas, pular vídeos e trainer (F5).
- Atualização automática pelo GitHub.
- Preservação das configurações e dos mods durante a atualização.

## Arquivos

- `BLACK-PC-Modloader-1.4.16.zip` — pacote de atualização do ModLoader.
- `BLACK-PC-Modloader-1.4.16-com-mods.zip` — pacote completo com o ModLoader e
  os mods oficiais.
- `BLACK-PC-ModKit-1.0.0.zip` — kit de desenvolvimento com o `blackmodkit.exe`
  pronto e o fonte.

## Instalação

1. Feche o jogo.
2. Extraia o pacote **dentro da pasta do port**, onde fica `black.exe`.
3. Confirme a substituição de `blackrt.dll`.
4. Abra `blacklauncher.exe`.

Para criar mods, baixe o ModKit separadamente e aponte-o para a pasta do port.
O guia completo está no [README](../README.md).

## Compatibilidade

- BLACK PC port 0.2.0 (Spark198rus).
- Windows 10/11, 32 ou 64 bits.
- O jogo precisa do dump em `game\`; ele não vem no pacote.
- Não é compatível com o BLACK-XBERecomp do TGP482.

## Atualizações

O launcher consulta a release mais recente do GitHub e baixa o pacote de
atualização. `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\` e `log\`
são preservados.

## Licença

GPL-2.0-or-later. Veja [LICENSE](../LICENSE).

BLACK é uma marca da Electronic Arts. Este projeto não inclui o jogo nem
arquivos do jogo.
