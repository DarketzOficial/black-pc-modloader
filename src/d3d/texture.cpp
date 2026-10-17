// Xbox textures -> host textures. Swizzled (Morton order) and linear layouts are decoded on the CPU into
// A8R8G8B8, DXT data is uploaded as is. Host copies are cached per resource header and re-uploaded when
// the header changes, the game locks the texture, or a sampled hash of the texel data changes (checked at the
// first use in a frame, every 16th frame for textures that have not changed for a while).
#include "d3d/device.h"
#include "d3d/dispatch.h"
#include "d3d/host.h"
#include "d3d/state.h"
#include "perf.h"

#include <algorithm>
#include <cstring>
#include <set>
#include <unordered_map>
#include <vector>
#include <wincodec.h>

namespace xd3d {
namespace {

struct Entry {
    IDirect3DBaseTexture9 *tex = nullptr;
    uint32_t data = 0, format = 0, size = 0;
    const void *palette = nullptr;
    uint32_t hash = 0;
    uint64_t hd_id = 0;           // content id of the HD replacement in use (0 = the game's own texture)
    bool dirty = false;
    uint32_t checked_frame = ~0u; // the frame the contents were last checked (hashing on every bind cost ~8 ms a frame)
    uint32_t stable = 0;          // consecutive checks without a change
};

// A texture whose contents did not change for this many checks is checked every 16th frame only (busy
// scenes bind hundreds of textures a frame; hashing each once a frame still cost ~3 ms). Textures that
// change (video frames, render-to-memory) stay at one check a frame; a Lock marks a texture dirty at once.
constexpr uint32_t kStableChecks = 30;

std::unordered_map<const Resource *, Entry> g_cache;
CRITICAL_SECTION g_cs;
struct Init {
    Init() { InitializeCriticalSection(&g_cs); }
} g_init;

uint32_t fmt_of(const PixelContainer *pc) { return (pc->Format >> FMT_FORMAT_SHIFT) & 0xFF; }

uint32_t sample_hash(const uint8_t *p, uint32_t bytes)
{
    uint32_t h = 2166136261u;
    uint32_t n = bytes / 4;
    const uint32_t *d = (const uint32_t *)p;
    uint32_t step = n > 8192 ? n / 4096 : 1;
    for (uint32_t i = 0; i < n; i += step) h = (h ^ d[i]) * 16777619u;
    return h;
}

// Morton-order offset tables for one mip level.
struct Swizzle {
    uint32_t mx = 0, my = 0;
    Swizzle(uint32_t w, uint32_t h)
    {
        uint32_t bit = 1;
        for (uint32_t i = 1; i < w || i < h; i <<= 1) {
            if (i < w) { mx |= bit; bit <<= 1; }
            if (i < h) { my |= bit; bit <<= 1; }
        }
    }
    static uint32_t deposit(uint32_t v, uint32_t mask)
    {
        uint32_t r = 0;
        for (uint32_t b = 1; mask; b <<= 1) {
            uint32_t low = mask & (~mask + 1);
            if (v & b) r |= low;
            mask &= mask - 1;
        }
        return r;
    }
};

uint32_t expand5(uint32_t v) { return (v << 3) | (v >> 2); }
uint32_t expand6(uint32_t v) { return (v << 2) | (v >> 4); }
uint32_t expand4(uint32_t v) { return v * 17; }

// Decode one texel of an uncompressed Xbox format into A8R8G8B8.
uint32_t decode(uint32_t fmt, const uint8_t *p, const uint32_t *pal)
{
    uint32_t v16 = p[0] | (p[1] << 8);
    switch (fmt) {
    case XFMT_A8R8G8B8: case XFMT_LIN_A8R8G8B8: return *(const uint32_t *)p;
    case XFMT_X8R8G8B8: case XFMT_LIN_X8R8G8B8: return *(const uint32_t *)p | 0xFF000000;
    case XFMT_R5G6B5: case XFMT_LIN_R5G6B5:
        return 0xFF000000 | expand5(v16 >> 11) << 16 | expand6((v16 >> 5) & 63) << 8 | expand5(v16 & 31);
    case XFMT_A1R5G5B5: case XFMT_LIN_A1R5G5B5:
        return ((v16 & 0x8000) ? 0xFF000000 : 0) | expand5((v16 >> 10) & 31) << 16 | expand5((v16 >> 5) & 31) << 8 | expand5(v16 & 31);
    case XFMT_X1R5G5B5: case XFMT_LIN_X1R5G5B5:
        return 0xFF000000 | expand5((v16 >> 10) & 31) << 16 | expand5((v16 >> 5) & 31) << 8 | expand5(v16 & 31);
    case XFMT_A4R4G4B4: case XFMT_LIN_A4R4G4B4:
        return expand4(v16 >> 12) << 24 | expand4((v16 >> 8) & 15) << 16 | expand4((v16 >> 4) & 15) << 8 | expand4(v16 & 15);
    case XFMT_R6G5B5: case XFMT_LIN_R6G5B5:
        return 0xFF000000 | expand6(v16 >> 10) << 16 | expand5((v16 >> 5) & 31) << 8 | expand5(v16 & 31);
    case XFMT_L8: case XFMT_LIN_L8: return 0xFF000000 | p[0] * 0x010101u;
    case XFMT_AL8: case XFMT_LIN_AL8: return p[0] * 0x01010101u;
    case XFMT_A8: case XFMT_LIN_A8: return (uint32_t)p[0] << 24 | 0xFFFFFF;
    case XFMT_A8L8: case XFMT_LIN_A8L8: return (uint32_t)p[1] << 24 | p[0] * 0x010101u;
    case XFMT_L16: case XFMT_LIN_L16: return 0xFF000000 | p[1] * 0x010101u;
    case XFMT_P8: return pal ? pal[p[0]] : 0xFF000000 | p[0] * 0x010101u;
    case XFMT_R8B8: case XFMT_LIN_R8B8: return 0xFF000000 | (uint32_t)p[1] << 16 | p[0];
    case XFMT_G8B8: case XFMT_LIN_G8B8: return 0xFF000000 | (uint32_t)p[1] << 8 | p[0];
    case XFMT_V8U8: return 0xFF000000 | ((p[0] ^ 0x80u) << 16) | ((p[1] ^ 0x80u) << 8);
    case XFMT_A8B8G8R8: case XFMT_LIN_A8B8G8R8: { uint32_t v = *(const uint32_t *)p; return (v & 0xFF00FF00) | (v & 0xFF) << 16 | (v >> 16 & 0xFF); }
    case XFMT_B8G8R8A8: case XFMT_LIN_B8G8R8A8: { uint32_t v = *(const uint32_t *)p; return (v << 24) | (v >> 8); }
    case XFMT_R8G8B8A8: case XFMT_LIN_R8G8B8A8: { uint32_t v = *(const uint32_t *)p; return (v << 24) | (v >> 24 & 0xFF) << 16 | (v >> 16 & 0xFF) << 8 | (v >> 8 & 0xFF); }
    case XFMT_D24S8: case XFMT_LIN_D24S8: case XFMT_F24S8: case XFMT_LIN_F24S8: { uint32_t d = *(const uint32_t *)p >> 24; return 0xFF000000 | d * 0x010101u; }
    case XFMT_D16: case XFMT_LIN_D16: case XFMT_F16: case XFMT_LIN_F16: return 0xFF000000 | p[1] * 0x010101u;
    default: return 0xFFFF00FF; // magenta: format not handled
    }
}

uint32_t yuv_to_rgb(int y, int u, int v)
{
    int c = y - 16, d = u - 128, e = v - 128;
    auto clamp = [](int x) { return x < 0 ? 0 : x > 255 ? 255 : x; };
    int r = clamp((298 * c + 409 * e + 128) >> 8), g = clamp((298 * c - 100 * d - 208 * e + 128) >> 8),
        b = clamp((298 * c + 516 * d + 128) >> 8);
    return 0xFF000000 | r << 16 | g << 8 | b;
}

// Write level `level` of `pc` (from Xbox memory) into a locked host rect.
void upload_level(const PixelContainer *pc, uint32_t face, uint32_t level, uint8_t *dst, uint32_t dst_pitch, const uint32_t *pal)
{
    uint32_t fmt = fmt_of(pc);
    const FormatInfo &fi = format_info(fmt);
    uint32_t pitch, w, h;
    uint32_t off = texture_level_offset(pc, face, level, &pitch, &w, &h);
    const uint8_t *src = (const uint8_t *)phys_to_virt(pc->Data) + off;
    if (fi.compressed) {
        uint32_t rows = (h + 3) / 4;
        for (uint32_t y = 0; y < rows; y++) memcpy(dst + y * dst_pitch, src + y * pitch, pitch < dst_pitch ? pitch : dst_pitch);
        return;
    }
    uint32_t bpp = fi.bpp / 8;
    if (fmt == XFMT_YUY2 || fmt == XFMT_UYVY) {
        for (uint32_t y = 0; y < h; y++) {
            const uint8_t *s = src + y * pitch;
            uint32_t *d = (uint32_t *)(dst + y * dst_pitch);
            for (uint32_t x = 0; x + 1 < w; x += 2, s += 4) {
                int y0, y1, u, v;
                if (fmt == XFMT_YUY2) { y0 = s[0]; u = s[1]; y1 = s[2]; v = s[3]; }
                else { u = s[0]; y0 = s[1]; v = s[2]; y1 = s[3]; }
                d[x] = yuv_to_rgb(y0, u, v);
                d[x + 1] = yuv_to_rgb(y1, u, v);
            }
        }
        return;
    }
    if (fi.linear) {
        for (uint32_t y = 0; y < h; y++) {
            const uint8_t *s = src + y * pitch;
            uint32_t *d = (uint32_t *)(dst + y * dst_pitch);
            for (uint32_t x = 0; x < w; x++) d[x] = decode(fmt, s + x * bpp, pal);
        }
        return;
    }
    Swizzle sw(w, h);
    for (uint32_t y = 0; y < h; y++) {
        uint32_t oy = Swizzle::deposit(y, sw.my);
        uint32_t *d = (uint32_t *)(dst + y * dst_pitch);
        uint32_t ox = 0;
        for (uint32_t x = 0; x < w; x++) {
            d[x] = decode(fmt, src + (ox | oy) * bpp, pal);
            ox = (ox - sw.mx) & sw.mx; // next x in Morton order
        }
    }
}

D3DFORMAT host_format(uint32_t fmt)
{
    switch (fmt) {
    case XFMT_DXT1: return D3DFMT_DXT1;
    case XFMT_DXT3: return D3DFMT_DXT3;
    case XFMT_DXT5: return D3DFMT_DXT5;
    default: return D3DFMT_A8R8G8B8;
    }
}

uint32_t total_bytes(const PixelContainer *pc)
{
    uint32_t w, h, d, levels;
    pixel_container_dims(pc, &w, &h, &d, &levels);
    uint32_t pitch, lw, lh;
    bool cube = (pc->Format & FMT_CUBEMAP) != 0;
    uint32_t last = texture_level_offset(pc, cube ? 5 : 0, levels - 1, &pitch, &lw, &lh);
    const FormatInfo &fi = format_info(fmt_of(pc));
    return last + pitch * (fi.compressed ? (lh + 3) / 4 : lh);
}

// Refill an existing host texture (same size/format) from Xbox memory.
void refill(IDirect3DBaseTexture9 *base, Resource *r, Palette *pal)
{
    auto *pc = (PixelContainer *)r;
    const uint32_t *palette = pal ? (const uint32_t *)phys_to_virt(pal->Data) : nullptr;
    if (base->GetType() == D3DRTYPE_CUBETEXTURE) {
        auto *t = (IDirect3DCubeTexture9 *)base;
        for (uint32_t f = 0; f < 6; f++)
            for (uint32_t l = 0; l < t->GetLevelCount(); l++) {
                D3DLOCKED_RECT lr;
                if (SUCCEEDED(t->LockRect((D3DCUBEMAP_FACES)f, l, &lr, nullptr, 0))) {
                    upload_level(pc, f, l, (uint8_t *)lr.pBits, lr.Pitch, palette);
                    t->UnlockRect((D3DCUBEMAP_FACES)f, l);
                }
            }
        return;
    }
    auto *t = (IDirect3DTexture9 *)base;
    for (uint32_t l = 0; l < t->GetLevelCount(); l++) {
        D3DLOCKED_RECT lr;
        if (SUCCEEDED(t->LockRect(l, &lr, nullptr, 0))) {
            upload_level(pc, 0, l, (uint8_t *)lr.pBits, lr.Pitch, palette);
            t->UnlockRect(l);
        }
    }
}

IDirect3DBaseTexture9 *create_and_upload(Resource *r, Palette *pal)
{
    auto *pc = (PixelContainer *)r;
    uint32_t fmt = fmt_of(pc);
    uint32_t w, h, d, levels;
    pixel_container_dims(pc, &w, &h, &d, &levels);
    if (levels == 0) levels = 1;
    bool cube = (pc->Format & FMT_CUBEMAP) != 0;
    uint32_t dim = (pc->Format >> FMT_DIMENSION_SHIFT) & 0xF;
    if (dim == 3) {
        KNOTE_ONCE("volume textures are not uploaded yet (%ux%ux%u fmt %s)", w, h, d, format_info(fmt).name);
        return nullptr;
    }
    const uint32_t *palette = pal ? (const uint32_t *)phys_to_virt(pal->Data) : nullptr;
    D3DFORMAT hf = host_format(fmt);
    IDirect3DDevice9 *dev = host3d::dev();
    if (cube) {
        IDirect3DCubeTexture9 *t = nullptr;
        if (FAILED(dev->CreateCubeTexture(w, levels, 0, hf, D3DPOOL_MANAGED, &t, nullptr))) return nullptr;
        for (uint32_t f = 0; f < 6; f++)
            for (uint32_t l = 0; l < levels; l++) {
                D3DLOCKED_RECT lr;
                if (SUCCEEDED(t->LockRect((D3DCUBEMAP_FACES)f, l, &lr, nullptr, 0))) {
                    upload_level(pc, f, l, (uint8_t *)lr.pBits, lr.Pitch, palette);
                    t->UnlockRect((D3DCUBEMAP_FACES)f, l);
                }
            }
        return t;
    }
    IDirect3DTexture9 *t = nullptr;
    if (FAILED(dev->CreateTexture(w, h, levels, 0, hf, D3DPOOL_MANAGED, &t, nullptr))) {
        logf("[tex] CreateTexture %ux%u x%u %s failed", w, h, levels, format_info(fmt).name);
        return nullptr;
    }
    for (uint32_t l = 0; l < levels; l++) {
        D3DLOCKED_RECT lr;
        if (SUCCEEDED(t->LockRect(l, &lr, nullptr, 0))) {
            upload_level(pc, 0, l, (uint8_t *)lr.pBits, lr.Pitch, palette);
            t->UnlockRect(l);
        }
    }
    return t;
}

} // namespace

// ---- --dump-textures: every unique game texture as a PNG plus an index (for HD texture mods) ----
namespace {

std::set<uint64_t> g_dumped;
std::set<const Resource *> g_dumped_res; // resources already dumped (content may be re-dumped)
int g_dump_count;

// WIC PNG writer; `rgba` is the port's decoded A8R8G8B8 (B,G,R,A byte order).
bool write_png(const wchar_t *path, const uint8_t *rgba, uint32_t w, uint32_t h)
{
    CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    IWICImagingFactory *f = nullptr;
    IWICStream *st = nullptr;
    IWICBitmapEncoder *enc = nullptr;
    IWICBitmapFrameEncode *fr = nullptr;
    bool ok = false;
    if (SUCCEEDED(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&f))) &&
        SUCCEEDED(f->CreateStream(&st)) && SUCCEEDED(st->InitializeFromFilename(path, GENERIC_WRITE)) &&
        SUCCEEDED(f->CreateEncoder(GUID_ContainerFormatPng, nullptr, &enc)) &&
        SUCCEEDED(enc->Initialize(st, WICBitmapEncoderNoCache)) && SUCCEEDED(enc->CreateNewFrame(&fr, nullptr)) &&
        SUCCEEDED(fr->Initialize(nullptr)) && SUCCEEDED(fr->SetSize(w, h))) {
        WICPixelFormatGUID pf = GUID_WICPixelFormat32bppBGRA;
        fr->SetPixelFormat(&pf);
        ok = SUCCEEDED(fr->WritePixels(h, w * 4, w * h * 4, (BYTE *)rgba)) && SUCCEEDED(fr->Commit()) &&
             SUCCEEDED(enc->Commit());
    }
    if (fr) fr->Release();
    if (enc) enc->Release();
    if (st) st->Release();
    if (f) f->Release();
    return ok;
}

