// The port's settings as rows of the in-game PC SETTINGS pages (fe/pages.cpp builds the pages, fe/calls.cpp receives
// "PCChange(row, -1 / +1)" from their list boxes and "PCSlider<row>(0-100)" from their sliders). Each row is a list of choices: the current one is found from
// g_cfg, a step applies the next one and saves black.ini. Aspect ratio and frame rate are
// read by the game at boot: a new choice is saved for the next run and shown with "NEXT START" until then.
#include "fe/pc_settings.h"

#include "common.h"
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
    if (i == 0) return L"MONITOR";
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

// A slider row has the 26 positions of the game's slider clip (Sound Setup's bullet bar): field of view 70-120 in
// steps of 2, brightness and gamma 0.5-3.0 in steps of 0.1, mouse sensitivity 0.25-6.5 in steps of 0.25.
constexpr int kSteps = 26;
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
const int kFpsCaps[] = {0, 30, 60}; // the game presents at most one frame per vblank (~60) anyway: above 60 is a no-op
int fps_cap_get()
{
    for (int i = 0; i < 3; i++)
        if (kFpsCaps[i] == g_cfg.fps_cap) return i;
    return 0;
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

int shown_fps() { return g_cfg.fps_next ? g_cfg.fps_next : g_cfg.fps; }
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
    {"FIELD OF VIEW", kSteps, [] { return step_of(g_cfg.fov, 70, 2); },
     [](int i) {
         g_cfg.fov = 70.0f + 2 * i;
         fov_set(g_cfg.fov);
     },
     [](int i) { return number(70.0f + 2 * i, L"%.0f") + (i == 0 ? L" (ORIGINAL)" : L""); }, true},
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
    {"FRAME RATE", 2, [] { return (int)(shown_fps() == 60); },
     [](int i) {
         int fps = i ? 60 : 30;
         g_cfg.fps_next = fps == g_cfg.fps ? 0 : fps;
     },
     [](int i) { return std::wstring(i ? L"60 FPS" : L"30 FPS (ORIGINAL)") + ((i ? 60 : 30) != g_cfg.fps ? L" (NEXT START)" : L""); }},
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
    {"VIEWMODEL FOV", kSteps, [] { return step_of(g_cfg.viewmodel_fov, 70, 2); },
     [](int i) { g_cfg.viewmodel_fov = 70.0f + 2 * i; },
     [](int i) { return number(70.0f + 2 * i, L"%.0f") + (i == 0 ? L" (ORIGINAL)" : L""); }, true},
    {"FMV SCALE", 2, [] { return g_cfg.fmv_scale ? 1 : 0; }, [](int i) { g_cfg.fmv_scale = i; },
     [](int i) { return i ? std::wstring(L"4:3") : std::wstring(L"FILL"); }},
    {"MAX FRAME RATE", 3, [] { return fps_cap_get(); }, [](int i) { g_cfg.fps_cap = kFpsCaps[i]; },
     [](int i) { return i ? number((float)kFpsCaps[i], L"%.0f") : std::wstring(L"OFF"); }},
    {"FPS COUNTER", 2, [] { return (int)g_cfg.fpscounter; }, [](int i) { g_cfg.fpscounter = i != 0; },
     [](int i) { return on_off(i); }},
    {"PAUSE WITHOUT FOCUS", 2, [] { return (int)g_cfg.pause_focus; }, [](int i) { g_cfg.pause_focus = i != 0; },
     [](int i) { return on_off(i); }},
    {"AIM MODE", 2, [] { return (int)g_cfg.aim_toggle; }, [](int i) { g_cfg.aim_toggle = i != 0; },
     [](int i) { return i ? std::wstring(L"TOGGLE") : std::wstring(L"HOLD"); }},
    {"WALK SPEED", 4, [] { return walk_speed_get(); }, [](int i) { g_cfg.walk_speed = kWalkSpeeds[i]; },
     [](int i) { return number((float)kWalkSpeeds[i], L"%.0f%%"); }},
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
// Rows past the fixed ones are the mods found in <port>\mods: one ON/OFF line each (see mods.h).
int mod_count() { return (int)mods::list().size(); }

} // namespace

