# SOLARIS · GAIA — guía de estilos

Guía de diseño para quien vaya a tocar las pantallas: tipo de letra, colores, piezas y distribución.
Verificada contra las hojas de estilo (`wwwroot/css`) el 02/10/2026. Si algo de aquí choca con el código, manda el código y
hay que corregir este documento.

> **CDM Auditorías Calidad** (05/10/2026): las secciones 1 a 8 son la guía de SOLARIS · GAIA tal cual. Cómo se aplica en
> esta web (una sola hoja, `site.css`), qué se le añadió y las piezas de **CDM No solución** están en la **sección 9**.

**Reglas de oro**
1. Antes de crear un estilo, usar un **token** y una **pieza** que ya existan.
2. **Nada de colores escritos a mano** en las vistas: todo color sale de una variable CSS.
3. **Sin librerías externas**: la red corporativa bloquea los CDN (cdnjs, Google Fonts). Gráficas en SVG generado en el servidor
   y JavaScript propio.
4. Todo en **español**: clases, variables, comentarios, textos.
5. **Sin emojis ni iconos «de IA»**, sin degradados de colores ni sombras de colores.
6. **No quitar funcionalidad** al rediseñar.
7. Confirmar el color con la imagen, no solo con el código.

---

## 1. Tipo de letra

| Uso | Fuente |
|---|---|
| Texto, tablas, botones | **Segoe UI Variable Text** (`--font-ui`), de reserva Segoe UI, Helvetica Neue, Arial |
| Título de página y cifras grandes | **Segoe UI Variable Display** (`--font-titulo`) |
| Logotipo «yoigo» (portada y menú) | Century Gothic (viene con Office), de reserva Arial Rounded |

- Es la letra de Windows 11: **no se carga nada** (ni `@font-face` ni Google Fonts). En Windows 10 cae a Segoe UI.
- Las define `movimiento.css`. Las hojas de cada pantalla declaran la versión sin «Variable» por si falta esa capa.
- **Display** solo en el título de la cabecera y en las cifras grandes (tarjetas de indicador, medidor, anillos).
- Cuerpo a **14 px**. Titulares con `text-wrap: balance`, párrafos con `pretty`.
- **Negrita controlada**: 600 para titulares y cifras, 500 para nombres en tablas. Nada de 700/800 por todas partes.
- Cifras tabulares (`tabular-nums`) en tablas, tarjetas, anillos y medidor.
- Cifras en cultura **es-ES**: dos decimales y `%` separado por un espacio fino («41,58 %»). En las celdas que se ordenan va el
  valor crudo en `data-valor`.

## 2. Colores

### 2.1 Por marca
La marca se marca en `<html data-marca="…">` y cambia el acento de toda la página.

| Marca | Acento | Oscuro del acento | Tema oscuro | Texto sobre el acento | Cabecera de tabla |
|---|---|---|---|---|---|
| **Yoigo · MásMóvil** | `#7b38c9` (morado) | `#5f27a3` | `#b48af0` | blanco | `#efe6fa` con tinta `#4a1f7d` |
| **Orange** | `#f16e00` (naranja) | `#cc5e00` | `#ff8a1f` | negro `#141414` | `#ffe3cc` con tinta `#7a3a00` |
| **Clanes y Formación** | `#f16e00` | enlaces `#cc5e00` | naranja aclarado | `#121316` | — |
| **Portada, menús y administración** | marino `#1f2a44` (hover `#2c3a5c`) | acento suave `#6b74b8` | botones `#e8eaf2` sobre `#1b2236` | — | — |
| **Acceso de SOLARIS (login)** | naranja `#f16e00` a ascua `#e2570a`, fondo `#fbf6f0` | tinta `#111a24` | noche cálida `#120d0a` | `#1b0d03` | — |

- **El texto que va encima del acento sale siempre de `--acento-texto`**: nunca se escribe a mano (blanco en morado, negro en
  naranja).
