// The port's settings as rows of the in-game PC SETTINGS pages (fe/pages.cpp builds the pages, fe/calls.cpp receives
// "PCChange(row, -1 / +1)" from their list boxes and "PCSlider<row>(0-100)" from their sliders). Each row is a list of choices: the current one is found from
// g_cfg, a step applies the next one and saves black.ini. Aspect ratio and frame rate are
// read by the game at boot: a new choice is saved for the next run and shown with "NEXT START" until then.
#include "fe/pc_settings.h"

#include "common.h"
#include "blur.h"
#include "d3d/device.h"
#include "d3d/host.h"
#include "fe/bind_list.h"
#include "fe/vars.h"
#include "fov.h"
#include "input/autoaim.h"
#include "mods.h"
#include "settings.h"

#include <algorithm>
#include <cmath>
#include <cwchar>

namespace pcset {
namespace {

bool close_to(float a, float b) { return std::fabs(a - b) < 0.01f; }
std::wstring on_off(bool on) { return on ? L"ON" : L"OFF"; }
std::wstring number(float v, const wchar_t *fmt = L"%.1f")
{
    wchar_t s[32];
    swprintf(s, 32, fmt, v);
    return s;
}

// RESOLUTION (PC SETTINGS > VIDEO): the window's client size, as the picture's height (the width follows from
// the aspect the game plays at). Entry 0 is the largest size of that aspect the desktop leaves free. The port
// renders at its own size (RESOLUTION SCALE below): this only decides how big the window is, also in fullscreen
// (the window is the whole monitor there, the choice applies again when you leave it).
const int kWindowHeights[] = {0, 480, 720, 768, 900, 1080, 1440, 2160};
constexpr int kWindowN = sizeof kWindowHeights / sizeof kWindowHeights[0];
int window_get()
{
    if (g_cfg.window_w <= 0 && g_cfg.window_h <= 0) return 0; // the desktop fit (the port's default)
    if (g_cfg.window_w <= 0)
        for (int i = 1; i < kWindowN; i++)
            if (kWindowHeights[i] == g_cfg.window_h) return i;
    return -1; // --window WxH from the command line or black.ini
}
void window_set(int i)
{
    g_cfg.window_w = 0;
    g_cfg.window_h = kWindowHeights[i];
    host3d::set_window_size(g_cfg.window_w, g_cfg.window_h);
}
std::wstring window_text(int i)
{
    wchar_t s[32];
    if (i < 0) { // --window WxH from the command line or black.ini: show the size it is
        if (g_cfg.window_w > 0 && g_cfg.window_h > 0) {
            swprintf(s, 32, L"%d X %d", g_cfg.window_w, g_cfg.window_h);
            return s;
        }
        return L"CUSTOM";
    }
    if (i == 0) return L"AUTOMATIC"; // the desktop fit: the game takes the monitor's own size
    int h = kWindowHeights[i], aw = g_cfg.widescreen ? 16 : 4, ah = g_cfg.widescreen ? 9 : 3;
    swprintf(s, 32, L"%d X %d", h * aw / ah, h);
    return s;
}

// RESOLUTION SCALE: the game's internal resolution, K x the Xbox buffer (640x480, 853x480 in widescreen, where
// the game draws its 16:9 picture into the 4:3 buffer: anamorphic). 3x is the port's default (2560x1440 in
// widescreen); the host render targets follow it, and the picture is then shown at the window's size.
const float kScales[] = {0.5f, 1.0f, 1.5f, 2.0f, 3.0f, 4.0f};
constexpr int kScaleN = sizeof kScales / sizeof kScales[0];
int scale_get()
{
    if (g_cfg.render_native || g_cfg.render_h > 0) return -1; // --resolution (native, a height or WxH)
    for (int i = 0; i < kScaleN; i++)
        if (close_to(g_cfg.scale, kScales[i])) return i;
    return -1;
}
void scale_set(int i)
{
    g_cfg.render_native = false;
    g_cfg.render_w = g_cfg.render_h = 0;
    g_cfg.scale = kScales[i];
    xd3d::request_render_change();
}
std::wstring scale_text(int i)
{
    if (i < 0) return L"CUSTOM";
    wchar_t s[32];
    swprintf(s, 32, L"%gX%s", kScales[i], i == 4 ? L" (ORIGINAL)" : L"");
    return s;
}

// A slider row has the 26 positions of the game's slider clip (Sound Setup's bullet bar): brightness and gamma
// 0.5-3.0 in steps of 0.1, mouse sensitivity 0.25-6.5 in steps of 0.25. The field of view, the viewmodel and the
// image filters have rows of their own with fewer positions (90-120 and 70-80 in steps of 2, 0-100 in steps of 5);
// the slider code below uses each row's count.
constexpr int kSteps = 26;
constexpr int kFovSteps = 16;   // 90..120 in steps of 2
constexpr int kVmSteps = 6;     // 70..80 in steps of 2 (80 is the cap: past 70 the arms' ends start to show)
constexpr int kFilterSteps = 21; // 0..100 in steps of 5 (0 = off)
int step_of(float v, float first, float step) { return std::clamp((int)std::lround((v - first) / step), 0, kSteps - 1); }
const float kReverb[] = {0.0f, 0.5f, 1.0f, 1.5f, 2.0f}; // 0 = off

template <size_t N> int nearest(const float (&list)[N], float v)
{
    int best = 0;
    for (int i = 1; i < (int)N; i++)
        if (std::fabs(list[i] - v) < std::fabs(list[best] - v)) best = i;
    return best;
}

// Choice tables for the update_port settings (same values as menu.cpp there).
const int kAniso[] = {0, 2, 4, 8, 16};
int aniso_get()
{
    for (int i = 0; i < 5; i++)
        if (kAniso[i] == g_cfg.aniso) return i;
    return 0;
}
// MAX FRAME RATE (the recomp's model): -1 just under the monitor's refresh, 0 unlimited, else that many fps.
const int kFpsCaps[] = {-1, 0, 30, 60, 75, 90, 120, 144, 165, 240, 360};
constexpr int kFpsCapCount = sizeof kFpsCaps / sizeof kFpsCaps[0];
int fps_cap_get()
{
    for (int i = 0; i < kFpsCapCount; i++)
        if (kFpsCaps[i] == g_cfg.fps_cap) return i;
    return 1; // unlimited for a command-line value outside the list
}
const int kWalkSpeeds[] = {25, 50, 75, 100};
int walk_speed_get()
{
    int best = 1; // 50: the update_port default
    for (int i = 0; i < 4; i++) {
        int d = kWalkSpeeds[i] - g_cfg.walk_speed, b = kWalkSpeeds[best] - g_cfg.walk_speed;
        if ((d < 0 ? -d : d) < (b < 0 ? -b : b)) best = i;
    }
    return best;
}

bool shown_wide() { return g_cfg.widescreen_next >= 0 ? g_cfg.widescreen_next != 0 : g_cfg.widescreen; }

struct Row {
    const char *label;
    int choices;
    int (*get)();
    void (*set)(int);
    std::wstring (*text)(int);
    bool slider = false;
    bool custom = false; // get() may be outside the list (a size or scale from the command line): show CUSTOM
};

const Row kRows[] = {
    // VIDEO
    {"RESOLUTION", kWindowN, window_get, window_set, window_text, false, true},
    {"RESOLUTION SCALE", kScaleN, scale_get, scale_set, scale_text, false, true},
    {"FIELD OF VIEW", kFovSteps, [] { return std::clamp(step_of(g_cfg.fov, 90, 2), 0, kFovSteps - 1); },
     [](int i) {
         g_cfg.fov = 90.0f + 2 * i;
         fov_set(g_cfg.fov);
     },
     [](int i) { return number(90.0f + 2 * i, L"%.0f"); }, true},
    {"BRIGHTNESS", kSteps, [] { return step_of(g_cfg.gain, 0.5f, 0.1f); }, [](int i) { g_cfg.gain = 0.5f + i * 0.1f; },
     [](int i) { return number(0.5f + i * 0.1f); }, true},
    {"GAMMA", kSteps, [] { return step_of(g_cfg.gamma, 0.5f, 0.1f); }, [](int i) { g_cfg.gamma = 0.5f + i * 0.1f; },
     [](int i) { return number(0.5f + i * 0.1f); }, true},
    {"FULLSCREEN", 2, [] { return (int)host3d::fullscreen(); }, [](int i) { host3d::set_fullscreen(i != 0); },
     [](int i) { return on_off(i); }},
    // DISPLAY
    {"ASPECT RATIO", 2, [] { return (int)!shown_wide(); },
     [](int i) {
         bool wide = i == 0;
         g_cfg.widescreen_next = wide == g_cfg.widescreen ? -1 : wide;
     },
     [](int i) { return std::wstring(i == 0 ? L"16:9" : L"4:3") + ((i == 0) != g_cfg.widescreen ? L" (NEXT START)" : L""); }},
    {"MAX FRAME RATE", kFpsCapCount, fps_cap_get, [](int i) { g_cfg.fps_cap = kFpsCaps[i]; },
     [](int i) {
         if (kFpsCaps[i] == -1) return std::wstring(L"REFRESH RATE");
         if (kFpsCaps[i] == 0) return std::wstring(L"UNLIMITED");
         return number((float)kFpsCaps[i], L"%.0f");
     }},
    {"FILL GEOMETRY SEAMS", 2, [] { return (int)g_cfg.seam_fill; }, [](int i) { g_cfg.seam_fill = i != 0; }, [](int i) { return on_off(i); }},
    {"EFFECTS AT XBOX SIZE", 2, [] { return (int)g_cfg.effects_native; },
     [](int i) {
         g_cfg.effects_native = i != 0;
         xd3d::request_render_rebuild();
     },
     [](int i) { return on_off(i); }},
    // AUDIO AND GAME
    {"ROOM REVERB", 5, [] { return g_cfg.reverb ? std::max(1, nearest(kReverb, g_cfg.reverb_gain)) : 0; },
     [](int i) {
         g_cfg.reverb = i != 0;
         if (i) g_cfg.reverb_gain = kReverb[i];
     },
     [](int i) { return i ? number(kReverb[i] * 100, L"%.0f%%") : std::wstring(L"OFF"); }},
    {"AUTO-AIM", 2, [] { return (int)g_cfg.autoaim; }, [](int i) { autoaim_set(i != 0); }, [](int i) { return on_off(i); }},
    {"SKIP ANY MOVIE", 2, [] { return (int)g_cfg.skip_movies; }, [](int i) { g_cfg.skip_movies = i != 0; }, [](int i) { return on_off(i); }},
    // CONTROLS
    {"MOUSE SENSITIVITY", kSteps, [] { return step_of(g_cfg.mouse_sens, 0.25f, 0.25f); },
     [](int i) { g_cfg.mouse_sens = 0.25f * (i + 1); }, [](int i) { return number(0.25f * (i + 1), L"%.2f"); }, true},
    {"INVERT MOUSE", 2, [] { return (int)g_cfg.mouse_invert; }, [](int i) { g_cfg.mouse_invert = i != 0; }, [](int i) { return on_off(i); }},
    {"KEY BINDINGS", 1, [] { return 0; }, [](int) {}, [](int) { return std::wstring(); }}, // opens the bindings page
    {"HUD 16:9", 2, [] { return (int)g_cfg.hud169; }, [](int i) { g_cfg.hud169 = i != 0; }, [](int i) { return on_off(i); }},
    {"FADING HUD", 2, [] { return (int)g_cfg.hud_fade; }, [](int i) { g_cfg.hud_fade = i != 0; }, [](int i) { return on_off(i); }},
    {"HD TEXTURES", 2, [] { return g_cfg.hd_textures.empty() ? 0 : 1; },
     [](int i) {
         g_cfg.hd_textures = i ? g_cfg.port_dir + L"\\mods\\hdtextures\\hd" : L"";
         settings_mark_cli("hd_textures");
     },
     [](int i) {
         // how much of the pack is done, beside ON: the textures it replaces over the game's textures in the area it
         // covers (d3d/texture.cpp hd_progress, from map.csv and the pack's progress.txt)
         if (i == 0) return on_off(0);
         int matched = 0, total = 0;
         xd3d::hd_progress(&matched, &total);
         if (matched <= 0 || total <= 0) return on_off(1);
         return on_off(1) + L" " + number(100.0f * (float)matched / (float)total, L"%.0f%%");
     }},
    // Settings from the update_port sources (see data/PORT_SETTINGS.md), on the pages above.
    {"ANISOTROPIC FILTERING", 5, [] { return aniso_get(); }, [](int i) { g_cfg.aniso = kAniso[i]; },
     [](int i) { return i ? number((float)kAniso[i], L"%.0fX") : std::wstring(L"OFF"); }},
    {"VIEWMODEL FOV", kVmSteps, [] { return std::clamp(step_of(g_cfg.viewmodel_fov, 70, 2), 0, kVmSteps - 1); },
     [](int i) { g_cfg.viewmodel_fov = 70.0f + 2 * i; },
     [](int i) { return number(70.0f + 2 * i, L"%.0f") + (i == 0 ? L" (ORIGINAL)" : L""); }, true},
    {"FMV SCALE", 2, [] { return g_cfg.fmv_scale ? 1 : 0; }, [](int i) { g_cfg.fmv_scale = i; },
     [](int i) { return i ? std::wstring(L"4:3") : std::wstring(L"FILL"); }},
    {"MAX FRAME RATE", kFpsCapCount, fps_cap_get, [](int i) { g_cfg.fps_cap = kFpsCaps[i]; },
     [](int i) {
         if (kFpsCaps[i] == -1) return std::wstring(L"REFRESH RATE");
         if (kFpsCaps[i] == 0) return std::wstring(L"UNLIMITED");
         return number((float)kFpsCaps[i], L"%.0f");
     }},
    {"FPS COUNTER", 2, [] { return (int)g_cfg.fpscounter; }, [](int i) { g_cfg.fpscounter = i != 0; },
     [](int i) { return on_off(i); }},
    {"PAUSE WITHOUT FOCUS", 2, [] { return (int)g_cfg.pause_focus; }, [](int i) { g_cfg.pause_focus = i != 0; },
     [](int i) { return on_off(i); }},
    {"AIM MODE", 2, [] { return (int)g_cfg.aim_toggle; }, [](int i) { g_cfg.aim_toggle = i != 0; },
     [](int i) { return i ? std::wstring(L"TOGGLE") : std::wstring(L"HOLD"); }},
    {"WALK SPEED", 4, [] { return walk_speed_get(); }, [](int i) { g_cfg.walk_speed = kWalkSpeeds[i]; },
     [](int i) { return number((float)kWalkSpeeds[i], L"%.0f%%"); }},
    // Frame generation: the game keeps its 60 fps (the real speed) and the port shows a generated frame in between,
    // so a 120 Hz display gets 120 pictures. Only when the game's own work fits in half a frame (see
    // d3d/device.cpp present_frame_gen); otherwise it presents as usual and the row says why.
    {"FRAME GENERATION", 2, [] { return (int)g_cfg.frame_gen; }, [](int i) { g_cfg.frame_gen = i != 0; },
     [](int i) { return on_off(i != 0); }},
    // The reload blur (blur.cpp): the smear the game puts over the picture while a weapon reloads. It is not a
    // restart-only row -- the reload start reads the port's value, so it applies in play at once, menu or no menu.
    {"WEAPON BLUR", 2, [] { return (int)g_cfg.weapon_blur; },
     [](int i) {
         g_cfg.weapon_blur = i != 0;
         blur_set(g_cfg.weapon_blur);
     },
     [](int i) { return on_off(i); }},
    // Image filters (d3d/device.cpp: the pass after the gamma ramp, before the anti-aliasing). They apply at once
    // (the present reads g_cfg every frame), so no restart. 0 = OFF.
    {"SHARPENING", kFilterSteps, [] { return std::clamp(step_of((float)g_cfg.sharpen, 0, 5), 0, kFilterSteps - 1); },
     [](int i) { g_cfg.sharpen = i * 5; },
     [](int i) { return i ? number((float)(i * 5), L"%.0f") : std::wstring(L"OFF"); }, true},
    {"DETAILS", kFilterSteps, [] { return std::clamp(step_of((float)g_cfg.details, 0, 5), 0, kFilterSteps - 1); },
     [](int i) { g_cfg.details = i * 5; },
     [](int i) { return i ? number((float)(i * 5), L"%.0f") : std::wstring(L"OFF"); }, true},
    {"CLARITY", kFilterSteps, [] { return std::clamp(step_of((float)g_cfg.clarity, 0, 5), 0, kFilterSteps - 1); },
     [](int i) { g_cfg.clarity = i * 5; },
     [](int i) { return i ? number((float)(i * 5), L"%.0f") : std::wstring(L"OFF"); }, true},
    {"VIBRANCE", kFilterSteps, [] { return std::clamp(step_of((float)g_cfg.vibrance, 0, 5), 0, kFilterSteps - 1); },
     [](int i) { g_cfg.vibrance = i * 5; },
     [](int i) { return i ? number((float)(i * 5), L"%.0f") : std::wstring(L"OFF"); }, true},
};
constexpr int kRowN = sizeof kRows / sizeof kRows[0];
constexpr int kKeyBindings = 15;
constexpr int kHud169Row = 16;
constexpr int kFadeRow = 17;
constexpr int kHdRow = 18;
constexpr int kAnisoRow = 19;
constexpr int kViewmodelRow = 20;
constexpr int kFmvRow = 21;
constexpr int kFpsCapRow = 22;
constexpr int kFpsCounterRow = 23;
constexpr int kPauseFocusRow = 24;
constexpr int kAimRow = 25;
constexpr int kWalkSpeedRow = 26;
constexpr int kBlurRow = 28;
constexpr int kSharpenRow = 29;
constexpr int kDetailsRow = 30;
constexpr int kClarityRow = 31;
constexpr int kVibranceRow = 32;
// Rows past the fixed ones are the mods found in <port>\mods: one ON/OFF line each (see mods.h).
int mod_count() { return (int)mods::list().size(); }

} // namespace

const std::vector<Group> &groups()
{
    static const std::vector<Group> g = {
        {"PCVideo", "VIDEO", {0, 1, kAnisoRow, 2, kViewmodelRow, kFmvRow}},
        // MAX FRAME RATE (row 7) is live now: 0 = unlimited, -1 = just under the monitor's refresh (recomp model).
        // Frame generation was removed with the fixed-rate world step; the row is gone from this page.
        // no brightness or gamma row here: the game's own picture menu (picturesetupxbox, its gamma slider writes the
        // game's gamma ramp, which the port's output pass applies) is the one the player uses for those, so the
        // port's own duplicates (BRIGHTNESS / GAMMA, still read from black.ini) were taken off these pages
        // FILL GEOMETRY SEAMS and EFFECTS AT XBOX SIZE came here from AUDIO AND GAME (2026-10-18): they are picture
        // effects, and WEAPON BLUR (blur.cpp) had to fit on a page of six rows
        {"PCDisplay", "DISPLAY", {6, 7, 8, 9, kFpsCounterRow, 5}},
        {"PCGame", "AUDIO AND GAME", {10, 11, 12, kBlurRow, kPauseFocusRow}},
        {"PCControls", "CONTROLS", {13, 14, kKeyBindings, kAimRow, kWalkSpeedRow}},
    };
    return g;
}

const Group &mods_group()
{
    // The launcher is where a mod (and the HD pack) is turned on and off: the game's page of the port's own in-play
    // rows keeps only what applies at once, so nothing on it needs a restart. The mods are still part of the settings
    // model (label / value / step below) — the launcher writes the same black.ini and the same marker files.
    // HD TEXTURES (kHdRow) is the launcher's row alone (2026-10-18): it is read from black.ini at boot and the way
    // in is the launcher's MODS tab, so this page keeps only the in-play rows. It stays in the model (kRows) and in
    // restart_only() — taking it off this list is what keeps it off both pages.
    // MODS (2026-10-20, user): the page also holds the four filters and the HUD rows, but the row that
    // opens it is still the familiar MODS entry. The internal page id stays PCImage so the built front end
    // and the existing tests keep the same page name; only the label the player sees is MODS.
    static Group g;
    g = {"PCImage", "MODS", {kHud169Row, kFadeRow, kSharpenRow, kDetailsRow, kClarityRow, kVibranceRow}};
    return g;
}

int count() { return kRowN + mod_count(); }

const char *label(int n)
{
    if (n >= 0 && n < kRowN) return kRows[n].label;
    int i = n - kRowN;
    return i >= 0 && i < mod_count() ? mods::list()[i].title.c_str() : "";
}

// The port's own rows are English (the game's own files are English, or the language mod's); on the Portuguese mod
// (mods.h: mod.ini "lang = ptbr") the name the port puts on the game's menus is Portuguese too, so a translated
// game does not show one English row among its own.
const char *title() { return mods::language() == "ptbr" ? "CONF. PC" : "PC SETTINGS"; }

std::wstring value(int n)
{
    if (n >= kRowN) {
        int i = n - kRowN;
        if (i < 0 || i >= mod_count()) return L"";
        const mods::Mod &m = mods::list()[i];
        std::wstring v = m.on && !m.blocked ? L"ON" : L"OFF";
        // the mod is for another language than the one being played (mod.ini "lang"): it is on, but its files
        // are never served (mods.h)
        if (m.blocked) v += L" (LANGUAGE)";
        // the game read this mod's files at start: a change here only takes effect at the next start, and the port
        // restarts itself once the player is back at the main menu (fe/restart.cpp)
        if (m.on != m.at_start) v += L" (REINICIAR)";
        return v;
    }
    if (n < 0) return L"";
    const Row &r = kRows[n];
    int i = std::min(r.get(), r.choices - 1);
    if (i < 0 && !r.custom) i = 0; // only the rows that can hold a value from outside their list show CUSTOM
    return r.text(i);
}

void step(int n, int dir)
{
    if (!dir) return;
    if (n >= kRowN) { // a mod: the marker file in its own folder is the state
        int i = n - kRowN;
        if (i < 0 || i >= mod_count()) return;
        bool on = mods::toggle(i);
        logf("fe: mod %s -> %s", mods::list()[i].title.c_str(), on ? "ON" : "OFF");
        return;
    }
    if (n < 0) return;
    const Row &r = kRows[n];
    int i = std::min(r.get(), r.choices - 1);
    i = i < 0 ? 0 : (i + (dir > 0 ? 1 : r.choices - 1)) % r.choices;
    r.set(i);
    settings_save();
    logf("fe: %s -> %s", r.label, narrow(r.text(i)).c_str());
}

bool is_slider(int n) { return n >= 0 && n < kRowN && kRows[n].slider; }

// A setting that only takes effect at the next start: the aspect and the HD pack are read by the game at boot, and
// the mods' files at boot and on a load. The game's own pages leave these out (fe/pages.cpp group_rows,
// fe/pause_ui.cpp groups): they are set in the launcher, which is the port's way in. MAX FRAME RATE applies live.
bool restart_only(int n) { return n == 6 || n == kHdRow || n >= kRowN; }

// The rows a page really has: the builder (fe/pages.cpp group_rows), the pause page's own list (fe/pause_ui.cpp) and
// the values (fill_page above) all take them from here, so a page can never show one setting's name beside another
// setting's value.
std::vector<int> page_rows(const Group &g)
{
    std::vector<int> out;
    for (int n : g.rows)
        if (!restart_only(n)) out.push_back(n);
    return out;
}

const char *link(int n) { return n == kKeyBindings ? "PCBindings" : nullptr; }

int slider_pos(int n)
{
    if (!is_slider(n)) return 0;
    int last = std::max(1, kRows[n].choices - 1);
    return std::clamp(kRows[n].get(), 0, last) * 100 / last;
}

void slide(int n, float pos)
{
    if (!is_slider(n)) return;
    const Row &r = kRows[n];
    int last = std::max(1, r.choices - 1);
    int i = std::clamp((int)std::lround(pos * last / 100.0f), 0, last);
    if (i == r.get()) return;
    r.set(i);
    settings_save();
    logf("fe: %s -> %s", r.label, narrow(r.text(i)).c_str());
}

void fill_page(int group)
{
    const std::vector<Group> &gs = groups();
    std::vector<int> rows;
    if (group >= 0 && group < (int)gs.size()) rows = page_rows(gs[group]);
    else if (group == (int)gs.size()) rows = page_rows(mods_group()); // the MODS page (its own, after the four groups)
    else return; // not a page of the port's own: the KEY BINDINGS page (100) fills these same values with its rows' keys
                 // (fe/bind_list.cpp) and the pause page with its own (fe/pause_ui.cpp)
    // The page's value slot i belongs to the i-th row the builder put on it (fe/pages.cpp group_rows) — the same
    // list, from the same page_rows call. Filling the slots from the group's own list instead put a later row's
    // value beside an earlier row once the pages started leaving rows out (the DISPLAY page showed 16:9 beside FPS
    // COUNTER and 60 FPS beside FULLSCREEN).
    for (int i = 0; i < fevar::kRows; i++) fe_var_set(fevar::kValue + i, i < (int)rows.size() ? value(rows[i]) : L"");
}

// a slider takes its start from externObject.FEPCS<setting> when its page is prepared, before the page reports itself,
// so these follow the settings all the time
void refresh_sliders()
{
    for (int n = 0; n < kRowN; n++)
        if (kRows[n].slider) fe_int_set(("FEPCS" + std::to_string(n)).c_str(), slider_pos(n));
}

} // namespace pcset
