// Host side of the graphics layer: the game window (own UI thread) and the Direct3D 9 device.
#pragma once

#include "common.h"

#include <d3d9.h>

namespace host3d {

// Starts the UI thread; returns once the window exists. A zero width fits the largest aspect_w:aspect_h client
// area on the screen; a zero height alone follows from the width and the aspect.
bool open_window(int width, int height, int aspect_w, int aspect_h);
HWND window();
void set_title(const char *text);
bool create_device(); // on the calling (game render) thread; back buffer = the largest monitor
// The part of the back buffer this frame uses (the client area, shrunk to fit the back buffer if needed) and
// the picture inside it (the game's aspect, centred; the rest are black bars). present() shows that part.
void frame_layout(RECT *frame, RECT *picture);
// Mouse while captured (a click in the window captures it; focus loss releases): raw counts since the last
// call, wheel notches (+ = away from the user), buttons bit 0-4 = left, right, middle, X1, X2.
bool mouse_captured();
void request_capture(bool on); // from any thread; capturing needs the window in front
// True while a menu page is up (input/bindings.cpp, each poll): a click in the window then belongs to the menu and
// must not capture the mouse, or the cursor would blink out and the click be taken for the capture.
void set_cursor_free(bool on);
void set_fullscreen(bool on);  // from any thread (PC SETTINGS > VIDEO > FULLSCREEN)
bool fullscreen();
// PC SETTINGS > VIDEO > RESOLUTION: the window's client size, 0 = the largest that fits the desktop (the
// width follows from the picture's aspect when only one is given). Fullscreen is the whole monitor: the size
// is remembered and applied when the window comes back. From any thread.
void set_window_size(int width, int height);
void mouse_take(int *dx, int *dy, int *wheel);
int mouse_buttons();
// The menu cursor (the mouse is free while a menu page is up; input/bindings.cpp): where the cursor is in the
// front end's stage (640x480, the picture keeping the game's aspect centred in the client area), and whether a
// button is down as Windows reports it. False while the mouse is captured or outside the picture.
bool cursor_stage(float *x, float *y);
// The same place, but a point outside the picture clamps to its edge: the port draws its own arrow (cursor.cpp)
// and the Windows cursor never shows over the window, so the arrow must be drawn wherever the pointer is.
bool cursor_stage_any(float *x, float *y);
// True while a menu page owns the pointer (input/bindings.cpp set_cursor_free): the port's own arrow is drawn then.
bool cursor_free();
bool cursor_down(int button); // 0 left, 1 right
// Wheel notches taken while a menu page owns the pointer (set_cursor_free): + = up (away from the user), - = down.
// input/bindings.cpp turns them into the menus' up/down inputs.
int menu_wheel();
IDirect3DDevice9 *dev();
// INTZ: a depth-stencil format that can also be sampled as a texture (driver extension, all current GPUs)
constexpr D3DFORMAT FMT_INTZ = (D3DFORMAT)MAKEFOURCC('I', 'N', 'T', 'Z');
bool intz();
void begin_scene();                         // BeginScene once per frame, lazily
void end_scene();                           // close the scene (before StretchRect); the next draw reopens one
HRESULT present();                          // EndScene + Present (D3DERR_DEVICELOST: see xd3d recover_device)
// Reset with the creation parameters: every D3DPOOL_DEFAULT resource and reference to the back buffer and depth
// surface must be released first; afterwards Direct3D is back at its default states.
bool reset_device();
bool window_closed();

} // namespace host3d
