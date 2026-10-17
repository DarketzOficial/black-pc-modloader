// Game timing: make the Xbox time-stamp counter users tick at the Xbox CPU clock (733.33 MHz).
#pragma once

void install_timing_patches(); // after image_map; known build only

// 60 fps: BLACK steps its world by a fixed frame time chosen at start-up from the video standard (game init
// 0x20C282: NTSC 1/30 s and 33 ms, PAL 1/25 s and 40 ms, into 0x2F3A38 / 0x2F3800; the game's own
// setFrameRate FUN_000BE560 does the same for any rate). Make the NTSC choice 1/60 s and 16 ms, and point the
// two other uses of the 1/30 constant (0x295FC0) at 1/60. Returns false if the code does not match.
bool install_60fps_patches();

// The seconds the world steps per frame, while the 60 fps patches are in (a no-op otherwise): the FPS cap
// (d3d/device.cpp) stretches it when it holds the presents below 60, so the world keeps real time at the
// lower rate (the game's own step at 30 fps was 1/30 s, see above).
void set_world_step(float seconds);