// Raw DXT blocks as a one-level .dds: the host D3D (or ffmpeg, later) decodes it.
bool write_dds(const wchar_t *path, const uint8_t *blocks, uint32_t bytes, uint32_t w, uint32_t h, const char *fourcc)
{
    FILE *f = _wfopen(path, L"wb");
    if (!f) return false;
    uint8_t hd[128];
    memset(hd, 0, sizeof hd);
    auto put32 = [&hd](int off, uint32_t v) { memcpy(hd + off, &v, 4); };
    memcpy(hd, "DDS ", 4);
    put32(4, 124);
    put32(8, 0x00081007); // CAPS | HEIGHT | WIDTH | PIXELFORMAT | LINEARSIZE
    put32(12, h);
    put32(16, w);
    put32(20, bytes);
    put32(76, 32);
    put32(80, 0x4); // DDPF_FOURCC
    memcpy(hd + 84, fourcc, 4);
    put32(108, 0x1000); // DDSCAPS_TEXTURE
    bool ok = fwrite(hd, 1, sizeof hd, f) == sizeof hd && fwrite(blocks, 1, bytes, f) == bytes;
    fclose(f);
    return ok;
}

// FNV-1a over the level-0 content plus the dimensions: a stable name for a game texture.
// DXT levels are hashed as raw blocks (upload_level copies them through untouched), every
// other format is decoded into `px` first, exactly like the renderer does.
uint64_t texture_content(const PixelContainer *pc, const uint32_t *pal, std::vector<uint8_t> &px,
                         const uint8_t **raw, uint32_t *raw_len)
{
    uint32_t w, h, d, levels;
    pixel_container_dims(pc, &w, &h, &d, &levels);
    const FormatInfo &fi = format_info(fmt_of(pc));
    *raw = nullptr;
    *raw_len = 0;
    const uint8_t *src;
    size_t len;
    if (fi.compressed) {
        uint32_t pitch, lw, lh;
        uint32_t off = texture_level_offset(pc, 0, 0, &pitch, &lw, &lh);
        src = (const uint8_t *)phys_to_virt(pc->Data) + off;
        len = (size_t)pitch * ((lh + 3) / 4);
        *raw = src;
        *raw_len = (uint32_t)len;
    } else {
        px.resize((size_t)w * h * 4);
        upload_level(pc, 0, 0, px.data(), w * 4, pal);
        src = px.data();
        len = px.size();
    }
    uint64_t id = 1469598103934665603ull;
    for (size_t i = 0; i < len; i++) id = (id ^ src[i]) * 1099511628211ull;
    id ^= ((uint64_t)w << 40) ^ ((uint64_t)h << 24);
    return id;
}

