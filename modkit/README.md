# BLACK PC ModKit

Ferramenta independente para **criar, validar, testar e empacotar mods** do
BLACK PC. Não é o launcher completo: não tem updater, doações, crash reporter nem
configurações do jogo. Só mexe em `mods\` e em pacotes `.bmod`.

## Requisitos

- Windows 10/11 com .NET Framework 4.8 (já vem no Windows).
- Uma instalação do BLACK PC port com `black.exe` e `game\default.xbe` para
  testar.
- Para compilar: .NET SDK 8 ou mais novo (`dotnet --version`).

## Compilar

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

O executável fica em `dist\blackmodkit.exe`. Para gerar o pacote de release,
copie o `dist\` junto com o kit.

Teste rápido do motor (sem abrir a janela):

```powershell
.\dist\blackmodkit.exe --selftest
```

Ele cria uma pasta temporária, testa conflito de prioridade, idioma, exportação,
importação, backup, hash, zip-slip e caminhos inválidos. O código de saída é `0`
quando tudo passa.

## Usar

1. Abra o `blackmodkit.exe`.
2. Escolha a pasta do port (onde ficam `black.exe` e `game\default.xbe`).
3. **Novo mod**: nome, pasta/id, prioridade e idioma.
4. **Adicionar pasta...**: escolha uma pasta com os arquivos já no caminho do
   jogo. Se ela tiver `game\`, o conteúdo dessa subpasta é usado.
5. **Aplicar** grava `mod.ini` e `mod.json`; **Validar** mostra erros e conflitos.
6. **Abrir jogo** inicia o `black.exe` para testar.
7. **Exportar .bmod** cria o pacote para a comunidade, com hashes SHA-256.

## Receber um mod

- **Importar .bmod**: verifica o pacote, instala em `mods\<id>` e mostra
  conflitos. Se já existir, o anterior vai para `mods\.backup`.
- **Importar pasta**: instala todos os `.bmod` de uma pasta **desligados**, para
  revisar um por um.
- Um arquivo repetido não quebra o jogo: o port usa a maior `priority`; no
  empate, a ordem da pasta. O launcher mostra o vencedor e permite desligar o mod
  importado.

## Formato

O port continua lendo só o `mod.ini` (`name`, `priority`, `lang`). O `mod.json` é
metadado do kit, com `id`, versão, autor, hashes e arquivos. O loader ignora o
`mod.json`; assim um mod antigo continua funcionando.

O pacote `.bmod` é um ZIP com `mod.json`, `mod.ini`, `game\` e, se houver,
`subtitles\`. O guia completo está em [`../docs/MODS.md`](../docs/MODS.md) e o
fluxo de envio em [`../comunidade/COMO-ENVIAR.md`](../comunidade/COMO-ENVIAR.md).

## Segurança

- O importador recusa caminhos absolutos, `..` e arquivos não declarados no
  manifesto; limita o pacote a 2 GB e 20.000 arquivos.
- Cada arquivo é conferido pelo tamanho e SHA-256 antes de ser instalado.
- Substituições guardam o mod anterior em `mods\.backup`.
- O app não se atualiza sozinho, não envia dados e não acessa a rede.

## Licença

GPL-2.0-or-later, a mesma do kit. Veja [`../LICENSE`](../LICENSE).
