# Laocoön · Sonda de ventanas

[中文](../README.md) · [English](README.en.md) · [Русский](README.ru.md) · [Français](README.fr.md) · **Español** · [العربية](README.ar.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · [Italiano](README.it.md)

> A las puertas de Troya todos querían meter ese caballo de madera en la ciudad. Solo Laocoonte se negó.
> Hundió su lanza en el vientre — y oyó armas chocando dentro.
>
> Esta herramienta hace lo mismo: **pincha una ventana y escucha lo que esconde.**

<p align="center">
  <img src="img/laocoon.gif" width="640" alt="Laocoonte llega desde el lado de la cabeza del caballo y clava su lanza en el vientre; de la brecha saltan guerreros armados, una etiqueta los identifica y él retrocede con la lanza en alto">
</p>

Ves una ventana que no reconoces — puede que algo indeseable. Pulsa `F2`, lleva la mira encima y haz clic. Sacará todo lo que se pueda extraer de esa ventana y del proceso que la sostiene.

- Un solo archivo `Laocoon.exe`, unos **208 KB**
- Cero dependencias de terceros: solo el .NET Framework 4.x que viene con Windows (Win10 / Win11, nada que instalar)
- Sin red, sin subidas, sin escribir en el registro, sin ningún archivo de configuración, sin agente en segundo plano (se descomprime, se ejecuta y no queda nada en el disco)
- Doce idiomas integrados: **中文 / English / Русский / Français / Español / العربية / Deutsch / Português / 日本語 / 한국어 / हिन्दी / Italiano**

> El nombre del archivo es `Laocoon.exe` en ASCII; el título de la ventana conserva la diéresis — `Laocoön`.
> Así las rutas, las líneas de comandos, los adjuntos de correo y la CI nunca tropiezan con caracteres no ASCII.

---

## Uso

<p align="center">
  <img src="img/detect.gif" width="640" alt="Tras pulsar F2 el retículo se fija en una ventana; sube un informe y una de las tres ventanas del proceso está oculta">
</p>

1. Doble clic en `Laocoon.exe` (para obtener el máximo detalle: **clic derecho → Ejecutar como administrador**)
2. Pulsa `F2`: el cursor se vuelve una cruz
3. Pasa sobre la ventana objetivo: un marco azul la rodea y un pequeño HUD negro flota al lado mostrando en vivo clase / título / PID / ventana padre
4. **Clic izquierdo** para fijar y capturar; **clic derecho** o **Esc** para cancelar
5. El informe completo aparece al instante: ventana, proceso, archivo, firma, ventanas ocultas y controles hijos, todo en un mismo sitio

La captura es única: nada se refresca por temporizador, nada se queda en segundo plano vigilándote. Cierra la ventana y no queda nada, salvo el archivo que hayas guardado tú con «Guardar informe».

## Idiomas de la interfaz

**12 idiomas** integrados; se cambian en cualquier momento desde la lista de abajo a la derecha, **sin reiniciar**:

| Código | Idioma | Título de la ventana |
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

Por qué estos doce: los idiomas de los cinco miembros permanentes del Consejo de Seguridad de la ONU como base, más el **árabe** (el único idioma de trabajo de la ONU que falta en esa lista), **alemán / portugués / japonés / coreano** (grandes economías y ecosistemas de software), **hindi** (el mayor mercado monolingüe por población) e **italiano** (cultura europea). Turco, indonesio, vietnamita y polaco no están: el punto de extensión queda abierto, añadir uno es añadir una tabla en `Lang.cs`.

En el primer arranque el idioma se decide en este orden:

```
--lang=xx en la línea de comandos  >  idioma del sistema  >  inglés
```

Es decir, un sistema chino arranca en chino, y los sistemas ruso / francés / español / árabe / alemán / portugués / japonés / coreano / hindi / italiano muestran el suyo; el resto cae en inglés (también se reconocen variantes regionales como `pt-BR`, `zh-Hant`, `ar-EG`). El cambio solo vale para esta ejecución: no se guarda nada; para fijar el idioma, arranca `Laocoon.exe --lang=ru` (o crea un acceso directo con ese argumento).

No solo se traduce la interfaz: **el cuerpo del informe** también — etiquetas, títulos de sección y avisos heurísticos («el ejecutable está en la carpeta temporal», «el proceso tiene N ventanas de nivel superior ocultas») se generan en el idioma actual.

### Cómo se trata el árabe (derecha a izquierda)

El árabe no es «cambiar la tabla de cadenas». En las pruebas aparecieron tres trampas:

1. **Interfaz en espejo**: toda la ventana se voltea con `RightToLeftLayout`, la barra va de derecha a izquierda y el botón principal queda en el extremo derecho.
2. **El informe no se refleja**: está lleno de rutas, hashes y nombres de clase. Reflejarlo invierte el principio y el final de cada línea, así que la caja del informe se queda siempre de izquierda a derecha. Solo la **página de bienvenida** (texto corrido) se alinea a la derecha en árabe.
3. **Los dos puntos saltan a la izquierda de su etiqueta**: el motor BiDi de GDI considera que los dos puntos de «etiqueta árabe + `:` + valor latino» pertenecen al tramo RTL de la derecha y dibuja `0 :المقبضx0001048E`. La solución es un LRM de ancho cero (U+200E) a ambos lados de los dos puntos para forzar la dirección, con el mismo aislamiento en los argumentos de formato (fechas, PID, nombres de clase); si no, una fecha sale como `04:28:37 08-11-2026`. El experimento comparativo con todas las variantes queda en el diagnóstico `--biditest`, para poder repetirlo si algún día se reescribe.

El **devanagari** (hindi) tampoco tiene ancho fijo, igual que el árabe: «rellenar con espacios para alinear» solo lo empeora. Por eso esos dos idiomas pasan el informe al formato `etiqueta: valor`: mejor sin alinear que fingiendo alineación.
Árabe, hindi, japonés y coreano usan cada uno la fuente que trae Windows (Segoe UI / Nirmala UI / Yu Gothic UI / Malgun Gothic); si falta, hay repliegue automático, sin cuadros ni excepciones.

## Qué se recoge

| Categoría | Contenido |
|---|---|
| Ventana | Handle, clase, título, bits de estilo (decodificados a nombres `WS_*` / `WS_EX_*`), rectángulo, área cliente, visible / minimizada, **siempre visible**, **ventana de herramienta**, **oculta por DWM**, ventana propietaria |
| Control alcanzado | Hasta el control hijo exacto: clase, ID, texto, ruta desde la ventana raíz |
| Proceso | PID / TID, ruta completa, **línea de comandos**, padre (nombre + PID), arquitectura (x86 / x64 / WOW64), **elevación**, sesión, hora de inicio |
| Archivo | Tamaño, creación / modificación, **MD5 / SHA1 / SHA256**, compañía, producto, descripción, versión, copyright |
| Firma | Si hay firma incrustada, firmante, emisor, validez, resultado de la cadena |
| Módulos | Rutas completas de todas las DLL cargadas por el proceso |
| Ventanas del proceso | **Todas** las ventanas de nivel superior de ese proceso, incluidas las ocultas (marcadas `[oculta]`): la ventana de fondo de un programa indeseable no se esconde aquí |
| Árbol de controles | Todo el árbol de controles hijos de la ventana objetivo: sangría, tamaño, ID, texto |
| Indicios de sospecha | Heurísticas: en Temp / Descargas / AppData, sin firma, sin nombre de compañía, siempre visible, número de ventanas ocultas, archivo creado en los últimos 7 días, etc. (**indicios, no un veredicto**) |

Todo el informe es texto plano: columnas alineadas y secciones separadas; puedes seleccionarlo y copiarlo, o usar «Guardar informe» para escribir un `.txt` en UTF-8. Para comprobar un archivo más a fondo, copia el `sha256` a cualquier servicio de tu confianza: esta herramienta solo captura en local, no se conecta ni sube nada.

## Limitaciones conocidas

- **Firma digital**: solo se comprueba la firma incrustada en el PE. Los archivos de Windows (por ejemplo `notepad.exe`) van firmados por catálogo, así que aquí aparece «sin firma incrustada»; es lo normal.
- Sin permisos de administrador, en procesos elevados o protegidos no se leen ruta / módulos / línea de comandos (aparece «permiso denegado»); el resto de campos sigue funcionando.
- `WM_GETTEXT` para el texto de controles aplica un tiempo límite de 60 ms en ventanas que no responden (`SMTO_ABORTIFHUNG`) para no congelar la interfaz; por eso, en contadas ventanas colgadas no hay texto.
- El árbol de controles se corta en 240 elementos y la lista de módulos en 400; lo que sobra se marca como truncado.
- El cálculo de hash se detiene a los 512 MB y por encima se omite (para no esperar una eternidad).
- El idioma sigue al sistema; fuera de los doce, se usa inglés. Las traducciones están escritas a mano, y términos propios de Windows (`WS_*` / `WS_EX_*`, `DWM Cloaked`) se dejan a propósito en inglés para que cada idioma no invente su propio vocabulario.
- La interfaz árabe va en espejo, pero **el informe siempre de izquierda a derecha**: son datos técnicos y reflejarlos los vuelve ilegibles. Es deliberado, no una traducción a medias.

## Historial de versiones

- **1.4.4** — Corregido un cierre inesperado ocasional al cambiar de idioma rápido manteniendo una flecha: la recreación de los identificadores de ventana ya no ocurre dentro del manejo de mensajes del desplegable, y el espejo RTL espera a que se suelten las teclas
- **1.4.3** — Ya no hay `Laocoon.ini`: el idioma sigue al sistema, el cambio manual solo vale para esta ejecución y el programa no deja ningún archivo
- **1.4.2** — Los tres archivos de idiomas unidos en un solo `Lang.cs`; puesta en orden, sin cambios de interfaz ni de textos
- **1.4.1** — Corregido: el selector de idioma perdía el foco al pasar al árabe o al volver, y el cambio por teclado se cortaba sin avisar; ahora el foco vuelve al selector
- **1.4** — Nuevo icono pixel art: 8 tamaños de 16 a 256 incrustados en el exe, iguales en la barra de título, la barra de tareas y el explorador
- **1.3** — se eliminan la salida JSON y la pestaña JSON; solo informe de texto
- **1.2** — se elimina la verificación en la nube (plantilla de URL + «abrir en el navegador»); solo captura local; `Laocoon.ini` guarda únicamente el idioma; README en 12 idiomas
- **1.1** — interfaz e informe en 12 idiomas; tratamiento RTL específico para árabe
- **1.0** — primera versión: apuntar, capturar, informe

## Compilar uno mismo

Hace falta el SDK de .NET (8 o superior). Doble clic en `build.bat`, o bien:

```bat
dotnet build src\Laocoon.csproj -c Release
```

El resultado queda en `src\bin\Release\net48\Laocoon.exe`.

> `build.bat` apunta `TEMP` a una carpeta local `.tmp` a propósito: en algunas máquinas el directorio temporal del sistema no admite escritura y la compilación falla de inmediato.

## Código fuente

- `src/Laocoon.cs` — el programa, unas 2225 líneas, todo P/Invoke de Win32, sin ningún paquete NuGet
- `src/Lang.cs` — núcleo de idiomas (enumeración, detección, fuente, ancho, búsqueda, repliegue) y las doce tablas en un solo archivo, unas 2250 líneas
- `src/Laocoon.ico` — el icono pixel art (8 tamaños, 16 a 256), generado desde una rejilla de 16×16 con `tools/make_icon.py`
- `docs/img/*.gif` — las dos animaciones pixel de este README, dibujadas fotograma a fotograma por `tools/make_gifs.py` (8 colores, rejilla 160×90, escalado entero)

Cada tabla tiene 124 claves, todas compiladas dentro del exe. Sin `.resources` ni satellite assemblies: en cualquier máquina Win10/11 funciona sin «falta el paquete de idioma, vuelvo al inglés».

Puntos de entrada principales:
- `Probe.HitTest` — recorre él mismo las ventanas de nivel superior en orden z (no usa `WindowFromPoint`, para no acertar en su propia capa)
- `Probe.DeepestAt` — baja con `ChildWindowFromPointEx` hasta el control más interno
- `Probe.Collect` — reúne ventana / proceso / archivo / firma / módulos / ventanas hermanas / árbol de controles
- `FrameForm` / `HudForm` — el marco azul transparente y el HUD que sigue al cursor al apuntar (ambos `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, no capturan el ratón)
- `L.T` / `L.F` — búsqueda de cadenas y formato con marcadores; una clave ausente cae en inglés en lugar de quedarse en blanco

## Opciones de diagnóstico (para trastear con el código)

```
Laocoon.exe --selftest out.txt     capturar la ventana de consola, imprimir el informe y escribirlo en un archivo
Laocoon.exe --aimtest              autoprueba del hit-testing y de la capa de puntería
Laocoon.exe --i18ncheck            revisar los 12 idiomas: claves, ancho de columna, fuentes, dirección
Laocoon.exe --langswitch           recorrer todos los idiomas y comprobar interfaz e informe
Laocoon.exe --focustest f.log [shot.png]  recorrer los 12 idiomas y comprobar que el selector conserva el foco
Laocoon.exe --langstress 300 [log] [--rtl] simula la repetición de flechas: recreación de handles y recursos
Laocoon.exe --biditest out.png     comparativa de 8 maquetaciones bidi (árabe + datos latinos)
Laocoon.exe --lang=ru              arrancar en un idioma dado (no se escribe en el ini)
Laocoon.exe --uishot out.png --lang=ar            renderizar la interfaz a PNG
Laocoon.exe --uishot out.png --lang=ar --welcome  solo la página de bienvenida
Laocoon.exe --uishot out.png --lang=ar --screen   capturar la pantalla (DrawToBitmap simula una selección en RTL)
```

Combinado con `--lang=`, `--uishot` renderiza las doce interfaces de una vez para revisarlas a ojo.
