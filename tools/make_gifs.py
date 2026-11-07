# -*- coding: utf-8 -*-
"""
Laocoon README 像素动画生成器

两个动效，共用同一套 8 色调色板（沿用图标的 6 色 + 2 个过渡色），
全部在 160x90 的逻辑网格上作画，最后按整数倍最近邻放大，边缘永远不模糊。

    laocoon.gif  —— 名字的来历：准星像一矛捅进木马腹部，里面掉出来的是窗口
    detect.gif   —— 它干什么：按 F2，准星压住窗口，报告升起，连藏起来的窗口也列出来

约定：
    * 所有坐标都是逻辑像素，不出现小数
    * 1px 是最细的线，2px 是强调线
    * 虚线 = 「这个窗口是隐身的」，红点 = 「这里不对劲」
    * 只有 8 个颜色，GIF 用固定调色板，不抖动

用法：
    python tools/make_gifs.py            # 生成到 docs/img/
    python tools/make_gifs.py --sheet    # 顺带导出 .tmp/sheet_*.png 对照图
"""

import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "docs", "img")
TMP = os.path.join(ROOT, ".tmp")

W, H = 160, 90          # 逻辑画布
S = 4                   # 放大倍数 -> 640x360
FPS = 10

BG = (23, 35, 59)       # #17233B 底
DEEP = (15, 24, 38)     # #0F1826 更深的底 / 洞口内部
EDGE = (44, 61, 89)     # #2C3D59 暗边
DIM = (58, 78, 110)     # #3A4E6E 次要结构
STEEL = (110, 134, 168)  # #6E86A8 标题栏 / 木马
BODY = (233, 241, 251)  # #E9F1FB 窗身 / 准星高光
MINT = (72, 226, 192)   # #48E2C0 准星
RED = (255, 92, 92)     # #FF5C5C 可疑红点

PALETTE = [BG, DEEP, EDGE, DIM, STEEL, BODY, MINT, RED]


# ---------------------------------------------------------------- 基础绘制

def rect(d, x, y, w, h, c):
    if w <= 0 or h <= 0:
        return
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)


def border(d, x, y, w, h, c, t=1):
    rect(d, x, y, w, t, c)
    rect(d, x, y + h - t, w, t, c)
    rect(d, x, y, t, h, c)
    rect(d, x + w - t, y, t, h, c)


def dashed_border(d, x, y, w, h, c, phase=0, dash=3, gap=2):
    n = dash + gap

    def on(i):
        return (i + phase) % n < dash

    for i in range(w):
        if on(i):
            d.point((x + i, y), fill=c)
            d.point((x + i, y + h - 1), fill=c)
    for i in range(h):
        if on(i):
            d.point((x, y + i), fill=c)
            d.point((x + w - 1, y + i), fill=c)


def circle_outline(d, cx, cy, r, c):
    lo, hi = (r - 0.5) ** 2, (r + 0.5) ** 2
    for yy in range(-r, r + 1):
        for xx in range(-r, r + 1):
            dd = xx * xx + yy * yy
            if lo <= dd <= hi:
                d.point((cx + xx, cy + yy), fill=c)


def disc(d, cx, cy, r, c):
    for yy in range(-r, r + 1):
        for xx in range(-r, r + 1):
            if xx * xx + yy * yy <= r * r:
                d.point((cx + xx, cy + yy), fill=c)


FONT = {
    "0": ("###", "#.#", "#.#", "#.#", "###"),
    "1": (".#.", "##.", ".#.", ".#.", "###"),
    "2": ("###", "..#", "###", "#..", "###"),
    "3": ("###", "..#", "###", "..#", "###"),
    "4": ("#.#", "#.#", "###", "..#", "..#"),
    "5": ("###", "#..", "###", "..#", "###"),
    "6": ("###", "#..", "###", "#.#", "###"),
    "7": ("###", "..#", "..#", "..#", "..#"),
    "8": ("###", "#.#", "###", "#.#", "###"),
    "9": ("###", "#.#", "###", "..#", "###"),
    "F": ("###", "#..", "##.", "#..", "#.."),
    "P": ("###", "#.#", "###", "#..", "#.."),
    "I": ("###", ".#.", ".#.", ".#.", "###"),
    "D": ("##.", "#.#", "#.#", "#.#", "##."),
    " ": ("...", "...", "...", "...", "..."),
}