- En Yoigo, la serie «morada» de las gráficas pasa al ámbar `#c99400` para no confundirse con el acento.
- Los menús y la portada van en marino y gris: el color de marca solo queda en los logotipos.

### 2.2 Fijos en todas las marcas
| Qué | Colores |
|---|---|
| **Series de gráficas** (Orange Boosted) | naranja `#f16e00`, azul `#4170d8`, verde `#228722`, rojo `#cd3c14`, lila `#a885d8` |
| **Bueno / malo** | verde `#228722` / rojo `#cd3c14` |
| **Semáforo pastel** (como el formato condicional de Excel) | verde `#c6efce`, ámbar `#ffeb9c`, rojo `#ffc7ce`, tinta `#141414`. En oscuro: `#2f5d3a`, `#5e5325`, `#5f2f37` con tinta `#f5f5f5` |
| **Sentimientos** | positivo `#2a9d8f`, neutro `#8d99ae`, mixto `#e9b44c`, negativo `#d1495b` |
| **Clanes: modelo ABCZ** | A `#F57E1B`, B `#00B0F0`, C `#00B050`, Z `#7030A0` |
| **Fondo / texto de la portada (claro)** | fondo `#f5f6f9`, tarjeta `#ffffff`, borde `#e4e7ee`, texto `#1b2236`, suave `#5f6780`, tenue `#9aa1b3` |
| **Fondo / texto de la portada (oscuro)** | fondo `#12121a`, tarjeta `#1c1c26`, borde `#2f2f3d`, texto `#f4f4f8` |

### 2.3 Tema claro / oscuro
- `tema.js` pone `data-tema="claro|oscuro"` en `<html>` (automático, claro u oscuro; se guarda en el navegador).
- Todo color es una variable de `:root` redefinida bajo `[data-tema="oscuro"]`.
- Base oscura en gris muy oscuro (no negro puro). **La elevación va con bordes, no con sombras.**
- Cuando algo «no se ve» en oscuro, casi siempre es un color escrito a mano.

## 3. Variables (tokens)

Hay dos juegos con el mismo significado, porque las hojas nacieron por separado:

| Qué | Pantallas de voz (`reporte.css`) | Informes (`informe.css`) |
|---|---|---|
| Acento | `--acento`, `--acento-osc` | `--acento`, `--acento-osc` |
| Texto sobre el acento | `--acento-texto` | `--acento-texto` |
| Texto | `--texto`, `--texto-suave`, `--texto-tenue` | `--text-primary`, `--text-secondary`, `--text-muted` |
| Superficie, fondo, borde | `--superficie`, `--fondo`, `--borde` | `--surface-1`, `--page`, `--border` |
| Cabecera de tabla | `--thead`, `--thead-texto` | `--thead-bg`, `--thead-texto` |
| Tarjeta de indicador | `--tile-borde`, `--tile-realce`, `--tile-filo` | los mismos |
| Semáforo | `--mapa-verde`, `--mapa-ambar`, `--mapa-rojo` | `--rampa-0` … `--rampa-6` |
| Cabecera (cromo) | `--cab-fondo`, `--cab-texto`, `--cab-borde` | los mismos |
| Forma | `--radio` 16 px, `--radio-sm` 8 px, `--alto-cabecera` 72 px | `--alto-cabecera` 72 px |

`movimiento.css` los une con `--mov-*` (`--mov-acento`, `--mov-texto`, `--mov-superficie`, `--mov-borde`…): lo que sea común a
los dos (asistente, animaciones) usa solo esos.

## 4. Distribución de una página

**Hojas de estilo**

| Hoja | Para qué |
|---|---|
| `reporte.css` | informes de voz (Yoigo y Orange) |
| `informe.css` | WhatsApp, Siebel y Clanes |
| `portal.css` | menús de cada marca |
| `login.css` | portada de marcas, login, «Sin acceso» y administración |
| `movimiento.css` / `.js` | capa común: letra, animaciones, impresión |
| `asistente.css` | el asistente |
| `accesos.css` | login, menú del usuario, candados |

