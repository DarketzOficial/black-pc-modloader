// "Pular video" hint drawn over a movie: while a movie plays (input/movies.cpp, --no-movie-skip off) the Windows
// cursor is hidden and no menu cursor is up, so the port shows a small key cap with a return arrow and the text.
// Same technique as the frame counter (d3d/fps.cpp): GDI+ renders it once into a texture and a small textured quad
// draws it bottom right, before the picture is presented.
#pragma once

// Called once per frame with the render size, before the picture is presented.
void fmv_hint_tick(int screen_w, int screen_h);
