# Laocoön · Fenster-Sonde

[中文](../README.md) · [English](README.en.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Español](README.es.md) · [العربية](README.ar.md) · **Deutsch** · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · [Italiano](README.it.md)

> Vor Troja wollten alle das hölzerne Pferd in die Stadt ziehen. Nur Laokoon weigerte sich.
> Er stieß seinen Speer in seinen Bauch — und hörte darin Waffen klirren.
>
> Dieses Werkzeug tut dasselbe: **stich in ein Fenster und höre, was darin verborgen ist.**

Da ist ein Fenster, das Sie nicht kennen — vielleicht etwas Unangenehmes. `F2` drücken, Fadenkreuz darüberziehen, klicken. Es legt alles offen, was sich über dieses Fenster und den dahinterstehenden Prozess herausfinden lässt.

- Einzelne Datei `Laocoon.exe`, etwa **202 KB**
- Keine Fremdabhängigkeiten — nur das .NET Framework 4.x, das Windows mitbringt (Win10 / Win11, nichts zu installieren)
- Kein Netz, kein Upload, keine Registry-Einträge, keine Konfigurationsdatei, kein Hintergrunddienst (entpacken, starten — nichts bleibt auf der Platte)
- Zwölf Sprachen eingebaut: **中文 / English / Русский / Français / Español / العربية / Deutsch / Português / 日本語 / 한국어 / हिन्दी / Italiano**

> Der Dateiname bleibt ASCII: `Laocoon.exe`. Im Fenstertitel steht weiterhin das Trema — `Laocoön`.
> So stolpern Pfade, Kommandozeilen, Mailanhänge und CI nie über Nicht-ASCII-Zeichen.

---

## Bedienung

1. Doppelklick auf `Laocoon.exe` (für die vollständigsten Daten: **Rechtsklick → Als Administrator ausführen**)
2. `F2` drücken — der Zeiger wird zum Fadenkreuz
3. Über das Zielfenster fahren: ein blauer Rahmen legt sich darum, daneben schwebt ein kleiner schwarzer HUD mit Klasse / Titel / PID / Elternfenster in Echtzeit
4. **Linksklick** fixiert und erfasst; **Rechtsklick** oder **Esc** bricht ab
5. Der vollständige Bericht erscheint sofort — Fenster, Prozess, Datei, Signatur, versteckte Fenster und Kindelemente, alles auf einen Blick

Die Erfassung ist einmalig: nichts aktualisiert sich im Takt, nichts bleibt im Hintergrund und schaut zu. Fenster zu — und es bleibt nichts übrig, außer der Datei, die Sie selbst mit „Bericht speichern“ geschrieben haben.

## Oberflächensprachen

**12 Sprachen** eingebaut, jederzeit über die Liste unten rechts umschaltbar, **ohne Neustart**:

| Code | Sprache | Fenstertitel |
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

Warum genau diese zwölf: die Sprachen der fünf ständigen Mitglieder des UN-Sicherheitsrats als Basis, dazu **Arabisch** (die einzige UN-Arbeitssprache, die dort fehlt), **Deutsch / Portugiesisch / Japanisch / Koreanisch** (große Volkswirtschaften und Software-Ökosysteme), **Hindi** (zahlenmäßig größter einsprachiger Markt) und **Italienisch** (europäische Kultur). Türkisch, Indonesisch, Vietnamesisch und Polnisch fehlen — der Erweiterungspunkt bleibt offen: eine Sprache mehr heißt eine Tabelle mehr in `Lang.cs`.

Beim ersten Start entscheidet diese Reihenfolge über die Sprache:

```
--lang=xx auf der Kommandozeile  >  Systemsprache  >  Englisch
```

Ein chinesisches System startet also auf Chinesisch, russische / französische / spanische / arabische / deutsche / portugiesische / japanische / koreanische / Hindi- / italienische Systeme zeigen ihre eigene Sprache, alles andere fällt auf Englisch zurück (regionale Varianten wie `pt-BR`, `zh-Hant`, `ar-EG` werden erkannt). Die Umschaltung gilt nur für den laufenden Start — gespeichert wird nichts; zum Festnageln `Laocoon.exe --lang=ru` starten (oder eine Verknüpfung mit diesem Argument anlegen).

Übersetzt wird nicht nur die Oberfläche, sondern auch **der Berichtstext** — Beschriftungen, Abschnittstitel und heuristische Anmerkungen („Die ausführbare Datei liegt im Temp-Ordner“, „Der Prozess hat N weitere versteckte Fenster oberster Ebene“) entstehen in der aktuellen Sprache.

### Wie Arabisch (rechts nach links) behandelt wird

Arabisch ist nicht „Stringtabelle austauschen“. Im Test kamen drei Fallen zum Vorschein:

