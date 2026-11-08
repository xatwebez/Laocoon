# Laocoön · Window Probe

[中文](../README.md) · **English** · [Русский](README.ru.md) · [Français](README.fr.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · [Italiano](README.it.md)

> Outside Troy everyone wanted to haul that wooden horse inside. Only Laocoön refused.
> He drove his spear into its belly — and heard metal clashing within.
>
> This tool does the same thing: **poke a window, and hear what it is hiding.**

## A note on the name: he did not save Troy

Laocoön was a priest of Troy. He distrusted the wooden horse, drove his spear into its belly, and urged the city to burn it.

He convinced no one. The gods were on the Greek side: two sea serpents rose from the water and crushed him and his two sons on the shore.
The Trojans read it as proof that he had offended heaven, and hauled the horse inside with an easier mind. That night the hidden soldiers opened the gates, and Troy fell.

This is the least romantic part of the story: **he was right, but he neither made anyone believe it nor lived to see it confirmed.**

That is also where this tool stands. It does not judge for you and does not draw conclusions. It lays out what can be read from a window and its process — paths, signatures, loaded modules, child controls, and a few things it finds suspicious.

Whether something is really suspicious, whether to act, whether to report it, whether this was a false alarm — that call is yours. It can be wrong, too: the "suspicion hints" are heuristics, not verdicts.

The report is a list of facts, not a sentence. Look carefully, then act carefully.

<p align="center">
  <img src="img/laocoon.gif" width="640" alt="Laocoon walks in from the horse's head side and drives a spear into its belly; armed men leap out of the breach and a label reads OCCUPIED, so he backs away with his spear raised">
</p>

You see a window you don't recognise — possibly something nasty. Press `F2`, drag the crosshair onto it, click. It lays out everything it can dig up about that window and the process behind it.

- Single file `Laocoon.exe`, about **384 KB**
- Zero third-party dependencies — only the .NET Framework 4.x that ships with Windows (Win10 / Win11, nothing to install)
- No registry writes, no config files, no background agent (unzip, run — and nothing is left on disk)
- The only network activity is one version check at startup: it reads a public manifest on GitHub and sends nothing about your machine; `--noupdate` turns it off
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

The dropdown is **tiered**: the five UN languages on top, a separator, then the languages of the major developed economies (German / Japanese / Korean / Italian / Portuguese), and below that a "More languages…" entry that opens a small grid panel with the rest on one screen — Arabic and Hindi live there now. Everyday languages never need scrolling, and any language added later lands in that panel automatically.

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

The whole report is plain text: columns line up, sections are separated, so you can select it all and copy, or use "Save report" to write a UTF-8 `.txt`. To check a file further, copy the `sha256` into any service you trust — this tool only captures locally and never uploads anything (the sole connection is the version check described above).

## Automatic updates

Nobody should have to hunt down a download page for a new build. A few seconds after launch, the program asks GitHub once whether anything newer than `1.6.0` exists.

- It reads one public plain-text manifest (`releases/latest/download/latest.txt`): version, download URL, SHA-256, size, date, one line of release notes
- It sends **nothing** about your machine — no device ID, no telemetry, no "who is using this"
- A new version does **not** pop a dialog: an "Update x.y.z" button appears in the toolbar and the status bar says one line. Ignore it and it will not nag again
- Downloading starts only when you click that button: progress bar with a phase line, and the SHA-256 is verified on the spot — a mismatch deletes the file instead of installing half of it
- Once verified, it writes a small replace script, quits, swaps the exe and starts it again. Nobody has to watch it happen
- If the swap fails (antivirus still holding the file, read-only folder), the script brings the old build back up and opens the download page — you never end up staring at a program that vanished
- A failed check (offline, blocked, corporate proxy) is skipped silently; nothing else changes
- `--noupdate` skips the check entirely; `--feed=` points it at an internal mirror or a local file

The manifest and the exe come out of the same CI run and the hash is computed there, so there is no gap where the manifest describes one file and the download delivers another.


## Known limitations

- **Digital signatures**: only PE embedded signatures are checked. Windows' own files (e.g. `notepad.exe`) are catalog-signed, so they show "no embedded signature" — that's expected.
- Without administrator rights, elevated or protected processes yield no path / modules / command line (shown as "access denied"); every other field still works.
- `WM_GETTEXT` for control text uses a 60 ms timeout on unresponsive windows (`SMTO_ABORTIFHUNG`) so the UI never freezes — which means a handful of hung windows give no text.
- The child tree is capped at 240 items and the module list at 400; anything beyond is marked as truncated.
- Hashing stops at 512 MB and is skipped beyond that (to avoid a long stall).
- UI language follows the system; anything outside the twelve falls back to English. Translations are hand-written, and Windows-specific terms such as `WS_*` / `WS_EX_*` and `DWM Cloaked` are deliberately left in English so each locale doesn't invent its own vocabulary.
- The Arabic UI is mirrored, but **the report always reads left-to-right** — it is technical data, and mirroring it makes it unreadable. That is deliberate, not an unfinished translation.

## Changelog

- **1.6.1** — Tiered language dropdown: the five UN languages on top, then the major developed economies' languages, with a "More languages…" entry opening a one-screen grid for the rest; new `--langmenu` diagnostic
- **1.6.0** — Automatic updates: after launch it checks the manifest on GitHub in the background; when a newer build exists an "Update" button appears in the toolbar, and clicking it downloads, verifies SHA-256, then swaps the exe and restarts. Failed checks pass silently, `--noupdate` turns it off
- **1.5.0** — New About window (`F1` or the last toolbar button): two pixel animations, one on where the name comes from and one on what the tool does; both are embedded in the exe, nothing extra to download
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

- `src/Laocoon.cs` — the program, ~3,191 lines, all Win32 P/Invoke, no NuGet packages at all
- `src/Lang.cs` — language core (enum, detection, font, width, lookup, fallback) and all twelve phrase tables in one file, about 2,611 lines
- `src/Update.cs` — the update module: version comparison, manifest parsing, download with SHA-256 verification, the self-replace script and the update window, about 1,195 lines
- `src/Laocoon.ico` — the pixel icon itself (8 sizes, 16–256), generated from a 16-cell master grid by `tools/make_icon.py`
- `docs/img/*.gif` — the two pixel animations in this README, drawn frame by frame by `tools/make_gifs.py` (8 colours, 160×90 grid, integer scaling)

Each table has 159 keys, all compiled into the exe. No `.resources`, no satellite assemblies — so it runs on any Win10/11 machine without ever falling back to English for a "missing language pack".

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
Laocoon.exe --aboutshot out.png [--lang=xx]  render the About window (both animations run a few frames)
Laocoon.exe --langmenu out.png [--lang=xx]  capture the open language dropdown and the second-level panel
Laocoon.exe --updatecheck [url|path]  run the real check once and print the comparison
Laocoon.exe --updatefetch url out.exe  download and verify; print whether sha256 matches
Laocoon.exe --updateplan new.exe old.exe [page]  write the replace script only, to inspect paths and quoting
Laocoon.exe --updateshot out.png [--lang=xx] [--state=dl|fail]  render the update window
```

Combining `--lang=` with `--uishot` renders all twelve UIs in one pass for a visual check.