def text(d, x, y, s, c, sc=1):
    for i, ch in enumerate(s.upper()):
        g = FONT.get(ch)
        if not g:
            continue
        for gy, row in enumerate(g):
            for gx, bit in enumerate(row):
                if bit == "#":
                    rect(d, x + i * 4 * sc + gx * sc, y + gy * sc, sc, sc, c)


# ---------------------------------------------------------------- 图形部件

def brackets(d, cx, cy, r, c, arm=None, t=1):
    """准星的四个角，开口朝内，正卡住一个东西的边缘。"""
    arm = arm if arm is not None else max(2, r // 2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            px, py = cx + sx * r, cy + sy * r
            x0 = min(px, px - sx * arm)
            y0 = min(py, py - sy * arm)
            rect(d, x0, py, arm + 1, t, c)      # 横臂
            rect(d, px, y0, t, arm + 1, c)      # 竖臂


def mini_window(d, x, y, w, h, hidden=False, dot=False, phase=0):
    """报告里的小窗口图标。虚线边框 + 深色内里 = 这个窗口是隐身的。"""
    if hidden:
        rect(d, x, y, w, h, DEEP)
        rect(d, x, y, w, 2, DIM)
        dashed_border(d, x, y, w, h, MINT, phase=phase, dash=2, gap=2)
    else:
        rect(d, x, y, w, h, BODY)
        rect(d, x, y, w, 2, STEEL)
        border(d, x, y, w, h, STEEL)
    if dot:
        rect(d, x + w - 5, y + 3, 2, 2, RED)


def big_window(d, x, y, w, h, lines=0, dark=False, dot=False):
    """桌面上的窗口。"""
    body = DEEP if dark else BODY
    rect(d, x, y, w, h, body)
    rect(d, x, y, w, 4, DIM if dark else STEEL)
    rect(d, x, y + h - 2, w, 2, EDGE)
    border(d, x, y, w, h, EDGE)
    if dot:
        rect(d, x + w - 5, y + 1, 2, 2, RED)
    for i in range(lines):
        lw = (w - 10) - (i * 7) % 12
        rect(d, x + 4, y + 8 + i * 4, max(6, lw), 1, DIM if dark else STEEL)


# 木马侧影：马身、斜脖子、朝右的马头，一次画成一个多边形
HORSE_BODY = [
    (44, 33), (46, 29), (92, 29), (96, 23), (102, 13), (107, 9),
    (109, 5), (113, 5), (113, 10), (126, 11), (131, 14), (131, 20),
    (119, 22), (110, 27), (105, 37), (102, 45), (98, 49),
    (50, 49), (45, 45), (44, 37),
]


def horse(d, ox=0, oy=0, hole=0, rim=None, hole_h=12):
    """木马：马身 + 四条腿 + 车台 + 两个轮子。hole 是腹部开口的宽度。"""
    def R(x, y, w, h, c):
        rect(d, x + ox, y + oy, w, h, c)

    def P(pts, c):
        d.polygon([(x + ox, y + oy) for x, y in pts], fill=c)

    # 地面投影
    R(46, 85, 58, 2, EDGE)

    # 尾巴（压在身后）
    P([(45, 30), (40, 34), (37, 43), (34, 55), (37, 55), (41, 44), (46, 36)], DIM)

    # 车台
    R(46, 69, 58, 4, STEEL)
    R(46, 72, 58, 1, EDGE)

    # 后腿（远）先画，前腿（近）压在上面
    R(54, 49, 5, 20, DIM)
    R(80, 49, 5, 20, DIM)
    R(64, 49, 5, 20, STEEL)
    R(90, 49, 5, 20, STEEL)
    for lx in (54, 64, 80, 90):
        R(lx, 67, 5, 2, EDGE)

    # 马身
    P(HORSE_BODY, STEEL)
    R(50, 47, 48, 2, DIM)          # 腹部暗边

    # 鬃毛：脖子背面几粒锯齿
    R(96, 21, 2, 3, DIM)
    R(99, 16, 2, 3, DIM)
    R(102, 12, 2, 3, DIM)

    # 眼与鼻
    R(116, 14, 2, 2, EDGE)
    R(127, 17, 1, 2, EDGE)

    # 轮子
    for wx in (60, 90):
        disc(d, wx + ox, 79 + oy, 5, STEEL)
        circle_outline(d, wx + ox, 79 + oy, 5, EDGE)
        rect(d, wx + ox - 1, 79 + oy - 1, 2, 2, EDGE)

    # 腹部开口
    if hole > 0:
        hx = 74 - hole // 2
        rect(d, hx + ox, 34 + oy, hole, hole_h, DEEP)
        if rim:
            border(d, hx + ox, 34 + oy, hole, hole_h, rim)
        if hole >= 20:
            # 被捅开的口子不该是标准矩形，四个小缺角就是木茬
            for nx, ny, nw in ((4, 33, 2), (21, 33, 3), (9, 46, 3), (26, 46, 2)):
                rect(d, hx + nx + ox, ny + oy, nw, 1, DEEP)


def icon(d, cx, cy, pulse=None):
    """程序自己的图标：窗口 + 准星括号 + 红点（放大版，收尾用）。"""
    rect(d, cx - 15, cy - 11, 30, 22, BODY)
    rect(d, cx - 15, cy - 11, 30, 5, STEEL)
    rect(d, cx - 15, cy + 9, 30, 2, EDGE)
    rect(d, cx + 8, cy - 10, 4, 4, RED)
    brackets(d, cx, cy, 20, MINT, arm=7, t=2)
    if pulse is not None:
        circle_outline(d, cx, cy, pulse, EDGE)


# ---------------------------------------------------------------- 帧容器

class Reel:
    def __init__(self):
        self.frames = []
        self.ms = 1000 // FPS

    def add(self, im, n=1):
        for _ in range(n):
            self.frames.append((im.copy(), self.ms))
        return im


def blank():
    im = Image.new("RGB", (W, H), BG)
    return im, ImageDraw.Draw(im)


# ---------------------------------------------------------------- 动画一：名字的来历

HOLE_Y = 33
HOLE_H = 14
WIN_Y = 36
BELLY = 40


def gif_origin():
    reel = Reel()
    MID = 74

    # 1) 木马静立
    im, d = blank()
    horse(d)
    reel.add(im, 6)

    # 2) 准星从左飞入，像一矛指向马腹
    x0, tgt = -12, MID
    for i in range(15):
        t = i / 14.0
        cx = int(round(x0 + (tgt - x0) * (1 - (1 - t) ** 2)))
        im, d = blank()
        horse(d)
        brackets(d, cx, BELLY, 9, MINT, arm=4)
        d.point((cx, BELLY), fill=BODY)
        reel.add(im)

    # 3) 捅进去：环扩散 + 木马被顶得一抖
    for i in range(5):
        im, d = blank()
        jitter = (-1, 1, -1, 1, 0)[i]
        horse(d, ox=jitter)
        circle_outline(d, MID + jitter, BELLY, 10 + i * 3, MINT if i < 3 else EDGE)
        brackets(d, MID + jitter, BELLY, 9, MINT, arm=4)
        if i < 3:
            k = i + 1
            for sx, sy in ((-10, -2), (9, -3), (-7, 5), (8, 6), (-4, -8), (5, -9)):
                d.point((MID + jitter + sx * k // 2, BELLY + sy * k // 2), fill=STEEL)
            for sx, sy in ((-6, 0), (5, -4), (-3, 4)):
                d.point((MID + jitter + sx, BELLY + sy), fill=BODY)
        reel.add(im)

    # 4) 腹部裂开一个口子
    for w in (4, 10, 16, 22, 28, 33, 37):
        im, d = blank()
        horse(d, hole=w, rim=MINT, hole_h=HOLE_H)
        brackets(d, MID, BELLY - 1, 13, MINT, arm=5)
        reel.add(im)

    # 5) 洞里亮出三个窗口，一个一个来
    positions = (58, 70, 82)
    for n in range(1, 4):
        for step in range(3):
            im, d = blank()
            horse(d, hole=37, rim=MINT, hole_h=HOLE_H)
            for k in range(n):
                wx = positions[k]
                if k == n - 1 and step == 0:
                    dashed_border(d, wx, WIN_Y, 10, 9, MINT, phase=0, dash=2, gap=2)
                else:
                    mini_window(d, wx, WIN_Y, 10, 9, hidden=(k == 1))
            reel.add(im)

    # 6) 中间那个闪红点：它是藏起来的
    for i in range(8):
        im, d = blank()
        horse(d, hole=37, rim=MINT, hole_h=HOLE_H)
        for k in range(3):
            mini_window(d, positions[k], WIN_Y, 10, 9, hidden=(k == 1),
                        dot=(k == 1 and i % 4 < 2))
        reel.add(im)

    # 7) 收束到程序图标
    im, d = blank()
    reel.add(im, 2)

    im, d = blank()
    brackets(d, 80, 45, 20, MINT, arm=7, t=2)
    reel.add(im)

    im, d = blank()
    rect(d, 65, 34, 30, 22, DEEP)
    rect(d, 65, 34, 30, 5, DIM)
    brackets(d, 80, 45, 20, MINT, arm=7, t=2)
    reel.add(im)

    im, d = blank()
    icon(d, 80, 45)
    reel.add(im)

    for i in range(3):
        im, d = blank()
        icon(d, 80, 45, pulse=23 + i * 4)
        reel.add(im)
    im, d = blank()
    icon(d, 80, 45)
    reel.add(im, 10)

    return reel


# ---------------------------------------------------------------- 动画二：检测过程

WIN_A = (8, 8, 52, 34)
WIN_B = (52, 22, 56, 34)
WIN_C = (110, 10, 40, 30)


def desktop(d, target=None, ants=0, hud=None, hot=None):
    big_window(d, *WIN_A, lines=5)
    big_window(d, *WIN_B, lines=5)
    # 桌面上那个「看着就不对劲」的窗口：深色界面 + 红点
    x, y, w, h = WIN_C
    big_window(d, x, y, w, h, lines=3, dark=True, dot=True)
    dashed_border(d, x, y, w, h, DIM, phase=0, dash=3, gap=3)

    # 任务栏
    rect(d, 0, 80, W, 10, DEEP)
    for i in range(4):
        rect(d, 6 + i * 9, 83, 5, 4, STEEL if i else DIM)

    if target:
        tx, ty, tw, th = target
        if hot == "solid":
            border(d, tx - 2, ty - 2, tw + 4, th + 4, MINT, t=2)
        else:
            dashed_border(d, tx - 2, ty - 2, tw + 4, th + 4, MINT,
                          phase=ants, dash=4, gap=3)

    if hud:
        hx, hy = hud
        rect(d, hx, hy, 44, 16, DEEP)
        border(d, hx, hy, 44, 16, STEEL)
        rect(d, hx + 3, hy + 3, 24, 1, DIM)
        text(d, hx + 3, hy + 6, "PID 4412", MINT)
        rect(d, hx + 3, hy + 13, 30, 1, DIM)


CURSOR = (
    "X.....",
    "XX....",
    "X.X...",
    "X..X..",
    "X...X.",
    "X....X",
    "X..XXX",
    "X..X..",
    "XX.X..",
    ".X.X..",
    "..XX..",
    "...X..",
)


def arrow(d, x, y):
    for gy, row in enumerate(CURSOR):
        for gx, ch in enumerate(row):
            if ch == "X":
                d.point((x + gx, y + gy), fill=BODY)


def keycap(d, x, y, pressed=False):
    oy = 1 if pressed else 0
    rect(d, x, y + 2, 16, 12, EDGE)
    rect(d, x, y + oy, 16, 12, DEEP if pressed else BG)
    border(d, x, y + oy, 16, 12, MINT if pressed else STEEL)
    text(d, x + 3, y + 3 + oy, "F2", MINT if pressed else STEEL)


def report(d, top, rows, hidden_step=0, blink=False):
    """报告面板。rows 是已经打出来的行数。"""
    rect(d, 0, top, W, H - top, DEEP)
    rect(d, 0, top, W, 1, MINT)

    # 左栏：一行行采集结果
    text(d, 4, top + 5, "PID 4412", MINT)
    widths = (70, 58, 66, 44, 62, 52, 68, 40, 56, 64, 46)
    for i in range(min(rows, len(widths))):
        rect(d, 4, top + 13 + i * 4, widths[i], 1, DIM if i % 3 else STEEL)

    # 右栏：这个进程的全部窗口，包括藏起来的
    if hidden_step > 0:
        bx, by, bw, bh = 96, top + 3, 58, 34
        border(d, bx, by, bw, bh, DIM)
        for k in range(3):
            if k < hidden_step:
                x = 100 + k * 18
                hidden = (k == 1)
                mini_window(d, x, by + 6, 14, 11, hidden=hidden,
                            dot=(hidden and blink))
        text(d, bx + 3, by + 25, "PID 4412", DIM)


def hud_at(cx, cy):
    """小信息框跟着准星走，始终在它右上方，不出画面。"""
    hx = min(max(cx + 12, 2), W - 46)
    hy = min(max(cy - 30, 2), H - 18)
    return (hx, hy)


def gif_detect():
    reel = Reel()
    b = WIN_B
    c = WIN_C

    # 1) 桌面静止，箭头停在下面
    for i in range(5):
        im, d = blank()
        desktop(d)
        keycap(d, 6, 64)
        arrow(d, 27, 64)
        reel.add(im)

    # 2) 按下 F2
    for i in range(4):
        im, d = blank()
        desktop(d)
        keycap(d, 6, 64, pressed=(i < 3))
        arrow(d, 27, 64)
        if i == 3:
            brackets(d, 32, 69, 11, MINT, arm=4)
        reel.add(im)

    # 3) 准星移到 B 窗口上
    sx, sy, ex, ey = 32, 69, 78, 38
    for i in range(12):
        t = i / 11.0
        cx = int(round(sx + (ex - sx) * t))
        cy = int(round(sy + (ey - sy) * t))
        im, d = blank()
        desktop(d)
        keycap(d, 6, 64)
        brackets(d, cx, cy, 11, MINT, arm=4)
        d.point((cx, cy), fill=BODY)
        reel.add(im)

    # 4) 压住 B：虚线框 + 浮动 HUD
    for i in range(5):
        im, d = blank()
        desktop(d, target=b, ants=i * 2, hud=hud_at(78, 38))
        keycap(d, 6, 64)
        brackets(d, 78, 38, 11, MINT, arm=4)
        reel.add(im)

    # 5) 移到可疑窗口 C
    sx, sy, ex, ey = 78, 38, 130, 25
    for i in range(10):
        t = i / 9.0
        cx = int(round(sx + (ex - sx) * t))
        cy = int(round(sy + (ey - sy) * t))
        im, d = blank()
        desktop(d, target=None if i < 4 else c, ants=i * 2,
                hud=None if i < 4 else hud_at(cx, cy))
        keycap(d, 6, 64)
        brackets(d, cx, cy, 11, MINT, arm=4)
        d.point((cx, cy), fill=BODY)
        reel.add(im)

    # 6) 点击锁定
    for i in range(4):
        im, d = blank()
        desktop(d, target=c, hot="solid")
        keycap(d, 6, 64)
        brackets(d, 130, 25, 11, MINT, arm=4)
        if i < 3:
            circle_outline(d, 130, 25, 13 + i * 4, MINT if i < 2 else EDGE)
        reel.add(im)

    # 7) 报告升起
    for top in (89, 81, 73, 65, 57, 51, 46, 44):
        im, d = blank()
        desktop(d, target=c, hot="solid")
        brackets(d, 130, 25, 11, MINT, arm=4)
        report(d, top, 0)
        reel.add(im)

    # 8) 逐行打出来
    for n in (1, 3, 5, 7, 9, 11):
        im, d = blank()
        desktop(d, target=c, hot="solid")
        brackets(d, 130, 25, 11, MINT, arm=4)
        report(d, 44, n)
        reel.add(im)

    # 9) 右栏：这个进程还有三个窗口，中间那个是藏起来的
    for step in (1, 2, 3):
        im, d = blank()
        desktop(d, target=c, hot="solid")
        brackets(d, 130, 25, 11, MINT, arm=4)
        report(d, 44, 11, hidden_step=step)
        reel.add(im)

    # 10) 红点闪两下，收尾
    for i in range(8):
        im, d = blank()
        desktop(d, target=c, hot="solid")
        brackets(d, 130, 25, 11, MINT, arm=4)
        report(d, 44, 11, hidden_step=3, blink=(i % 4 < 2))
        reel.add(im)

    im, d = blank()
    desktop(d, target=c, hot="solid")
    report(d, 44, 11, hidden_step=3, blink=True)
    reel.add(im, 6)

    return reel


# ---------------------------------------------------------------- 输出

def save(reel, path):
    # 相邻完全相同的帧合并，只调时长，不做无意义的重绘
    merged = []
    for im, ms in reel.frames:
        if merged and merged[-1][0].tobytes() == im.tobytes():
            merged[-1][1] += ms
        else:
            merged.append([im, ms])

    pal = Image.new("P", (1, 1))
    flat = []
    for c in PALETTE:
        flat += list(c)
    pal.putpalette(flat + [0] * (768 - len(flat)))

    qs = [im.resize((W * S, H * S), Image.NEAREST).quantize(
        palette=pal, dither=Image.NONE) for im, _ in merged]
    durs = [ms for _, ms in merged]

    qs[0].save(path, save_all=True, append_images=qs[1:], duration=durs,
               loop=0, optimize=True, disposal=1)
    print("%-24s %2d 帧  %6.1f KB" % (os.path.basename(path), len(qs),
                                      os.path.getsize(path) / 1024.0))


def sheet(reel, path, picks):
    """把指定的几帧并排拼成一张对照图，方便肉眼检查。"""
    cols = 3
    rows = (len(picks) + cols - 1) // cols
    tile_w, tile_h = W * 2, H * 2
    out = Image.new("RGB", (cols * (tile_w + 8) + 8, rows * (tile_h + 8) + 8), (0, 0, 0))
    for n, i in enumerate(picks):
        im = reel.frames[min(i, len(reel.frames) - 1)][0].resize((tile_w, tile_h), Image.NEAREST)
        gx = 8 + (n % cols) * (tile_w + 8)
        gy = 8 + (n // cols) * (tile_h + 8)
        out.paste(im, (gx, gy))
    out.save(path)
    print("对照图 %s -> %s" % (path, out.size))


def main():
    for p in (OUT, TMP):
        if not os.path.isdir(p):
            os.makedirs(p)

    a = gif_origin()
    b = gif_detect()
    save(a, os.path.join(OUT, "laocoon.gif"))
    save(b, os.path.join(OUT, "detect.gif"))

    if "--sheet" in sys.argv:
        sheet(a, os.path.join(TMP, "sheet_origin.png"),
              [2, 12, 20, 24, 30, 42, 48, 62, len(a.frames) - 1])
        sheet(b, os.path.join(TMP, "sheet_detect.png"),
              [3, 7, 20, 30, 40, 50, 58, 62, len(b.frames) - 1])


if __name__ == "__main__":
    main()
