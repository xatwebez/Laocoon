# Laocoön · Sonda finestre

[中文](../README.md) · [English](README.en.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · **Italiano**

> Davanti a Troia tutti volevano trascinare quel cavallo di legno dentro la città. Solo Laocoonte si rifiutò.
> Confisse la lancia nel ventre — e sentì armi cozzare all'interno.
>
> Questo strumento fa la stessa cosa: **punge una finestra e ascolta cosa nasconde.**

Vedi una finestra che non riconosci — magari qualcosa di indesiderato. Premi `F2`, porta il mirino sopra, clicca. Tira fuori tutto ciò che si può estrarre da quella finestra e dal processo che la sostiene.

- Un solo file `Laocoon.exe`, circa **199 KB**
- Zero dipendenze di terze parti: solo il .NET Framework 4.x fornito con Windows (Win10 / Win11, niente da installare)
- Nessuna rete, nessun upload, nessuna scrittura nel registro, nessun agente in background (un solo `Laocoon.ini` accanto all'exe, giusto per ricordare la lingua)
- Dodici lingue integrate: **中文 / English / Русский / Français / Español / العربية / Deutsch / Português / 日本語 / 한국어 / हिन्दी / Italiano**

> Il nome del file è `Laocoon.exe`, in ASCII; il titolo della finestra conserva la dieresi — `Laocoön`.
> Così percorsi, righe di comando, allegati e CI non inciampano mai in caratteri non ASCII.

---

## Uso

1. Doppio clic su `Laocoon.exe` (per il massimo dei dettagli: **clic destro → Esegui come amministratore**)
2. Premi `F2`: il cursore diventa un mirino
3. Passa sulla finestra bersaglio: un riquadro blu la circonda e un piccolo HUD nero fluttua accanto, mostrando in tempo reale classe / titolo / PID / finestra padre
4. **Clic sinistro** per agganciare e acquisire; **clic destro** o **Esc** per annullare
5. Il rapporto completo compare subito: finestra, processo, file, firma, finestre nascoste e controlli figli, tutto in un colpo d'occhio

L'acquisizione è una tantum: niente si aggiorna a intervalli, niente resta in background a osservarti. Chiudi la finestra e non resta nulla — tranne il file che hai scritto tu con «Salva rapporto».

## Lingue dell'interfaccia

**12 lingue** integrate, cambiabili in qualsiasi momento dall'elenco in basso a destra, **senza riavviare**:

| Codice | Lingua | Titolo della finestra |
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

Perché proprio queste dodici: le lingue dei cinque membri permanenti del Consiglio di sicurezza dell'ONU come base, più **arabo** (l'unica lingua di lavoro ONU che manca in quella lista), **tedesco / portoghese / giapponese / coreano** (grandi economie ed ecosistemi software), **hindi** (il maggior mercato monolingue per popolazione) e **italiano** (cultura europea). Turco, indonesiano, vietnamita e polacco non ci sono — il punto di estensione resta aperto: aggiungere una lingua significa aggiungere una tabella in `Lang.cs`.

Al primo avvio la lingua viene decisa in quest'ordine, e la scelta finisce in `lang=` del file `Laocoon.ini`:

```
--lang=xx sulla riga di comando  >  lang=xx in Laocoon.ini  >  lingua di sistema  >  inglese
```

Quindi un sistema cinese parte in cinese; sistemi russo / francese / spagnolo / arabo / tedesco / portoghese / giapponese / coreano / hindi / italiano mostrano la propria; tutto il resto ricade sull'inglese (varianti regionali come `pt-BR`, `zh-Hant`, `ar-EG` sono riconosciute). Per fissare la lingua, scrivi `lang=ru` in `Laocoon.ini`.

Non viene tradotta solo l'interfaccia: anche **il corpo del rapporto** — etichette, titoli di sezione e avvisi euristici («l'eseguibile si trova nella cartella temporanea», «il processo ha N altre finestre di primo livello nascoste») sono generati nella lingua corrente.

### Come viene gestito l'arabo (da destra a sinistra)

L'arabo non è «sostituire la tabella delle stringhe». Nei test sono emerse tre trappole:

1. **Interfaccia specchiata**: l'intera finestra viene ribaltata con `RightToLeftLayout`, la barra degli strumenti va da destra a sinistra e il pulsante principale finisce all'estrema destra.
2. **Il rapporto non viene specchiato**: è pieno di percorsi, hash e nomi di classe. Lo specchiamento inverte inizio e fine di ogni riga, quindi la casella del rapporto resta sempre da sinistra a destra. Solo la **pagina di benvenuto** (testo corrente) è allineata a destra in arabo.
3. **I due punti saltano a sinistra dell'etichetta**: il motore BiDi di GDI considera i due punti di «etichetta araba + `:` + valore latino» parte del segmento RTL di destra e disegna `0 :المقبضx0001048E`. La soluzione è un LRM a larghezza zero (U+200E) su entrambi i lati dei due punti per forzare la direzione, con lo stesso isolamento sugli argomenti di formattazione (date, PID, nomi di classe) — altrimenti una data diventa `04:28:37 08-11-2026`. L'esperimento comparativo con tutte le varianti resta nella diagnostica `--biditest`, così si può rieseguire in caso di riscrittura.

Il **devanagari** (hindi) non ha larghezza fissa più dell'arabo: «riempire di spazi per allineare» peggiora solo le cose. Per questo quelle due lingue passano il rapporto al formato `etichetta: valore` — meglio senza allineamento che con un allineamento finto.
Arabo, hindi, giapponese e coreano usano ciascuno il font fornito da Windows (Segoe UI / Nirmala UI / Yu Gothic UI / Malgun Gothic); se manca, il ripiego è automatico, senza quadrati né eccezioni.

## Cosa viene raccolto

| Categoria | Contenuto |
|---|---|
| Finestra | Handle, classe, titolo, bit di stile (decodificati in nomi `WS_*` / `WS_EX_*`), rettangolo, area client, visibile / minimizzata, **sempre in primo piano**, **finestra strumento**, **nascosta da DWM**, finestra proprietaria |
| Controllo colpito | Fino al controllo figlio esatto: classe, ID, testo, percorso dalla finestra radice |
| Processo | PID / TID, percorso completo, **riga di comando**, padre (nome + PID), architettura (x86 / x64 / WOW64), **elevazione**, sessione, ora di avvio |
| File | Dimensione, creazione / modifica, **MD5 / SHA1 / SHA256**, azienda, prodotto, descrizione, versione, copyright |
| Firma | Presenza di firma incorporata, firmatario, emittente, validità, esito della catena |
| Moduli | Percorsi completi di tutte le DLL caricate dal processo |
| Finestre del processo | **Tutte** le finestre di primo livello di quel processo, comprese le nascoste (marcate `[nascosta]`) — la finestra di sfondo di un programma indesiderato non si nasconde qui |
| Albero dei controlli | L'intero albero dei controlli figli della finestra bersaglio: rientro, dimensioni, ID, testo |
| Indizi di sospetto | Euristiche: si trova in Temp / Download / AppData, non firmato, senza nome azienda, in primo piano, numero di finestre nascoste, file creato negli ultimi 7 giorni e così via (**indizi, non verdetti**) |

L'intero rapporto è testo semplice: colonne allineate, sezioni separate; puoi selezionarlo e copiarlo, oppure usare «Salva rapporto» per scrivere un `.txt` in UTF-8. Per controllare un file più a fondo, copia lo `sha256` nel servizio che preferisci: questo strumento acquisisce solo in locale, non si collega e non carica nulla.

## Limiti noti

- **Firma digitale**: viene verificata solo la firma incorporata nel PE. I file di Windows (come `notepad.exe`) sono firmati tramite catalogo, quindi qui appare «nessuna firma incorporata» — è normale.
- Senza diritti di amministratore, i processi elevati o protetti non forniscono percorso / moduli / riga di comando (appare «accesso negato»); gli altri campi funzionano comunque.
- `WM_GETTEXT` per il testo dei controlli applica un timeout di 60 ms sulle finestre che non rispondono (`SMTO_ABORTIFHUNG`) per non congelare l'interfaccia — per questo su rare finestre bloccate il testo manca.
- L'albero dei controlli si ferma a 240 elementi e l'elenco dei moduli a 400; l'eccedenza è segnalata come troncata.
- L'hash si ferma a 512 MB, oltre viene saltato (per non aspettare all'infinito).
- La lingua segue il sistema; fuori dalle dodici si usa l'inglese. Le traduzioni sono scritte a mano e i termini tipici di Windows (`WS_*` / `WS_EX_*`, `DWM Cloaked`) restano volutamente in inglese, così ogni lingua non si inventa un vocabolario proprio.
- L'interfaccia araba è specchiata, ma **il rapporto resta sempre da sinistra a destra** — sono dati tecnici, specchiati diventano illeggibili. È una scelta, non una traduzione a metà.