**Estructura (la misma en todos los informes)**
1. **Cabecera** pegajosa de **72 px**, con una línea de 3 px del color de marca debajo. Tres partes: el logotipo a la izquierda
   (vuelve al menú de su marca), el título grande con su subtítulo en el centro, y a la derecha el botón de tema, el menú del
   usuario y «Salir».
2. **Pestañas** (solo en los informes de voz): una pastilla por página, la activa resaltada.
3. **Cuerpo `.tablero`**: ancho de **1.120 a 1.720 px**, centrado. A la izquierda el **panel de filtros** (268 px; en Siebel
   330 px), a la derecha el contenido. Por debajo de 1.120 px hay scroll horizontal en vez de reordenar.
4. **Un solo punto de ruptura**, a **760 px** (móvil): el panel sube y todo se apila.
5. **No** usar `auto-fit` en las tiras de indicadores ni añadir más breakpoints: la composición es fija a propósito.

**Panel de filtros**: sin scroll interno, a la altura de la pantalla. Lo siempre visible (fechas, semana, super, team, agente,
marca, cola…) arriba; el resto, en «Más filtros». Un desplegable con menos de dos opciones **se oculta**. Los filtros son en
cascada: cada lista solo ofrece lo que existe con el resto de la selección.

**Portada y menús**: estilo ejecutivo, marino y grises, tarjetas grandes de marca en fila (3 en la portada, 4 en el menú de
Yoigo, 3 en el de Orange). Debajo de 1.100 px, una por fila.

## 5. Piezas que ya existen (usar antes de inventar)

| Pieza | Clase | Notas |
|---|---|---|
| Tira de indicadores | `.resumen` / `.resumen-dato` (voz), `.kpis` / `.tile` (informes) | tarjeta con radio 16 px, borde fino, filo de 3 px arriba y realce radial; la destacada es `.destacada` |
| Tarjeta KPI con icono | `.tile.kpi-m` | iconos de trazo en `Service/IconosKpi.cs` |
| Bloque | `.tarjeta` (voz), `.card` (informes) | radio 16 px |
| Pareja de columnas | `.rejilla-2` | siempre dos |
| Tabla que se ordena y colorea | `table.ranking[data-mapa]` | `th[data-sentido]` (1 más es mejor, −1 más es peor, 0 sin color) y `td[data-valor]`; los grupos con menos de 20 llamadas van al final y en gris; fila de totales sin color |
| Gráfica por tiempo | `.crono-tarjeta` con `crono-linea principal/secundaria`, área y pastillas | SVG del servidor |
| Barras horizontales | `.hbarras > .hbarra` | `.hbarra-media` en pareja |
| Árbol desplegable | `#tablaTipologico`, `tr.padre` / `tr.hija` | `ccvbpe.js` |
| Parciales | `_GraficaLineas`, `_Dispersion`, `_EvolucionOrange`, `_Tendencia`, `_PanelFiltros`, `_ErrorDatos` | |

**Gráficas**: se generan en el servidor como SVG. Para que salga la **ficha al pasar el ratón** basta con poner un `<title>` en
cada punto, barra o banda con el formato «Etiqueta · Serie valor · …». `graficas.js` hace el resto (ficha, guía vertical,
desvanecido). Los textos SVG se emiten como `HtmlString` (Razor reserva `<text>`).

## 6. Movimiento

- Tiempos: 160 ms (clic), 220 ms (hover), 440 ms (aparición de tarjetas), 780 ms (cifras que cuentan y barras que crecen), 45 ms
  entre tarjetas hermanas. Curva `cubic-bezier(.22, .61, .36, 1)`, **sin rebote**.
- Las barras crecen con `transform: scaleX`, nunca animando el ancho.
- Foco visible con teclado (2 px del acento).
- **Con «reducir movimiento» no se anima nada** (`prefers-reduced-motion`): hay que respetarlo en todo lo nuevo.
- Hay barra de carga naranja arriba al navegar o aplicar filtros.