void dump_texture(const PixelContainer *pc, const uint32_t *pal, const Palette *pal_obj)
{
    uint32_t w, h, d, levels;
    pixel_container_dims(pc, &w, &h, &d, &levels);
    if (w < 16 || h < 16 || d != 1) return; // skip the tiny ones (icons, ramps)
    const FormatInfo &fi = format_info(fmt_of(pc));
    std::vector<uint8_t> px;
    const uint8_t *raw;
    uint32_t raw_bytes;
    uint64_t id = texture_content(pc, pal, px, &raw, &raw_bytes);
    if (!g_dumped.insert(id).second) return;
    char name[96];
    snprintf(name, sizeof name, "tex_%ux%u_%s%08x%08x.%s", w, h, (pc->Format & FMT_CUBEMAP) ? "cube_" : "",
             (uint32_t)(id >> 32), (uint32_t)id, fi.compressed ? "dds" : "png");
    std::wstring path = g_cfg.dump_textures + L"\\" + widen(name);
    bool written = fi.compressed ? write_dds(path.c_str(), raw, raw_bytes, w, h, fi.name)
                                 : write_png(path.c_str(), px.data(), w, h);
    if (written) g_dump_count++;
    static FILE *index;
    if (!index) {
        std::wstring ip = g_cfg.dump_textures + L"\\index.csv";
        bool fresh = true; // keep dumping into the same folder across sessions
        if (FILE *t = _wfopen(ip.c_str(), L"rb")) {
            fseek(t, 0, SEEK_END);
            fresh = ftell(t) == 0;
            fclose(t);
        }
        index = _wfopen(ip.c_str(), fresh ? L"w" : L"a");
        if (index && fresh) fprintf(index, "id,w,h,format,levels,cube,paletted,file\n");
    }
    if (index) {
        fprintf(index, "%016llx,%u,%u,%s,%u,%u,%u,%s\n", (unsigned long long)id, w, h, format_info(fmt_of(pc)).name,
                levels, (pc->Format & FMT_CUBEMAP) ? 1 : 0, pal_obj ? 1 : 0, name);
        fflush(index); // the port ends with TerminateProcess: nothing else flushes
    }
    if (g_dump_count % 100 == 0) logf("[dump] %d textures so far (%ux%u %s)", g_dump_count, w, h, format_info(fmt_of(pc)).name);
}

} // namespace

