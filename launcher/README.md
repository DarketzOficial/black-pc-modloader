# BLACK PC — Launcher

The configuration window that ships inside the modloader package. It is the single player-facing
front end for the BLACK PC port and BLACK-XBERecomp. The selected target controls which settings
file is edited and which executable **PLAY** starts.

It is a small C# **WPF** app targeting **.NET Framework 4.8**, so the executable (~0.5 MB)
runs on any Windows 10/11 without installing a runtime. The exe carries the game icon and the
`BLACK` logo is shown on the banner.

## What it does

- Reads and writes the port's own `black.ini` (the same file the in-game `PC SETTINGS` use).
  Only the keys it owns are ever rewritten — comments, unknown keys and the port's ordering
  are preserved byte for byte.
- Tabs, grouped by what they change: **VÍDEO / GRÁFICOS / DESEMPENHO / ÁUDIO E JOGO / CONTROLES / MODS / AVANÇADO**.
  Every row has a **"?"** beside the label; hovering it shows that setting's explanation (the row tooltip
  still works too). The image filters, HDR, anti-aliasing, ambient occlusion, draw distance, the effects at
  Xbox size, the weapon blur and the geometry seams live in **GRÁFICOS**; frame cap, VSync and the FPS
  counter in **DESEMPENHO**.
