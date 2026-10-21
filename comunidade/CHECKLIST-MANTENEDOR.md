# Checklist do mantenedor

Use este roteiro ao receber um `.bmod`.

## Validação automática

```powershell
python ferramentas\validar_mod.py mods\meu-mod
python ferramentas\validar_pacote.py comunidade\pacotes\meu-mod-1.0.0.bmod
```

O `validar_pacote.py` confere:

- ZIP válido e sem criptografia;
- `mod.json` na raiz, formato 1, `id`/`name`/`version` válidos;
- cada arquivo declarado existe, com tamanho e SHA-256 corretos;
- nenhum arquivo extra fora do `mod.json` (evita payload escondido);
- caminhos sem `..`, absolutos ou caracteres inválidos;
- `mod.ini` presente e coerente com prioridade/idioma do manifesto;
- limite de 2 GB e 20.000 arquivos (proteção contra zip bomb).

## Instalação limpa

1. Abra o `modkit` em uma instalação de teste do port.
2. **Importar .bmod**: se já existir o mesmo `id`, escolha substituir; o launcher
   guarda o anterior em `mods\.backup`.
3. Veja a aba **Conflitos**:
   - mesmo conteúdo: não é problema;
   - conteúdo diferente: o vencedor é o de maior prioridade; no empate, a pasta.
4. Se o conflito não for intencional, peça ao autor para renomear o arquivo ou
   ajustar a prioridade. Não edite o mod de outra pessoa sem avisar.
5. Ligue só o mod novo (ou o conjunto que está sendo testado) e abra o jogo.

## Teste no jogo

- Abra o menu e entre em uma partida/fase onde o arquivo é usado.
- Confira texturas, textos, ícones, vídeo e legenda, conforme o mod.
- Veja o `black.log`: cada mod aparece com prioridade, idioma e, quando o jogo
  abre um arquivo, qual mod o entregou.
- Se o jogo não abrir, desligue o mod pelo `.off` ou pela pasta `mods` e confirme
  que o problema é o mod.

## Publicação

- Se o mod passou, mantenha o `.bmod` em `comunidade/pacotes/` e inclua a pasta
  no pacote de mods da próxima versão.
- Se o mod é grande, ele pode ficar fora do Git e entrar apenas no pacote de
  release; registre o link e o hash no PR/issue.
- Atualize o `id`/`version` quando o autor mandar uma correção; não substitua um
  pacote já publicado sem registrar a mudança.
- Nunca commite dados pessoais, caminhos locais, webhooks, chaves ou o jogo.