// ---- --hd-textures <dir>: swap game textures for HD art listed in <dir>\map.csv ----
namespace {

std::unordered_map<uint64_t, std::wstring> g_hd; // content id -> image file
int g_hd_used;
bool g_hd_loaded;

// map.csv lines: "<content id as 16 hex digits>,<image file>" (any path the file system accepts)
void load_hd_map()
{
    std::wstring path = g_cfg.hd_textures + L"\\map.csv";
    FILE *f = _wfopen(path.c_str(), L"r");
    if (!f) {
        logf("[hd] no map.csv in %s", narrow(g_cfg.hd_textures).c_str());
        return;
    }
    char line[2048];
    while (fgets(line, sizeof line, f)) {
        char *comma = strchr(line, ',');
        if (!comma) continue;
        *comma = 0;
        uint64_t id = _strtoui64(line, nullptr, 16);
        char *file = comma + 1;
        size_t n = strlen(file);
        while (n && (file[n - 1] == '\n' || file[n - 1] == '\r' || file[n - 1] == ' ' || file[n - 1] == '\t'))
            file[--n] = 0;
        if (id && n) {
            std::wstring path = widen(file);
            bool absolute = path.size() > 1 && path[1] == L':'; // "C:\..." keeps its drive
            g_hd[id] = absolute ? path : g_cfg.hd_textures + L"\\" + path;
        }
    }
    fclose(f);
    logf("[hd] %zu textures mapped from %s", g_hd.size(), narrow(g_cfg.hd_textures).c_str());
}

IDirect3DBaseTexture9 *upload_bgra(const uint8_t *px, uint32_t w, uint32_t h)
{
    IDirect3DDevice9 *dev = host3d::dev();
    IDirect3DTexture9 *t = nullptr;
    if (!dev || FAILED(dev->CreateTexture(w, h, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_MANAGED, &t, nullptr))) return nullptr;
    D3DLOCKED_RECT lr;
    if (SUCCEEDED(t->LockRect(0, &lr, nullptr, 0))) {
        for (uint32_t y = 0; y < h; y++)
            memcpy((uint8_t *)lr.pBits + (size_t)y * lr.Pitch, px + (size_t)y * w * 4, (size_t)w * 4);
        t->UnlockRect(0);
    }
    return t;
}

IDirect3DBaseTexture9 *load_wic_texture(const std::wstring &path)
{
    CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    IWICImagingFactory *f = nullptr;
    IWICBitmapDecoder *dec = nullptr;
    IWICBitmapFrameDecode *fr = nullptr;
    IWICFormatConverter *cv = nullptr;
    IDirect3DBaseTexture9 *tex = nullptr;
    if (SUCCEEDED(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&f))) &&
        SUCCEEDED(f->CreateDecoderFromFilename(path.c_str(), nullptr, GENERIC_READ, WICDecodeMetadataCacheOnLoad,
                                               &dec)) &&
        SUCCEEDED(dec->GetFrame(0, &fr)) && SUCCEEDED(f->CreateFormatConverter(&cv)) &&
        SUCCEEDED(cv->Initialize(fr, GUID_WICPixelFormat32bppBGRA, WICBitmapDitherTypeNone, nullptr, 0,
                                 WICBitmapPaletteTypeCustom))) {
        UINT w = 0, h = 0;
        if (SUCCEEDED(cv->GetSize(&w, &h)) && w && h) {
            std::vector<uint8_t> px((size_t)w * h * 4);
            if (SUCCEEDED(cv->CopyPixels(nullptr, w * 4, (UINT)px.size(), px.data())))
                tex = upload_bgra(px.data(), w, h);
        }
    }
    if (cv) cv->Release();
    if (fr) fr->Release();
    if (dec) dec->Release();
    if (f) f->Release();
    return tex;
}

