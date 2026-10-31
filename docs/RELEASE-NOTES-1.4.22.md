# ModLoader 1.4.22

## Acentos nos menus (o "N|O" virou "NAO" de verdade)

- Os icones de tecla personalizados pintavam retangulos de 24-32 px no atlas da
  fonte, e a fonte do pack pt-BR guarda os glifos acentuados justamente nos vaos
  transparentes desses retangulos: o acento era apagado e sobrava uma barra
  vertical ("N|O", "MISS|O", "VERS|O", "F|CIL"). O swap agora pinta apenas a arte
  do proprio botao (o blob opaco no centro da celula), deixando o resto do atlas
  exatamente como veio da fonte.
- Conferido nas telas: credito do titulo (VERSAO 1.3 DA TRADUCAO PARA PC),
  loading (NAO DESLIGUE SEU PC), menu principal (CONTINUAR A MISSAO, OPCOES) e
  briefing/dificuldade (FACIL, DIFICIL, OBJETIVOS PRIMARIOS), com texturas HD
  ligadas e desligadas. Os keycaps continuam aparecendo (SELECIONAR, CONFIRMAR,
  VOLTAR).

## Outros dumps do mesmo jogo

- O port conferia o SHA-256 do arquivo inteiro do default.xbe e recusava qualquer
  dump diferente, mesmo quando o codigo era o mesmo (certificado, padding ou
  secoes de dados regravadas mudam o arquivo sem mudar o jogo). Agora ele compara
  a secao executavel (.text): outro dump do mesmo build NTSC-U entra normalmente,
  e um build realmente diferente continua sendo recusado com a mensagem certa.

## Port

- BLACK PC port 0.2.1.

- O pacote de atualizacao e o BLACK-PC-Modloader-1.4.22.zip; o pacote completo com mods e o
  BLACK-PC-Modloader-1.4.22-com-mods.zip.
- A atualizacao nao toca em black.ini, launcher.ini, savedata\, game\, mods\ nem log\.

## Novidades do port (ja vinham no build)

- LEAN tatico: inclinar a visao com V/B (o corpo rola e o olho desloca). Ajuste em
  CONTROLES > ANGULO DA INCLINACAO e DESLOCAMENTO DA INCLINACAO (0 desliga).
- WEAPON BUFFERS: memoria para arquivos de arma (.db) de mods, em AUDIO E JOGO.
  ORIGINAL e o jogo de fabrica; 2/4/8 MB aceitam armas maiores de mods.
- Guardas de crash dos logs dos jogadores (wbinvd/invd, HUD/arma, stream de audio,
  efeitos) seguem instaladas.
