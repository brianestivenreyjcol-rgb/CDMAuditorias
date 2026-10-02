# SOLARIS · GAIA — guía de estilos

Guía de diseño para quien vaya a tocar las pantallas: tipo de letra, colores, piezas y distribución.
Verificada contra las hojas de estilo (`wwwroot/css`) el 02/10/2026. Si algo de aquí choca con el código, manda el código y
hay que corregir este documento.

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
