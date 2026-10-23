#!/usr/bin/env python3
# BLACK PC ModLoader - native Linux launcher
#
# The game and the modloader are Win32 files from the BLACK PC port (Spark198rus).
# This launcher is native Linux/Python: it installs the modloader files into the
# game folder and starts black.exe through Wine. It never runs the game itself.
#
# It needs Python 3.8+ with Tkinter and Wine (wine / wine64 / a custom command).

from __future__ import annotations

import configparser
import hashlib
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import tarfile
import tempfile
import threading
import urllib.error
import urllib.request
import webbrowser
import zipfile
from pathlib import Path
from tkinter import filedialog, messagebox
from tkinter import ttk
import tkinter as tk

APP_VERSION = "1.4.18"
REPO = "DarketzOficial/black-pc-modloader"
RELEASES_PAGE = f"https://github.com/{REPO}/releases"
API_LATEST = f"https://api.github.com/repos/{REPO}/releases/latest"
DISCORD = "https://discord.gg/HKpgA9ghVE"
BASE = Path(__file__).resolve().parent
CONFIG_PATH = BASE / "launcher-linux.ini"
LAUNCHER = BASE / "blacklauncher-linux.py"
LEIA = BASE / "LEIA-ME-LINUX.txt"

DEFAULTS = {
    "game_dir": "",
    "wine": "wine",
    "wineprefix": "",
    "update_auto": "1",
    "windowed": "1",
    "skip_videos": "0",
    "dxvk": "1",
    "esync": "1",
    "fsync": "0",
    "mangohud": "0",
    "gamescope": "0",
    "preset": "Equilibrado",
}


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""):
            h.update(b)
    return h.hexdigest()


def version_tuple(s: str) -> tuple:
    s = s.strip().lstrip("vV")
    m = re.match(r"(\d+(?:\.\d+)*)", s)
    return tuple(int(x) for x in m.group(1).split(".")) if m else (0,)


class Config:
    def __init__(self, path: Path):
        self.path = path
        self.p = configparser.ConfigParser()
        if path.exists():
            try:
                self.p.read(path, encoding="utf-8")
            except Exception:
                self.p = configparser.ConfigParser()
        if not self.p.has_section("launcher"):
            self.p.add_section("launcher")
        changed = False
        for k, v in DEFAULTS.items():
            if not self.p.has_option("launcher", k):
                self.p.set("launcher", k, v)
                changed = True
        # First run (or an install that predates the key): automatic updates on.
        if not self.p.has_option("launcher", "update_auto"):
            self.p.set("launcher", "update_auto", "1")
            changed = True
        gd = self.p.get("launcher", "game_dir", fallback="").strip()
        if not gd and (BASE / "black.exe").is_file():
            self.p.set("launcher", "game_dir", str(BASE))
            changed = True
        if changed:
            self.save()

    def get(self, key: str) -> str:
        return self.p.get("launcher", key, fallback=DEFAULTS.get(key, "")).strip()

    def set(self, key: str, value: str) -> None:
        self.p.set("launcher", key, str(value))

    def save(self) -> None:
        with self.path.open("w", encoding="utf-8") as f:
            self.p.write(f, space_around_delimiters=True)


def find_exe(game: Path) -> Path | None:
    for name in ("black.exe", "BLACK.exe", "Black.exe"):
        p = game / name
        if p.is_file():
            return p
    return None


def game_ok(game: Path) -> bool:
    return game.is_dir() and find_exe(game) is not None and (game / "game").is_dir()


def copy_tree(src: Path, dst: Path) -> None:
    dst.mkdir(parents=True, exist_ok=True)
    for item in src.iterdir():
        target = dst / item.name
        if item.is_dir():
            copy_tree(item, target)
        else:
            shutil.copy2(item, target)


def ini_get(path: Path, key: str, default: str = "") -> str:
    if not path.is_file():
        return default
    pat = re.compile(r"^\s*" + re.escape(key) + r"\s*=\s*(.*?)\s*$", re.I)
    try:
        for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
            m = pat.match(line)
            if m:
                return m.group(1)
    except OSError:
        pass
    return default


