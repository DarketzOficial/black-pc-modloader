#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-2.0-or-later
"""Valida pacotes .bmod antes de entrarem no modloader.

Uso:
    python validar_pacote.py comunidade/pacotes/meu-mod-1.0.0.bmod
    python validar_pacote.py comunidade/pacotes/*.bmod

Confere o formato, os caminhos, os hashes SHA-256 e o mod.ini. Não instala nada.
O pacote é um ZIP com mod.json na raiz, mod.ini e game/; a pasta .bmod tem o
mesmo formato. O script é o mesmo usado pelo GitHub Actions.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except (AttributeError, OSError):
    pass

MAX_BYTES = 2 * 1024 * 1024 * 1024
MAX_ENTRIES = 20000
RESERVED = {"mod.json", ".off"}
ID_RE = re.compile(r"^[a-z0-9][a-z0-9._-]{0,63}$")
LANG_RE = re.compile(r"^[a-z0-9_-]+$")


def fail(message: str) -> None:
    print(f"[erro] {message}")


def warn(message: str) -> None:
    print(f"[aviso] {message}")


def info(message: str) -> None:
    print(f"[info] {message}")


def sha256_stream(stream) -> str:
    digest = hashlib.sha256()
    while True:
        block = stream.read(1024 * 1024)
        if not block:
            break
        digest.update(block)
    return digest.hexdigest().upper()


def normalize(path: str) -> str:
    return path.replace("\\", "/").strip()


def safe_path(path: str) -> bool:
    if not path or "\x00" in path:
        return False
    if path.startswith("/") or re.match(r"^[A-Za-z]:", path):
        return False
    parts = path.split("/")
    if any(part in {"", ".", ".."} for part in parts):
        return False
    return True


def read_ini(data: bytes) -> tuple[dict[str, str], list[str]]:
    values: dict[str, str] = {}
    errors: list[str] = []
    for encoding in ("utf-8-sig", "utf-8", "cp1252"):
        try:
            text = data.decode(encoding)
            break
        except UnicodeDecodeError:
            continue
    else:
        return values, ["mod.ini não está em UTF-8 nem cp1252"]

    for number, raw in enumerate(text.splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith(("#", ";")):
            continue
        if "=" not in line:
            errors.append(f"mod.ini linha {number}: falta '='")
            continue
        key, value = line.split("=", 1)
        key = key.strip().lower()
        value = value.strip()
        if not value:
            errors.append(f"mod.ini linha {number}: '{key}' está vazio")
            continue
        values[key] = value
    return values, errors


def validate(path: Path) -> bool:
    print(f"Pacote: {path}")
    if not path.is_file():
        fail("o arquivo não existe")
        return False

    try:
        archive = zipfile.ZipFile(path, "r")
    except (OSError, zipfile.BadZipFile) as exc:
        fail(f"não é um ZIP válido: {exc}")
        return False

    with archive:
        infos = archive.infolist()
        if len(infos) > MAX_ENTRIES:
            fail(f"pacote grande demais ({len(infos)} entradas)")
            return False
        total = sum(item.file_size for item in infos)
        if total > MAX_BYTES:
            fail("pacote grande demais (mais de 2 GB descompactado)")
            return False
        for item in infos:
            if item.flag_bits & 0x1:
                fail(f"entrada criptografada não é aceita: {item.filename}")
                return False

        manifest_info = None
        for item in infos:
            entry_name = normalize(item.filename)
            if entry_name == "mod.json" and not item.is_dir():
                manifest_info = item
                break
        if manifest_info is None:
            fail("falta mod.json na raiz do pacote")
            return False

        try:
            raw = archive.read(manifest_info)
            if len(raw) > 1024 * 1024:
                fail("mod.json grande demais")
                return False
            manifest = json.loads(raw.decode("utf-8-sig"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
            fail(f"mod.json inválido: {exc}")
            return False

        if not isinstance(manifest, dict):
            fail("mod.json precisa ser um objeto")
            return False
        if manifest.get("format") != 1:
            fail("mod.json: format diferente de 1")
            return False
        identifier = str(manifest.get("id", "")).strip()
        name = str(manifest.get("name", "")).strip()
        version = str(manifest.get("version", "")).strip()
        priority = manifest.get("priority", 0)
        lang = str(manifest.get("lang", "")).strip().lower()
        files = manifest.get("files")
        if not ID_RE.fullmatch(identifier):
            fail("mod.json: id deve ser minúsculo (a-z, 0-9, '.', '_' ou '-') e começar por letra/número")
            return False
        if not name:
            fail("mod.json: name vazio")
            return False
        if not version:
            fail("mod.json: version vazio")
            return False
        if not isinstance(priority, int):
            fail("mod.json: priority precisa ser inteiro")
            return False
        if lang and not LANG_RE.fullmatch(lang):
            fail("mod.json: lang inválido")
            return False
        if not isinstance(files, list) or not files:
            fail("mod.json: files vazio")
            return False
        for field in ("requires", "conflicts"):
            value = manifest.get(field, [])
            if value is None:
                continue
            if not isinstance(value, list) or any(not isinstance(item, str) for item in value):
                fail(f"mod.json: {field} precisa ser uma lista de ids")
                return False
            if any(not ID_RE.fullmatch(item) for item in value):
                fail(f"mod.json: {field} tem um id inválido")
                return False

        declared: dict[str, dict] = {}
        for entry in files:
            if not isinstance(entry, dict):
                fail("mod.json: item de files inválido")
                return False
            relative = normalize(str(entry.get("path", "")))
            if not safe_path(relative):
                fail(f"mod.json: caminho inválido: {relative!r}")
                return False
            if relative.lower() in RESERVED:
                fail(f"mod.json: caminho reservado: {relative}")
                return False
            key = relative.lower()
            if key in declared:
                fail(f"mod.json: caminho repetido: {relative}")
                return False
            declared[key] = entry

        actual: dict[str, zipfile.ZipInfo] = {}
        for item in infos:
            entry_name = normalize(item.filename)
            if item.is_dir():
                if not safe_path(entry_name.rstrip("/")):
                    fail(f"pacote com diretório inválido: {item.filename}")
                    return False
                continue
            if not safe_path(entry_name):
                fail(f"pacote com caminho inválido: {item.filename}")
                return False
            key = entry_name.lower()
            if key == "mod.json":
                continue
            if key in actual:
                fail(f"pacote com arquivo repetido: {entry_name}")
                return False
            actual[key] = item
            if key not in declared:
                fail(f"arquivo não declarado no mod.json: {entry_name}")
                return False

        for key, entry in declared.items():
            item = actual.get(key)
            if item is None:
                fail(f"falta o arquivo declarado: {entry.get('path')}")
                return False
            size = entry.get("size", 0)
            if size and int(size) != item.file_size:
                fail(f"tamanho diferente do mod.json: {entry.get('path')}")
                return False
            expected = str(entry.get("sha256", "")).strip()
            if not expected:
                fail(f"mod.json sem sha256 para: {entry.get('path')}")
                return False
            try:
                with archive.open(item) as stream:
                    actual_hash = sha256_stream(stream)
            except (OSError, zipfile.BadZipFile) as exc:
                fail(f"não foi possível ler {entry.get('path')}: {exc}")
                return False
            if actual_hash.lower() != expected.lower():
                fail(f"hash diferente do mod.json: {entry.get('path')}")
                return False

        ini_key = "mod.ini"
        ini_item = actual.get(ini_key)
        if ini_item is None:
            fail("o pacote precisa de mod.ini (o port lê esse arquivo)")
            return False
        try:
            ini_values, ini_errors = read_ini(archive.read(ini_item))
        except OSError as exc:
            fail(f"não foi possível ler mod.ini: {exc}")
            return False
        for error in ini_errors:
            fail(error)
        if ini_errors:
            return False
        if not ini_values.get("name"):
            fail("mod.ini: falta name")
            return False
        try:
            ini_priority = int(ini_values.get("priority", "0"))
        except ValueError:
            fail("mod.ini: priority não é inteiro")
            return False
        if ini_priority != priority:
            fail(f"mod.ini priority ({ini_priority}) difere do mod.json ({priority})")
            return False
        ini_lang = ini_values.get("lang", "").lower()
        if ini_lang != lang:
            fail(f"mod.ini lang ({ini_lang!r}) difere do mod.json ({lang!r})")
            return False
        if ini_values.get("name", "") != name:
            warn("mod.ini name difere do mod.json name; o port usa o do mod.ini")

        info(f"{identifier} {version} - {len(declared)} arquivo(s), {total // 1024} KiB")
        print("Pacote válido.")
        return True


def main() -> int:
    parser = argparse.ArgumentParser(description="Valida pacotes .bmod do BLACK PC.")
    parser.add_argument("packages", nargs="+", type=Path, help="arquivo(s) .bmod")
    args = parser.parse_args()

    ok = True
    for path in args.packages:
        if not validate(path.expanduser()):
            ok = False
        print()
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
