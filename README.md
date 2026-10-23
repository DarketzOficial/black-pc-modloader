# BLACK PC — ModLoader + Kit de desenvolvimento de mods

**BLACK PC** é o port do clássico **BLACK** (Xbox) para Windows, feito por
[Spark198rus](https://github.com/Spark198rus/black-pc), com uma camada de mods e um
launcher próprio.

Este repositório é o **kit aberto para criar mods** desse port. Ele reúne o
**ModKit** (mod launcher), o guia do formato `.bmod`, validadores, um
mod de exemplo e o fluxo de envio da comunidade.

> **O ModLoader é distribuído pela aba [Releases](https://github.com/DarketzOficial/black-pc-modloader/releases).**
> Baixe sempre a versão mais recente por lá. As versões são atualizadas com
> frequência e os pacotes ficam anexados às releases.
>
> O jogo **não** vem no pacote. Você precisa do seu dump de BLACK (Xbox NTSC-U) em
> `game\`, ao lado de `black.exe`.

---

## Baixar e instalar o ModLoader

1. Baixe o [BLACK PC port](https://github.com/Spark198rus/black-pc) e deixe o jogo
   em `game\`:

   ```text
   BLACK\                         (pasta do port)
     black.exe
     blackrt.dll
     game\                        default.xbe + arquivos do jogo
     savedata\
   ```

2. Abra a página de [Releases](https://github.com/DarketzOficial/black-pc-modloader/releases)
   e baixe um destes pacotes:

   | Asset | Quando usar |
   |---|---|
   | `BLACK-PC-Modloader-1.4.18-com-mods.zip` | Primeira instalação no Windows. Traz o ModLoader e os mods oficiais (PT-BR, texturas HD, ícones e prompts). |
   | `BLACK-PC-Modloader-1.4.18.zip` | Windows: pacote pequeno, usado para atualizar um ModLoader que já está instalado. |
   | `BLACK-PC-Modloader-1.4.18-linux.zip` | Linux: launcher nativo + `blackrt.dll` + `languages`. O jogo roda pelo Wine. |
   | `BLACK-PC-Modloader-1.4.18-linux-com-mods.zip` | Linux: o mesmo + mods oficiais. |
   | `BLACK-PC-ModKit-1.0.0.zip` | Kit de desenvolvimento com o `blackmodkit.exe` pronto para usar. Não é necessário para jogar. |

   Os nomes dos pacotes do ModLoader mudam conforme a versão. Use sempre os
   assets da release mais recente.

3. **Feche o jogo.** Extraia o conteúdo **dentro da pasta do port**, confirmando a
   substituição de `blackrt.dll`.

4. Abra **`blacklauncher.exe`**. Na primeira abertura, o launcher mostra as
   novidades da versão. Depois use **AVANÇADO > Informações da atualização** para
   rever as notas ou **PROCURAR ATUALIZAÇÃO** para atualizar.

5. Escolha os ajustes e mods e clique em **JOGAR**.

> Guarde uma cópia do seu `blackrt.dll`. O pacote completo também inclui
> `uninstaller-modloader.exe`, que remove a pasta `mods` e restaura o DLL anterior.
> O `uninstaller` não toca em `savedata\` nem em `black.ini`.

### Linux

O launcher Linux é **nativo** (Python/Tkinter); o jogo e o `blackrt.dll` são Win32
e rodam pelo **Wine** com suporte 32 bits.

1. Instale `python3`, `python3-tk` e `wine`/`wine32`.
2. Extraia `BLACK-PC-Modloader-1.4.18-linux-com-mods.zip` (ou o `-linux.zip`) na
   pasta do port, ou em qualquer pasta.
3. Rode `bash run-blacklauncher.sh` (ou `python3 blacklauncher-linux.py`).
4. Escolha a pasta do port, confirme o comando Wine e clique em
   **Instalar / Atualizar ModLoader** e depois **Jogar**.
5. A atualização automática já vem ligada na primeira instalação
   (`update_auto = 1` em `launcher-linux.ini`).
6. Na aba **Desempenho**, use **Instalar DXVK** (D3D9 → Vulkan), os presets
   Qualidade/Equilibrado/Desempenho, ESync/FSync, Gamescope e MangoHud.
   O DXVK é baixado do GitHub e copiado para o prefixo Wine.

### Atualizações

O launcher consulta a release mais recente do GitHub e baixa o pacote de
atualização. A atualização preserva:

- `black.ini` e `launcher.ini`;
- `savedata\`;
- `game\`;
- `mods\`;
- `log\`.

As notas de cada versão ficam na própria release e em
[`docs/RELEASE-NOTES-1.4.18.md`](docs/RELEASE-NOTES-1.4.18.md).

---

## Novidades da 1.4.18

- Corrigido o crash ao dar uma arma pelo trainer sem o sub-objeto de dados (`0x0013AE3`, arma 9).
- Corrigido o crash ao aplicar um efeito com entrada inválida na tabela (`0x001A7992`).
- Corrigido o crash no stream de áudio quando o ponteiro do formato está inválido (`0x00085A8A`).
- Aviso explícito para builds do jogo não suportadas (o port exige o `default.xbe` NTSC-U).
- **HUD:** só vida e munição em 16:9; objetivo, dicas e prompts no 4:3 original.
- **Launcher:** atualização automática ligada na primeira instalação.
- Port inalterado em **0.2.1**.

---
## O que tem neste repositório

| Pasta / arquivo | Conteúdo |
|---|---|
| [`modkit/`](modkit/) | Fonte do **ModKit**, o gerenciador de mods para criar, validar e empacotar mods. O executável pronto está no asset `BLACK-PC-ModKit-1.0.0.zip` da release. |
| [`docs/MODS.md`](docs/MODS.md) | Guia completo do formato de mod: estrutura, `mod.ini`, prioridade, idioma, legendas e conflitos. |
| [`exemplo/meu-mod/`](exemplo/meu-mod/) | Mod de exemplo com `mod.ini`, `mod.json` e arquivos de placeholders para testar o kit. |
| [`ferramentas/`](ferramentas/) | Validadores em Python para uma pasta de mod e para pacotes `.bmod`. |
| [`comunidade/`](comunidade/) | Como enviar um mod, checklist do mantenedor e a caixa de entrada de pacotes. |
| [`.github/workflows/`](.github/workflows/) | Validação automática dos `.bmod` enviados por pull request. |

O kit **não** inclui o jogo, arquivos do jogo, o port nem builds do ModLoader.
Esses arquivos ficam fora do repositório; o ModLoader é baixado pela página de
Releases.

---

## Comece a criar um mod

1. Baixe `BLACK-PC-ModKit-1.0.0.zip` na página de Releases e abra
   `blackmodkit.exe`, ou compile o fonte com
   [`modkit/build.ps1`](modkit/build.ps1).
2. Escolha a pasta do port, onde ficam `black.exe` e `game\default.xbe`.
3. Clique em **Novo mod** e informe nome, pasta/id, prioridade e idioma.
4. Use **Adicionar pasta...** para copiar os arquivos para dentro de `game\`,
   mantendo o caminho original.
5. Clique em **Validar** e depois em **Abrir jogo** para testar.
6. Se estiver tudo certo, clique em **Exportar .bmod** para gerar o pacote da
   comunidade.

Também dá para começar pelo exemplo:

```powershell
# Copie exemplo\meu-mod para a pasta mods\ do seu port
Copy-Item exemplo\meu-mod <pasta-do-port>\mods\meu-mod -Recurse
```

O exemplo começa desligado por causa do arquivo `.off`. Use o ModKit para ligar
ou apague o `.off`. Os arquivos de exemplo são placeholders; troque-os pelos
arquivos reais do jogo.

---

## Como um mod funciona

Um mod é uma pasta dentro de `mods\`, na pasta do port:

```text
BLACK\
  mods\
    meu-mod\
      mod.ini                  (opcional)
      .off                     (opcional: mod desligado)
      game\
        language\strings\MainUS.bin
        export\frontend\Core.bin
        videos\02_n.xmv
        ...
      subtitles\               (opcional: legendas .srt)
        02_n.srt
```

O caminho dentro de `game\` é o mesmo caminho do arquivo dentro do jogo. O
ModLoader entrega o arquivo do mod no lugar do original enquanto o mod estiver
ligado. Nada é alterado na instalação: desligar o mod volta a usar o arquivo
original.

### `mod.ini`

O `mod.ini` é o arquivo que o port lê. Uma opção por linha, no formato
`chave = valor`:

```ini
name = MINHA TEXTURA DO MURRO
priority = 5
lang = ptbr
```

| Chave | Para que serve |
|---|---|
| `name` | Nome mostrado na aba **MODS**. Sem ele, o port usa o nome da pasta. |
| `priority` | Número. Quando dois mods trazem o mesmo arquivo, o de maior prioridade vence. O padrão é `0`. |
| `lang` | Idioma do mod (`en`, `ptbr`, ...). Um mod de outro idioma fica bloqueado enquanto o idioma ativo for diferente. |

Sem `lang`, o mod vale em qualquer idioma. O `mod.json` é metadado do kit
(versão, autor, hashes); o port continua lendo apenas o `mod.ini`, então mods
antigos continuam funcionando.

### Ligar e desligar

- Sem `.off`, o mod começa **ligado**.
- Com `.off`, o mod começa **desligado**.
- O estado fica dentro da própria pasta do mod.
- Textos e outros arquivos lidos só no início do jogo precisam de um novo início
  para trocar. O launcher mostra `(REINICIAR)` nesse caso.

### Conflitos

Se dois mods ligados entregam o mesmo arquivo:

- vence o de maior `priority`;
- no empate, vence a ordem da pasta;
- se o conteúdo for idêntico, o ModKit marca como “mesmo conteúdo”;
- o perdedor continua instalado, mas não é usado naquele arquivo.

A aba **Conflitos** do ModKit mostra o caminho, os mods envolvidos e quem vence.

### Legendas

Um mod pode trazer uma pasta `subtitles\` com um `.srt` para cada vídeo:

```text
mods\meu-mod\subtitles\02_n.srt    ->    videos\02_n.xmv
```

Use SRT normal, em UTF-8. O tempo começa no início do vídeo.

Mais detalhes: [`docs/MODS.md`](docs/MODS.md).

---

## ModKit (mod launcher)

O `modkit` é uma ferramenta separada, só para mods:

- lista os mods, liga/desliga e mostra idioma, prioridade e conflitos;
- cria a pasta do mod, adiciona arquivos e valida a estrutura;
- exporta um `.bmod` com `mod.json` e hashes SHA-256;
- importa pacotes da comunidade, guarda o mod anterior em `mods\.backup` e
  instala lotes desligados para revisão.

Ele **não** tem updater, crash reporter nem opções do jogo. Só mexe em `mods\`
e em pacotes `.bmod`.

### Compilar o ModKit

Requisitos: Windows 10/11, .NET Framework 4.8 e .NET SDK 8 ou mais novo.

```powershell
cd modkit
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

O executável fica em `modkit\dist\blackmodkit.exe`.

### Testar o motor

```powershell
modkit\dist\blackmodkit.exe --selftest
```

O autoteste cria uma pasta temporária e verifica conflito de prioridade, idioma,
criação e leitura de pacote, importação, backup, hashes, arquivos extras,
zip-slip, caminhos inválidos e abertura das janelas. O código de saída é `0`
quando tudo passa.

---

## Testar um mod no jogo

- `--no-mods` inicia com todos os mods desligados.
- `--all-mods` liga todos os mods; útil para descobrir qual mod causa um problema.
- O `black.log` registra cada mod encontrado, a prioridade e o idioma.
- Em caso de conflito, o mod de maior `priority` vence. No empate, vale a ordem
  das pastas.

---

## Enviar um mod para a comunidade

O fluxo completo está em [`comunidade/README.md`](comunidade/README.md). O
resumo:

1. O autor exporta um `.bmod` pelo ModKit e testa no jogo.
2. Envia por pull request para `comunidade/pacotes/` ou por issue.
3. O CI roda `ferramentas/validar_pacote.py` e confere formato, caminhos, tamanho
   e SHA-256 de cada arquivo, além de arquivos não declarados.
4. O mantenedor importa em uma instalação limpa, confere conflitos e testa.
5. O mod entra no próximo pacote de mods; se conflitar, entra desligado.

### Validar antes de enviar

Pasta de mod:

```powershell
python ferramentas\validar_mod.py exemplo\meu-mod --allow-empty
python ferramentas\validar_mod.py mods\meu-mod
```

Pacote `.bmod`:

```powershell
python ferramentas\validar_pacote.py comunidade\pacotes\meu-mod-1.0.0.bmod
```

O validador de pacote é o mesmo usado no GitHub Actions. Ele recusa caminhos
absolutos, `..`, arquivos não declarados, hashes errados e `mod.ini` incoerente
com o manifesto.

---

## Compatibilidade

| | |
|---|---|
| **Funciona** | BLACK PC port do Spark198rus — https://github.com/Spark198rus/black-pc |
| **Não funciona** | BLACK-XBERecomp do TGP482 (projeto diferente, sem `blackrt.dll`) |
| **Versão do port** | 0.2.1 |
| **Jogo** | BLACK Xbox NTSC-U com dump em `game\` (o pacote não inclui o jogo) |
| **Sistema** | Windows 10/11, 32 ou 64 bits |
| **Hardware mínimo** | 4 GB de RAM, 1 GB de VRAM (Direct3D 9), 5 GB livres |

O `blackrt.dll` do ModLoader substitui o DLL do port. Não misture os arquivos com
o BLACK-XBERecomp.

---

## Segurança e limites do kit

- O importador recusa caminhos absolutos, `..`, links e arquivos não declarados.
- Cada arquivo é conferido pelo tamanho e SHA-256 antes de ser instalado.
- O pacote é limitado a 2 GB e 20.000 arquivos.
- Nenhum executável do pacote é executado.
- Substituições guardam o mod anterior em `mods\.backup`.
- O ModKit não se atualiza sozinho, não envia dados e não acessa a rede.
- O kit não distribui o jogo nem arquivos do jogo.

Encontrou um problema no kit ou em um mod? Abra uma issue usando os modelos em
[`.github/ISSUE_TEMPLATE/`](.github/ISSUE_TEMPLATE/).

---

## Documentação e licença

- Formato completo do mod: [`docs/MODS.md`](docs/MODS.md).
- Política de releases e assets: [`docs/RELEASES.md`](docs/RELEASES.md).
- Notas da versão atual: [`docs/RELEASE-NOTES-1.4.18.md`](docs/RELEASE-NOTES-1.4.18.md).
- Como enviar: [`comunidade/COMO-ENVIAR.md`](comunidade/COMO-ENVIAR.md).
- Checklist do mantenedor: [`comunidade/CHECKLIST-MANTENEDOR.md`](comunidade/CHECKLIST-MANTENEDOR.md).
- Fonte do ModKit: [`modkit/`](modkit/).

**Licença:** GPL-2.0-or-later. Veja [`LICENSE`](LICENSE).

BLACK é uma marca da Electronic Arts. Este kit não inclui o jogo nem arquivos do
jogo; você precisa ter o BLACK original e uma instalação do port para testar.

### Créditos

- **Spark198rus** — BLACK PC port.
- **DarketzOficial** — ModLoader, launcher e kit de mods.
- Comunidade de mods do BLACK PC.