## 7. SOLARIS (portada y acceso)

- La plataforma se llama **SOLARIS** y dentro están los informes de GAIA. El logotipo (`_MarcaSolaris`, SVG propio) va en la
  esquina de la portada, los menús, «Sin acceso» y la administración. Los informes siguen con su cabecera propia.
- **Login**: logotipo arriba a la izquierda, el sol a la izquierda y la tarjeta de acceso a la derecha (debajo en móvil). Fondo
  blanco cálido con chispas naranjas; en oscuro, noche cálida con un resplandor detrás del sol.
- El botón «Entrar» va en degradado naranja → ascua con texto negro.

## 8. Lo que NO hay que hacer
- Librerías de gráficas, fuentes o iconos desde un CDN.
- Colores, tamaños de letra o tipografías nuevas «porque queda bien».
- Envolver cifras de una tarjeta en `<span>` sin mirar las reglas genéricas de su contenedor (`.resumen-dato span` ya encogió
  las cifras una vez): las reglas nuevas van con `:is(...)` para ganarles.
- Unificar fórmulas entre páginas «porque deberían ser iguales»: cada informe define sus porcentajes a su manera.
- Poner `<style>` o `<script>` con lógica dentro de las vistas: las vistas solo llevan marcado.
- Olvidar el modo oscuro: probar siempre claro y oscuro, y a 375 px de ancho.

---

## 9. CDM Auditorías Calidad (esta web)

Cómo se aplica la guía en `C:\Proyectos\CDM Auditorias Calidad` (ASP.NET Core MVC, Razor). Tiene dos informes con el
mismo aspecto: **Auditorías** (General y Formación & Calidad, el Power BI «CDM Auditorías Calidad») y **CDM No solución**
(traído de ranking-mvc el 05/10/2026). Verificado contra `site.css` y `site.js` el 05/10/2026.

### 9.1 Hojas, scripts e iconos

| En SOLARIS | Aquí |
|---|---|
| `reporte.css`, `informe.css`, `portal.css`, `movimiento.css` | **una sola hoja**, `wwwroot/css/site.css`, con los nombres de variable de las pantallas de voz (`--acento`, `--texto`, `--superficie`, `--borde`, `--thead`…) |
| `graficas.js`, `movimiento.js` | `wwwroot/js/site.js`: fichas, guía vertical, tablas que se ordenan, cifras que cuentan y filtros |
| `tema.js` | igual (`wwwroot/js/tema.js`, en `<head>`); guarda la elección en `localStorage` con la clave `cdm-tema` |
| `Service/IconosKpi.cs` | `Infraestructura/Iconos.cs`: iconos de trazo y el logotipo (marino con el visto bueno naranja) |
| `_MarcaSolaris`, login, menú del usuario, «Salir» | no hay: la web no está dentro de SOLARIS ni tiene inicio de sesión. La cabecera lleva logotipo, título y botón de tema |

- Marca: los dos informes van con `data-marca="orange"` (por defecto); la portada y la página de error, con
  `data-marca="portada"` (marino y grises). Se elige con `ViewData["Marca"]`.
- Cifras: `Formato` (Auditorías, notas en fracción 0–1) y `FormatoNoSolucion` (No solución, porcentajes ya en tanto por
  cien). Los dos escriben dos decimales y `%` con espacio fino que no se parte (U+202F).
- Capturas para revisar el aspecto: Edge sin ventana con `--force-prefers-reduced-motion` (si no, sale a mitad de las
  animaciones) y `--blink-settings=preferredColorScheme=1` (claro) o `=0` (oscuro).

### 9.2 Variables añadidas