uint32_t fourcc_of(const char *s) { return (uint32_t)s[0] | (uint32_t)s[1] << 8 | (uint32_t)s[2] << 16 | (uint32_t)s[3] << 24; }

// A .dds as it comes out of the packs: DXT1/3/5 or 32-bit, usually with a mip chain.
IDirect3DBaseTexture9 *load_dds_texture(const std::wstring &path)
{
    FILE *f = _wfopen(path.c_str(), L"rb");
    if (!f) return nullptr;
    uint8_t hd[128];
    if (fread(hd, 1, sizeof hd, f) != sizeof hd || memcmp(hd, "DDS ", 4) != 0) {
        fclose(f);
        return nullptr;
    }
    auto u32 = [&hd](int off) { uint32_t v; memcpy(&v, hd + off, 4); return v; };
    uint32_t flags = u32(8), h = u32(12), w = u32(16);
    uint32_t mips = (flags & 0x20000) ? u32(28) : 1;
    uint32_t pf_flags = u32(80), fourcc = u32(84), bpp = u32(88);
    if (!mips) mips = 1;
    D3DFORMAT fmt = D3DFMT_UNKNOWN;
    if (pf_flags & 0x4) {
        if (fourcc == fourcc_of("DXT1")) fmt = D3DFMT_DXT1;
        else if (fourcc == fourcc_of("DXT3")) fmt = D3DFMT_DXT3;
        else if (fourcc == fourcc_of("DXT5")) fmt = D3DFMT_DXT5;
    } else if ((pf_flags & 0x40) && (bpp == 32 || bpp == 24)) {
        fmt = D3DFMT_A8R8G8B8;
    }
    if (!w || !h || fmt == D3DFMT_UNKNOWN) {
        fclose(f);
        return nullptr;
    }
    bool compressed = fmt == D3DFMT_DXT1 || fmt == D3DFMT_DXT3 || fmt == D3DFMT_DXT5;
    IDirect3DDevice9 *dev = host3d::dev();
    IDirect3DTexture9 *t = nullptr;
    if (!dev || FAILED(dev->CreateTexture(w, h, mips, 0, fmt, D3DPOOL_MANAGED, &t, nullptr))) {
        fclose(f);
        return nullptr;
    }
    std::vector<uint8_t> buf;
    for (uint32_t l = 0; l < mips; l++) {
        uint32_t lw = w >> l ? w >> l : 1, lh = h >> l ? h >> l : 1;
        uint32_t pitch, rows;
        if (compressed) {
            pitch = ((lw + 3) / 4) * (fmt == D3DFMT_DXT1 ? 8 : 16);
            rows = (lh + 3) / 4;
        } else {
            pitch = lw * (bpp / 8);
            rows = lh;
        }
        buf.resize((size_t)pitch * rows);
        if (fread(buf.data(), 1, buf.size(), f) != buf.size()) break;
        D3DLOCKED_RECT lr;
        if (FAILED(t->LockRect(l, &lr, nullptr, 0))) continue;
        if (compressed) {
            for (uint32_t y = 0; y < rows; y++)
                memcpy((uint8_t *)lr.pBits + (size_t)y * lr.Pitch, buf.data() + (size_t)y * pitch,
                       pitch < (uint32_t)lr.Pitch ? pitch : (uint32_t)lr.Pitch);
        } else if (bpp == 32) {
            for (uint32_t y = 0; y < rows; y++)
                memcpy((uint8_t *)lr.pBits + (size_t)y * lr.Pitch, buf.data() + (size_t)y * pitch,
                       pitch < (uint32_t)lr.Pitch ? pitch : (uint32_t)lr.Pitch);
        } else { // 24-bit BGR
            for (uint32_t y = 0; y < rows; y++) {
                const uint8_t *s = buf.data() + (size_t)y * pitch;
                uint8_t *d = (uint8_t *)lr.pBits + (size_t)y * lr.Pitch;
                for (uint32_t x = 0; x < lw; x++, s += 3, d += 4) {
                    d[0] = s[0];
                    d[1] = s[1];
                    d[2] = s[2];
                    d[3] = 0xFF;
                }
            }
        }
        t->UnlockRect(l);
    }
    fclose(f);
    return t;
}

