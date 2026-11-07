# -*- coding: utf-8 -*-
"""
Laocoon README 像素动画生成器

两个动效，共用同一套 8 色调色板（沿用图标的 6 色 + 2 个过渡色），
全部在 160x90 的逻辑网格上作画，最后按整数倍最近邻放大，边缘永远不模糊。

    laocoon.gif  —— 名字的来历：一矛捅进木马腹部，披甲的小人从破口里跳出来，
                    准星锁定其中一个，旁边弹出信息框
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

import math
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


# ---------------------------------------------------------------- 木马

# 比例照真实侧影取：腿长 ≈ 体深，头长 ≈ 半个体长。
# 坐标是 160x90 逻辑网格上的绝对值，改动时整段平移即可。
GROUND = 79
HX = -10            # 整匹马的横向位移：给右侧的准星和信息框留出地方
BELLY = 42          # 矛尖扎进去的高度
HOLE_CX = 72        # 腹部开口的横向中心
HCX = HOLE_CX + HX  # 位移之后开口的实际横坐标


def _mask(shapes):
    m = Image.new("L", (W, H), 0)
    dm = ImageDraw.Draw(m)
    for kind, data in shapes:
        if kind == "e":
            dm.ellipse(data, fill=255)
        else:
            dm.polygon(data, fill=255)
    return m


def _blit(d, m, body=STEEL, edge=EDGE, under=DIM):
    """遮罩一次成形：内部填 body，外圈 1px 描边，朝下的边压暗做出体积。"""
    px = m.load()

    def on(x, y):
        return 0 <= x < W and 0 <= y < H and px[x, y] > 0

    for y in range(H):
        for x in range(W):
            if not on(x, y):
                continue
            if on(x - 1, y) and on(x + 1, y) and on(x, y - 1) and on(x, y + 1):
                d.point((x, y), fill=body)
            else:
                d.point((x, y), fill=under if not on(x, y + 1) else edge)


def _mv(pts, ox, oy):
    return [(x + ox, y + oy) for x, y in pts]


# 远侧的腿、尾巴：先画，随后被马身压住一半
FAR_LEGS = [[(60, 46), (67, 46), (66, 74), (62, 74)],
            [(77, 46), (84, 46), (83, 74), (79, 74)]]
TAIL = [(47, 31), (43, 33), (40, 42), (37, 53), (35, 64), (39, 66),
        (41, 56), (43, 45), (49, 36)]

# 近侧马身。脖子从肩部斜向右上，头接在脖子顶端、口鼻朝右下
def capsule(p1, p2, r):
    """沿一条轴线拉出的等宽多边形：用来画头这种又长又细的部位。"""
    dx, dy = p2[0] - p1[0], p2[1] - p1[1]
    n = math.hypot(dx, dy)
    nx, ny = -dy / n, dx / n
    pts = [(p1[0] + nx * t, p1[1] + ny * t) for t in range(-r, r + 1)]
    pts += [(p2[0] + nx * t, p2[1] + ny * t) for t in range(r, -r - 1, -1)]
    return pts


NECK = [(83, 28), (99, 35), (105, 18), (96, 7)]
HEAD = [capsule((99, 12), (116, 17), 6), capsule((114, 17), (124, 20), 4)]
EARS = [[(97, 10), (99, 2), (102, 10)], [(104, 9), (106, 2), (109, 10)]]
NEAR_LEGS = [[(51, 46), (59, 46), (57, 78), (54, 78)],
             [(85, 46), (93, 46), (91, 78), (88, 78)]]


def horse(d, ox=0, oy=0, hole=0, hole_h=12):
    """木马侧影。hole 是腹部开口的宽度，0 表示还没被捅开。"""
    # 远侧腿与尾巴暗一档
    _blit(d, _mask([("p", _mv(p, ox, oy)) for p in FAR_LEGS]), body=DIM, edge=EDGE, under=EDGE)
    _blit(d, _mask([("p", _mv(TAIL, ox, oy))]), body=STEEL, edge=EDGE, under=DIM)

    near = [
        ("e", (46 + ox, 26 + oy, 98 + ox, 52 + oy)),     # 躯干
        ("e", (42 + ox, 23 + oy, 70 + ox, 51 + oy)),     # 臀
        ("e", (82 + ox, 29 + oy, 102 + ox, 51 + oy)),    # 前胸
        ("p", _mv(NECK, ox, oy)),
    ]
    near += [("p", _mv(h, ox, oy)) for h in HEAD]
    near += [("p", _mv(e, ox, oy)) for e in EARS]
    near += [("p", _mv(l, ox, oy)) for l in NEAR_LEGS]

    m = _mask(near)
    hx = HOLE_CX - hole // 2
    if hole >= 10:
        # 被劈开的木口不是矩形：四条边都带木茬
        dm = ImageDraw.Draw(m)
        dm.polygon(_mv([(hx + 1, 34), (hx + hole - 4, 33), (hx + hole - 1, 36),
                        (hx + hole, 42), (hx + hole - 4, 46), (hx + 3, 45),
                        (hx - 1, 41), (hx, 36)], ox, oy), fill=0)
    elif hole > 0:
        dm = ImageDraw.Draw(m)
        dm.rectangle([hx + ox, 34 + oy, hx + hole - 1 + ox, 34 + hole_h - 1 + oy], fill=0)

    _blit(d, m)

    px = m.load()

    def on(x, y):
        return 0 <= x < W and 0 <= y < H and px[x, y] > 0

    # 鬃毛：直接沿脖子背缘（左上方那一条）压暗一档，永远贴着真实轮廓
    for y in range(10 + oy, 31 + oy):
        for x in range(85 + ox, 107 + ox):
            if on(x, y) and on(x - 1, y) and not on(x - 2, y):
                d.point((x, y), fill=DIM)

    # 木板拼缝：只在马身内部画，出头的地方不画
    for sy in (33 + oy, 41 + oy):
        for x in range(50 + ox, 95 + ox):
            if on(x, sy) and on(x, sy - 1) and on(x, sy + 1) and on(x - 1, sy) and on(x + 1, sy):
                d.point((x, sy), fill=EDGE)

    # 眼、鼻孔
    rect(d, 107 + ox, 14 + oy, 2, 2, EDGE)
    rect(d, 122 + ox, 19 + oy, 1, 1, EDGE)

    # 蹄子
    for lx, lw in ((53, 5), (88, 5)):
        rect(d, lx + ox, 76 + oy, lw, 2, EDGE)

    if hole > 0:
        # 洞口一圈是刚劈开的木断面：亮一档，形状跟着缺口走
        # 光从上方来：只有洞口下沿的那条木茬是亮的，其余留在暗部
        for y in range(32 + oy, 49 + oy):
            for x in range(hx - 2 + ox, hx + hole + 3 + ox):
                if on(x, y) and not on(x, y - 1):
                    d.point((x, y), fill=BODY)




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

# ---------------------------------------------------------------- 动画一：名字的来历

def ground(d):
    rect(d, 6, GROUND, W - 12, 1, EDGE)


def spear(d, tip, y):
    """一支矛：杆身用准星色（它是「瞄准」这个动作本身），矛尖更亮。"""
    rect(d, max(0, tip - 38), y, min(38, tip - 6), 1, MINT)
    d.polygon([(tip - 6, y - 2), (tip - 2, y - 2), (tip, y),
               (tip - 2, y + 2), (tip - 6, y + 2)], fill=BODY)


def soldier(d, cx, feet):
    """披甲的小人：尖顶盔、面孔、胸甲、圆盾、一支长矛。feet 是脚底。"""
    top = feet - 22

    # 长矛：从脚边伸到头顶之上，握在右手
    rect(d, cx + 5, feet - 28, 1, 26, STEEL)
    rect(d, cx + 5, feet - 28, 1, 4, BODY)
    rect(d, cx + 2, top + 10, 3, 1, STEEL)         # 伸出去的胳膊

    # 腿
    rect(d, cx - 2, feet - 5, 2, 5, DIM)
    rect(d, cx + 1, feet - 5, 2, 5, DIM)

    # 胸甲：上身亮、下摆暗
    rect(d, cx - 2, top + 9, 5, 4, STEEL)
    rect(d, cx - 2, top + 13, 5, 4, DIM)
    border(d, cx - 2, top + 9, 5, 8, EDGE)

    # 脖子压暗一像素，把头和身体分开
    rect(d, cx - 1, top + 8, 3, 1, EDGE)

    # 面孔
    rect(d, cx - 1, top + 5, 3, 3, BODY)

    # 尖顶头盔 + 帽檐
    d.polygon([(cx - 2, top + 5), (cx - 2, top + 2), (cx, top),
               (cx + 2, top + 2), (cx + 2, top + 5)], fill=STEEL)
    rect(d, cx - 2, top + 4, 5, 1, EDGE)

    # 圆盾
    disc(d, cx - 6, top + 12, 4, STEEL)
    circle_outline(d, cx - 6, top + 12, 4, EDGE)
    disc(d, cx - 6, top + 12, 1, DIM)


def tipbox(d, x, y, rows):
    """信息框：和报告里的浮动 HUD 是同一套写法。rows 是已经打出来的行数。"""
    rect(d, x, y, 40, 19, DEEP)
    border(d, x, y, 40, 19, STEEL)
    if rows >= 1:
        rect(d, x + 3, y + 3, 22, 1, DIM)
    if rows >= 2:
        text(d, x + 3, y + 7, "PID 4412", MINT)
    if rows >= 3:
        rect(d, x + 3, y + 13, 28, 1, DIM)
        rect(d, x + 3, y + 16, 18, 1, DIM)


def gif_origin():
    reel = Reel()

    def frame():
        im, d = blank()
        ground(d)
        return im, d

    # 1) 木马静立
    for _ in range(6):
        im, d = frame()
        horse(d, ox=HX)
        reel.add(im)

    # 2) 一支矛从左飞来，直取马腹
    for i in range(9):
        t = i / 8.0
        tip = int(round(-10 + (HCX + 9) * (1 - (1 - t) ** 2)))
        im, d = frame()
        horse(d, ox=HX)
        spear(d, tip, BELLY)
        reel.add(im)

    # 3) 扎进去：木屑迸溅，马身被顶得一抖
    for i in range(5):
        j = (-1, 1, -1, 1, 0)[i]
        im, d = frame()
        horse(d, ox=HX + j)
        circle_outline(d, HCX + j, BELLY, 6 + i * 2, MINT if i < 3 else EDGE)
        k = i + 1
        for sx, sy in ((-11, -3), (10, -4), (-8, 4), (9, 5), (-5, -9), (6, -10)):
            d.point((HCX + j + sx * k // 2, BELLY + sy * k // 2), fill=STEEL)
        for sx, sy in ((-6, 1), (6, -3), (-3, 5)):
            d.point((HCX + j + sx, BELLY + sy), fill=BODY)
        if i < 2:
            spear(d, HCX + 9, BELLY)          # 矛还插在木头上
        reel.add(im)

    # 4) 腹部裂开：木口不是标准矩形
    for w in (4, 10, 16, 21, 24):
        im, d = frame()
        horse(d, ox=HX, hole=w)
        reel.add(im)

    # 5) 披甲的小人一个接一个跳出来，落到马前面的地上
    lands = (94, 116, 138)
    done = []
    for n, lx in enumerate(lands):
        for i in range(8):
            t = i / 7.0
            x = (1 - t) ** 2 * (HCX + 12) + 2 * (1 - t) * t * (HCX + 26 + n * 6) + t * t * lx
            y = (1 - t) ** 2 * (BELLY + 8) + 2 * (1 - t) * t * (BELLY - 14) + t * t * (GROUND - 1)
            im, d = frame()
            horse(d, ox=HX, hole=24)
            for px in done:
                soldier(d, px, GROUND - 1)
            if i < 2:                                   # 从破口里崩出来的几点光
                for sx, sy in ((10, -4), (13, 1), (9, 5), (15, -1)):
                    d.point((HCX + sx, BELLY + sy - i * 2), fill=MINT)
            soldier(d, int(round(x)), int(round(y)))
            reel.add(im)
        done.append(lx)
        # 落地，扬起一点尘
        im, d = frame()
        horse(d, ox=HX, hole=24)
        for px in done:
            soldier(d, px, GROUND - 1)
        for dx in (-5, -2, 3, 5):
            d.point((lx + dx, GROUND - 1), fill=EDGE)
        reel.add(im)

    # 6) 准星从洞口移到最右边那个小人身上，收紧
    tx, ty = 138, GROUND - 13
    for i in range(7):
        t = i / 6.0
        cx = int(round(HCX + (tx - HCX) * t))
        cy = int(round(BELLY + (ty - BELLY) * t))
        r = int(round(17 - 5 * t))
        im, d = frame()
        horse(d, ox=HX, hole=24)
        for px in lands:
            soldier(d, px, GROUND - 1)
        brackets(d, cx, cy, r, MINT, arm=4)
        d.point((cx, cy), fill=BODY)
        reel.add(im)

    def scene(rows):
        im, d = frame()
        horse(d, ox=HX, hole=24)
        for px in lands:
            soldier(d, px, GROUND - 1)
        brackets(d, tx, ty, 12, MINT, arm=4)
        d.point((tx, ty), fill=BODY)
        rect(d, tx, 44, 1, 11, MINT)         # 引线
        tipbox(d, 117, 24, rows)
        return im

    # 7) 旁边的信息框逐行打出来
    for rows in (1, 2, 3):
        reel.add(scene(rows))

    # 8) 停住
    for _ in range(14):
        reel.add(scene(3))

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
              [2, 12, 17, 22, 30, 40, 48, 58, len(a.frames) - 1])
        sheet(b, os.path.join(TMP, "sheet_detect.png"),
              [3, 7, 20, 30, 40, 50, 58, 62, len(b.frames) - 1])


if __name__ == "__main__":
    main()
