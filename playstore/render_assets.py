#!/usr/bin/env python3
"""
Generate Play Store store-listing assets from Fast Track design tokens.

Outputs (all into playstore/assets/):
  - playstore_icon_512.png     — Play Store app icon (square, no padding)
  - feature_graphic_1024x500.png — Hero graphic at the top of the listing

Why direct Pillow drawing and not SVG rasterization:
  Reportlab / cairosvg both need native libraries (libcairo, rlPyCairo) that
  aren't on this machine. The icon geometry is simple enough to redraw with
  PIL primitives, and that lets us keep all asset generation self-contained.

Run from repo root: python3 playstore/render_assets.py
"""

from PIL import Image, ImageDraw, ImageFont
import math
import os

# --- Palette (matches Resources/Styles/Colors.xaml) ---------------------------
SURFACE       = (19, 19, 19, 255)        # #131313
SURFACE_TINT  = (31, 26, 46, 255)        # #1F1A2E   ambient gradient top
TRACK_GREY    = (42, 42, 42, 255)        # #2A2A2A
PRIMARY       = (196, 192, 255, 255)     # #C4C0FF   indigo fasting arc
SECONDARY     = (169, 211, 139, 255)     # #A9D38B   sage eating arc
SAND          = (255, 227, 176, 255)     # #FFE3B0   hub
DEEP_INDIGO   = (74, 63, 190, 255)       # #4A3FBE   halo
TEXT          = (250, 250, 250, 255)     # #FAFAFA
TEXT_MUTED    = (200, 196, 214, 255)     # #C8C4D6   OnSurfaceVariant

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANROPE   = os.path.join(REPO_ROOT, "Resources/Fonts/Manrope-Variable.ttf")
INTER     = os.path.join(REPO_ROOT, "Resources/Fonts/Inter-Variable.ttf")
OUT_DIR   = os.path.join(REPO_ROOT, "playstore/assets")
os.makedirs(OUT_DIR, exist_ok=True)


def _arc_polygon(cx, cy, radius, stroke_width, start_deg, end_deg, segments=180):
    """Build a thick-arc polygon (so we get rounded caps + clean ends) using
    a fan of segments between inner and outer radii. PIL's draw.arc is hairline
    so we synthesize thickness ourselves."""
    inner = radius - stroke_width / 2
    outer = radius + stroke_width / 2
    pts = []
    # outer edge
    for i in range(segments + 1):
        t = start_deg + (end_deg - start_deg) * i / segments
        rad = math.radians(t)
        pts.append((cx + outer * math.cos(rad), cy + outer * math.sin(rad)))
    # inner edge, reversed
    for i in range(segments, -1, -1):
        t = start_deg + (end_deg - start_deg) * i / segments
        rad = math.radians(t)
        pts.append((cx + inner * math.cos(rad), cy + inner * math.sin(rad)))
    return pts


def _round_cap(draw, cx, cy, angle_deg, radius, stroke_width, color):
    """Round end cap for an arc — a filled circle centered on the arc end."""
    rad = math.radians(angle_deg)
    ex = cx + radius * math.cos(rad)
    ey = cy + radius * math.sin(rad)
    r = stroke_width / 2
    draw.ellipse([ex - r, ey - r, ex + r, ey + r], fill=color)