IDirect3DBaseTexture9 *load_hd_texture(const std::wstring &path)
{
    IDirect3DBaseTexture9 *t = load_dds_texture(path);
    if (!t) t = load_wic_texture(path);
    if (!t) logf("[hd] could not load %s", narrow(path).c_str());
    return t;
}

} // namespace

// The HD pack's own progress for the MODS page (device.h): the textures it replaces and the game's textures in the area
// it covers. map.csv is the pair list the port itself reads (`load_hd_map`); progress.txt is one number written by
// tools/pack_hd_mod.py ("total = <the game's textures in the covered area>"). Both are read once, from --hd-textures
// when the setting is on, or from the mod's own folder (mods\hdtextures\hd) so the row shows the number while it is off.
void hd_progress(int *matched, int *total)
{
    static int s_matched = -1, s_total;
    if (s_matched < 0) {
        s_matched = s_total = 0;
        std::wstring dir = g_cfg.hd_textures;
        if (dir.empty()) dir = g_cfg.port_dir + L"\\mods\\hdtextures\\hd";
        if (FILE *f = _wfopen((dir + L"\\map.csv").c_str(), L"r")) {
            char line[2048];
            while (fgets(line, sizeof line, f)) {
                if (line[0] == '#') continue;
                char *comma = strchr(line, ',');
                if (comma && _strtoui64(line, nullptr, 16)) s_matched++;
            }
            fclose(f);
        }
        if (FILE *f = _wfopen((dir + L"\\progress.txt").c_str(), L"r")) {
            char line[256];
            while (fgets(line, sizeof line, f)) {
                if (line[0] == '#') continue;
                if (!strstr(line, "total")) continue;
                char *eq = strchr(line, '=');
                if (eq) s_total = atoi(eq + 1);
            }
            fclose(f);
        }
        logf("[hd] pack: %d textures replaced, %d in the covered area", s_matched, s_total);
    }
    *matched = s_matched;
    *total = s_total;
}