| Variable | Qué es |
|---|---|
| `--serie-3`, `--serie-4`, `--serie-5` | el verde, el rojo y el lila de las series de 2.2 (`--serie-1` y `--serie-2` ya existían) |
| `--atencion` / `--atencion-texto` | tramo «atención» de No solución: ámbar `#c99400` para rellenos y `#8a6500` para texto; en oscuro, `#e9b44c` los dos |
| `--serie` | color de una serie **por marca**, fijo (si se filtra una, las demás no cambian): lo ponen `.serie-orange` (naranja), `.serie-jazztel` (azul), `.serie-masmovil` (verde) y `.serie-yoigo` (lila) |
| `--relleno` | color de una barra **por tono**: `.tono-critico` / `.relleno-malo` (rojo), `.tono-atencion` (ámbar), `.tono-bueno` (verde), `.tono-neutro` / `.relleno-tenue` (gris), `.relleno-proceso` (azul) y `.relleno-atencion` (naranja). `.hbarra-relleno` y `.hbarra-media-relleno` lo usan si está; si no, su color de siempre |
| `--fraccion` | largo de una barra de `.hbarras.libre` (0–1) sobre el hueco que deja su cifra: la cifra nunca se sale |

En las vistas solo van, en línea, posiciones y tamaños de gráficos (`left`, `top`, `width`, `--fraccion`), nunca colores.

### 9.3 Piezas de Auditorías

| Pieza | Dónde |
|---|---|
| Tira de 5 indicadores | `.resumen` / `.resumen-dato` (`.destacada` = la nota); `.resumen-cobertura` es la barra de «Total agentes» |
| Gráficas por tiempo | `.crono-tarjeta`: columnas (`.columna`) y línea suavizada (`crono-linea principal`, `crono-area`, `.meta` discontinua al 50 %) |
| Barras por sector | `.hbarras > .hbarra` con la nota en pareja (`.hbarra-media`); pulsar una barra filtra |
| Top 10 | `table.ranking[data-mapa]` con `th[data-sentido]` y `td[data-valor]` |
| Panel y desplegables | `_PanelFiltros`, `Shared/_Desplegable` (lo usan los dos informes), «Más filtros» con contador |
| Pestañas | `.pestanas` (`.pestana`) y, a la derecha, `.pestanas-vista` con `.segmento` (Día / Semana / Mes) |
| Portada | tres tarjetas en fila: **General**, **CDM No solución** y **GAIA Formación** (Formación & Calidad se abre desde las pestañas) |

### 9.4 Piezas de CDM No solución

Usa las de Auditorías (cabecera, pestañas, panel, `_Desplegable`, `.resumen`, `.tarjeta`, `.rejilla-2`, `.hbarras`,
`.crono-linea`, `.punto`, `table.ranking`, `.aviso`) y añade solo lo que no existía:

| Pieza | Clase | Notas |
|---|---|---|
| Barras sin alto fijo | `.hbarras.libre > .hbarra` | nombre con su detalle (`.sub-ns`), barra con su cifra y una nota en la tercera columna |
| Líneas por marca | `.lineas-ns` con `crono-linea` y `.punto` dentro de `.serie-*` | los tramos que tocan un día parcial, `crono-linea discontinua` |
| Pastilla de tasa | `.chip` + `.chip-critico` / `.chip-atencion` / `.chip-bueno` | la tasa y su desvío frente a la media, con el semáforo pastel |
| Filas que se despliegan | `.filas-ns > details.desplegable-ns > summary.fila-ns` | columnas con `.cols-n3`, `.cols-averia` o `.cols-impedimento`; cabecera `.fila-ns-cabecera` con los colores de `--thead`; cuerpo `.cuerpo-ns` |
| Llamadas de ejemplo | `.muestras-ns` / `.muestra-ns` | filo de 3 px del acento; «Copiar ID» con `[data-copiar]` |
| Atajos de fecha | `.atajos-ns` / `.atajo` (`.activo`) | Todo el periodo, 7 días, 30 días, último mes cerrado |
| Mientras se traen los datos | `.preparando-ns` con `.girando` y `[data-recargar-en]` | la página se vuelve a pedir sola cada 10 s |
| Textos | `.nota-ns` (explicación bajo un título), `.pie-ns` (pie con la fuente), `.estado-ns` (fecha de los datos junto a las pestañas), `.puesto-ns`, `.sub-ns` | |
| Aviso informativo | `.aviso.aviso-info` | el `.aviso` normal es ámbar; este va sobre la superficie |
| Reparto en una línea | `.veredicto-ns` con `span.relleno-*` | cuadradito del color de su `--relleno` y la cifra en negrita (la causa de la no solución) |
| Comprobaciones | `ul.comprobaciones-ns` con `FormatoNoSolucion.ChipComprobacion` | pastilla «Correcto» (verde), «Revisar» (ámbar) o «Nota» (sin color) y el texto al lado |
| Texto largo bajo un nombre | `.sub-ns.envuelve` | el `.sub-ns` normal corta con «…»; este ocupa varias líneas |
| Etiquetas de impacto en las gráficas | `.fin-serie` (último valor de cada serie, a la derecha, con su `--serie`), `.valor.valor-serie` (pico con el color de su serie), `.valor.con-fondo` (etiqueta sobre otras barras, con el fondo de la tarjeta), `.meta` + `.meta-texto` (media) y `.grafica.con-meta` (hueco a la derecha para la cifra de la media) | se rotula lo que importa —media, pico, máximo y mínimo, último valor—, no cada punto; los días parciales y provisionales no cuentan como pico, máximo ni mínimo |