const std::vector<Group> &groups()
{
    static const std::vector<Group> g = {
        {"PCVideo", "VIDEO", {0, 1, kAnisoRow, 2, kViewmodelRow, kFmvRow}},
        // no frame-rate cap row: FRAME RATE (30 / 60) on this page is the port's own limit, and the game presents
        // at most one frame per vblank anyway (the cap is still read from black.ini / --fps-cap: see PORT_SETTINGS.md)
        // no brightness or gamma row here: the game's own picture menu (picturesetupxbox, its gamma slider writes the
        // game's gamma ramp, which the port's output pass applies) is the one the player uses for those, so the
        // port's own duplicates (BRIGHTNESS / GAMMA, still read from black.ini) were taken off these pages
        {"PCDisplay", "DISPLAY", {6, 7, kFpsCounterRow, 5}},
        {"PCGame", "AUDIO AND GAME", {8, 9, 10, 11, 12, kPauseFocusRow}},
        {"PCControls", "CONTROLS", {13, 14, kKeyBindings, kAimRow, kWalkSpeedRow}},
    };
    return g;
}

const Group &mods_group()
{
    static Group g;
    g = {"PCMods", "MODS", {kHud169Row, kFadeRow, kHdRow}};
    // a page holds six rows and the port's own three come first: mods beyond the room left are not shown
    int room = 6 - (int)g.rows.size();
    for (int i = 0; i < mod_count() && i < room; i++) g.rows.push_back(kRowN + i);
    if (mod_count() > room) {
        static int told;
        if (told++ < 1)
            logf("mods: %d mods installed, %d of them fit the MODS page", mod_count(), room);
    }
    return g;
}

int count() { return kRowN + mod_count(); }

const char *label(int n)
{
    if (n >= 0 && n < kRowN) return kRows[n].label;
    int i = n - kRowN;
    return i >= 0 && i < mod_count() ? mods::list()[i].title.c_str() : "";
}

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

// aspect ratio and frame rate are read by the game at boot; the mods' files are read at boot and when a menu or a
// level loads, so a change takes effect on the next start
bool restart_only(int n) { return n == 6 || n == 7 || n == kHdRow || n >= kRowN; }

const char *link(int n) { return n == kKeyBindings ? "PCBindings" : nullptr; }

int slider_pos(int n)
{
    if (!is_slider(n)) return 0;
    return std::clamp(kRows[n].get(), 0, kSteps - 1) * 100 / (kSteps - 1);
}

void slide(int n, float pos)
{
    if (!is_slider(n)) return;
    const Row &r = kRows[n];
    int i = std::clamp((int)std::lround(pos * (kSteps - 1) / 100.0f), 0, kSteps - 1);
    if (i == r.get()) return;
    r.set(i);
    settings_save();
    logf("fe: %s -> %s", r.label, narrow(r.text(i)).c_str());
}

void fill_page(int group)
{
    const std::vector<Group> &gs = groups();
    const std::vector<int> *rows = nullptr;
    if (group >= 0 && group < (int)gs.size()) rows = &gs[group].rows;
    else if (group == (int)gs.size()) rows = &mods_group().rows; // the MODS page (its own row, after the four groups)
    if (!rows) return;
    for (int i = 0; i < fevar::kRows; i++) {
        int n = i < (int)rows->size() ? (*rows)[i] : -1;
        fe_var_set(fevar::kValue + i, n >= 0 ? value(n) : L"");
    }
}

// a slider takes its start from externObject.FEPCS<setting> when its page is prepared, before the page reports itself,
// so these follow the settings all the time
void refresh_sliders()
{
    for (int n = 0; n < kRowN; n++)
        if (kRows[n].slider) fe_int_set(("FEPCS" + std::to_string(n)).c_str(), slider_pos(n));
}

} // namespace pcset
