# Receber mods da comunidade

Esta pasta é a **caixa de entrada** de mods do BLACK PC. O objetivo é receber um
pacote fechado, validar antes de instalar e nunca deixar um envio quebrar a
instalação de quem já tem mods.

## Formato aceito

O único formato aceito é **`.bmod`**: um ZIP com `mod.json` na raiz, `mod.ini` e
os arquivos do jogo dentro de `game\`. O `modkit` exporta nesse formato.

```text
meu-mod-1.0.0.bmod
  mod.json          (id, nome, versão, autor, prioridade, idioma, hashes)
  mod.ini           (o que o port lê: name, priority, lang)
  game\
    language\strings\MainUS.bin
    ...
  subtitles\        (opcional)
    02_n.srt
```

O `mod.json` é metadado do kit; o port continua lendo só o `mod.ini`. O launcher
confere os hashes SHA-256 e recusa arquivos não declarados, caminhos com `..` e
qualquer coisa que tente sair da pasta do mod.

## Duas formas de enviar

1. **Pull request** (preferido): copie o `.bmod` para `comunidade/pacotes/` e
   abra um PR. O GitHub Actions roda `ferramentas/validar_pacote.py` e mostra o
   erro no PR, se houver.
2. **Issue**: use o modelo **Enviar mod** e anexe o `.bmod` (ou um link direto
   do arquivo). O mantenedor baixa, valida e importa.

Passo a passo completo: [`COMO-ENVIAR.md`](COMO-ENVIAR.md).

## Como o mod entra no modloader

O mantenedor importa o pacote com o `modkit` em uma instalação limpa:

- o pacote é verificado antes de copiar;
- se já existir um mod com o mesmo `id`, o antigo vai para `mods\.backup`;
- conflitos de arquivo são mostrados com o vencedor pela prioridade;
- um lote de pacotes entra **desligado** para revisão, evitando conflito
  acidental;
- depois do teste no jogo, o mod entra no pacote de mods da próxima versão.

O jogo não é alterado: o modloader entrega os arquivos do mod no lugar dos
originais enquanto ele estiver ligado.

## Regras para não criar conflito

- Um mod = uma pasta = um `id`. Não reutilize o `id` de outro mod para uma versão
  diferente; use `meu-mod` e `meu-mod-2`, ou o campo `version`.
- Se dois mods entregam **o mesmo arquivo**, só um vence: maior `priority`; no
  empate, a ordem da pasta. Declare isso na descrição do envio.
- Mods de texto devem declarar `lang` (`ptbr`, `en`, ...). O port bloqueia o mod
  de outro idioma em vez de misturar textos.
- Prefira arquivos pequenos e só os que realmente mudam. Não envie o jogo
  inteiro nem arquivos sem alteração.
- Teste com o jogo antes de enviar: abra o jogo, veja o arquivo no jogo e confira
  o `black.log` se algo não aparecer.

## O que não é aceito

- executáveis, instaladores, scripts ou arquivos fora do formato de mod;
- pacotes sem `mod.json` ou sem `mod.ini`;
- arquivos com caminho absoluto, `..`, links ou hashes errados;
- conteúdo de jogo completo ou material que você não tem direito de distribuir;
- pacotes que sobrescrevem mods de outras pessoas sem declarar o conflito.

Dúvidas sobre o formato: [`../docs/MODS.md`](../docs/MODS.md).