### 9.5 Reglas aprendidas en esta web

- **El panel de filtros pegajoso necesita `z-index`** (20, por debajo de la cabecera, 50): sin él, sus desplegables quedan
  detrás de las tarjetas animadas.
- **`label.opcion[hidden] { display: none }`**: el buscador oculta opciones con `[hidden]` y el `display: flex` de la
  opción lo anulaba.
- **Filtros al marcar**: cada casilla filtra y el desplegable sigue abierto (mismo texto de búsqueda, desplazamiento y
  foco). Si no cabe debajo, se abre hacia arriba (`.hacia-arriba`).
- **Al filtrar no se repiten las animaciones de entrada**: el contenido nuevo lleva `.actualizado` y las cifras pasan del
  valor anterior al nuevo.
- **Fechas en la URL**: las que coinciden con el rango por defecto no se escriben, para que un enlace guardado siga al último
  día con datos. En Auditorías el rango por defecto es el calendario entero; en No solución, los últimos 30 días
  (`data-defecto` en cada fecha y `data-fechas-juntas` en el formulario).
- **Eje X**: la última etiqueta siempre se escribe; las intermedias, solo si no la pisan.
- **Barras con cifra al lado**: el largo sale de `--fraccion` sobre el hueco libre, no de un porcentaje fijo; si no, en
  tarjetas estrechas la cifra pisa la nota.
- **No solución**: las diferencias entre dos porcentajes se escriben con `%` y signo («+0,65 %»), porque son una resta, no una
  variación relativa. Más es peor: la variación que sube va en rojo (`.peor`) y la que baja en verde (`.mejor`).
- **Panel de No solución**: fechas con atajos, Sector, Supervisor, Team leader, Agente y Marca, todo a la vista (no hay
  «Más filtros»); supervisor, TL y agente se encadenan.


### 9.6 Piezas de GAIA Formación (`/gaia`, 06-10-2026)

Mismas cabecera, pestañas, panel, `_Desplegable`, `.resumen`, `.tarjeta`, `.rejilla-2`, `.crono-linea`, `table.ranking`,
`.filas-ns` (filas que se despliegan), `.chip`, `.aviso` y `.preparando-ns` que Auditorías y No solución. Vistas en
`Views/Gaia/` (`_LayoutGaia`, `_InformeGaia`, `_PanelGaia`, una por pestaña y tres gráficas parciales). Los formatos y las
clases de color de los umbrales están en `Views/Gaia/AyudasGaia.cs` (`@functions` no funciona en `_ViewImports`; se importa
allí con `@using static`). Lo nuevo:

| Pieza | Clase | Notas |
|---|---|---|
| Tira de 10 indicadores | `.resumen` con 10 `.resumen-dato` | dos filas de cinco; el 6.º al 10.º entran con su escalón |
| Tira de criterios | `.resumen.en-4` + `.resumen-dato.sin-icono` | cuatro por fila, sin icono, con `.resumen-cobertura` como barra y el peso en la nota |
| Selector de indicador | `.barra-kpi` con `details.multi` y `a.opcion-enlace` (`.activa`) | cada enlace es `data-parcial` y recarga con `?kpi=`; no hace falta JS nuevo |
| Columnas por etapa | `_ColumnasEtapaGaia` (`.grafica.con-meta`) | media discontinua con su cifra, máximo y mínimo; con `PorUmbral`, `.columna-umbral` + `.tono-*` |
| Línea diaria + volumen | `_LineaDiaGaia` (`.grafica.lineas-ns.sin-eje-x`) y `_VolumenDiaGaia` (`.grafica.corta`, `.columna-volumen`) | comparten posiciones; el eje X va en el volumen; máximo y mínimo solo de días con 10 llamadas o más (`MinLlamadasFiable`); `.valor.debajo` pone el mínimo bajo su punto |
| Barra de adherencia | `.medidor` (`.medidor-pista`, `.medidor-relleno` con `--fraccion` y `.tono-*`, `.medidor-marca` en el 40 % y el 70 %) | `.adherencia-cifra` es la cifra grande |
| Celdas frente al total | `.mejor-total` / `.peor-total` | semáforo pastel al 55 %; margen de 1 punto o 10 % del total; agentes con menos de 10 llamadas, en gris (`tr.pocas`) y sin tinte |
| Fila de total | `.ranking tr.total` | fondo `--superficie-2`, negrita |
| Cabecera que no ordena | `.ranking .cab-columna` | no usar `.ordenar` si no ordena: site.js lo ordenaría en el navegador |
| Matriz agente × etapa | `.tabla-matriz-gaia` | primera columna pegajosa; celdas con `mapa-rojo` / `mapa-ambar` / `mapa-verde` según los umbrales de la adherencia |
| Punto de marca | `.marca-punto` con `.serie-*` | color fijo por marca |
| Llamadas desplegables | `.filas-ns.filas-llamadas` con `.cols-llamada`, `.celda-doble`, `.recorta` | la marca y la etapa van bajo el agente para caber en el ancho mínimo; en móvil la fila hace scroll y el detalle no |
| Detalle de llamada | `.detalle-gaia` (dos columnas), `.calificaciones-gaia` (con `.chip` Sí / No / N/A), `dl.datos-gaia`, `.texto-largo`, `.detalle-gaia-ids` | los ID se copian con `[data-copiar]` |
| Paginación | `.paginacion` con `.pestanas-vista` y `.segmento` (`.desactivado`) | de 100 en 100 |
| Aviso plegable | `.aviso-plegable` | los avisos del Excel de nómina, cerrado por defecto |
| Tinte de ejemplo | `.muestra-tinte` (+ `.mejor-total`, `.peor-total`, `.mapa-*`) | muestras de color en las notas |

- Variables: no hay nuevas; todo sale de `--malo`, `--atencion`, `--bueno`, `--mapa-*`, `--serie-*` y `--superficie-2`.
- Iconos nuevos en `Iconos.cs`: `transferir`, `baja-cliente`, `etiqueta` y `carrito`.
- La portada pasa a **tres tarjetas** en fila (General, CDM No solución y GAIA Formación): `.portada` sube a 1.320 px y la
  tarjeta a 410 px; las «ventajas» se quedan en 1.040 px.
- Estado de los datos de GAIA: sin datos, `.preparando-ns` (se recarga sola cada 10 s); tras un fallo sin reintento, el error
  y «Volver a intentarlo». Panel: fechas (por defecto, todo el rango) y siete desplegables, sin «Más filtros».