def _draw_icon_mark(canvas, draw, cx, cy, scale):
    """Render the Fast Track ring + arcs + hub centred at (cx, cy), scaled
    relative to the canonical 456-unit viewBox the SVG uses. The caller
    must pass an RGBA-mode canvas / draw so the translucent halos composite
    properly with whatever sits underneath."""
    # Soft halos — true alpha so they glow against the background rather than
    # painting a flat darker circle. We draw on a temporary RGBA layer and
    # alpha_composite it onto the canvas so PIL handles the blend correctly.
    halo = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    halo_draw = ImageDraw.Draw(halo, "RGBA")
    halo_outer_r = 180 * scale
    halo_inner_r = 138 * scale
    # Lower alphas than the original SVG (0.20/0.15) so the halo reads as a
    # subtle depth glow rather than a hard darker disc on a flat dark canvas.
    halo_draw.ellipse(
        [cx - halo_outer_r, cy - halo_outer_r, cx + halo_outer_r, cy + halo_outer_r],
        fill=(*DEEP_INDIGO[:3], int(0.10 * 255)))
    halo_draw.ellipse(
        [cx - halo_inner_r, cy - halo_inner_r, cx + halo_inner_r, cy + halo_inner_r],
        fill=(*DEEP_INDIGO[:3], int(0.06 * 255)))
    canvas.alpha_composite(halo)

    # Track ring — dark grey behind the colour arcs so the seam between them is invisible.
    track_r = 110 * scale
    track_w = 40 * scale
    track_outer = track_r + track_w / 2
    track_inner = track_r - track_w / 2
    draw.ellipse(
        [cx - track_outer, cy - track_outer, cx + track_outer, cy + track_outer],
        fill=TRACK_GREY)
    draw.ellipse(
        [cx - track_inner, cy - track_inner, cx + track_inner, cy + track_inner],
        fill=SURFACE)

    # Fasting arc — indigo, ~240° clockwise from top. SVG angles are clockwise
    # but PIL angles are clockwise too with 0° at +X. Top = -90° (or 270°).
    # 240° starting from top going clockwise → ends at 150° past top = -90+240 = 150°.
    arc_start = -90       # top
    fast_end  = -90 + 240 # 150°  (bottom-left)
    eat_end   = -90       # back to top (additional 120° from fast_end)
    fast_pts = _arc_polygon(cx, cy, track_r, track_w, arc_start, fast_end)
    draw.polygon(fast_pts, fill=PRIMARY)
    _round_cap(draw, cx, cy, arc_start, track_r, track_w, PRIMARY)
    _round_cap(draw, cx, cy, fast_end,  track_r, track_w, PRIMARY)

    # Eating arc — sage, the remaining ~120° back to the top.
    eat_pts = _arc_polygon(cx, cy, track_r, track_w, fast_end, fast_end + 120)
    draw.polygon(eat_pts, fill=SECONDARY)
    _round_cap(draw, cx, cy, fast_end + 120, track_r, track_w, SECONDARY)

    # Centre hub — sand fill with a soft white highlight (true alpha on top).
    hub_r = 34 * scale
    draw.ellipse([cx - hub_r, cy - hub_r, cx + hub_r, cy + hub_r], fill=SAND)
    hl = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    hl_draw = ImageDraw.Draw(hl, "RGBA")
    hl_r = 10 * scale
    hl_cx = cx - 6 * scale
    hl_cy = cy - 6 * scale
    hl_draw.ellipse(
        [hl_cx - hl_r, hl_cy - hl_r, hl_cx + hl_r, hl_cy + hl_r],
        fill=(255, 255, 255, int(0.45 * 255)))
    canvas.alpha_composite(hl)


def render_play_store_icon():
    """512×512 PNG, no transparency, no rounded corners (Play applies its own mask)."""
    size = 1024  # render at 2x for downscale antialiasing
    img = Image.new("RGBA", (size, size), SURFACE)
    draw = ImageDraw.Draw(img, "RGBA")
    scale = size / 456
    _draw_icon_mark(img, draw, size / 2, size / 2, scale)
    img = img.resize((512, 512), Image.LANCZOS).convert("RGB")
    out = os.path.join(OUT_DIR, "playstore_icon_512.png")
    img.save(out, "PNG", optimize=True)
    print(f"  ✓ {os.path.relpath(out, REPO_ROOT)} (512×512)")