- Key bindings with click-and-press capture (the same spelling and de-duplication rules as the
  port's in-game `KEY BINDINGS` page).
- The installed mods (`mods\*`), read the way each author packed them (a `game\` folder, the
  game's own folders at the top of the mod, or one wrapper folder below — the same rule as the
  port's, `Core/Mods.cs` `FindRoot`), with the same on/off marker (`.off`) and language blocking
  the port uses.
- The **weapon blur** switch (GRÁFICOS), the port's `weaponblur` key: the smear the game puts
  over the picture while the weapon reloads, on by default. The game's own `PC SETTINGS` has the
  same row (AUDIO AND GAME).
- **Trapaças (F5)** (MODS), the port's `trainer` key: with it on, F5 in the game opens the trainer's
  cheat menu over the picture (mouse free, the game keeps running; F5 closes it and the cheats stay).
  Infinite ammo, infinite grenades and god mode, plus the ARMAS tab that lists the current level's
  weapons and puts the one clicked in the player's hand. Per session only — the switch here is the
  on/off that sticks.
- The port's own Xbox-pad settings (**CONTROLES > JOYSTICK DE XBOX**): the sticks' dead zone, the
  right stick's sensitivity, vibration and the right stick's inverted axis. They are `black.ini`-only
  (`pad_deadzone`, `pad_sens`, `pad_vibration`, `pad_invert`) — the game's own menus have no row for
  them (`input/xinput.cpp` applies them). Right below them, **BOTÕES DO CONTROLE XBOX / XBOX CONTROLLER
  BUTTONS** lists every pad button with what the game does with it by default (the front end's
  `FE_ACTION_*` values, e.g. A = reload, X = action/use, RT = fire), for the player who wants to
  personalise the pad in the game's own `CONTROL SETUP > CUSTOMISE`.
- **Sound**: a looping background track (half volume) and interface effects — `hover.mp3` when the mouse
  enters an **option** (a switch, a list, a slider, a text field) or a **button** (the tabs, the footer's,
  the key fields, the window's own buttons; never the row around them), `select.mp3` on any click and
  `saved.mp3` with the **Salvado com sucesso / Saved successfully** notice when **SALVAR** writes `black.ini`.
  The speaker button next to **RESTAURAR** mutes and unmutes everything (remembered in `launcher.ini`).
  The clips are not loose files: they live encrypted inside `launcher.sounds`, one file beside the launcher
  (see [The sound file](#the-sound-file)).
- **JOGAR / PLAY** saves and starts `black.exe`. It stays greyed out (with the reason on hover)
  until `black.exe` and `game\default.xbe` are actually there.
- The target selector can also open BLACK-XBERecomp. It finds one matching sibling installation
  automatically or lets the player select its `BLACK.exe`; Recomp options are saved in that
  installation's `black.toml`, and its mods stay in that installation's `mods\` folder.
- Selecting Recomp starts its own `BLACK.exe` directly. The PC port's `black.ini`, DLL, updater,
  crash watcher and game files are not used for that launch.
- Language **PT-BR / English**, remembered in `launcher.ini` (not `black.ini`, which the port
  rewrites on exit).

Small touches that make it feel like an app and not a form:

- **Keyboard**: every toggle, slider, combo and key button is reachable with `Tab` and driven
  with `Space`/arrows; the focused control shows a ring. In a key field, `Delete` clears the
  slot and `Esc` cancels the capture.
- **Unsaved-changes feedback**: the moment anything changes, **SALVAR** turns gold and the
  status line says so; closing with pending changes asks before quitting.
- **It remembers where you were**: window size, position (maximised or not) and the last tab
  are restored next time, and kept in `launcher.ini` (nothing on-screen goes off-screen).
- **Panels redraw themselves**: assigning a key or flipping a mod refreshes just that list, so
  the view does not jump back to the top.
- **It says who made it**: the window's title is **ModLoader 1.4.10 for Black PC Port** (the version comes
  from the build), the banner carries the small **ModLoader 1.4.10** line under the BLACK logo and
  **By Darketz** in the top right corner, with the **YouTube** and **Discord** icons beside it (the
  Discord one opens `https://discord.gg/HKpgA9ghVE`).
- **It has its own title bar.** The system's is gone (`WindowStyle="None"` + `WindowChrome`): the launcher
  draws the caption itself, in the theme, with the name on the left and its own minimise / maximise / close
  buttons on the right (the close one turns red under the mouse, as every window's does). Everything the old
  bar did still works — dragging, double-click to maximise, resizing from the edges, Aero snap and the
  taskbar label — because the window is still a normal one; the chrome only redraws its non-client area.

For the PC port, the game itself still has its `PC SETTINGS` menus, and both read the same `black.ini`. What
differs is the split: a setting that only takes effect at the next start of the game (aspect
ratio, the HD pack, the mods) is **only** in the launcher — the game's pages leave those
rows out — so nothing in the game ever says "restart". MAX FRAME RATE is in both and applies
live (0 = unlimited, -1 = the monitor's refresh, or a fixed value). VERTICAL SYNC is launcher-only
(the game's DISPLAY page already holds its six rows): it waits for the desktop compositor's refresh
each picture (`vsync`, `--vsync` / `--no-vsync`). ANTI-ALIASING is launcher-only for the same reason:
OFF, the port's own edge-directed filter (FXAA, the default) or SMAA 1x, which is sharper and costs a
few milliseconds (`aa`, `--no-aa` / `--fxaa` / `--smaa`). AMBIENT OCCLUSION darkens the creases and corners
where the geometry folds into itself — the recomp's SSAO, at half the render size with 8 samples (ON) or at the
render size with the recomp's 16 (HIGH, sharper in the fine creases and several times heavier); `ao`,
`--ao` / `--ao-high` / `--no-ao`. HDR is launcher-only as well: a look on the final picture — glow from the bright
parts, highlights that roll off instead of clipping, a little more contrast and colour — with its own strength
slider (`hdr`, `hdr_strength`, `--hdr` / `--no-hdr` / `--hdr-strength N`). DRAW DISTANCE is launcher-only too (the
game's pages have no room left): 1x keeps the game's own LOD and cull distances, up to 16x keeps characters, props
and ground cover in reach longer, and 0.2x-0.75x shortens everything for slower PCs — below 1x the port's fog closes
in to that distance (20 m at 0.2x), free of charge (the game's own vertex fog), so what the shorter cull drops
fades into the fog instead of popping in (`draw_distance`, `--draw-distance N`).

## Recomp target

When **BLACK — XBE Recomp** is selected, the same launcher shows the display, graphics, gameplay,
audio and subtitle options supported by that build. It writes only matching top-level values to
the selected Recomp's `black.toml`, preserving unrelated keys and tables. File mods use that
installation's `mods\` folder and the Recomp's native file overlay. The ModLoader DLL is not
copied or loaded, and the PC port remains unchanged. Recomp does not use the PC port's crash
watcher or updater; unsupported PC-only controls are hidden for that target.
The target line shows executable version metadata when present and the selected XBE's SHA-1 when
`assets\default.xbe` is available. The hash identifies that file; it does not certify that a build
accepts it.
The same resolution presets and HD texture toggle/folder picker are available for both targets.
Recomp writes width/height to its settings and reads compatible `.dds` packs from the selected
folder (default `textures\` beside `BLACK.exe`); unsupported PC-port-only options remain hidden.

## Crash reports and logs

The visible launcher does not start `black.exe` itself any more. **PLAY** starts a hidden second
instance (`blacklauncher.exe --watch "<port>"`), which opens the game and waits for it. When the game
ends, that watcher moves `black.log` and `black-crash-*.log` from the port root into `log\game\` and
`log\crash\` (so the root stays clean) and closes silently after a normal exit.

If the game exits with an error, or writes a new `black-crash-*.log`, the watcher shows **CrashWindow**
in the launcher's own frame (same caption, banner, logo, colours and buttons). The player sees the log
name, the ModLoader/port versions, the PC and the menu where the game was, and chooses **ENVIAR** or
**NAO ENVIAR**. ENVIAR posts the log and the system/game information to the project's Discord webhook
(`#logs-black`); nothing is sent before that click. The webhook is a hidden `launcher.ini` preference,
`crash_webhook`, for test mirrors; the default is the project's channel. Attempts are written to
`log\launcher\reports.log` (no log contents).

If the player starts `black.exe` directly and it fails, the next launcher start finds the crash log in the
port root, moves it to `log\crash\` and shows the same report once.

The report includes the full `black-crash-*.log` (or `black.log` when the port left no crash log) as an
attachment, the ModLoader and port versions, Windows, CPU, GPU, RAM, screen, Windows user/machine, the
last front-end page, the active mods and the relevant `black.ini` settings. No save, password or game
file is sent.

## Changelog after an update

The updater saves the release body as `log\launcher\changelog-<version>.txt` before it restarts.
On the first start of the new version, the launcher shows **ChangelogWindow** once: the release
notes are grouped into section cards and the body scrolls when it is longer than the window. The
`changelog_seen` preference in `launcher.ini` remembers the version already seen. To open it again,
the player uses **ADVANCED > Update information**; that button always shows the notes of the
running version. A fresh install that has no saved notes uses the built-in text in `Core/Changelog.cs`.

## HD textures folder

The advanced tab keeps the HD texture pack path. The default is the port's
`mods\hdtextures\hd`; **CHANGE FOLDER/TROCAR PASTA** opens the native Windows file dialog at the
current folder and asks for `map.csv`. The folder that contains the chosen file is written to
`black.ini` when the player presses **SAVE**. The **X** beside the button clears the path; empty still means HD textures off.

## Support page

After the Discord icon in the banner, **DOAR/DONATE** opens `DonationWindow`: the PIX QR code on the
left and LivePix, Bitcoin, Dogecoin, Ethereum and USDT on the right. Every wallet address has a
copy button; LivePix opens the donation page in the browser. The page thanks the players and says
that the project resources, including the AI used during development (Claude Code), are paid for.
The project itself remains free.

## Updates (GitHub)


The launcher updates itself from the project's GitHub, through
`https://api.github.com/repos/DarketzOficial/black-pc-modloader/releases/latest`:

- **When**: one second after opening, if **AVANÇADO > PROCURAR ATUALIZAÇÃO AUTOMATICAMENTE** is on
  (the default; `update_auto` in `launcher.ini`), and whenever the footer's **PROCURAR ATUALIZAÇÃO**
  is pressed. The automatic check is silent when there is nothing new or the network fails.
- **What counts**: a release whose tag is a version (`1.4.10`, `2.0`; `v` is also accepted). The old full
  package (`modloader-1.0`) has no version tag and is ignored. The release must carry a `.zip`
  with `blacklauncher.exe` at its root (the updater prefers a name containing `modloader`).
- **The window**: **Nova atualização (x):** in the launcher's theme, with the release notes and
  **ATUALIZAR** / **DEIXAR PRA DEPOIS**. Atualizar downloads to `<port>\update\download`, extracts to
  `<port>\update\files`, validates `blacklauncher.exe`, saves only unsaved `black.ini` changes, starts
  the hidden `%TEMP%\black-pc-update-<pid>.ps1` helper, and closes.
- **The helper** waits for the launcher process to exit, overlays the package on the port, starts the
  launcher again and removes `<port>\update`. It **never** copies `black.ini`, `launcher.ini`,
  `black.log`, `savedata\`, `game\`, `mods\` or `update\`. The result is in
  `%TEMP%\black-pc-update.log`.
- **The package** is generated by `python pack_dist.py` as `dist\update\black-pc-modloader.zip`
  (`blacklauncher.exe`, `blackrt.dll`, `LEIA-ME.txt` at the root); attach it to the GitHub release.
  Tags, notes and the local test server are described in `data\UPDATES.md` in the port folder.
- `update_api` is a hidden `launcher.ini` preference for pointing the check at a local mirror; it is
  not shown in the UI.

## Building

```
dotnet build -c Release        # -> bin/Release/net48/blacklauncher.exe
```

or `./build.ps1` (builds and, with `-Deploy`, copies the exe next to `D:\black-pc\black.exe`).

`.NET Framework 4.x` targeting packs are **not** required: the build pulls the reference
assemblies from the `Microsoft.NETFramework.ReferenceAssemblies` NuGet package.

## The sound file

The four clips ship as one encrypted file, **`launcher.sounds`**, next to `blacklauncher.exe`:
no mp3s sit in the port folder, and the file does not spell out what it holds (each clip is
stored under a hash of its name). `Core/SoundBank.cs` is the whole format — AES-256-CBC per clip
with its own IV, an HMAC-SHA256 signature checked before any clip is used, and keys derived with
PBKDF2 from a passphrase that is in the executable.

That last sentence is the honest one: this is **obfuscation, not secrecy**. Anyone who wants the
audio can take it out of the exe; what the bank buys is a tidier port folder and no accidental
"here are the mp3s, copy them". A damaged or edited bank is refused (the launcher falls back to
loose mp3s, else stays silent) instead of being played as noise.

WPF's `MediaPlayer` only opens media **by path**, never from memory, so the launcher writes each
decrypted clip to a temporary file for as long as it runs — created with delete-on-close, erased
when it closes, and any left behind by a killed run is swept on the next start.

To change a clip or rebuild the bank (the originals are kept in `Assets/sounds/`):

```
dotnet build _test\sounds\Sounds.csproj -c Release
_test\sounds\bin\Release\net48\sounds.exe pack  D:\black-pc          # -> D:\black-pc\launcher.sounds
_test\sounds\bin\Release\net48\sounds.exe check D:\black-pc          # plays each clip back, reports length
```

`pack` takes each mp3 from the port folder when it is there, else from `Assets/sounds/`; `check`
really opens every clip with the media stack, which is the only way to know the file decrypted.

## Layout

| File | What |
|------|------|
| `Core/IniFile.cs` | black.ini reader/writer (the port's `settings.cpp` syntax) |
| `Core/Config.cs` | the managed keys, the port's defaults, save/reset, the multi-key settings |
| `Core/SettingsSchema.cs` | the tabs and rows (`fe/pc_settings.cpp` mirrored), labels PT/EN |
| `Core/KeyBindings.cs` | the key names/spelling (`input/bindings.cpp`) and the action list |
| `Core/Mods.cs` | the `mods\*` scan (the mod's root found by its contents), `mod.ini`, the `.off` marker, the HD-pack progress |
| `Core/GameLauncher.cs` | finding `black.exe`/`game\default.xbe`, starting the game and the hidden watcher |
| `Core/LogStore.cs` | moving `black.log`/`black-crash-*.log` into `log\game` and `log\crash` |
| `Core/CrashReporter.cs` | system/game information and the Discord webhook multipart report |
| `Core/CrashWatcher.cs` | hidden `--watch` mode: starts the game, waits and shows the report on failure |
| `CrashWindow.xaml(.cs)` | the crash report window, in the launcher frame |
| `ChangelogWindow.xaml(.cs)` | the update-notes window, once per update and from ADVANCED |
| `DonationWindow.xaml(.cs)` | the support page (PIX QR code and the other donation options) |
| `Core/DonationInfo.cs` | the support URLs and wallet addresses |
| `Core/Changelog.cs` | the built-in notes used when no release notes were saved |
| `Core/Audio.cs` | the music loop and the interface effects (`MediaPlayer`), the mute switch |
| `Core/SoundBank.cs` | the encrypted `launcher.sounds` file: format, keys, pack/unpack |
| `Core/Localization.cs` | every string, PT and EN |
| `Core/Updater.cs` | GitHub release check, download, safe extract, the PowerShell install helper |
| `Core/ThemedDialog.cs` | the themed message box (save-on-close, restore, play/update messages) |
| `UI/UiBuilder.cs` | the generated rows, the bindings, mods and pad panels, the "?" help bubbles |
| `MainWindow.xaml(.cs)` | the window: its own title bar, banner, tabs, footer, social icons, key capture, the save notice, the update checks |
| `UpdateWindow.xaml(.cs)` | the **Nova atualização** dialog: notes, version, download progress, Atualizar/Deixar pra depois |
| `Theme.xaml` | the dark theme (toggle switches, combo boxes, sliders, tabs, help bubbles, progress, the caption's buttons) |
| `Assets/sounds/` | the four mp3s themselves (the bank is built from these) |
| `_test/render/` | dev tool: draws one tab into a PNG at full height, without opening a window |
| `_test/engine/` | dev tool: reads/writes `black.ini` through `Core/` without the UI |
| `_test/sounds/` | dev tool: packs/checks `launcher.sounds` with the launcher's own `SoundBank` |

## Notes

- **Rows the user asked out or renamed.** `ANTI-ALIASING` left the game's VIDEO page (the launcher's GRÁFICOS carries
  it now; RESOLUTION SCALE 3X is the edge smoothing in the game's page). The resolution's automatic entry reads
  **AUTOMÁTICO / AUTOMATIC** (`MakeResolution` now follows the window's language; it always showed the PT label) and
  AIM MODE's choices read **SEGURAR / ALTERNAR** in Portuguese (HOLD / TOGGLE in English).
- **Help texts wrap.** A row's tool tip is a `TextBlock` with `TextWrapping` and a `MaxWidth` of 440 (the "?" bubble
  uses 420). WPF's default tool tip is a single line as wide as the text, so the longer helps (max frame rate) ran
  past the screen edge and were cut off.
- **No restart warnings.** The launcher is the pre-launch front end, so the rows whose setting applies at the next start
  carry no "REINICIAR"/"RESTART" chip, and `Core/Localization.cs`/`UI/UiBuilder.cs` no longer carry that chip at all
  (nor the AVANÇADO note that named the same settings). Those settings are gone from the game's menus as well — the
  port's `restart_only()` is the single list.
- **Combo text is set explicitly.** Inside a `ControlTemplate` a nested `Control` (the combo's own `ToggleButton`) does
  **not** inherit `Foreground` from the templated parent, so a plain `ContentPresenter` fell back to the system's black
  text — black on the dark panel. The toggle and its `ContentPresenter` now `TemplateBinding` the foreground (and the
  dropdown items too). Worth knowing: `PrintWindow` renders WPF differently from the screen and **hid** this for a while;
  the visual checks are done with a real `CopyFromScreen` capture, or with `PrintWindow(hwnd, hdc, 2)` — the
  `PW_RENDERFULLCONTENT` flag asks the compositor for the real surface and reads the window faithfully (the old
  "PrintWindow lies" warning is about flag 0) without needing the window in front.
- `black.exe` **must** stay the game process: it is the address-space owner that reserves
  `0x10000–0x800000` for the XBE, which `blackrt.dll` maps over it (see the port's `stub/stub.c`).
  The launcher never replaces it — it only starts it.
- Values in `black.ini` cannot contain spaces (the port parses them with `sscanf "%127s"`), so a
  port folder whose path has spaces cannot hold an absolute HD-textures path. Same limit the
  port itself has.
