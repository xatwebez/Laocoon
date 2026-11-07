# Laocoön · Window Probe

[中文](../README.md) · **English** · [Русский](README.ru.md) · [Français](README.fr.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · [Italiano](README.it.md)

> Outside Troy everyone wanted to haul that wooden horse inside. Only Laocoön refused.
> He drove his spear into its belly — and heard metal clashing within.
>
> This tool does the same thing: **poke a window, and hear what it is hiding.**

<p align="center">
  <img src="img/laocoon.gif" width="640" alt="Laocoon walks in from the horse's head side and drives a spear into its belly; armed men leap out of the breach, a label names them, and he backs away with his spear raised">
</p>

You see a window you don't recognise — possibly something nasty. Press `F2`, drag the crosshair onto it, click. It lays out everything it can dig up about that window and the process behind it.

- Single file `Laocoon.exe`, about **208 KB**
- Zero third-party dependencies — only the .NET Framework 4.x that ships with Windows (Win10 / Win11, nothing to install)
- No network, no upload, no registry writes, no config files, no background agent (unzip, run — and nothing is left on disk)
- Twelve languages built in: **中文 / English / Русский / Français / Español / العربية / Deutsch / Português / 日本語 / 한국어 / हिन्दी / Italiano**

> The file is ASCII `Laocoon.exe`; the window title keeps the diaeresis — `Laocoön`.
> That way paths, command lines, mail attachments and CI never trip over non-ASCII characters.

---

## Usage

<p align="center">
  <img src="img/detect.gif" width="640" alt="After F2 the crosshair locks onto a window; a report rises from the bottom, and one of the three windows of that process is hidden">
</p>

1. Double-click `Laocoon.exe` (for the fullest results: **right-click → Run as administrator**)
2. Press `F2` — the cursor turns into a crosshair
3. Move over the target window: a blue frame wraps it and a small black HUD floats alongside, showing class name / title / PID / parent in real time
4. **Left-click** to lock on and capture; **right-click** or **Esc** cancels
5. The full report appears right away — window, process, file, signature, hidden windows and child controls, all in one place

Capturing is one-shot: nothing refreshes on a timer, nothing sits in the background watching you. Close the window and nothing remains — except the file you wrote yourself with "Save report".

## Interface languages

**12 languages** built in; switch any time from the dropdown in the bottom-right, **no restart**:

| Code | Language | Window title |
|---|---|---|
| `zh` | 中文 | Laocoön 拉奥孔 · 窗口探针 |
| `en` | English | Laocoön · Window Probe |
| `ru` | Русский | Laocoön · Зонд окон |
| `fr` | Français | Laocoön · Sonde de fenêtres |
| `es` | Español | Laocoön · Sonda de ventanas |
| `ar` | العربية | Laocoön · فاحص النوافذ |
| `de` | Deutsch | Laocoön · Fenster-Sonde |
| `pt` | Português | Laocoön · Sonda de janelas |
| `ja` | 日本語 | Laocoön · ウィンドウ探針 |
| `ko` | 한국어 | Laocoön · 창 프로브 |
| `hi` | हिन्दी | Laocoön · विंडो प्रोब |
| `it` | Italiano | Laocoön · Sonda finestre |

How the twelve were chosen: the five permanent members of the UN Security Council as the base, plus **Arabic** (the one UN working language that list misses), **German / Portuguese / Japanese / Korean** (major economies and software ecosystems), **Hindi** (the largest single-language market by population) and **Italian** (European culture). Turkish, Indonesian, Vietnamese and Polish are not included — the extension point is there, adding one means one more table in `Lang.cs`.

On first run the language is decided in this order:

```
--lang=xx on the command line  >  system UI language  >  English
```

So a Chinese system starts in Chinese, and Russian / French / Spanish / Arabic / German / Portuguese / Japanese / Korean / Hindi / Italian systems each show their own; everything else falls back to English (`pt-BR`, `zh-Hant`, `ar-EG` and similar regional forms are all recognised). A switch lasts only for the current run — nothing is saved; to pin the UI to one language, launch `Laocoon.exe --lang=ru` (or make a shortcut carrying that argument).

The **report body** is translated too — labels, section headings and heuristic notes ("executable resides in the temp folder", "the process has N more hidden top-level windows") are all generated in the current language.

### How Arabic (right-to-left) is handled

Arabic is not just "swap the string table". Three traps showed up in testing:

1. **Mirrored UI**: the whole form flips via `RightToLeftLayout`; the toolbar runs right-to-left and the primary button sits at the far right.
2. **The report is not mirrored**: it is full of paths, hashes and window class names. Mirroring reverses the beginning and end of every line, so the report box stays left-to-right. Only the **welcome page** (prose) is right-aligned in Arabic.
3. **The colon jumps to the left of its label**: GDI's BiDi engine treats the colon in "Arabic label + `:` + Latin value" as part of the right-hand RTL run and renders `0 :المقبضx0001048E`. The fix is a zero-width LRM (U+200E) on both sides of the colon to force direction, with the same isolation applied to format arguments (dates, PIDs, class names) — otherwise a date comes out as `04:28:37 08-11-2026`. The side-by-side experiment for all this lives in the `--biditest` diagnostic, so a future reimplementation can re-run it.

**Devanagari** has no fixed advance width any more than Arabic does, so "pad with spaces to align" only makes it worse. Those two languages switch the report to `label: value` instead — better unaligned than pretending to be aligned.
Arabic, Hindi, Japanese and Korean each use the font Windows ships for them (Segoe UI / Nirmala UI / Yu Gothic UI / Malgun Gothic); if one is missing it falls back automatically rather than drawing tofu boxes or throwing.

## What gets collected

| Category | Contents |
|---|---|
| Window | Handle, class, title, style bits (decoded into `WS_*` / `WS_EX_*` names), rect, client area, visible / minimised, **always-on-top**, **tool window**, **DWM-cloaked**, owner window |
| Hit control | Down to the exact child: class, control ID, control text, path from the root window to that control |
| Process | PID / TID, full path, **command line**, parent (name + PID), architecture (x86 / x64 / WOW64), **elevated**, session, start time |
| File | Size, created / modified, **MD5 / SHA1 / SHA256**, company, product, description, file version, copyright |
| Signature | Whether an embedded signature exists, signer, issuer, validity, chain verification result |
| Modules | Full paths of every DLL the process has loaded |
| Windows of the process | **All** top-level windows of that process, hidden ones included (marked `[hidden]`) — a rogue app's background window can't hide here |
| Child tree | The whole child control tree of the target window, with indentation, size, control ID and text |
| Suspicion hints | Heuristics: lives in Temp / Downloads / AppData, unsigned, no company name, topmost, number of hidden windows, file created within 7 days, etc. (**hints only, not a verdict**) |

The whole report is plain text: columns line up, sections are separated, so you can select it all and copy, or use "Save report" to write a UTF-8 `.txt`. To check a file further, copy the `sha256` into any service you trust — this tool only captures locally, never connects or uploads.

## Known limitations

- **Digital signatures**: only PE embedded signatures are checked. Windows' own files (e.g. `notepad.exe`) are catalog-signed, so they show "no embedded signature" — that's expected.
- Without administrator rights, elevated or protected processes yield no path / modules / command line (shown as "access denied"); every other field still works.
- `WM_GETTEXT` for control text uses a 60 ms timeout on unresponsive windows (`SMTO_ABORTIFHUNG`) so the UI never freezes — which means a handful of hung windows give no text.
- The child tree is capped at 240 items and the module list at 400; anything beyond is marked as truncated.
- Hashing stops at 512 MB and is skipped beyond that (to avoid a long stall).
- UI language follows the system; anything outside the twelve falls back to English. Translations are hand-written, and Windows-specific terms such as `WS_*` / `WS_EX_*` and `DWM Cloaked` are deliberately left in English so each locale doesn't invent its own vocabulary.
- The Arabic UI is mirrored, but **the report always reads left-to-right** — it is technical data, and mirroring it makes it unreadable. That is deliberate, not an unfinished translation.

## Changelog

- **1.4.4** — Fixed an occasional crash when holding an arrow key to switch languages fast: the window-handle rebuild no longer happens inside the picker's own message handling, and the RTL flip waits until the keys are released
- **1.4.3** — No more `Laocoon.ini`: the language follows the system, a manual switch lasts for the current run only, and the program leaves no file on disk
- **1.4.2** — The three language files merged into one `Lang.cs`; pure housekeeping, no change to UI, wording or behaviour
- **1.4.1** — Fixed the language picker losing focus when switching to or from Arabic, which silently broke keyboard switching; focus now returns to the list so arrow keys keep working
- **1.4** — New pixel-art icon: 8 sizes from 16 to 256 embedded in the exe, identical in title bar, taskbar and Explorer
- **1.3** — JSON output and the JSON tab removed; plain-text report only
- **1.2** — Cloud lookup removed (URL template + "open in browser"); local capture only; `Laocoon.ini` stores the language only; README available in 12 languages
- **1.1** — UI and report localised into 12 languages; dedicated RTL handling for Arabic
- **1.0** — First release: aim, capture, report

## Building it yourself

You need the .NET SDK (8 or newer). Double-click `build.bat`, or:

```bat
dotnet build src\Laocoon.csproj -c Release
```

The output is `src\bin\Release\net48\Laocoon.exe`.

> `build.bat` deliberately points `TEMP` at a local `.tmp` folder, because on some machines the system temp directory isn't writable and the build fails outright.

## Source layout

- `src/Laocoon.cs` — the program, ~2,225 lines, all Win32 P/Invoke, no NuGet packages at all
- `src/Lang.cs` — language core (enum, detection, font, width, lookup, fallback) and all twelve phrase tables in one file, about 2,250 lines
- `src/Laocoon.ico` — the pixel icon itself (8 sizes, 16–256), generated from a 16-cell master grid by `tools/make_icon.py`
- `docs/img/*.gif` — the two pixel animations in this README, drawn frame by frame by `tools/make_gifs.py` (8 colours, 160×90 grid, integer scaling)

Each table has 124 keys, all compiled into the exe. No `.resources`, no satellite assemblies — so it runs on any Win10/11 machine without ever falling back to English for a "missing language pack".

Main entry points:
- `Probe.HitTest` — walks top-level windows in z-order itself for hit testing (not `WindowFromPoint`, so it never hits its own overlay)
- `Probe.DeepestAt` — drills down with `ChildWindowFromPointEx` to the innermost control
- `Probe.Collect` — gathers window / process / file / signature / modules / sibling windows / child tree
- `FrameForm` / `HudForm` — the transparent blue frame and the following HUD while aiming (both `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, so they never eat mouse input)
- `L.T` / `L.F` — string lookup and placeholder formatting; a missing key falls back to English rather than rendering blank

## Diagnostic switches (for poking at the code)

```
Laocoon.exe --selftest out.txt     capture the console window, print the report, write it to a file
Laocoon.exe --aimtest              self-test hit testing and the aiming overlay
Laocoon.exe --i18ncheck            check all 12 languages for missing keys, column width, fonts, direction
Laocoon.exe --langswitch           walk through every language, verify UI text and report follow
Laocoon.exe --focustest f.log [shot.png]  cycle through all 12 languages, check the picker keeps focus
Laocoon.exe --langstress 300 [log] [--rtl] simulate key-repeat switching, check handle rebuilds and resource stability
Laocoon.exe --biditest out.png     side-by-side of 8 bidi layouts (Arabic + Latin data)
Laocoon.exe --lang=ru              start in a given language (not written to the ini)
Laocoon.exe --uishot out.png --lang=ar            render the UI to a PNG
Laocoon.exe --uishot out.png --lang=ar --welcome  render the welcome page only
Laocoon.exe --uishot out.png --lang=ar --screen   grab the screen instead (DrawToBitmap fakes a selection in RTL boxes)
```

Combining `--lang=` with `--uishot` renders all twelve UIs in one pass for a visual check.