void pixel_container_dims(const PixelContainer *pc, uint32_t *w, uint32_t *h, uint32_t *d, uint32_t *levels)
{
    if (pc->Size) {
        *w = (pc->Size & SIZE_WIDTH_MASK) + 1;
        *h = ((pc->Size >> SIZE_HEIGHT_SHIFT) & 0xFFF) + 1;
        *d = 1;
        *levels = 1;
    } else {
        *w = 1u << ((pc->Format >> FMT_USIZE_SHIFT) & 0xF);
        *h = 1u << ((pc->Format >> FMT_VSIZE_SHIFT) & 0xF);
        *d = 1u << ((pc->Format >> FMT_PSIZE_SHIFT) & 0xF);
        *levels = (pc->Format >> FMT_MIPMAP_SHIFT) & 0xF;
        if (!*levels) *levels = 1;
    }
}

uint32_t texture_level_offset(const PixelContainer *pc, uint32_t face, uint32_t level, uint32_t *pitch, uint32_t *w, uint32_t *h)
{
    uint32_t tw, th, td, levels;
    pixel_container_dims(pc, &tw, &th, &td, &levels);
    const FormatInfo &fi = format_info(fmt_of(pc));
    if (pc->Size) { // linear: one level
        *pitch = (((pc->Size >> SIZE_PITCH_SHIFT) & 0xFF) + 1) * 64;
        *w = tw;
        *h = th;
        return 0;
    }
    auto level_bytes = [&](uint32_t l, uint32_t *p, uint32_t *lw, uint32_t *lh) {
        uint32_t x = tw >> l, y = th >> l;
        if (!x) x = 1;
        if (!y) y = 1;
        *lw = x;
        *lh = y;
        if (fi.compressed) {
            uint32_t bw = (x + 3) / 4, bh = (y + 3) / 4;
            *p = bw * fi.bpp * 2;
            return *p * bh;
        }
        *p = x * fi.bpp / 8;
        return *p * y;
    };
    uint32_t off = 0, p, lw, lh;
    if (face) {
        uint32_t face_bytes = 0;
        for (uint32_t l = 0; l < levels; l++) face_bytes += level_bytes(l, &p, &lw, &lh);
        face_bytes = (face_bytes + 127) & ~127u; // cube faces are 128-byte aligned
        off = face * face_bytes;
    }
    for (uint32_t l = 0; l < level; l++) off += level_bytes(l, &p, &lw, &lh);
    level_bytes(level, pitch, w, h);
    return off;
}