1. **Gespiegelte Oberfläche**: das ganze Fenster wird über `RightToLeftLayout` gedreht, die Werkzeugleiste läuft von rechts nach links, die Hauptschaltfläche sitzt ganz rechts.
2. **Der Bericht wird nicht gespiegelt**: darin stehen Pfade, Hashes, Fensterklassen. Spiegeln vertauscht Anfang und Ende jeder Zeile, deshalb bleibt das Berichtsfeld immer links nach rechts. Nur die **Begrüßungsseite** (Fließtext) ist auf Arabisch rechtsbündig.
3. **Der Doppelpunkt springt links neben seine Beschriftung**: GDIs BiDi-Engine ordnet den Doppelpunkt in „arabische Beschriftung + `:` + lateinischer Wert“ dem rechten RTL-Lauf zu und zeichnet `0 :المقبضx0001048E`. Die Lösung ist ein Zero-Width-LRM (U+200E) auf beiden Seiten des Doppelpunkts, der die Richtung erzwingt; dieselbe Isolation bekommen Formatargumente (Datum, PID, Klassennamen) — sonst wird aus einem Datum `04:28:37 08-11-2026`. Der Vergleichsversuch mit allen Schreibweisen steckt im Diagnoseschalter `--biditest`, damit er bei einer Neuimplementierung erneut laufen kann.

**Devanagari** (Hindi) hat genauso wenig feste Zeichenbreite wie Arabisch — „mit Leerzeichen auffüllen zum Ausrichten“ macht es nur schlimmer. Diese beiden Sprachen stellen den Bericht deshalb auf `Beschriftung: Wert` um: lieber unausgerichtet als scheinbar ausgerichtet.
Arabisch, Hindi, Japanisch und Koreanisch nutzen jeweils die von Windows mitgelieferte Schrift (Segoe UI / Nirmala UI / Yu Gothic UI / Malgun Gothic); fehlt sie, wird automatisch zurückgefallen — ohne Kästchen und ohne Ausnahme.

## Was erfasst wird

| Kategorie | Inhalt |
|---|---|
| Fenster | Handle, Klasse, Titel, Stilbits (zu `WS_*` / `WS_EX_*`-Namen dekodiert), Rechteck, Clientbereich, sichtbar / minimiert, **immer im Vordergrund**, **Toolfenster**, **DWM-verdeckt**, Besitzerfenster |
| Getroffenes Steuerelement | Bis zum genauen Kindelement: Klasse, Steuerelement-ID, Text, Pfad vom Wurzelfenster dorthin |
| Prozess | PID / TID, vollständiger Pfad, **Befehlszeile**, Elternprozess (Name + PID), Architektur (x86 / x64 / WOW64), **erhöhte Rechte**, Sitzung, Startzeit |
| Datei | Größe, Erstellt / Geändert, **MD5 / SHA1 / SHA256**, Firma, Produkt, Beschreibung, Dateiversion, Copyright |
| Signatur | Ob eine eingebettete Signatur existiert, Unterzeichner, Aussteller, Gültigkeit, Ergebnis der Kette |
| Module | Vollständige Pfade aller vom Prozess geladenen DLLs |
| Fenster des Prozesses | **Alle** Fenster oberster Ebene dieses Prozesses, auch versteckte (mit `[versteckt]` markiert) — das Hintergrundfenster einer unerwünschten Software versteckt sich hier nicht |
| Steuerelementbaum | Der komplette Baum der Kindelemente des Zielfensters: Einrückung, Größe, ID, Text |
| Verdachtshinweise | Heuristik: liegt in Temp / Downloads / AppData, unsigniert, kein Firmenname, im Vordergrund, Anzahl versteckter Fenster, Datei in den letzten 7 Tagen erstellt usw. (**Hinweise, kein Urteil**) |

Der ganze Bericht ist reiner Text: Spalten sind ausgerichtet, Abschnitte getrennt — alles markieren und kopieren, oder über „Bericht speichern“ als UTF-8-`.txt` schreiben. Zur weiteren Prüfung den `sha256` in eine Vertrauensstelle eigener Wahl kopieren — dieses Werkzeug erfasst nur lokal, verbindet sich nicht und lädt nichts hoch.

## Bekannte Einschränkungen

- **Digitale Signatur**: nur in die PE eingebettete Signaturen werden geprüft. Windows-eigene Dateien (etwa `notepad.exe`) sind per Katalog signiert, dort steht also „keine eingebettete Signatur“ — das ist normal.
- Ohne Administratorrechte liefern erhöhte oder geschützte Prozesse weder Pfad noch Module oder Befehlszeile („Zugriff verweigert“); alle anderen Felder funktionieren weiter.
- `WM_GETTEXT` für Steuerelementtexte hat bei nicht reagierenden Fenstern 60 ms Zeitlimit (`SMTO_ABORTIFHUNG`), damit die Oberfläche nie einfriert — deshalb bleibt bei einzelnen hängenden Fenstern der Text aus.
- Der Steuerelementbaum endet bei 240 Einträgen, die Modulliste bei 400; darüber wird „gekürzt“ vermerkt.
- Hashing stoppt bei 512 MB, darüber wird übersprungen (damit es nicht ewig dauert).
- Die Sprache folgt dem System; außerhalb der zwölf wird Englisch verwendet. Die Übersetzungen sind von Hand geschrieben, und Windows-spezifische Begriffe (`WS_*` / `WS_EX_*`, `DWM Cloaked`) bleiben bewusst englisch, damit nicht jede Sprache ihr eigenes Vokabular erfindet.
- Die arabische Oberfläche ist gespiegelt, **der Bericht bleibt aber immer links nach rechts** — das sind technische Daten, gespiegelt sind sie unlesbar. Das ist Absicht, keine unfertige Übersetzung.

