# Como enviar um mod

## 1. Crie e teste no ModKit

1. Abra `modkit\dist\blackmodkit.exe` (ou compile com
   `modkit\build.ps1`).
2. Escolha a pasta do port, onde ficam `black.exe` e `game\default.xbe`.
3. **Novo mod**: escolha nome, pasta (id), prioridade e idioma.
4. **Adicionar pasta...**: selecione a pasta com os arquivos já no caminho do
   jogo. Se ela tiver uma subpasta `game`, o conteúdo dela é usado.
5. **Aplicar** e **Validar**. Corrija os erros antes de continuar.
6. **Abrir jogo** e teste de verdade. Confira o `black.log` se o arquivo não
   aparecer.
7. **Exportar .bmod**. O pacote já leva `mod.json` com os hashes.

O nome do arquivo deve seguir `id-versão.bmod`, por exemplo
`texto-ptbr-1.2.0.bmod`. Não use espaços nem acentos no `id`.

## 2. Envie por pull request (preferido)

1. Faça um fork do repositório público do kit.
2. Copie o `.bmod` para `comunidade/pacotes/`.
3. Rode o validador na sua máquina:

   ```powershell
   python ferramentas\validar_pacote.py comunidade\pacotes\seu-mod-1.0.0.bmod
   ```

4. Abra um pull request explicando:
   - o que o mod muda;
   - como testou (jogo, menu, fase, vídeo);
   - se ele conflita com outro mod e qual prioridade usar;
   - se o idioma é `ptbr`, `en` ou vale para todos.
5. O GitHub Actions valida o pacote. Se o CI falhar, corrija o pacote e envie de
   novo; não envie outro arquivo por cima sem apagar o antigo.

## 3. Envie por issue (se não usar Git)

Abra uma issue com o modelo **Enviar mod** e anexe o `.bmod`. Se o GitHub não
aceitar o anexo, hospede o arquivo em um link direto (Google Drive/Dropbox com
download liberado) e coloque o link na issue. Não mande executáveis nem arquivos
fora do `.bmod`.

Inclua:

- `id`, nome e versão;
- autor (ou "anônimo");
- descrição curta;
- prioridade sugerida;
- idioma;
- mods com que ele conflita, se souber;
- confirmação de que testou no jogo.

## 4. O que o mantenedor faz

1. Baixa o pacote e roda `validar_pacote.py`.
2. Importa com o `modkit` em uma instalação limpa. Se já existir o mesmo
   `id`, o launcher guarda o antigo em `mods\.backup`.
3. Confere os conflitos: em caso de arquivo repetido, maior `priority` vence;
   no empate, a ordem da pasta decide.
4. Testa no jogo.
5. Se estiver tudo certo, o pacote entra no próximo pacote de mods. Se houver
   conflito, o mod entra desligado e o autor é avisado.

## Checklist antes de enviar

- [ ] O pacote abre no launcher e passa em **Validar**.
- [ ] O mod foi testado no jogo, não só no launcher.
- [ ] O `id` é único e não tem espaços/acentos.
- [ ] O `mod.ini` e o `mod.json` têm o mesmo `name`, `priority` e `lang`.
- [ ] Só os arquivos alterados estão em `game\`; nada de jogo completo.
- [ ] Legendas `.srt` têm o mesmo nome do vídeo (`02_n.xmv` -> `02_n.srt`).
- [ ] Se conflita com outro mod, isso está escrito no envio.
- [ ] O pacote não tem `.exe`, `.bat`, `.ps1`, instalador ou link simbólico.
- [ ] Você tem direito de distribuir o conteúdo alterado.