## Cronologia versioni

- **1.4.2** — I tre file delle lingue riuniti in un unico `Lang.cs`; solo riordino del codice, interfaccia e testi invariati
- **1.4.1** — Corretto: l'elenco delle lingue perdeva il focus passando all'arabo o tornando indietro, interrompendo il cambio da tastiera senza avvisi; ora il focus torna sull'elenco
- **1.4** — Nuova icona pixel art: 8 dimensioni da 16 a 256 incorporate nell'exe, identiche nella barra del titolo, nella barra delle applicazioni e in Esplora file
- **1.3** — rimossi l'uscita JSON e la scheda JSON; solo rapporto testuale
- **1.2** — rimossa la verifica nel cloud (modello di URL + «apri nel browser»); solo acquisizione locale; `Laocoon.ini` conserva solo la lingua; README in 12 lingue
- **1.1** — interfaccia e rapporto in 12 lingue; gestione RTL dedicata all'arabo
- **1.0** — prima versione: mira, acquisizione, rapporto

## Compilare da soli

Serve l'SDK .NET (8 o superiore). Doppio clic su `build.bat`, oppure:

```bat
dotnet build src\Laocoon.csproj -c Release
```

Il risultato è in `src\bin\Release\net48\Laocoon.exe`.

> `build.bat` punta `TEMP` a una cartella locale `.tmp` di proposito: su alcune macchine la cartella temporanea di sistema non è scrivibile e la build fallisce subito.

## Sorgenti

- `src/Laocoon.cs` — il programma, circa 2225 righe, solo P/Invoke Win32, nessun pacchetto NuGet
- `src/Lang.cs` — nucleo delle lingue (enumerazione, rilevamento, font, larghezza, ricerca, ripiego) e le dodici tabelle in un unico file, circa 2250 righe
- `src/Laocoon.ico` — l'icona pixel art stessa (8 dimensioni, da 16 a 256), generata da una griglia 16×16 con `tools/make_icon.py`

Ogni tabella ha 124 chiavi, tutte compilate nell'exe. Niente `.resources`, niente satellite assembly: su qualsiasi macchina Win10/11 funziona subito, senza «pacchetto lingua mancante, ripiego sull'inglese».

Punti di ingresso principali:
- `Probe.HitTest` — percorre da sé le finestre di primo livello in ordine z (non `WindowFromPoint`, per non colpire mai il proprio overlay)
- `Probe.DeepestAt` — scende con `ChildWindowFromPointEx` fino al controllo più interno
- `Probe.Collect` — raccoglie finestra / processo / file / firma / moduli / finestre sorelle / albero dei controlli
- `FrameForm` / `HudForm` — il riquadro blu trasparente e l'HUD che segue durante la mira (entrambi `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, non catturano il mouse)
- `L.T` / `L.F` — ricerca della stringa e formattazione con segnaposto; una chiave mancante ricade sull'inglese invece di restare vuota

## Opzioni di diagnostica (per lavorare sul codice)

```
Laocoon.exe --selftest out.txt     acquisire la finestra della console, stampare il rapporto e scriverlo su file
Laocoon.exe --aimtest              autotest di hit-testing e dell'overlay di mira
Laocoon.exe --i18ncheck            controllare le 12 lingue: chiavi, larghezza colonne, font, direzione
Laocoon.exe --langswitch           scorrere tutte le lingue e verificare interfaccia e rapporto
Laocoon.exe --focustest f.log [shot.png]  scorrere tutte e 12 le lingue e verificare che l'elenco mantenga il focus
Laocoon.exe --biditest out.png     confronto di 8 impaginazioni bidi (arabo + dati latini)
Laocoon.exe --lang=ru              avviare in una lingua specifica (non scrive nell'ini)
Laocoon.exe --uishot out.png --lang=ar            rendere l'interfaccia in PNG
Laocoon.exe --uishot out.png --lang=ar --welcome  solo la pagina di benvenuto
Laocoon.exe --uishot out.png --lang=ar --screen   catturare lo schermo (DrawToBitmap simula una selezione in RTL)
```

Insieme a `--lang=`, `--uishot` rende tutte e dodici le interfacce in un colpo per un controllo visivo.