## Versionsverlauf

- **1.4.3** — Keine `Laocoon.ini` mehr: die Sprache folgt dem System, manuelles Umschalten gilt nur für den laufenden Start, und es bleibt keine Datei zurück
- **1.4.2** — Die drei Sprachdateien zu einer `Lang.cs` zusammengelegt; reine Aufräumarbeit, Oberfläche und Texte unverändert
- **1.4.1** — Behoben: Die Sprachliste verlor beim Wechsel nach oder aus Arabisch den Fokus, wodurch die Tastaturumschaltung still abbrach; der Fokus kehrt jetzt zurück
- **1.4** — Neues Pixel-Icon: 8 Größen von 16 bis 256 in die exe eingebettet, gleich in Titelleiste, Taskleiste und Explorer
- **1.3** — JSON-Ausgabe und JSON-Reiter entfernt; nur noch Textbericht
- **1.2** — Cloud-Prüfung entfernt (URL-Vorlage + „im Browser öffnen“), nur noch lokale Erfassung; `Laocoon.ini` speichert nur die Sprache; README in 12 Sprachen
- **1.1** — Oberfläche und Bericht in 12 Sprachen; eigene RTL-Behandlung für Arabisch
- **1.0** — erste Version: Anvisieren, Erfassen, Bericht

## Selbst kompilieren

Benötigt das .NET SDK (8 oder neuer). Doppelklick auf `build.bat`, oder:

```bat
dotnet build src\Laocoon.csproj -c Release
```

Das Ergebnis liegt in `src\bin\Release\net48\Laocoon.exe`.

> `build.bat` setzt `TEMP` absichtlich auf einen lokalen Ordner `.tmp`: auf manchen Rechnern ist das System-Temp-Verzeichnis nicht beschreibbar und der Build scheitert sofort.

## Quellcode

- `src/Laocoon.cs` — das Programm, rund 2225 Zeilen, ausschließlich Win32-P/Invoke, kein einziges NuGet-Paket
- `src/Lang.cs` — Sprachkern (Enum, Erkennung, Schrift, Breite, Nachschlagen, Rückfall) und alle zwölf Tabellen in einer Datei, rund 2250 Zeilen
- `src/Laocoon.ico` — das Pixel-Icon selbst (8 Größen, 16–256), erzeugt aus einem 16×16-Raster von `tools/make_icon.py`

Jede Tabelle hat 124 Schlüssel, alle in die exe kompiliert. Keine `.resources`, keine Satellite Assemblies — auf jedem Win10/11-Rechner läuft es sofort, ohne „Sprachpaket fehlt, Rückfall auf Englisch“.

Zentrale Einstiegspunkte:
- `Probe.HitTest` — läuft die Fenster oberster Ebene selbst in z-Reihenfolge ab (nicht `WindowFromPoint`, damit es nie die eigene Überlagerung trifft)
- `Probe.DeepestAt` — steigt mit `ChildWindowFromPointEx` bis zum innersten Steuerelement hinab
- `Probe.Collect` — sammelt Fenster / Prozess / Datei / Signatur / Module / Geschwisterfenster / Steuerelementbaum
- `FrameForm` / `HudForm` — der transparente blaue Rahmen und der mitlaufende HUD beim Anvisieren (beide `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, sie fangen keine Mausereignisse)
- `L.T` / `L.F` — Textnachschlag und Formatierung mit Platzhaltern; ein fehlender Schlüssel fällt auf Englisch zurück statt leer zu bleiben

## Diagnoseschalter (zum Arbeiten am Code)

```
Laocoon.exe --selftest out.txt     Konsolenfenster erfassen, Bericht drucken und in Datei schreiben
Laocoon.exe --aimtest              Selbsttest für Trefferprüfung und Zielüberlagerung
Laocoon.exe --i18ncheck            die 12 Sprachen prüfen: Schlüssel, Spaltenbreite, Schriften, Richtung
Laocoon.exe --langswitch           alle Sprachen durchschalten, Oberfläche und Bericht vergleichen
Laocoon.exe --focustest f.log [shot.png]  alle 12 Sprachen durchschalten und prüfen, ob die Liste den Fokus behält
Laocoon.exe --biditest out.png     Vergleich von 8 Bidi-Layouts (Arabisch + lateinische Daten)
Laocoon.exe --lang=ru              in einer bestimmten Sprache starten (schreibt nicht in die ini)
Laocoon.exe --uishot out.png --lang=ar            Oberfläche als PNG rendern
Laocoon.exe --uishot out.png --lang=ar --welcome  nur die Begrüßungsseite
Laocoon.exe --uishot out.png --lang=ar --screen   Bildschirm abgreifen (DrawToBitmap malt in RTL-Feldern eine falsche Auswahl)
```

Zusammen mit `--lang=` rendert `--uishot` alle zwölf Oberflächen in einem Durchgang zur Sichtprüfung.