def ini_set(path: Path, key: str, value: str) -> None:
    pat = re.compile(r"^\s*" + re.escape(key) + r"\s*=", re.I)
    lines = []
    found = False
    if path.is_file():
        try:
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            lines = []
    for i, line in enumerate(lines):
        if pat.match(line):
            lines[i] = f"{key} = {value}"
            found = True
            break
    if not found:
        lines.append(f"{key} = {value}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


class Launcher(tk.Tk):
    def __init__(self):
        super().__init__()
        self.cfg = Config(CONFIG_PATH)
        self.title(f"BLACK PC ModLoader {APP_VERSION} — Linux")
        self.geometry("980x700")
        self.minsize(860, 600)
        self.game = Path(self.cfg.get("game_dir")).expanduser().resolve() if self.cfg.get("game_dir") else None
        self.update_info = None
        self.game_proc = None
        self._build()
        self._refresh_game()
        self.after(1400, self._auto_update)

    # ---------------------------------------------------------------- layout
    def _build(self):
        style = ttk.Style(self)
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass
        style.configure("Title.TLabel", font=("Sans", 18, "bold"))
        style.configure("Sub.TLabel", foreground="#666")
        style.configure("Go.TButton", font=("Sans", 11, "bold"))

        head = ttk.Frame(self, padding=(16, 12, 16, 4))
        head.pack(fill="x")
        ttk.Label(head, text="BLACK PC ModLoader", style="Title.TLabel").pack(side="left")
        ttk.Label(head, text=f"  {APP_VERSION}  •  Linux", style="Sub.TLabel").pack(side="left", pady=(6, 0))

        self.nb = ttk.Notebook(self)
        self.nb.pack(fill="both", expand=True, padx=12, pady=6)
        self._tab_game()
        self._tab_performance()
        self._tab_mods()
        self._tab_settings()
        self._tab_updates()

        bar = ttk.Frame(self, padding=(12, 4, 12, 8))
        bar.pack(fill="x")
        self.status = ttk.Label(bar, text="Pronto.", style="Sub.TLabel")
        self.status.pack(side="left")
        ttk.Button(bar, text="Discord", command=lambda: webbrowser.open(DISCORD)).pack(side="right", padx=2)
        ttk.Button(bar, text="GitHub", command=lambda: webbrowser.open(f"https://github.com/{REPO}")).pack(side="right", padx=2)

    def _tab_game(self):
        f = ttk.Frame(self.nb, padding=16)
        self.nb.add(f, text="Jogo")
        ttk.Label(f, text="Pasta do port (onde está o black.exe)").pack(anchor="w")
        row = ttk.Frame(f)
        row.pack(fill="x", pady=(4, 12))
        self.game_var = tk.StringVar()
        ttk.Entry(row, textvariable=self.game_var).pack(side="left", fill="x", expand=True)
        ttk.Button(row, text="Procurar…", command=self._browse_game).pack(side="left", padx=4)
        ttk.Button(row, text="Usar esta pasta", command=self._use_here).pack(side="left")

        opts = ttk.LabelFrame(f, text="Wine", padding=10)
        opts.pack(fill="x", pady=4)
        r1 = ttk.Frame(opts)
        r1.pack(fill="x", pady=2)
        ttk.Label(r1, text="Comando", width=12).pack(side="left")
        self.wine_var = tk.StringVar(value=self.cfg.get("wine"))
        ttk.Entry(r1, textvariable=self.wine_var).pack(side="left", fill="x", expand=True)
        ttk.Label(r1, text="(wine, wine64, /caminho/wine)", style="Sub.TLabel").pack(side="left", padx=4)
        r2 = ttk.Frame(opts)
        r2.pack(fill="x", pady=2)
        ttk.Label(r2, text="WINEPREFIX", width=12).pack(side="left")
        self.prefix_var = tk.StringVar(value=self.cfg.get("wineprefix"))
        ttk.Entry(r2, textvariable=self.prefix_var).pack(side="left", fill="x", expand=True)
        ttk.Button(r2, text="Procurar…", command=self._browse_prefix).pack(side="left", padx=4)

        flags = ttk.Frame(opts)
        flags.pack(fill="x", pady=(6, 0))
        self.windowed_var = tk.BooleanVar(value=self.cfg.get("windowed") != "0")
        self.skip_var = tk.BooleanVar(value=self.cfg.get("skip_videos") == "1")
        ttk.Checkbutton(flags, text="Janela 1280x720", variable=self.windowed_var).pack(side="left")
        ttk.Checkbutton(flags, text="Pular vídeos", variable=self.skip_var).pack(side="left", padx=12)

        btns = ttk.Frame(f)
        btns.pack(fill="x", pady=16)
        ttk.Button(btns, text="Instalar / Atualizar ModLoader", command=self._install, style="Go.TButton").pack(side="left")
        ttk.Button(btns, text="Jogar", command=self._play, style="Go.TButton").pack(side="left", padx=8)
        ttk.Button(btns, text="Abrir pasta", command=self._open_game).pack(side="left")
        ttk.Button(btns, text="Restaurar blackrt.dll original", command=self._restore).pack(side="right")

        self.game_info = ttk.Label(f, text="", style="Sub.TLabel")
        self.game_info.pack(anchor="w", pady=8)
        ttk.Label(f, text=(
            "O launcher é nativo no Linux; o jogo e o modloader são Win32 e rodam pelo Wine.\n"
            "Extraia este pacote na pasta do port (a mesma do black.exe) ou escolha a pasta acima.\n"
            "O jogo não vem no pacote: use o seu dump NTSC-U na pasta game/."
        ), style="Sub.TLabel").pack(anchor="w", pady=12)

    def _tab_performance(self):
        f = ttk.Frame(self.nb, padding=16)
        self.nb.add(f, text="Desempenho")
        presets = ttk.LabelFrame(f, text="Preset do black.ini", padding=10)
        presets.pack(fill="x", pady=4)
        r = ttk.Frame(presets)
        r.pack(fill="x")
        self.preset_var = tk.StringVar(value=self.cfg.get("preset") or "Equilibrado")
        ttk.Label(r, text="Preset", width=12).pack(side="left")
        ttk.Combobox(r, textvariable=self.preset_var, values=["Qualidade", "Equilibrado", "Desempenho"],
                     state="readonly", width=16).pack(side="left")
        ttk.Button(r, text="Aplicar preset", command=self._apply_preset).pack(side="left", padx=6)
        ttk.Label(presets, text=(
            "Qualidade: escala 3x e AA.  Equilibrado: 2x e AA, 60 fps.  Desempenho: 1.5x, sem AA e efeitos no tamanho Xbox."
        ), style="Sub.TLabel").pack(anchor="w", pady=(6, 0))

        winebox = ttk.LabelFrame(f, text="Wine / DXVK", padding=10)
        winebox.pack(fill="x", pady=4)
        self.dxvk_var = tk.BooleanVar(value=self.cfg.get("dxvk") != "0")
        self.esync_var = tk.BooleanVar(value=self.cfg.get("esync") != "0")
        self.fsync_var = tk.BooleanVar(value=self.cfg.get("fsync") == "1")
        self.mangohud_var = tk.BooleanVar(value=self.cfg.get("mangohud") == "1")
        self.gamescope_var = tk.BooleanVar(value=self.cfg.get("gamescope") == "1")
        ttk.Checkbutton(winebox, text="DXVK (D3D9 → Vulkan; melhor desempenho)", variable=self.dxvk_var).pack(anchor="w")
        ttk.Checkbutton(winebox, text="ESync", variable=self.esync_var).pack(anchor="w")
        ttk.Checkbutton(winebox, text="FSync (precisa de kernel compatível)", variable=self.fsync_var).pack(anchor="w")
        ttk.Checkbutton(winebox, text="MangoHud (FPS)", variable=self.mangohud_var).pack(anchor="w")
        ttk.Checkbutton(winebox, text="Gamescope (janela/fullscreen do jogo)", variable=self.gamescope_var).pack(anchor="w")
        br = ttk.Frame(winebox)
        br.pack(fill="x", pady=(8, 0))
        ttk.Button(br, text="Instalar DXVK no prefixo", command=self._install_dxvk).pack(side="left")
        ttk.Button(br, text="Criar prefixo 32-bit", command=self._create_prefix).pack(side="left", padx=6)
        ttk.Button(br, text="Abrir prefixo", command=self._open_prefix).pack(side="left")
        ttk.Label(winebox, text=(
            "O prefixo é o WINEPREFIX da aba Jogo; vazio = ~/.wine. O DXVK é baixado do GitHub "
            "(doitsujin/dxvk) e copiado para system32/syswow64. O black.exe continua Win32."
        ), style="Sub.TLabel").pack(anchor="w", pady=(8, 0))
        self.perf_info = ttk.Label(f, text="", style="Sub.TLabel")
        self.perf_info.pack(anchor="w", pady=8)
        self._refresh_perf()

    def _tab_mods(self):
        f = ttk.Frame(self.nb, padding=16)
        self.nb.add(f, text="Mods")
        ttk.Label(f, text="Mods na pasta mods/ do jogo (o arquivo .off desliga o mod).", style="Sub.TLabel").pack(anchor="w")
        row = ttk.Frame(f)
        row.pack(fill="both", expand=True, pady=8)
        cols = ("name", "status", "priority")
        self.mods = ttk.Treeview(row, columns=cols, show="headings", selectmode="browse")
        for c, t, w in (("name", "Mod", 520), ("status", "Estado", 120), ("priority", "Prioridade", 100)):
            self.mods.heading(c, text=t)
            self.mods.column(c, width=w, anchor="w" if c == "name" else "center")
        sb = ttk.Scrollbar(row, orient="vertical", command=self.mods.yview)
        self.mods.configure(yscrollcommand=sb.set)
        self.mods.pack(side="left", fill="both", expand=True)
        sb.pack(side="right", fill="y")
        self.mods.bind("<Double-1>", lambda e: self._toggle_mod())
        btns = ttk.Frame(f)
        btns.pack(fill="x")
        ttk.Button(btns, text="Atualizar lista", command=self._refresh_mods).pack(side="left")
        ttk.Button(btns, text="Ativar / Desativar", command=self._toggle_mod).pack(side="left", padx=6)
        ttk.Button(btns, text="Abrir pasta mods", command=self._open_mods).pack(side="left")
        self.mods_info = ttk.Label(f, text="", style="Sub.TLabel")
        self.mods_info.pack(anchor="w", pady=6)

    def _tab_settings(self):
        f = ttk.Frame(self.nb, padding=16)
        self.nb.add(f, text="Configurações")
        box = ttk.LabelFrame(f, text="black.ini do jogo", padding=10)
        box.pack(fill="x", pady=4)
        self.lang_var = tk.StringVar(value="pt-BR")
        self.hud_var = tk.BooleanVar(value=True)
        self.full_var = tk.BooleanVar(value=False)
        r = ttk.Frame(box)
        r.pack(fill="x", pady=3)
        ttk.Label(r, text="Idioma", width=12).pack(side="left")
        ttk.Combobox(r, textvariable=self.lang_var, values=["en", "pt-BR", "es", "fr", "de", "it", "id", "ru"],
                     state="readonly", width=12).pack(side="left")
        ttk.Label(box, text="HUD 16:9 afeta só vida/munição; os textos ficam no 4:3 original.", style="Sub.TLabel").pack(anchor="w", pady=(6, 2))
        ttk.Checkbutton(box, text="HUD 16:9 (vida/munição)", variable=self.hud_var).pack(anchor="w")
        ttk.Checkbutton(box, text="Tela cheia", variable=self.full_var).pack(anchor="w")
        btns = ttk.Frame(f)
        btns.pack(fill="x", pady=12)
        ttk.Button(btns, text="Salvar no black.ini", command=self._save_black).pack(side="left")
        ttk.Button(btns, text="Abrir black.ini", command=self._open_black).pack(side="left", padx=6)
        ttk.Button(btns, text="Abrir pasta do jogo", command=self._open_game).pack(side="left")
        ttk.Label(f, text=(
            "Dependências no Linux: python3, python3-tk e Wine (32 bits para o port).\n"
            "Exemplo Debian/Ubuntu:  sudo apt install python3 python3-tk wine wine32\n"
            "O prefixo Wine precisa ter o Direct3D 9 disponível; o port usa D3D9."
        ), style="Sub.TLabel").pack(anchor="w", pady=8)

    def _tab_updates(self):
        f = ttk.Frame(self.nb, padding=16)
        self.nb.add(f, text="Atualizações")
        ttk.Label(f, text=f"Versão instalada: {APP_VERSION}", style="Title.TLabel").pack(anchor="w")
        ttk.Label(f, text="A procura automática liga na primeira instalação (update_auto = 1).", style="Sub.TLabel").pack(anchor="w", pady=(2, 8))
        row = ttk.Frame(f)
        row.pack(fill="x")
        ttk.Button(row, text="Procurar atualização", command=lambda: self._check_update(False)).pack(side="left")
        self.update_btn = ttk.Button(row, text="Baixar e instalar", command=self._apply_update, state="disabled")
        self.update_btn.pack(side="left", padx=6)
        ttk.Button(row, text="Abrir releases", command=lambda: webbrowser.open(RELEASES_PAGE)).pack(side="left")
        self.update_text = tk.Text(f, height=16, wrap="word", relief="flat", background="#f4f4f4")
        self.update_text.pack(fill="both", expand=True, pady=10)
        self.update_text.insert("1.0", "Clique em Procurar atualização.")
        self.update_text.configure(state="disabled")

    # ---------------------------------------------------------------- helpers
    def _set_status(self, text: str):
        self.status.configure(text=text)
        self.update_idletasks()

    def _game_or_warn(self) -> Path | None:
        raw = self.game_var.get().strip()
        g = Path(raw).expanduser().resolve() if raw else (self.game.resolve() if self.game else None)
        if not g or not game_ok(g):
            messagebox.showerror("Pasta do jogo", "Escolha a pasta do port que contém black.exe e game/default.xbe.")
            return None
        self.game = g
        self.cfg.set("game_dir", str(g))
        self.cfg.save()
        return g

    def _browse_game(self):
        d = filedialog.askdirectory(title="Pasta do port (black.exe)")
        if d:
            self.game_var.set(d)
            self.game = Path(d).resolve()
            self._refresh_game()

    def _use_here(self):
        self.game_var.set(str(BASE))
        self.game = BASE
        self._refresh_game()

    def _browse_prefix(self):
        d = filedialog.askdirectory(title="Prefixo Wine")
        if d:
            self.prefix_var.set(d)

    def _prefix_path(self) -> Path:
        p = self.prefix_var.get().strip()
        return Path(p).expanduser().resolve() if p else (Path.home() / ".wine")

    def _refresh_perf(self):
        if not hasattr(self, "perf_info"):
            return
        p = self._prefix_path()
        s64 = p / "drive_c" / "windows" / "syswow64" / "d3d9.dll"
        s32 = p / "drive_c" / "windows" / "system32" / "d3d9.dll"
        have = s64.exists() or s32.exists()
        self.perf_info.configure(text=f"Prefixo: {p}  •  DXVK/d3d9.dll: {'instalado' if have else 'não instalado'}")

    def _apply_preset(self):
        g = self._game_or_warn()
        if not g:
            return
        p = self.preset_var.get()
        if p == "Qualidade":
            vals = {"scale": "3.000", "aa": "1", "effectsnative": "0", "fps_cap": "0", "vsync": "0"}
        elif p == "Desempenho":
            vals = {"scale": "1.500", "aa": "0", "effectsnative": "1", "fps_cap": "60", "vsync": "0"}
        else:
            vals = {"scale": "2.000", "aa": "1", "effectsnative": "0", "fps_cap": "60", "vsync": "0"}
        try:
            ini_set(g / "black.ini", "render", "scale")
            for k, v in vals.items():
                ini_set(g / "black.ini", k, v)
            ini_set(g / "black.ini", "hudfade", "1")
            self.cfg.set("preset", p)
            self.cfg.save()
            self._load_black(g)
            self._set_status(f"Preset {p} aplicado no black.ini.")
        except OSError as e:
            messagebox.showerror("Preset", str(e))

    def _create_prefix(self):
        prefix = self._prefix_path()
        wine = self.wine_var.get().strip() or "wine"
        env = os.environ.copy()
        env["WINEPREFIX"] = str(prefix)
        env["WINEARCH"] = "win32"
        env.setdefault("WINEDEBUG", "-all")

        def work():
            try:
                subprocess.run(shlex.split(wine) + ["wineboot", "-u"], env=env,
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=180)
                self.after(0, lambda: (self._refresh_perf(), self._set_status("Prefixo 32-bit criado/atualizado.")))
            except FileNotFoundError:
                self.after(0, lambda: messagebox.showerror("Wine", f"Comando não encontrado: {wine}"))
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Prefixo Wine", str(e)))

        self._set_status("Criando prefixo Wine 32-bit…")
        threading.Thread(target=work, daemon=True).start()

    def _open_prefix(self):
        p = self._prefix_path()
        if not p.exists():
            messagebox.showinfo("Prefixo Wine", "O prefixo ainda não existe. Use Criar prefixo 32-bit.")
            return
        webbrowser.open(p.as_uri())

    def _install_dxvk(self):
        prefix = self._prefix_path()
        if not (prefix / "drive_c").exists():
            messagebox.showinfo("DXVK", "Crie o prefixo Wine primeiro (aba Desempenho).")
            return

        def work():
            try:
                req = urllib.request.Request(
                    "https://api.github.com/repos/doitsujin/dxvk/releases/latest",
                    headers={"User-Agent": f"BLACK-PC-Linux/{APP_VERSION}"})
                with urllib.request.urlopen(req, timeout=30) as r:
                    data = json.load(r)
                asset = next((a for a in data.get("assets", [])
                              if a.get("name", "").endswith(".tar.gz") and "src" not in a.get("name", "")), None)
                if not asset:
                    raise RuntimeError("release do DXVK sem .tar.gz")
                self.after(0, lambda: self._set_status(f"Baixando DXVK {data.get('tag_name', '')}…"))
                with tempfile.TemporaryDirectory(prefix="black-dxvk-") as td:
                    tarpath = Path(td) / "dxvk.tar.gz"
                    req = urllib.request.Request(asset["browser_download_url"],
                                                 headers={"User-Agent": f"BLACK-PC-Linux/{APP_VERSION}"})
                    with urllib.request.urlopen(req, timeout=180) as r, tarpath.open("wb") as f:
                        shutil.copyfileobj(r, f)
                    with tarfile.open(tarpath, "r:gz") as tf:
                        tf.extractall(td)
                    x32 = next((p for p in Path(td).rglob("d3d9.dll") if p.parent.name == "x32"), None)
                    x64 = next((p for p in Path(td).rglob("d3d9.dll") if p.parent.name == "x64"), None)
                    if not x32:
                        raise RuntimeError("d3d9.dll x32 não encontrado no DXVK")
                    s32 = prefix / "drive_c" / "windows" / "system32"
                    s64 = prefix / "drive_c" / "windows" / "syswow64"
                    s32.mkdir(parents=True, exist_ok=True)
                    if s64.is_dir():
                        s64.mkdir(parents=True, exist_ok=True)
                        for src, dst in ((x32, s64 / "d3d9.dll"), (x64, s32 / "d3d9.dll")):
                            if dst.exists() and not (dst.with_name("d3d9.dll.wine")).exists():
                                shutil.copy2(dst, dst.with_name("d3d9.dll.wine"))
                            if src:
                                shutil.copy2(src, dst)
                    else:
                        dst = s32 / "d3d9.dll"
                        if dst.exists() and not dst.with_name("d3d9.dll.wine").exists():
                            shutil.copy2(dst, dst.with_name("d3d9.dll.wine"))
                        shutil.copy2(x32, dst)
                self.after(0, lambda: (self.dxvk_var.set(True), self.cfg.set("dxvk", "1"), self.cfg.save(),
                                       self._refresh_perf(), self._set_status("DXVK instalado no prefixo.")))
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("DXVK", f"Falha:\n{e}"))

        self._set_status("Instalando DXVK…")
        threading.Thread(target=work, daemon=True).start()

    def _refresh_game(self):
        g = Path(self.game_var.get()).expanduser().resolve() if self.game_var.get().strip() else (self.game.resolve() if self.game else None)
        if g and game_ok(g):
            dll = g / "blackrt.dll"
            info = f"Jogo: {g}\nblack.exe: {find_exe(g).name}  •  blackrt.dll: {'presente' if dll.exists() else 'ausente'}"
            if dll.exists() and (BASE / "blackrt.dll").is_file():
                info += f"  •  modloader: {'igual ao pacote' if sha256(dll) == sha256(BASE / 'blackrt.dll') else 'diferente do pacote (clique Instalar)'}"
            self.game_info.configure(text=info)
            self._load_black(g)
            self._refresh_mods()
        else:
            self.game_info.configure(text="Nenhum port válido selecionado.")
            self.mods.delete(*self.mods.get_children())
            self.mods_info.configure(text="")

    def _load_black(self, g: Path):
        p = g / "black.ini"
        self.lang_var.set(ini_get(p, "language", "pt-BR") or "pt-BR")
        self.hud_var.set(ini_get(p, "hud169", "1") != "0")
        self.full_var.set(ini_get(p, "fullscreen", "0") != "0")

    def _open_game(self):
        g = self._game_or_warn()
        if g:
            webbrowser.open(g.as_uri())

    def _open_mods(self):
        g = self._game_or_warn()
        if g:
            (g / "mods").mkdir(exist_ok=True)
            webbrowser.open((g / "mods").as_uri())

    def _open_black(self):
        g = self._game_or_warn()
        if g and (g / "black.ini").exists():
            webbrowser.open((g / "black.ini").as_uri())
        else:
            messagebox.showinfo("black.ini", "O black.ini ainda não existe; abra o jogo uma vez.")

    # ---------------------------------------------------------------- install / play
    def _install(self):
        g = self._game_or_warn()
        if not g:
            return
        src = BASE / "blackrt.dll"
        if not src.is_file():
            messagebox.showerror("ModLoader", "blackrt.dll não encontrado ao lado do launcher. Extraia o pacote inteiro.")
            return
        dst = g / "blackrt.dll"
        try:
            if dst.exists() and sha256(dst) != sha256(src):
                backup = g / "blackrt.dll.original-do-port"
                if not backup.exists():
                    shutil.copy2(dst, backup)
                    self._set_status("Backup do blackrt.dll original criado.")
            shutil.copy2(src, dst)
            if (BASE / "languages").is_dir():
                copy_tree(BASE / "languages", g / "languages")
            if (BASE / "mods").is_dir():
                (g / "mods").mkdir(exist_ok=True)
                for child in (BASE / "mods").iterdir():
                    if child.is_dir() and not (g / "mods" / child.name).exists():
                        copy_tree(child, g / "mods" / child.name)
            self._set_status("ModLoader instalado/atualizado.")
            self._refresh_game()
            messagebox.showinfo("ModLoader", "ModLoader instalado.\nAgora use Jogar para abrir o jogo pelo Wine.")
        except OSError as e:
            messagebox.showerror("ModLoader", f"Falha ao instalar:\n{e}")

    def _restore(self):
        g = self._game_or_warn()
        if not g:
            return
        backup = g / "blackrt.dll.original-do-port"
        if not backup.is_file():
            messagebox.showinfo("Restaurar", "Não há backup (blackrt.dll.original-do-port).")
            return
        try:
            shutil.copy2(backup, g / "blackrt.dll")
            self._set_status("blackrt.dll original restaurado.")
            self._refresh_game()
        except OSError as e:
            messagebox.showerror("Restaurar", str(e))

    def _play(self):
        g = self._game_or_warn()
        if not g:
            return
        exe = find_exe(g)
        if exe is None:
            return
        self.cfg.set("wine", self.wine_var.get().strip() or "wine")
        self.cfg.set("wineprefix", self.prefix_var.get().strip())
        self.cfg.set("windowed", "1" if self.windowed_var.get() else "0")
        self.cfg.set("skip_videos", "1" if self.skip_var.get() else "0")
        self.cfg.set("dxvk", "1" if self.dxvk_var.get() else "0")
        self.cfg.set("esync", "1" if self.esync_var.get() else "0")
        self.cfg.set("fsync", "1" if self.fsync_var.get() else "0")
        self.cfg.set("mangohud", "1" if self.mangohud_var.get() else "0")
        self.cfg.set("gamescope", "1" if self.gamescope_var.get() else "0")
        self.cfg.save()
        prefix = self._prefix_path()
        if not (prefix / "drive_c").exists():
            if messagebox.askyesno("Prefixo Wine", f"O prefixo {prefix} ainda não existe. Criar agora (32-bit)?"):
                self._create_prefix()
            return
        wine = self.wine_var.get().strip() or "wine"
        args = shlex.split(wine) + [str(exe)]
        if self.windowed_var.get():
            args += ["--window", "1280x720", "--no-fullscreen"]
        if self.skip_var.get():
            args += ["--skip-videos"]
        env = os.environ.copy()
        env["WINEPREFIX"] = str(prefix)
        env.setdefault("WINEDEBUG", "-all")
        if self.esync_var.get():
            env["WINEESYNC"] = "1"
        if self.fsync_var.get():
            env["WINEFSYNC"] = "1"
        if self.mangohud_var.get():
            env["MANGOHUD"] = "1"
        if self.dxvk_var.get():
            old = env.get("WINEDLLOVERRIDES", "").strip(";,")
            env["WINEDLLOVERRIDES"] = (old + ";d3d9=n,b").strip(";")
        cmd = args
        if self.gamescope_var.get():
            gs = ["gamescope", "-W", "1280", "-H", "720"]
            if self.windowed_var.get():
                gs += ["-w", "1280", "-h", "720"]
            else:
                gs += ["-f"]
            cmd = gs + ["--"] + args
        if not (prefix / "drive_c" / "windows" / "syswow64" / "d3d9.dll").exists() and \
           not (prefix / "drive_c" / "windows" / "system32" / "d3d9.dll").exists():
            self._set_status("DXVK não instalado: usando o D3D9 do Wine (wined3d).")
        try:
            self.game_proc = subprocess.Popen(cmd, cwd=str(g), env=env)
            self._set_status(f"Jogo aberto (PID {self.game_proc.pid}).")
            self.after(1000, self._poll_game)
        except FileNotFoundError as e:
            what = "gamescope" if self.gamescope_var.get() else wine
            messagebox.showerror("Não encontrado",
                                 f"Não encontrei '{what}'.\n\nInstale o Wine (ex.: sudo apt install wine wine32) "
                                 "e o gamescope se estiver usando a opção dele.")
        except OSError as e:
            messagebox.showerror("Jogar", str(e))

    def _poll_game(self):
        if self.game_proc and self.game_proc.poll() is not None:
            self.game_proc = None
            self._set_status("Jogo fechado.")
        elif self.game_proc:
            self.after(1500, self._poll_game)

    # ---------------------------------------------------------------- mods
    def _refresh_mods(self):
        self.mods.delete(*self.mods.get_children())
        g = Path(self.game_var.get()).expanduser().resolve() if self.game_var.get().strip() else (self.game.resolve() if self.game else None)
        if not g or not (g / "mods").is_dir():
            self.mods_info.configure(text="Pasta mods/ vazia ou inexistente.")
            return
        count = 0
        for d in sorted((g / "mods").iterdir(), key=lambda p: p.name.lower()):
            if not d.is_dir():
                continue
            name, prio = d.name, 0
            ini = d / "mod.ini"
            if ini.is_file():
                try:
                    for line in ini.read_text(encoding="utf-8", errors="replace").splitlines():
                        if "=" in line:
                            k, v = line.split("=", 1)
                            k, v = k.strip().lower(), v.strip()
                            if k == "name" and v:
                                name = v
                            elif k == "priority":
                                try:
                                    prio = int(v)
                                except ValueError:
                                    pass
                except OSError:
                    pass
            state = "DESLIGADO" if (d / ".off").exists() else "LIGADO"
            self.mods.insert("", "end", iid=str(d), values=(name, state, prio))
            count += 1
        self.mods_info.configure(text=f"{count} mod(s) em {g / 'mods'}")

    def _toggle_mod(self):
        sel = self.mods.selection()
        if not sel:
            return
        d = Path(sel[0])
        off = d / ".off"
        try:
            if off.exists():
                off.unlink()
            else:
                off.touch()
            self._refresh_mods()
        except OSError as e:
            messagebox.showerror("Mods", str(e))

    # ---------------------------------------------------------------- settings
    def _save_black(self):
        g = self._game_or_warn()
        if not g:
            return
        p = g / "black.ini"
        try:
            ini_set(p, "language", self.lang_var.get())
            ini_set(p, "hud169", "1" if self.hud_var.get() else "0")
            ini_set(p, "fullscreen", "1" if self.full_var.get() else "0")
            self._set_status("black.ini atualizado.")
        except OSError as e:
            messagebox.showerror("black.ini", str(e))

    # ---------------------------------------------------------------- updates
    def _auto_update(self):
        if self.cfg.get("update_auto") != "0":
            self._check_update(True)

    def _check_update(self, silent: bool):
        def work():
            try:
                req = urllib.request.Request(API_LATEST, headers={"User-Agent": f"BLACK-PC-Linux/{APP_VERSION}"})
                with urllib.request.urlopen(req, timeout=20) as r:
                    data = json.load(r)
                tag = str(data.get("tag_name", "0")).lstrip("vV")
                if version_tuple(tag) <= version_tuple(APP_VERSION):
                    self.after(0, lambda: self._show_update(None, silent))
                    return
                assets = data.get("assets") or []
                asset = None
                for a in assets:
                    n = a.get("name", "")
                    if n.endswith("-linux.zip") and "com-mods" not in n:
                        asset = a
                        break
                self.after(0, lambda: self._show_update((tag, data.get("body", ""), asset, data.get("html_url")), silent))
            except Exception as e:
                if not silent:
                    self.after(0, lambda: messagebox.showerror("Atualizações", f"Não foi possível procurar:\n{e}"))
                else:
                    self.after(0, lambda: self._set_status("Sem conexão para procurar atualização."))
        threading.Thread(target=work, daemon=True).start()

    def _show_update(self, info, silent):
        self.update_info = info
        self.update_text.configure(state="normal")
        self.update_text.delete("1.0", "end")
        if not info:
            self.update_text.insert("1.0", f"Você já está na versão mais nova ({APP_VERSION}).")
            self.update_btn.configure(state="disabled")
            if not silent:
                self._set_status("Nenhuma atualização.")
        else:
            tag, body, asset, url = info
            self.update_text.insert("1.0", f"Nova versão {tag} disponível.\n\n{body}\n\nRelease: {url}")
            self.update_btn.configure(state="normal" if asset else "disabled")
            if not asset:
                self.update_text.insert("end", "\n\nEsta release ainda não tem um pacote Linux.")
            if not silent:
                self._set_status(f"Atualização {tag} disponível.")
            if silent:
                if messagebox.askyesno("Atualização", f"Nova versão {tag} do ModLoader. Baixar e instalar agora?"):
                    self._apply_update()
        self.update_text.configure(state="disabled")

    def _apply_update(self):
        if not self.update_info:
            return
        tag, body, asset, url = self.update_info
        if not asset:
            webbrowser.open(RELEASES_PAGE)
            return
        dest = BASE / ".update"
        try:
            if dest.exists():
                shutil.rmtree(dest)
            dest.mkdir()
            zpath = dest / asset["name"]
            self._set_status(f"Baixando {asset['name']}…")
            req = urllib.request.Request(asset["browser_download_url"], headers={"User-Agent": f"BLACK-PC-Linux/{APP_VERSION}"})
            with urllib.request.urlopen(req, timeout=120) as r, zpath.open("wb") as f:
                shutil.copyfileobj(r, f)
            with zipfile.ZipFile(zpath) as z:
                z.extractall(dest / "files")
            root = dest / "files"
            if not (root / "blacklauncher-linux.py").is_file():
                raise RuntimeError("O pacote não tem blacklauncher-linux.py.")
            apply = dest / "apply.sh"
            apply.write_text(
                "#!/bin/sh\n"
                "cd \"$(dirname \"$0\")\"\n"
                "sleep 1\n"
                "cp -a files/. ..\n"
                "cd ..\n"
                "python3 blacklauncher-linux.py\n"
                "rm -rf .update\n",
                encoding="utf-8",
            )
            apply.chmod(0o755)
            subprocess.Popen(["/bin/sh", str(apply)], cwd=str(dest))
            self._set_status("Atualização baixada; reiniciando…")
            self.after(300, self.destroy)
        except Exception as e:
            messagebox.showerror("Atualizar", f"Falha:\n{e}")


def selftest() -> int:
    ok = True
    print(f"BLACK PC ModLoader Linux {APP_VERSION}")
    print(f"base: {BASE}")
    for name in ("blackrt.dll", "languages", "blacklauncher-linux.py"):
        p = BASE / name
        print(f"  {'ok' if p.exists() else 'MISSING'}  {name}")
        ok = ok and p.exists()
    try:
        import tkinter  # noqa: F401
        print("  ok  tkinter")
    except Exception as e:
        print(f"  MISSING  tkinter ({e})")
        ok = False
    if sys.platform.startswith("win"):
        print("  note: Windows selftest; Wine/Jogo não testados neste host.")
    return 0 if ok else 1


def main() -> int:
    if "--selftest" in sys.argv:
        return selftest()
    if "--version" in sys.argv:
        print(APP_VERSION)
        return 0
    app = Launcher()
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
