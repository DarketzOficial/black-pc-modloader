# ModLoader 1.4.10

Tag: `1.4.10`
Nome da release: **ModLoader 1.4.10**

Esta atualização organiza os logs do jogo e adiciona o relatório de erro do launcher.

## Novidades

- **Relatório de erro**: quando o jogo fecha com erro, aparece uma janela no mesmo visual do launcher,
  com **ENVIAR** e **NÃO ENVIAR**. O **ENVIAR** manda o log e as informações do PC para o canal
  `#logs-black`; nada é enviado sem esse clique.
- O relatório inclui o `black-crash-*.log` (anexo), as versões do ModLoader e do port, Windows, CPU,
  GPU, RAM, tela, usuário/máquina do Windows, o menu onde o jogo estava, os mods ativos e os ajustes
  do `black.ini`. Nenhum save, senha ou arquivo pessoal é enviado.
- **Logs organizados**: `black.log` e `black-crash-*.log` saem da raiz e vão para `log\game\` e
  `log\crash\`; o launcher também registra as tentativas em `log\launcher\reports.log`.
- Se o jogador abrir `black.exe` direto e o jogo falhar, o próximo início do launcher encontra o
  crash log na raiz e mostra a mesma janela uma vez.
- A atualização não toca em `black.ini`, `launcher.ini`, `savedata\`, `game\`, `mods\` nem em `log\`.
- **Corre??o de crash**: ao descarregar uma fase, o port podia tratar o c?digo FVF `0x444`
  como um vertex shader e ler o endere?o `0x444` (fechava o jogo). Agora handles FVF/inv?lidos
  s?o ignorados.
- **Menu do jogo**: a tela do port volta a se chamar **MODS** (o titulo continua
  **CONF. PC** com o mod PT-BR ativo).
- **Pasta das texturas HD**: bot?o **TROCAR PASTA** abre o seletor do Windows para escolher o
  `map.csv`; o padr?o ? `mods\hdtextures\hd`. O **X** ao lado limpa o caminho.
- **Apoio ao projeto**: bot?o **DOAR** no banner, com QR code do PIX, LivePix, Bitcoin,
  Dogecoin, Ethereum e USDT (com bot?o de copiar). O projeto continua gratuito.
- **Tela de novidades**: depois de atualizar, o launcher mostra as notas desta release uma vez,
  com secoes e rolagem. Para rever, use **AVANCADO > Informacoes da atualizacao**.

## Arquivos

- Atualização do launcher: `black-pc-modloader.zip` (anexo).
- Pacote base do loader: `BLACK-PC-Modloader-1.4.10.zip` (os mods PT-BR, HD etc. sao pacotes separados).

## Compatibilidade

- BLACK PC port 0.2.0 (Spark198rus).
- Windows 10/11; o jogo precisa do dump em `game\` (o pacote não inclui o jogo).
