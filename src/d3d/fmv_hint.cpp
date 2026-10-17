// The "Pular video" hint shown while a movie plays (input/movies.cpp movie_playing_once). The port hides the
// Windows cursor over the window, and no menu cursor is up during a movie, so this is the only thing that tells the
// player the movie can be skipped. A small key cap drawn with a return arrow ("enter") and the text beside it.
// Same technique as the frame counter (d3d/fps.cpp): GDI+ renders it into a texture once and a textured quad draws
// it bottom right, before the picture is presented.
#include "d3d/fmv_hint.h"

#include "common.h"
#include "d3d/device.h"
#include "d3d/host.h"
#include "input/movies.h"

#include <d3d9.h>
#include <gdiplus.h>

#include <algorithm>
#include <cmath>

#pragma comment(lib, "gdiplus.lib")

namespace {

IDirect3DTexture9 *g_tex = nullptr;
int g_tex_w, g_tex_h;
ULONG_PTR g_gdiplus;

bool make_texture(int &w, int &h)
{
    if (g_tex) {
        w = g_tex_w;
        h = g_tex_h;
        return true;
    }
    IDirect3DDevice9 *dev = host3d::dev();
    if (!dev) return false;
    if (!g_gdiplus) {
        Gdiplus::GdiplusStartupInput in;
        if (Gdiplus::GdiplusStartup(&g_gdiplus, &in, nullptr) != Gdiplus::Ok) return false;
    }
    const wchar_t *text = L"Pular video";
    const int size = 15, pad = 6, gap = 8, keyw = 30, keyh = 24;
    std::wstring wtext(text);
    Gdiplus::RectF box;
    {
        Gdiplus::Bitmap probe(8, 8, PixelFormat32bppARGB);
        Gdiplus::Graphics g(&probe);
        Gdiplus::Font font(L"Segoe UI", (Gdiplus::REAL)size, Gdiplus::FontStyleRegular, Gdiplus::UnitPixel);
        g.MeasureString(wtext.c_str(), (INT)wtext.size(), &font, Gdiplus::RectF(0, 0, 4096, 256), &box);
    }
    int tw = (int)std::ceil(box.Width);
    w = pad + keyw + gap + tw + pad;
    h = pad * 2 + (keyh > (int)std::ceil(box.Height) ? keyh : (int)std::ceil(box.Height));
    if (w <= 0 || h <= 0 || w > 512 || h > 128) return false;
    Gdiplus::Bitmap bmp(w, h, PixelFormat32bppARGB);
    {
        Gdiplus::Graphics g(&bmp);
        g.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAlias);
        g.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);
        g.Clear(Gdiplus::Color(0, 0, 0, 0));
        Gdiplus::SolidBrush panel(Gdiplus::Color(150, 0, 0, 0));
        g.FillRectangle(&panel, 0, 0, w, h);
        // the key cap
        int kx = pad, ky = (h - keyh) / 2;
        Gdiplus::SolidBrush cap(Gdiplus::Color(255, 232, 232, 232));
        Gdiplus::Pen border(Gdiplus::Color(255, 20, 20, 20), 2.0f);
        g.FillRectangle(&cap, kx, ky, keyw, keyh);
        g.DrawRectangle(&border, kx, ky, keyw, keyh);
        // the return arrow inside the cap
        Gdiplus::Pen arrow(Gdiplus::Color(255, 20, 20, 20), 2.4f);
        float ax0 = kx + keyw * 0.70f, ax1 = kx + keyw * 0.32f;
        float ay0 = ky + keyh * 0.26f, ay1 = ky + keyh * 0.62f;
        g.DrawLine(&arrow, ax0, ay0, ax1, ay0);
        g.DrawLine(&arrow, ax1, ay0, ax1, ay1);
        g.DrawLine(&arrow, ax1, ay1, ax1 - keyw * 0.14f, ay1 - keyh * 0.16f);
        g.DrawLine(&arrow, ax1, ay1, ax1 + keyw * 0.14f, ay1 - keyh * 0.16f);
        // the text
        Gdiplus::Font font(L"Segoe UI", (Gdiplus::REAL)size, Gdiplus::FontStyleRegular, Gdiplus::UnitPixel);
        Gdiplus::StringFormat fmt;
        fmt.SetAlignment(Gdiplus::StringAlignmentNear);
        fmt.SetLineAlignment(Gdiplus::StringAlignmentCenter);
        Gdiplus::RectF layout((Gdiplus::REAL)(pad + keyw + gap), 0, (Gdiplus::REAL)tw, (Gdiplus::REAL)h);
        Gdiplus::SolidBrush fill(Gdiplus::Color(255, 255, 255, 255));
        g.DrawString(wtext.c_str(), (INT)wtext.size(), &font, layout, &fmt, &fill);
    }
    Gdiplus::Rect lock(0, 0, w, h);
    Gdiplus::BitmapData bits;
    if (bmp.LockBits(&lock, Gdiplus::ImageLockModeRead, PixelFormat32bppARGB, &bits) != Gdiplus::Ok) return false;
    if (FAILED(dev->CreateTexture(w, h, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_MANAGED, &g_tex, nullptr))) {
        bmp.UnlockBits(&bits);
        g_tex = nullptr;
        return false;
    }
    D3DLOCKED_RECT lr;
    if (SUCCEEDED(g_tex->LockRect(0, &lr, nullptr, 0))) {
        for (int y = 0; y < h; y++)
            memcpy((uint8_t *)lr.pBits + (size_t)y * lr.Pitch, (const uint8_t *)bits.Scan0 + (size_t)y * bits.Stride,
                   (size_t)w * 4);
        g_tex->UnlockRect(0);
    }
    bmp.UnlockBits(&bits);
    g_tex_w = w;
    g_tex_h = h;
    return true;
}