IDirect3DBaseTexture9 *host_texture(Resource *r, Palette *pal)
{
    if (!r || !r->Data) return nullptr;
    perf::Scope perf_scope(perf::kTexture);
    auto *pc = (PixelContainer *)r;
    EnterCriticalSection(&g_cs);
    Entry &e = g_cache[r];
    uint32_t frame = g_frame_count.load(std::memory_order_relaxed);
    bool skip = e.checked_frame == frame || (e.stable >= kStableChecks && ((frame + ((uintptr_t)r >> 6)) & 15) != 0);
    if (skip && e.tex && !e.dirty && e.data == pc->Data && e.format == pc->Format && e.size == pc->Size &&
        e.palette == (pal ? phys_to_virt(pal->Data) : nullptr)) {
        IDirect3DBaseTexture9 *t = e.tex;
        LeaveCriticalSection(&g_cs);
        return t;
    }
    e.checked_frame = frame;
    uint32_t bytes = total_bytes(pc);
    uint32_t hash = sample_hash((const uint8_t *)phys_to_virt(pc->Data), bytes);
    const void *palptr = pal ? phys_to_virt(pal->Data) : nullptr;
    if (fmt_of(pc) == XFMT_P8 && pal) hash ^= sample_hash((const uint8_t *)palptr, 1024);
    bool same_shape = e.tex && e.data == pc->Data && e.format == pc->Format && e.size == pc->Size;
    bool stale = !same_shape || e.dirty || e.hash != hash || (fmt_of(pc) == XFMT_P8 && e.palette != palptr);
    bool was_stale = stale;
    e.stable = stale ? 0 : std::min<uint32_t>(e.stable + 1, kStableChecks);
    // --dump-textures: only once the content has settled (early snapshots are the game's
    // uninitialised memory, and every later change would dump the same texture again)
    if (!g_cfg.dump_textures.empty() && !was_stale && e.stable >= kStableChecks &&
        g_dumped_res.insert(r).second)
        dump_texture(pc, pal ? (const uint32_t *)phys_to_virt(pal->Data) : nullptr, pal);
    // --hd-textures: if this exact content is in the pack, upload that instead
    if (!g_cfg.hd_textures.empty()) {
        if (!g_hd_loaded) {
            g_hd_loaded = true;
            load_hd_map();
        }
        if (was_stale) {
            std::vector<uint8_t> px;
            const uint8_t *raw;
            uint32_t raw_len;
            uint64_t cid = texture_content(pc, pal ? (const uint32_t *)phys_to_virt(pal->Data) : nullptr, px,
                                           &raw, &raw_len);
            auto it = g_hd.find(cid);
            if (it != g_hd.end() && e.hd_id == cid && e.tex) {
                IDirect3DBaseTexture9 *t = e.tex;
                LeaveCriticalSection(&g_cs);
                return t;
            }
            if (it != g_hd.end()) {
                IDirect3DBaseTexture9 *t = load_hd_texture(it->second);
                if (t) {
                    if (e.tex) e.tex->Release();
                    e.tex = t;
                    e.hd_id = cid;
                    e.data = pc->Data;
                    e.format = pc->Format;
                    e.size = pc->Size;
                    e.palette = palptr;
                    e.hash = hash;
                    e.dirty = false;
                    e.stable = kStableChecks;
                    uint32_t dw, dh, dd, dl;
                    pixel_container_dims(pc, &dw, &dh, &dd, &dl);
                    ++g_hd_used;
                    if (g_hd_used <= 20 || g_hd_used % 50 == 0)
                        logf("[hd] #%d %ux%u %s <- %s", g_hd_used, dw, dh, format_info(fmt_of(pc)).name,
                             narrow(it->second).c_str());
                    LeaveCriticalSection(&g_cs);
                    return t;
                }
            }
            if (e.hd_id) { // the resource now holds something the pack does not cover
                e.hd_id = 0;
                if (e.tex) e.tex->Release();
                e.tex = nullptr;
                same_shape = false; // no host texture to refill
                stale = true;
            }
        }
    }
    if (stale && same_shape) { // contents changed (video frames, render-to-memory): refill in place
        refill(e.tex, r, pal);
        perf::add_texture_upload(bytes);
        e.palette = palptr;
        e.hash = hash;
        e.dirty = false;
        stale = false;
    }
    if (stale) {
        if (e.tex) e.tex->Release();
        e.tex = create_and_upload(r, pal);
        perf::add_texture_upload(bytes);
        e.data = pc->Data;
        e.format = pc->Format;
        e.size = pc->Size;
        e.palette = palptr;
        e.hash = hash;
        e.dirty = false;
        static std::atomic<int> uploads{0};
        if (++uploads <= 40 || g_trace_d3d) {
            uint32_t w, h, d, l;
            pixel_container_dims(pc, &w, &h, &d, &l);
            logf("[tex] upload %p: %ux%u x%u %s%s, data 0x%08x", r, w, h, l, format_info(fmt_of(pc)).name,
                 (pc->Format & FMT_CUBEMAP) ? " cube" : "", pc->Data);
        }
    }
    IDirect3DBaseTexture9 *t = e.tex;
    LeaveCriticalSection(&g_cs);
    return t;
}

void texture_mark_dirty(const Resource *r)
{
    EnterCriticalSection(&g_cs);
    auto it = g_cache.find(r);
    if (it != g_cache.end()) it->second.dirty = true;
    LeaveCriticalSection(&g_cs);
}

void texture_forget(const Resource *r)
{
    EnterCriticalSection(&g_cs);
    auto it = g_cache.find(r);
    if (it != g_cache.end()) {
        if (it->second.tex) it->second.tex->Release();
        g_cache.erase(it);
    }
    LeaveCriticalSection(&g_cs);
}

} // namespace xd3d