def render_feature_graphic():
    """1024×500 hero card — icon left, wordmark + tagline right."""
    W, H = 1024, 500
    # Render at 2x for crisp text + arcs, downscale at the end.
    # RGBA mode required because _draw_icon_mark uses alpha_composite for halos.
    s = 2
    img = Image.new("RGBA", (W * s, H * s), SURFACE)
    draw = ImageDraw.Draw(img, "RGBA")

    # Vertical ambient gradient: indigo tint at the top fading to surface.
    grad = Image.new("RGBA", (1, H * s))
    for y in range(H * s):
        t = y / (H * s)
        # ease-out so the tint concentrates at the top.
        e = max(0.0, 1 - (t * 1.8))
        r = int(SURFACE_TINT[0] * e + SURFACE[0] * (1 - e))
        g = int(SURFACE_TINT[1] * e + SURFACE[1] * (1 - e))
        b = int(SURFACE_TINT[2] * e + SURFACE[2] * (1 - e))
        grad.putpixel((0, y), (r, g, b, 255))
    grad = grad.resize((W * s, H * s))
    img.paste(grad, (0, 0))

    # Icon on the left — fits in a 380px-tall slot at full quality.
    icon_d = 380 * s
    icon_cx = 230 * s
    icon_cy = (H * s) // 2
    scale = icon_d / 456 * 0.85  # leave a touch of padding inside the slot
    _draw_icon_mark(img, draw, icon_cx, icon_cy, scale)

    # Right column — wordmark + tagline.
    title = "Fast Track"
    tagline = "Privacy-first intermittent fasting"
    sub = "Local-only  ·  No accounts  ·  No analytics"

    # Sizes calibrated so the title clears the right edge with breathing room:
    # title ~96sp in design units → 96*s px at our 2x render, ends well left of W*s.
    title_font   = ImageFont.truetype(MANROPE, 96 * s)
    tagline_font = ImageFont.truetype(INTER,   30 * s)
    sub_font     = ImageFont.truetype(INTER,   22 * s)

    # Variable fonts: set the weight axis explicitly so we don't fall back to
    # the default (thin) weight. Manrope/Inter both expose a 'wght' axis.
    def _set_weight(font, weight):
        try: font.set_variation_by_axes([weight])
        except Exception: pass
    _set_weight(title_font,   700)  # Bold for the brand wordmark
    _set_weight(tagline_font, 500)  # Medium — readable but not shouty
    _set_weight(sub_font,     400)  # Regular for the muted caption

    text_x = 460 * s
    # Use full ascent+descent (font metrics) for line height instead of glyph
    # bbox — bbox is content-fitted and clips below the baseline, which is
    # what caused "Privacy-first" to kiss the bottom of "Fast Track" before.
    title_lh   = title_font.getmetrics()[0]   + title_font.getmetrics()[1]
    tagline_lh = tagline_font.getmetrics()[0] + tagline_font.getmetrics()[1]
    sub_lh     = sub_font.getmetrics()[0]     + sub_font.getmetrics()[1]
    line_gap   = 12 * s

    block_h = title_lh + tagline_lh + sub_lh + line_gap * 2
    cur_y = (H * s - block_h) // 2

    draw.text((text_x, cur_y), title, font=title_font, fill=TEXT)
    cur_y += title_lh + line_gap
    draw.text((text_x, cur_y), tagline, font=tagline_font, fill=PRIMARY)
    cur_y += tagline_lh + line_gap
    draw.text((text_x, cur_y), sub, font=sub_font, fill=TEXT_MUTED)

    # Bottom-right accent: tiny colour-palette swatches as a subtle brand cue.
    palette = [PRIMARY, SECONDARY, (255, 179, 181, 255), SAND, DEEP_INDIGO]
    sw_r = 10 * s
    sw_gap = 18 * s
    sw_y = (H - 32) * s
    sw_x = (W - 32) * s - (len(palette) - 1) * sw_gap - sw_r
    for c in palette:
        draw.ellipse([sw_x - sw_r, sw_y - sw_r, sw_x + sw_r, sw_y + sw_r], fill=c)
        sw_x += sw_gap

    img = img.resize((W, H), Image.LANCZOS).convert("RGB")
    out = os.path.join(OUT_DIR, "feature_graphic_1024x500.png")
    img.save(out, "PNG", optimize=True)
    print(f"  ✓ {os.path.relpath(out, REPO_ROOT)} (1024×500)")


if __name__ == "__main__":
    print("Rendering Play Store assets...")
    render_play_store_icon()
    render_feature_graphic()
    print("Done. Assets in playstore/assets/")