// alpha 0..1: the hint fades in as the movie starts, stays up for a few seconds and then fades away (fmv_hint_tick)
void draw_quad(int screen_w, int screen_h, int w, int h, float alpha)
{
    IDirect3DDevice9 *dev = host3d::dev();
    if (!dev || !g_tex) return;
    struct V {
        float x, y, z, rhw;
        DWORD color;
        float u, v;
    };
    const float scale = screen_h / 480.0f; // the HUD's own scale, so the hint keeps its size in the picture
    const float tw = w * scale, th = h * scale, m = 12.0f * scale;
    const float x1 = screen_w - m, y1 = screen_h - m;
    const float x0 = x1 - tw, y0 = y1 - th;
    const DWORD a = (DWORD)std::clamp(alpha, 0.0f, 1.0f) * 255u;
    const DWORD color = (a << 24) | 0x00FFFFFFu;
    const V quad[4] = {{x0, y0, 0, 1, color, 0, 0},
                       {x1, y0, 0, 1, color, 1, 0},
                       {x0, y1, 0, 1, color, 0, 1},
                       {x1, y1, 0, 1, color, 1, 1}};
    // Save everything this quad touches: the game's draws re-send their state only when the HLE cache sees a
    // change, so a leaked state would corrupt every draw after this one.
    IDirect3DVertexShader9 *vs = nullptr;
    IDirect3DPixelShader9 *ps = nullptr;
    IDirect3DBaseTexture9 *tex0 = nullptr;
    DWORD fvf = 0;
    DWORD rs[8], s0[4], s1[2];
    const D3DRENDERSTATETYPE rk[8] = {D3DRS_ZENABLE, D3DRS_ZWRITEENABLE, D3DRS_LIGHTING, D3DRS_CULLMODE,
                                     D3DRS_ALPHABLENDENABLE, D3DRS_SRCBLEND, D3DRS_DESTBLEND, D3DRS_ALPHATESTENABLE};
    const D3DTEXTURESTAGESTATETYPE sk[4] = {D3DTSS_COLOROP, D3DTSS_COLORARG1, D3DTSS_ALPHAOP, D3DTSS_ALPHAARG1};
    dev->GetVertexShader(&vs);
    dev->GetPixelShader(&ps);
    dev->GetFVF(&fvf);
    dev->GetTexture(0, &tex0);
    for (int i = 0; i < 8; i++) dev->GetRenderState(rk[i], &rs[i]);
    for (int i = 0; i < 4; i++) dev->GetTextureStageState(0, sk[i], &s0[i]);
    for (int i = 0; i < 2; i++) dev->GetTextureStageState(1, sk[i * 2], &s1[i]);
    dev->SetVertexShader(nullptr);
    dev->SetPixelShader(nullptr);
    dev->SetRenderState(D3DRS_ZENABLE, FALSE);
    dev->SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
    dev->SetRenderState(D3DRS_LIGHTING, FALSE);
    dev->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE);
    dev->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
    dev->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
    dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);
    dev->SetRenderState(D3DRS_ALPHATESTENABLE, FALSE);
    dev->SetTexture(0, g_tex);
    dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
    dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE);
    dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
    dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
    dev->SetTextureStageState(1, D3DTSS_COLOROP, D3DTOP_DISABLE);
    dev->SetTextureStageState(1, D3DTSS_ALPHAOP, D3DTOP_DISABLE);
    dev->SetFVF(D3DFVF_XYZRHW | D3DFVF_DIFFUSE | D3DFVF_TEX1);
    dev->DrawPrimitiveUP(D3DPT_TRIANGLESTRIP, 2, quad, sizeof(V));
    for (int i = 0; i < 8; i++) dev->SetRenderState(rk[i], rs[i]);
    for (int i = 0; i < 4; i++) dev->SetTextureStageState(0, sk[i], s0[i]);
    for (int i = 0; i < 2; i++) dev->SetTextureStageState(1, sk[i * 2], s1[i]);
    dev->SetTexture(0, tex0);
    dev->SetFVF(fvf);
    dev->SetVertexShader(vs);
    dev->SetPixelShader(ps);
    if (tex0) tex0->Release();
    if (vs) vs->Release();
    if (ps) ps->Release();
}

} // namespace

void fmv_hint_tick(int screen_w, int screen_h)
{
    // The hint comes up with the movie, fades in, stays for three seconds and fades away on its own: it only has to
    // be read once, and after that it would just sit on top of the picture. A movie that ends and another that starts
    // (or the same scene again) brings it back from the top.
    static bool was_playing;
    static ULONGLONG movie_at;
    bool playing = movie_playing_once();
    if (playing && !was_playing) movie_at = GetTickCount64();
    was_playing = playing;
    if (screen_w <= 1 || screen_h <= 1 || !playing) return;
    constexpr float kFadeIn = 400.0f, kHold = 3000.0f, kFadeOut = 600.0f;
    float t = (float)(GetTickCount64() - movie_at);
    float alpha;
    if (t < kFadeIn) alpha = t / kFadeIn;
    else if (t < kHold) alpha = 1.0f;
    else alpha = 1.0f - (t - kHold) / kFadeOut;
    if (alpha <= 0.0f) return;
    int w = 0, h = 0;
    if (!make_texture(w, h)) return;
    draw_quad(screen_w, screen_h, w, h, alpha);
}
