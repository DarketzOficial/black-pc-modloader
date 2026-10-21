#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-2.0-or-later
"""Valida a estrutura de um mod de arquivo do BLACK PC.

Uso:
    python validar_mod.py caminho\\do\\mod
    python validar_mod.py caminho\\do\\mod --allow-empty

O script não altera o mod. Ele confere:
  - se a pasta realmente contém arquivos do jogo;
  - o mod.ini (name, priority, lang) e chaves desconhecidas;
  - o marcador .off;
  - legendas .srt sem vídeo correspondente;
  - arquivos de exemplo que ficaram no mod.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# No Windows o console nem sempre usa UTF-8; evita erro ao imprimir acentos.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except (AttributeError, OSError):
    pass

GAME_DIRS = {"chars", "data", "export", "language", "levels", "sound", "videos"}
GAME_FILES = {"default.xbe", "globdata.bin"}
PLACEHOLDER_PREFIXES = ("coloque-", "leia-me", "readme", "read-me")
KNOWN_INI_KEYS = {"name", "priority", "lang"}


def fail(message: str) -> None:
    print(f"[erro] {message}")


def warn(message: str) -> None:
    print(f"[aviso] {message}")


def info(message: str) -> None:
    print(f"[info] {message}")


def read_text(path: Path) -> str:
    data = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-8", "cp1252"):
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            continue
    return data.decode("utf-8", errors="replace")


def is_placeholder(path: Path) -> bool:
    name = path.name.lower()
    return name in {".gitkeep", "thumbs.db", "desktop.ini"} or name.startswith(PLACEHOLDER_PREFIXES)


def root_here(folder: Path) -> Path | None:
    game = folder / "game"
    if game.is_dir():
        return game
    if any((folder / name).is_dir() for name in GAME_DIRS):
        return folder
    if any((folder / name).is_file() for name in GAME_FILES):
        return folder
    return None


def find_root(mod: Path) -> tuple[Path | None, list[Path]]:
    """Espelha a descoberta do port: game\\, pastas do jogo na raiz ou uma pasta embrulho."""
    chain: list[Path] = []
    current = mod
    for _ in range(3):
        chain.append(current)
        found = root_here(current)
        if found is not None:
            return found, chain

        children = []
        try:
            for child in current.iterdir():
                if child.is_dir() and root_here(child) is not None:
                    children.append(child)
        except OSError:
            return None, chain

        if len(children) != 1:
            return None, chain
        current = children[0]
    return None, chain


def read_ini(path: Path) -> tuple[dict[str, str], list[str]]:
    values: dict[str, str] = {}
    errors: list[str] = []
    try:
        lines = read_text(path).splitlines()
    except OSError as exc:
        return values, [f"não foi possível ler {path.name}: {exc}"]

    for number, raw in enumerate(lines, 1):
        line = raw.strip()
        if not line or line.startswith(("#", ";")):
            continue
        if "=" not in line:
            errors.append(f"mod.ini linha {number}: falta o '=' (use chave = valor)")
            continue
        key, value = line.split("=", 1)
        key = key.strip().lower()
        value = value.strip()
        if key not in KNOWN_INI_KEYS:
            errors.append(f"mod.ini linha {number}: chave desconhecida '{key}'")
            continue
        if not value:
            errors.append(f"mod.ini linha {number}: '{key}' está vazio")
            continue
        values[key] = value
    return values, errors


def game_files(root: Path) -> list[Path]:
    files: list[Path] = []
    try:
        for path in root.rglob("*"):
            if path.is_file() and not is_placeholder(path):
                files.append(path)
    except OSError:
        pass
    return sorted(files)


def subtitles(chain: list[Path]) -> list[Path]:
    found: list[Path] = []
    seen: set[Path] = set()
    for folder in chain:
        sub = folder / "subtitles"
        if not sub.is_dir():
            continue
        for path in sorted(sub.glob("*.srt")):
            if path not in seen:
                seen.add(path)
                found.append(path)
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description="Valida um mod de arquivo do BLACK PC.")
    parser.add_argument("mod", type=Path, help="pasta do mod (a que fica dentro de mods\\)")
    parser.add_argument("--allow-empty", action="store_true",
                        help="aceita um mod sem arquivos de jogo (útil para o modelo)")
    args = parser.parse_args()

    mod = args.mod.expanduser().resolve()
    print(f"Mod: {mod}")

    if not mod.is_dir():
        fail("a pasta do mod não existe")
        return 1

    root, chain = find_root(mod)
    if root is None:
        fail("não encontrei game\\ nem as pastas do jogo (chars, data, export, language, levels, sound, videos)")
        info("a pasta pode ser o nível errado, ou o zip veio com uma pasta embrulho a mais")
        return 1

    # Estado: o marcador fica na pasta do mod, não na pasta game.
    state = "desligado (.off)" if (mod / ".off").exists() else "ligado"
    print(f"Arquivos do jogo: {root}")
    print(f"Estado: {state}")

    # mod.ini: o port lê o primeiro que encontrar descendo a pasta.
    ini_path = None
    for folder in chain:
        candidate = folder / "mod.ini"
        if candidate.is_file():
            ini_path = candidate
            break

    ini_errors: list[str] = []
    values: dict[str, str] = {}
    if ini_path is not None:
        values, ini_errors = read_ini(ini_path)
        print(f"mod.ini: {ini_path}")
        for error in ini_errors:
            fail(error)
        if "priority" in values:
            try:
                int(values["priority"])
            except ValueError:
                fail("mod.ini: 'priority' precisa ser um número inteiro")
        if "lang" in values and not re.fullmatch(r"[a-z0-9_-]+", values["lang"]):
            fail("mod.ini: 'lang' deve usar letras minúsculas, números, '_' ou '-' (ex.: ptbr)")
    else:
        info("sem mod.ini; o port usa o nome da pasta e não bloqueia por idioma")

    files = game_files(root)
    placeholders = [p for p in root.rglob("*") if p.is_file() and is_placeholder(p)]
    print(f"Arquivos de jogo: {len(files)}")
    if not files and not args.allow_empty:
        fail("nenhum arquivo de jogo encontrado (só avisos/modelos). Use --allow-empty para validar o modelo.")
    if placeholders and files:
        warn("ainda há arquivos de aviso/modelo junto com os arquivos do mod; apague os avisos")
    if not ini_errors and files:
        print("Estrutura básica: OK")

    # Legendas: cada .srt deve ter um vídeo com o mesmo nome em algum lugar do mod.
    videos = {p.stem.lower() for p in files if p.suffix.lower() in {".xmv", ".bik", ".mpg", ".mpeg"}}
    srt_files = subtitles(chain)
    if srt_files:
        print(f"Legendas: {len(srt_files)}")
        for srt in srt_files:
            if srt.stem.lower() not in videos:
                warn(f"legenda sem vídeo correspondente no mod: {srt.name}")

    errors = bool(ini_errors)
    if not files and not args.allow_empty:
        errors = True
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
