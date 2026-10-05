# CDM Auditorías Calidad — contexto para continuar el trabajo

> Documento vivo. Lo mantiene la IA que trabaja en el proyecto: qué hay, qué se decidió,
> por qué, y qué falta. Si eres otra IA retomando el trabajo, **lee esto entero primero**.
> Idioma del usuario: **español** (todo lo que vea el usuario, en español; código y
> comentarios también en español).

---

## 0. Resumen rápido (estado al 05-10-2026)

- **Qué es**: el Power BI «CDM Auditorías Calidad» pasado a una web ASP.NET Core MVC (.NET 8,
  Razor), con sus páginas General y Formación & Calidad, sus medidas y sus filtros. Desde el
  05-10-2026 lleva además **CDM No solución** (`/nosolucion`), traído de ranking-mvc (sección 3 ter).
- **Dónde**: `C:\Proyectos\CDM Auditorias Calidad\` (solución `CDM Auditorias Calidad.sln`).
  En GitHub: repositorio **privado** `brianestivenreyjcol-rgb/CDMAuditorias`, rama `main`.
- **Aspecto**: sigue `docs/guia-de-estilos.md` (guía SOLARIS · GAIA que dio el usuario).
- **Una sola dirección: el puerto 5180** (pedido del usuario el 02-10-2026: «deja solo 1»).
  - `http://localhost:5180/general` en este equipo, `http://10.148.223.143:5180/general` para los
    compañeros: es el mismo servidor (`arrancar.cmd`, `publicacion\app`), con la **versión
    actual** (publicada el 02-10-2026).
  - El perfil de Visual Studio también usa el 5180: solo puede haber una en marcha (cerrar la
    ventana de `arrancar.cmd` antes de F5, y al revés). Ya no se usan el 5157 ni el 5158.
- **Datos**: SQL Server `10.148.226.40\REPORTING` con la misma consulta del PBI
  (`Consultas/Auditorias.sql`), más la nómina (`Consultas/Nomina.sql`). Todo en memoria,
  recarga cada 30 min. **No solución** sale de **BigQuery** por ODBC (DSN `BQCOL`): un cubo de 90
  días guardado en disco que se renueva solo cada 12 h.
- **Ventana de datos**: 3 meses atrás + el mes en curso (hoy, julio a octubre). ICEBERG ya trae
  julio; las tablas WEB empiezan el 01/08 y el usuario **avisará** cuando el origen traiga 4 meses.
- **Pendiente principal**: **publicar en el 5180 la causa de cada no solución** (atención o proceso,
  sección 3 ter): el usuario publicó la primera versión de No solución el 05-10-2026 a las 12:43 y
  lo de después está probado solo en una copia temporal. Cuenta de servicio para SQL, decidir si hace
  falta inicio de sesión, y julio de WEB cuando avise el usuario (sección 7).

---

## 1. Objetivo

Pasar a web el informe de Power BI **«CDM Auditorías Calidad»** (auditorías de calidad de
llamadas/chats del call center), manteniendo sus pantallas, filtros y cálculos.

- Power BI de origen: estaba en `Power bi\` (formato PBIP: `*.SemanticModel` = modelo y
  consultas, `*.Report` = páginas y visuales, todo en texto). **El usuario quitó esa carpeta
  del proyecto el 02-10-2026**; sigue en el historial de git (commit `2caa2bd`:
  `git show 2caa2bd:"Power bi/…"`). Todo lo necesario de él está resumido en la sección 2.
- Web: `CDM Auditorias Calidad\` (lo creó el usuario con la plantilla MVC de Visual Studio).
- Mismo estilo de trabajo que su otro proyecto, `C:\Proyectos\ranking-mvc` (MVC + Razor, sin
  API JSON, acceso a datos en `Servicios/`). De allí se reutilizaron ideas (lector de `.env`,
  gráficas SVG con textos en HTML) y, el 05-10-2026, se **copió** el motor de CDM No solución
  (sección 3 ter): es una copia, no código compartido, así que un arreglo en un proyecto no
  llega solo al otro.

---

## 2. Análisis del Power BI

### 2.1 Modelo (`CDM Auditorias Calidad Prueba.SemanticModel/definition`)

| Tabla | Qué es |
|---|---|
| `Auditorias` | Importación de una consulta SQL (ver 2.2). Columnas: `Fecha`, `legajo`, `Sector`, `Super`, `Team`, `Agente`, `ID_Llamada`, `CorreoAuditor`, `Nombre_Auditor`, `Cargo_Auditor`, `Respuesta` (nota 0–1), `Base` (`WEB` / `ICEBERG`). |
| `Calendario` | Tabla calculada `CALENDAR(MIN(Auditorias[Fecha]), MAX(Auditorias[Fecha]))`, marcada como tabla de fechas. Columnas `Año`, `NumMes`, `Mes` (`"MMM"`), `Día` (= `DAY()`, día del mes), `Semana` (= `WEEKNUM(fecha, 2)`, semana que empieza en lunes), etc. |
| `Fecha_Parametro` | Parámetro de campo con 3 opciones: `Día`, `Semana`, `Mes` → cambia el eje X de los dos gráficos de evolución. |
| `Medidas` | Medidas DAX (ver 2.3). |

Relación única: `Auditorias[Fecha]` → `Calendario[Fecha]` (el calendario filtra a las
auditorías, no al revés).

### 2.2 Consulta SQL de auditorías

Servidor `10.148.226.40\REPORTING`, base `REPORTING` (usa nombres de 3 partes, cruza bases).
Copia en `CDM Auditorias Calidad/Consultas/Auditorias.sql`, igual que la del PBI salvo dos
cosas: sin el `ORDER BY` final y con la ventana de ICEBERG ampliada a `- 3` (ver más abajo).
La web la lee **en cada carga** desde la carpeta de la aplicación (en desarrollo, la del
proyecto; en producción, `publicacion\app`): un cambio en el `.sql` vale con pulsar
«Actualizar». Por partes (CTE):

- **`NominaAntiguedad`** (líneas 8–26): `Planificacion.Nomina.NominaAntiguedad`, una fila por
  legajo y día (`ROW_NUMBER` por fecha y legajo) → sector (`sec_descrip`), super, team, nombre.
- **`Nomina`** (28–46): `Planificacion.Nomina.Nomina_User_Avaya` (legajo ↔ usuario Avaya por
  día) cruzada con la anterior → la nómina con usuario Avaya, que es la que se usa para saber
  de quién es cada auditoría.
- **`Auditores`** (48–71): `NominaAntiguedad` + `Planificacion.Nomina.PD_Usuarios` (correo) →
  nombre y cargo del auditor.
- **`Auditorias`** (73–135) = **WEB**: une `Reporting.WO.AuditoriasWhatsapp`,
  `AuditoriasJazztel` y `AuditoriasOrange`.
  - WhatsApp y Jazztel: la nota viene en `Respuesta` (texto, a veces con `%`; si es > 1 se
    divide entre 100).
  - Orange: la nota se calcula con pesos: Saludo 10 %, Soy claro/fiable 15 %, Solucionó
    25 %, Resumió 20 %, Pregunta solución 20 %, Despedida 10 % (`SI` o `N/A` puntúan).
- **`WEB`** (137–188): cruza esas auditorías con `Nomina` por fecha y usuario Avaya del
  agente (**INNER JOIN**: auditorías sin agente en nómina ese día se pierden) y con
  `Auditores` por correo (normaliza `@masorange.es` → `@orange.es`).
- **`ICEBERG`** (190–222): `ModulosIceberg.Calidad.PlantillaCalidadUnificada` (nota en
  `Nota Calidad` sobre 100), con `Nomina` por legajo y fecha. Trae **3 meses atrás + el mes en
  curso** (`DATEADD(MONTH, DATEDIFF(MONTH,0,GETDATE())-3, 0)` hasta fin del mes actual): en el
  PBI era `- 2`; lo cambió el usuario el 02-10-2026 para tener julio.
- Al final: `WEB UNION ALL ICEBERG WHERE Agente IS NOT NULL`.

Medido el 01-10-2026: ~15.150 filas (WEB 5.638 desde 01-08; ICEBERG 9.510 desde 01-08), la
consulta tarda ~2 s.

### 2.2 bis Consulta de nómina (de la web, no del PBI)

`CDM Auditorias Calidad/Consultas/Nomina.sql`, para la tarjeta «Total agentes» (ver 3): es la
misma cadena que el CTE `Nomina` de arriba (`Nomina_User_Avaya` + `NominaAntiguedad` con
`RN = 1`) pero devolviendo además el **cargo** (`car_descrip`). Parámetros `@Desde`/`@Hasta` =
rango de las auditorías cargadas. Devuelve `Fecha, legajo, Sector, Super, Team, Cargo`
(~205.000 filas del 01-08 al 02-10, ~2,5 s).

Lo que se vio al explorarla (02-10-2026, desde el 01-08): unos 2.000 legajos con usuario
Avaya; por cargo, 1.555 «Agente», 259 «Agente en Capacitacion», 106 «Team Leader»,
29 «Formador», 21 «Técnico de Calidad», 18 «Aprendiz Sena Etapa Productiva»… De los auditados,
1.433 son «Agente», 121 en capacitación, 57 Team Leader, 16 aprendices SENA y 15 técnicos de
calidad. `Nomina_User_Avaya` trae también **fechas futuras** (planificadas, hasta noviembre).

### 2.3 Medidas DAX y cómo se reproducen

| Medida | DAX | En la web |
|---|---|---|
| Total Auditorías | `COUNTROWS(Auditorias)` | nº de filas filtradas |
| Nota Promedio de Calidad | `AVERAGE(Auditorias[Respuesta])` | media de las notas no nulas |
| Agentes Auditados | `DISTINCTCOUNT(Auditorias[legajo])` | **sustituida** por «Total agentes» (ver 3), a petición del usuario |
| Auditorías de la Semana | del lunes de la semana de `MAX(Calendario[Fecha])` hasta esa fecha | igual |
| Auditorías Semana Anterior | la anterior con `DATEADD(-7, DAY)` → lunes..(máx − 7) | igual |
| Auditorías del Mes | `DATESMTD` → día 1 del mes de la fecha máxima hasta ella | igual |
| Auditorías Mes Anterior | `DATESMTD` sobre `DATEADD(-1, MONTH)` | igual |
| Var % vs período anterior (total) | mismo filtro de fechas desplazado `-1 MONTH` | **cambiada**: el mismo tiempo, justo antes (ver 2.5 y 3) |
| Var pp nota | `(nota − nota período anterior) × 100` | `(nota − nota del mismo tiempo justo antes) × 100` (ver 2.5 y 3) |
| Meta de Calidad | constante `0,5` («para la línea punteada del gráfico») | línea discontinua al 50 % en el gráfico de nota |

`MAX(Calendario[Fecha])` es la última fecha del rango elegido en el filtro de fecha/mes (los
filtros de sector, auditor, etc. no la cambian, porque el calendario no se filtra desde
`Auditorias`). Como `Calendario` está marcada como tabla de fechas, las medidas de
semana/mes/período anterior **anulan el filtro de Mes** y usan solo sus propias fechas.

`DATEADD(-1, MONTH)`: cada fecha pasa al mismo día del mes anterior (si no existe, al último
día), y si el rango incluye el último día de un mes, el resultado llega hasta el último día
del mes anterior (30-09 → 31-08). El resultado se recorta al rango del calendario.

Todo esto está en `Servicios/Tablero/CalculadoraTablero.cs` y `Periodos.cs`, con pruebas.

### 2.4 Páginas del informe

1. **Menú** (portada, imagen de fondo con dos botones): «General» y «Formación y Calidad».
   ⚠️ En el PBI, el botón «Formación y Calidad» apunta a una página que no existe
   (`7f694fdf5024230e8068`).
2. **General** y 3. **Formación & Calidad**: mismo diseño (1672 × 941):
   - Barra izquierda: logo (vuelve al menú), «FILTROS» con botón de borrar filtros, filtros
     **Fecha** (rango), **Mes**, **Sector**, **Super**, **Team**, **Auditor**,
     **Cargo_Auditor**, **Base** (desplegables de selección múltiple), y la tabla
     **Descargable** (Base, Fecha, Super, Team, Agente, Sector, Auditor, Cargo, Respuesta).
   - Cabecera: «AUDITORÍAS | CONTROL Y CALIDAD» + nombre de la página, navegador de páginas
     y selector Día / Semana / Mes.
   - 5 tarjetas KPI (medida HTML `Tarjetas KPI`) con su variación (verde ▲ / rojo ▼).
   - «Evolución de Auditorías» (columnas), «Nota Promedio de Calidad» (línea suavizada),
     «Total Auditorías y Nota Promedio de Calidad por Sector» (barras), «Top 10 Auditores».
   - **Diferencias entre páginas**: Formación & Calidad tiene un filtro de página
     `Cargo_Auditor ∈ {Formador, Formador PP, Técnico de Calidad, Técnico de Calidad PP}` y
     arranca agrupando por **Mes**; General no filtra cargos y arranca por **Semana**.
   - Interacción: al pulsar un auditor del top 10 se resalta su reparto por sector.

### 2.5 Fallos o rarezas del PBI detectados (y qué hace la web)

| PBI | Web |
|---|---|
| El botón del menú «Formación y Calidad» no navega (página inexistente). | Formación & Calidad se abre desde las pestañas del informe (en la portada no tiene tarjeta, a petición del usuario). |
| La página Formación & Calidad muestra el subtítulo «General» (usa la medida `Titulo Encabezado General`). | Muestra «Formación & Calidad». |
| «Día» del eje X es `DAY()` (1–31): con varios meses junta el día 5 de julio con el 5 de agosto. | Agrupa por fecha real (dd/mm). |
| `Semana` y `Mes` no llevan año: entre años distintos se mezclan. | Se agrupa por año + semana / año + mes (la etiqueta sigue siendo el nº de semana o el mes). |
| El «período anterior» de Total y Nota es `DATEADD(-1, MONTH)` de las fechas elegidas: con más de un mes **se solapa** con ellas y es **más corto** (con todo el rango, 01/08–02/10 frente a 01/08–02/09 → +102 %, sin sentido; el usuario lo vio el 02-10-2026). | Se compara con **el mismo tiempo, justo antes** (`Periodos.PeriodoAnterior`); si no hay datos de todo ese período, «Sin período anterior con datos». |
| Si el período anterior no tiene datos, la variación de nota sale como la nota entera en pp (blank = 0). | Muestra «Sin datos del período anterior». |
| La tabla «Descargable» agrupa filas idénticas y suma su `Respuesta`. | Exporta una fila por auditoría. |
| La medida `Meta de Calidad` existe pero ningún visual la usa. | Se dibuja como línea discontinua en el gráfico de nota. |

---

## 3. Decisiones de la web

- **Datos en memoria**: igual que el modo importación de Power BI. Al arrancar, cada
  `Auditorias:MinutosRecarga` minutos (30) y al pulsar «Actualizar» se ejecutan las consultas
  y se guardan todas las filas en memoria (~15.000 auditorías + ~205.000 filas de nómina,
  ~5 s en total, en segundo plano). Todos los cálculos se hacen en C# sobre esas listas (cada
  página tarda < 150 ms). Si una recarga falla, se siguen sirviendo los datos anteriores y se
  avisa en la página.
- **Sin librerías de gráficos ni Bootstrap**: gráficas dibujadas en el servidor (SVG estirado
  al plano + textos en HTML encima, alto fijo). Se quitaron `wwwroot/lib` (Bootstrap, jQuery)
  y la página Privacy de la plantilla.
- **Filtros**: formulario GET (la URL se puede compartir y el botón Atrás funciona). `site.js`
  lo envía por `fetch` con la cabecera `X-Parcial: 1`; el servidor devuelve solo el partial
  `_Informe` y se sustituye `#informe`. Sin JavaScript también funciona (enlaces normales y un
  botón «Aplicar filtros» en `<noscript>`).
  - **Se filtra al marcar** (pedido del usuario el 02-10-2026): cada casilla aplica el filtro
    al momento y el desplegable **sigue abierto** tras recargar (mismo texto de búsqueda,
    mismo desplazamiento y el foco en la casilla) para poder marcar más. Ya no hay botón
    «Aplicar»; «Quitar selección (n)» aparece cuando hay algo marcado. Las fechas filtran al
    cambiarlas. Un clic fuera o Escape cierran el desplegable.
  - Buscador si hay más de 8 opciones: **filtra mientras se escribe** (sin tildes ni
    mayúsculas, sobre el nombre de la opción), dice «Sin coincidencias para «…»» si no queda
    ninguna, e Intro no envía el formulario. Ojo: las opciones se ocultan con `[hidden]` y
    `label.opcion` es `display: flex`, así que hace falta `label.opcion[hidden] { display: none }`
    (sin esa regla el buscador no ocultaba nada; lo vio el usuario el 02-10-2026). Cada opción
    lleva su cifra de auditorías.
  - Las opciones se filtran entre sí, como los segmentadores del PBI (cada filtro muestra lo
    que queda con los demás); las marcadas se ven siempre, aunque queden a 0.
  - Valores vacíos = «(En blanco)», como en Power BI.
  - Las fechas que coinciden con el borde del calendario no se mandan en la URL, para que un
    enlace guardado siga cogiendo los datos nuevos.
  - Al cambiar de página (General ↔ Formación) se conservan fecha y mes; el resto no (en el
    PBI los segmentadores no estaban sincronizados).
  - **Los desplegables van por encima del contenido**: el panel de filtros es `position:
    sticky` (crea su propia capa de apilamiento) y las tarjetas animadas crean las suyas; sin
    `z-index` en el panel, los desplegables quedaban **detrás** de las tarjetas (lo vio el
    usuario el 02-10-2026). Se arregló con `z-index: 20` en `.panel-filtros` (por debajo de la
    cabecera, 50). En móvil el panel es `position: relative` con el mismo `z-index`.
  - Si un desplegable no cabe debajo (los de «Más filtros»), `site.js` lo abre hacia arriba
    (`.hacia-arriba`) y ajusta el alto de la lista al hueco que hay.
- **Clic para filtrar**: pulsar una barra de sector o un auditor del top 10 añade (o quita)
  ese filtro; sustituye la interacción de resaltado del PBI.
- **Descargable**: botón «Descargar Excel» (.xlsx, ClosedXML) con el detalle filtrado.
- **Tarjetas**: al pasar el ratón dicen qué fechas comparan.
- **Período anterior de «Total auditorías» y de la nota** (lo eligió el usuario el 02-10-2026,
  porque el +102 % del PBI no tenía lógica): **el mismo tiempo, justo antes**, en
  `Periodos.PeriodoAnterior`:
  - fechas que empiezan un día 1 y acaban a fin de mes, o en la última fecha con datos (mes en
    curso) → los mismos meses de antes con el mismo corte de día: septiembre → agosto entero;
    01–02/10 → 01–02/09; agosto + septiembre → junio + julio;
  - cualquier otro rango → los mismos días justo antes: 21–27/09 → 14–20/09.
  - Nunca se solapa con lo elegido. Si ese período empieza antes que los datos (con el rango
    completo, que empieza el 01/08), no hay variación: «Sin período anterior con datos», y la
    ficha dice qué período haría falta.
  - «Auditorías de la semana» y «del mes» siguen las medidas del PBI (semana hasta la fecha
    frente a la anterior; mes hasta la fecha frente al anterior), que sí tienen sentido.
- **Tarjeta «Total agentes»** (02-10-2026, en lugar de «Agentes auditados»): agentes en nómina
  frente a los que tienen auditoría.
  - Total = legajos distintos de `Consultas/Nomina.sql` en las fechas elegidas, con los
    filtros de **sector, super y team** (los de auditor, cargo de auditor y base no aplican a
    la nómina) y con cargo en `Auditorias:CargosAgente`: **Agente, Agente en Capacitacion y
    Aprendiz Sena Etapa Productiva** (lo eligió el usuario; se compara sin mayúsculas).
  - Auditados = de ese total, los que tienen al menos una auditoría con todos los filtros. Se
    pinta «1.487 auditados · 86,45 %» con una barra de cobertura (nunca pasa del 100 %).
  - Si la nómina no se puede leer, la tarjeta sale «—» con el motivo en la ficha y el resto
    del informe sigue.
- **Portada**: la tarjeta «General» (el 02-10-2026 el usuario pidió quitar la de Formación) y, desde
  el 05-10-2026, «CDM No solución»; «Formación &
  Calidad» se abre desde las pestañas del informe.
- **Top 10**: agrupa por auditor y cargo (como el PBI: un auditor con dos cargos sale en dos
  filas); los auditores con menos de 20 auditorías van al final y en gris.
- **Credenciales**: en `CDM Auditorias Calidad/.env` (no se versiona ni se publica; plantilla
  en `env.ejemplo`). Variables: `AUDITORIAS_DB_HOST`, `AUDITORIAS_DB_NAME` (REPORTING),
  `AUDITORIAS_DB_USER`, `AUDITORIAS_DB_PASSWORD`, `DB_TRUST_SERVER_CERTIFICATE=true`. Una
  variable de entorno con el mismo nombre tiene prioridad. **Se copiaron de la cuenta `DB_2`
  del `.env` de producción de ranking-mvc** (es un login personal del usuario; ver 7).
- **Sin inicio de sesión**: como el PBI, cualquiera que llegue a la URL ve los datos (hay
  nombres de agentes y auditores). Ver 7.

---

## 3 bis. Estilo: guía SOLARIS · GAIA (desde el 02-10-2026)

El usuario pidió seguir **`docs/guia-de-estilos.md`** (guía de diseño de su plataforma SOLARIS ·
GAIA; las secciones 1–8 son las suyas tal cual y la **sección 9**, añadida el 05-10-2026, recoge cómo
se aplica en esta web, las variables añadidas y las piezas de Auditorías y de No solución: es lo
primero que hay que mirar). **Antes de tocar el aspecto, léela.** Lo que se aplicó:

- **Letra**: Segoe UI Variable Text (`--font-ui`) y Display (`--font-titulo`, solo en el título
  de la cabecera y en las cifras grandes); no se carga ninguna fuente. Negrita 600 en titulares
  y cifras, 500 en nombres de tabla; nada de 700/800.
- **Cifras**: dos decimales y `%` con espacio fino que no se parte (U+202F): «50,14 %».
  `Formato.Porcentaje`, `Formato.Decimal2`, `Formato.PorcentajeEntero` (ejes).
- **Colores**: todos son variables de `:root` en `site.css`, redefinidas en
  `[data-tema="oscuro"]`. Informe = marca **Orange** (`--acento #f16e00`, texto encima negro,
  cabecera de tabla `#ffe3cc`/`#7a3a00`). Portada y error = `data-marca="portada"` (marino
  `#1f2a44` y grises; el naranja solo en el logotipo). Bueno/malo `#228722`/`#cd3c14`;
  semáforo pastel en la tabla; series de gráfica `#f16e00` y `#4170d8`.
  - La marca se pone en `<html data-marca>` desde `ViewData["Marca"]` (por defecto `orange`).
  - **Nada de colores en las vistas** (solo en `site.css`; el Excel y el favicon llevan los
    suyos porque no son vistas).
- **Tema claro/oscuro**: `wwwroot/js/tema.js` (en `<head>`, antes del CSS) pone
  `data-tema="claro|oscuro"` y `data-tema-preferido="auto|claro|oscuro"`; botón en la cabecera
  que va pasando auto → claro → oscuro; se guarda en `localStorage` (`cdm-tema`). En oscuro la
  elevación va con bordes, no con sombras.
- **Distribución**: cabecera pegajosa de 72 px con línea de 3 px del acento (logotipo que vuelve
  al menú, título y subtítulo en el centro, botón de tema); pestañas (General / Formación) y a la
  derecha «Agrupar por» Día/Semana/Mes; `.tablero` de 1.120 a 1.720 px con el panel de filtros
  de 268 px. Por debajo de 1.120 px hay scroll horizontal; **un solo punto de ruptura, 760 px**
  (todo se apila). La portada, además, pasa a una tarjeta por fila por debajo de 1.100 px (lo
  dice la guía). No usar `auto-fit` ni añadir más puntos de ruptura.
- **Panel de filtros**: sin scroll interno y pegajoso; siempre a la vista Fecha, Mes, Sector,
  Super, Team y Auditor; Cargo auditor y Base en «Más filtros» (`TableroModelo.CamposEnMasFiltros`;
  se abre solo si tiene algo marcado). Un desplegable con menos de dos opciones no se pinta
  (`GrupoFiltro.Visible`).
- **Piezas con los nombres de la guía**: `.resumen` / `.resumen-dato` (tira de indicadores con
  icono, filo de 3 px y realce radial; `.destacada` = la nota; `.resumen-cobertura` = la barra de
  «Total agentes»), `.tarjeta`, `.rejilla-2`, `.crono-tarjeta` con `crono-linea principal` y
  `crono-area`, `.hbarras > .hbarra` con `.hbarra-media` (la nota del sector, en azul, en pareja
  con la cantidad), `table.ranking[data-mapa]` con `th[data-sentido]` y `td[data-valor]`,
  parciales `_PanelFiltros` y `_ErrorDatos`.
- **Fichas al pasar el ratón**: cada periodo de las gráficas lleva una banda invisible con un
  `<title>` «Etiqueta · Serie valor · …»; `site.js` lo convierte en ficha, con guía vertical y el
  resto de periodos desvanecido. (La guía habla de `graficas.js` de SOLARIS; aquí no existe y lo
  hace `site.js`.) También tienen ficha las tarjetas de indicador y las barras de sector.
- **Movimiento**: 160/220/440/780 ms, 45 ms entre tarjetas hermanas, curva
  `cubic-bezier(.22,.61,.36,1)`; las barras crecen con `scaleX`/`scaleY`; barra de carga
  naranja arriba al aplicar filtros. Con «reducir movimiento» no se anima nada (CSS y JS).
  - Las animaciones de entrada (tarjetas que aparecen, barras que crecen, cifras que cuentan
    desde cero) **solo se ven al cargar la página**. Al filtrar, el contenido nuevo lleva la
    clase `.actualizado` (sin animaciones de entrada) y las cifras pasan del valor anterior al
    nuevo, para que marcar casillas seguidas no parpadee.
- **Sin emojis ni iconos de color**: iconos de trazo en `Infraestructura/Iconos.cs` (también el
  logotipo); se quitaron el GIF y las tarjetas decorativas de la portada.
- No se puso la marca SOLARIS (no hay logotipo ni se sabe si la web va dentro de esa
  plataforma): ver 7.

---

## 3 ter. CDM No solución (desde el 05-10-2026)

El usuario pidió el 05-10-2026 «implementar lo de No solución del otro proyecto» aquí. Es la
pantalla `/cdm` de ranking-mvc (en producción allí, en el 5173), traída a `/nosolucion` con el
aspecto de este proyecto. **Allí sigue existiendo**: son dos copias independientes.

**Qué mide**: la encuesta de solución de las llamadas de Call Bogotá.
`% no solución = no solucionadas ÷ (solucionadas + no solucionadas)`, sobre llamadas
**entrantes encuestadas**. Universo: `channel = Call`; YOIGO y MASMOVIL en
`serviceProviderLocation = JAZZBOG`, JAZZTEL y ORANGE en `JZZ_BOGOTA`. El equipo sale de cruzar
`primaryAgentId` con la nómina (`NominaBogota`): por `DataOrb` en YOIGO/MASMOVIL y por la
extensión `Avaya` en JAZZTEL/ORANGE (la ficha «Cruce con nómina» debe rondar el 99,9 %; por
debajo del 99 % la llave estaría mal). Más detalle de negocio, en la bóveda de ranking-mvc:
`ranking-mvc\Documentacion\Negocio\Fuentes\CDM No solución.md`.

**De dónde sale**: BigQuery por ODBC (driver Simba, **DSN de usuario `BQCOL`**, con la cuenta de
Google del usuario), tablas `mo-vendor-management-reporting.JZZBOGOTA.ALL_dataorb_to_mo_insight_all_data_flattened_GAMMA`
y `…JZZBOGOTA.NominaBogota`. Se traen **90 días** hasta ayer en dos tramos de 45 (con 90 de golpe
el driver fallaba), se agregan en un **cubo** y se guardan en disco (~60 MB):

- Desarrollo: `CDM Auditorias Calidad\App_Data\cache_nosolucion.json` (ni se versiona ni se publica).
- Producción: `publicacion\datos\cache_nosolucion.json` (fuera de `publicacion\app`, para que
  `publicar.cmd` no la borre). Lo fija `appsettings.Production.json`.
- **Ninguna petición espera a BigQuery**: sin cubo, la página enseña «Preparando los datos» y se
  vuelve a pedir sola cada 10 s (la primera descarga tarda ~1 min). Con un cubo de más de 12 h, se
  sirve el que hay y se renueva en segundo plano. «Actualizar ahora» lo fuerza (como mucho una vez
  cada 30 s; aquí no hay inicio de sesión, así que lo puede pulsar cualquiera).
- **El driver no admite dos consultas a la vez** en el mismo proceso: todas pasan por un turno
  único (`FuenteBigQuery.Turno`), con timeout y tres reintentos.
- Los últimos 5 días son **provisionales** (las encuestas llegan tarde) y un día cuya extracción
  no llega al 80 % de la mediana de su día de la semana es **parcial** (línea discontinua).
- `Datos\impedimentos_tabla.json`: tabla fija de etiquetas de impedimento → categorías (la
  «aprendida del informe del 10/09»); viaja con el código.

**Pestañas** (todas con el mismo panel de filtros: fechas con atajos —todo, 7 días, 30 días, último
mes cerrado; **por defecto, los últimos 30 días**—, Sector (cola de entrada), Supervisor, Team
leader, Agente y Marca, en cascada; supervisor, TL y agente se encadenan):

- **Resumen**: 5 indicadores (no solución con su variación frente a los mismos días previos,
  no solucionadas, encuestadas con cobertura, llamadas entrantes, cruce con nómina), evolución
  diaria del % por marca (color fijo por marca), encuestas por día, los días que peor cerraron y
  «qué queda fuera de la cifra».
- **Equipos**: ranking por % de supervisores, TL o agentes (suelo de encuestas prorrateado al
  rango), buscador, orden por columna **en el servidor** (para que el CSV salga igual que la
  tabla), desvío frente a la media y aviso si la cobertura es desigual.
- **Motivos**: áreas N2 en barras (por no solucionadas en absoluto) y las 25 tipologías N3; cada
  una se despliega con 10 llamadas de ejemplo (texto e ID con «Copiar ID») y su CSV.
- **Sin acceso a internet**: indicadores, atención frente a proceso, la rúbrica ítem a ítem,
  los impedimentos (desplegables con reparto por marca y servicio y llamadas de ejemplo) y las
  averías N4 › N5.
- **CSV**: todas las no solucionadas con los filtros (`/nosolucion/csv`, opcionalmente de una
  tipología `n3` o un impedimento `bit`), la tabla de equipos y las tipologías.

**Qué se trajo y qué se rehízo**:

- **Tal cual** (solo cambia el espacio de nombres): `Servicios/NoSolucion/` (`FuenteBigQuery`,
  `CacheNoSolucion`, `Cubo`, `Ensamblador`, `Agregados`, `FiltrosCdm`, `ServicioCdm`) y
  `Servicios/Comun/` (`FormatoPython`, `ExportacionCsv`, `Errores`), con sus pruebas
  (`NoSolucionTests.cs` contra la salida del Python de referencia en `Fixtures/nosolucion_python.json`,
  y las del servicio de pantallas de `CdmTests.cs`).
- **Adaptado**: `ServicioNoSolucion` lee `OpcionesNoSolucion` (sección `NoSolucion` de
  appsettings; en ranking-mvc eran las variables `NOSOL_*` del `.env`) y resuelve las rutas
  respecto a la carpeta de la aplicación.
- **Rehecho con el estilo de aquí**: controlador `NoSolucionController`, modelos
  `Models/NoSolucion` y vistas `Views/NoSolucion`. Usa las piezas de Auditorías (cabecera,
  pestañas, `_Desplegable` —movido a `Views/Shared`—, tira de indicadores, tarjetas, `.hbarras`,
  `crono-linea`, `table.ranking`, filtros que se aplican al marcar con recarga parcial) y solo
  añade lo que no existía (pastillas de tasa, filas que se despliegan, llamadas de ejemplo,
  atajos de fecha, «preparando»). Todo está en la **sección 9 de `docs/guia-de-estilos.md`**.
- **Quitado** respecto a ranking-mvc: el permiso `ver_nosolucion`, el botón solo para admin y el
  recuerdo de filtros en la sesión (aquí no hay inicio de sesión; los filtros van en la URL).
- **Portada**: segunda tarjeta, «CDM No solución», junto a «General».

### ¿Atención o proceso? Y las llamadas repetidas (05-10-2026, tarde)

El usuario pidió, «en base al contexto» de su bóveda `C:\Proyectos\Soporte YGMM` (el proceso de
soporte técnico de YGMM: Schaman, escalados, envío de técnico, cierre y encuesta…), que la pestaña
**Sin acceso a internet** diga si cada no solución fue de **atención** o de **proceso**, y que se
valide si había **llamadas duplicadas** en «Qué frena el proceso».

- **Duplicadas: no hay.** Comprobado en BigQuery (05-09 → 04-10): 20.589 registros = 20.589
  `conversationId` distintos; las 1.638 no solucionadas son 1.638 llamadas distintas. 277
  conversaciones son tramos de una llamada transferida (mismo `externalConversationId` con `_1`,
  `_2`…): se cuentan aparte porque cada tramo tiene su agente y su encuesta. Lo que «inflaba» la tabla
  es que **casi todas las no solucionadas traen 2 o 3 impedimentos** y cuentan en cada fila (las filas
  suman 3.631 con 1.598 llamadas). La página lo dice ahora: tarjeta **«¿Hay llamadas repetidas?»**
  (calculada en cada carga, con el filtro de marca), y en «Qué frena» el número de llamadas distintas
  y, por fila, cuántas traen **solo ese** impedimento y con cuáles viene.
- **Una sola causa por llamada** (`Servicios/NoSolucion/CausaNoSolucion.cs`, reglas documentadas allí):
  *Proceso* (impedimento de proceso y atención correcta), *Proceso, con fallos de atención* (las dos:
  revisar si el escalado o el técnico eran evitables; en la bóveda, «Escalados de Voz»: el 90 % de los
  escalados a N2 eran errores de N1), *Atención* (sin impedimento de proceso y con cierre abrupto,
  impedimento del agente o rúbrica peor que la mediana de las solucionadas), *Cliente* y *Sin causa*.
  Son los mismos cuadrantes de antes con nombres de soporte, más la separación cliente / sin causa,
  cada uno desplegable con sus razones, 10 llamadas de ejemplo y su CSV. CSV nuevo con **cada llamada
  y su causa** y las señales que la deciden: `/nosolucion/internet/causas/csv` (`?causa=` opcional).
- **Etiquetas nuevas por palabras clave** (`ClasificadorEtiquetas.cs`): las etiquetas de impedimento
  las redacta la IA de DataOrb y salen nuevas; la tabla del 10/09 no conocía las de 596 de las 1.638
  no solucionadas y todas iban a «Otro». Ahora una etiqueta **que no está en la tabla** pasa por reglas
  de palabras clave (orden y excepciones revisados contra las etiquetas reales de 90 días: «tienda» no
  es técnico, «dependiente» no es «pendiente», la falta de permisos del agente es proceso, no atención…);
  si ninguna encaja, sigue en «Otro». La tabla manda siempre sobre las palabras.
- **Resultado** (05-09 → 04-10): proceso 904 (55,19 %), proceso con fallos de atención 494 (30,16 %),
  atención 140 (8,55 %), cliente 34, sin causa 66. En las dos causas con atención, la señal que más pesa
  es la rúbrica (85 %): ojo, la mediana de ítems fallados de las solucionadas es **1 de 7**, así que
  basta con fallar 2 para contar como «atención». Si al usuario le parece estricto, el umbral está en
  `Ensamblador` (mediana) y se puede subir.
- **Cubo v3**: tres cortes nuevos por día (`ver`, `dup`, `causas`) y la consulta de internet trae
  `conversationId` y la llamada física. La firma cambia (`…-v3-…`), así que al publicar **el cubo se
  vuelve a traer de BigQuery** (~1 min de «Preparando los datos»). Las pruebas que comparan con el
  Python de referencia dejan fuera solo lo nuevo; lo nuevo tiene sus pruebas (`CausaNoSolucionTests`).
- Ojo: la API de Storage de BigQuery dio una vez «failed to connect to all addresses» (red); la página
  reintenta sola y a la segunda trajo los datos.

---

## 4. Estructura del proyecto

```
C:\Proyectos\CDM Auditorias Calidad\
├─ CONTEXTO_IA.md                 ← este documento
├─ README.md                      ← presentación corta para GitHub
├─ docs\guia-de-estilos.md        ← guía de estilos SOLARIS · GAIA (referencia del aspecto)
├─ CDM Auditorias Calidad.sln     ← web + pruebas
├─ publicar.cmd / arrancar.cmd    ← publicación en este equipo (ver 5)
├─ publicacion\                   ← (sin versionar) app publicada + .env de producción
├─ capturas\                      ← (sin versionar) capturas con datos reales
│                                   (la carpeta «Power bi\» con el PBIP se quitó el 02-10-2026; en git, commit 2caa2bd)
├─ CDM Auditorias Calidad\        ← la web (ASP.NET Core MVC, .NET 8)
│  ├─ Program.cs                  ← cultura es-ES, servicios, rutas por atributo
│  ├─ appsettings.json            ← sección "Auditorias" (OpcionesAuditorias)
│  ├─ appsettings.Production.json ← RutaEnv = ..\.env (publicacion\.env)
│  ├─ .env / env.ejemplo          ← credenciales SQL (el .env no se versiona)
│  ├─ Consultas\Auditorias.sql    ← la consulta del PBI, copiada tal cual (sin ORDER BY)
│  ├─ Consultas\Nomina.sql        ← nómina por día con cargo (tarjeta «Total agentes»)
│  ├─ Datos\impedimentos_tabla.json ← No solución: etiquetas de impedimento → categorías (fija)
│  ├─ App_Data\                   ← (sin versionar) caché de No solución en desarrollo (~60 MB)
│  ├─ Controllers\
│  │  ├─ HomeController.cs        ← "/" portada (Menú) y "/error"
│  │  ├─ TableroController.cs     ← "/general", "/formacion", "/{pagina}/descargar", POST "/datos/recargar"
│  │  └─ NoSolucionController.cs  ← "/nosolucion" (+ /equipos, /motivos, /internet), los CSV y POST "/nosolucion/actualizar"
│  ├─ Models\                     ← Auditoria y RegistroNomina (filas), InstantaneaAuditorias,
│  │                                TableroModelo (+ TarjetaKpi, GrupoFiltro…), MenuModelo, CabeceraModelo
│  │  └─ NoSolucion\              ← PaginaNoSolucion (+ una por pestaña), FormatoNoSolucion
│  ├─ Infraestructura\            ← Formato (es-ES, espacio fino), Iconos (trazo + logotipo), EscalaGrafico
│  ├─ Servicios\
│  │  ├─ Configuracion\           ← LectorDotEnv, DatosConexion, OpcionesAuditorias, OpcionesNoSolucion
│  │  ├─ Datos\                   ← RepositorioAuditorias (SQL: auditorías y nómina), AlmacenAuditorias (memoria),
│  │  │                             RecargaPeriodica
│  │  ├─ Tablero\                 ← CalculadoraTablero (las medidas DAX y «Total agentes»), Periodos (fechas DAX),
│  │  │                             FiltrosTablero (URL), PaginaTablero (General / Formación)
│  │  ├─ Exportacion\             ← ExportadorExcel
│  │  ├─ NoSolucion\              ← (de ranking-mvc) FuenteBigQuery, CacheNoSolucion, Cubo, Ensamblador,
│  │  │                             Agregados, FiltrosCdm, ServicioCdm, ServicioNoSolucion; (de aquí)
│  │  │                             CausaNoSolucion (atención o proceso), ClasificadorEtiquetas
│  │  └─ Comun\                   ← (de ranking-mvc) FormatoPython, ExportacionCsv, Errores
│  ├─ Views\
│  │  ├─ Shared\_Layout, _Cabecera ← documento base (data-marca, tema.js) y cabecera común
│  │  ├─ Home\Index.cshtml        ← portada (solo «General»)
│  │  ├─ Tablero\Index            ← cabecera + _Informe
│  │  ├─ Tablero\_Informe         ← lo que se sustituye al filtrar: pestañas, panel, tira, rejillas
│  │  ├─ Tablero\_PanelFiltros, _ErrorDatos (_Desplegable está en Shared: lo usan los dos informes)
│  │  ├─ Tablero\Graficos\        ← _Columnas, _Linea, _Sectores (hbarras), _TopAuditores (ranking)
│  │  └─ NoSolucion\              ← _LayoutNoSolucion, _InformeNoSolucion (#informe), _PanelNoSolucion,
│  │                                Resumen, Equipos, Motivos, Internet, _LineasNs, _EncuestasDiarias,
│  │                                _BarrasNs (hbarras), _MuestrasNs
│  └─ wwwroot\                    ← css\site.css (variables de la guía), js\tema.js, js\site.js, favicon.svg
└─ CDM Auditorias Calidad.Tests\  ← xUnit: PeriodosTests, CalculadoraTableroTests, NoSolucionTests y
                                    CdmTests (de ranking-mvc, con Fixtures\nosolucion_python.json) y
                                    CausaNoSolucionTests (causa, repetidas, palabras clave); 290 casos
```

Parámetros de la URL: `desde`, `hasta` (yyyy-MM-dd), `mes` (yyyy-MM), `sector`, `super`,
`team`, `auditor`, `cargo`, `base` (repetibles), `vista` (`dia` / `semana` / `mes`).

`OpcionesAuditorias` (appsettings → `Auditorias`): `RutaEnv`, `MinutosRecarga` (30),
`SegundosConsulta` (300), `MetaCalidad` (0,5), `CargosFormacion` (filtro de la página
Formación), `CargosAgente` (quién cuenta como agente en «Total agentes»).

`OpcionesNoSolucion` (appsettings → `NoSolucion`): `Odbc` (`DSN=BQCOL;`), `RutaCache`
(`App_Data\cache_nosolucion.json`; en producción `..\datos\cache_nosolucion.json`), `Ventana` (90),
`DiasProvisionales` (5), `Tramo` (45), `RefrescoHoras` (12), `RutaTablaImpedimentos`.

Parámetros de `/nosolucion…`: `desde`, `hasta`, `servicio`, `supervisor`, `tl`, `agente`, `marca`
(repetibles); en Equipos, además, `nivel` (`supervisor` / `tl` / `agente`), `orden`, `dir` y `q`.

---

## 5. Cómo se ejecuta

- **Una sola dirección**: todo va por el **5180** (el usuario lo pidió así el 02-10-2026).
  - **Desarrollo**: abrir `CDM Auditorias Calidad.sln` en Visual Studio y F5 (perfil `http`), o
    `dotnet run --launch-profile http` en la carpeta del proyecto → `http://localhost:5180/general`.
    Lee las credenciales de `CDM Auditorias Calidad\.env`. Como producción usa el mismo puerto,
    hay que cerrar antes la ventana «CDM Auditorias Calidad (5180)».
  - Si hace falta probar algo sin parar producción, levantar una copia temporal en otro puerto
    (`ASPNETCORE_URLS=http://localhost:<puerto>`, `dotnet run --no-build --no-launch-profile`)
    y **pararla al terminar**: el usuario no quiere varias direcciones en marcha.
  - Las vistas Razor se compilan con el proyecto: tras cambiar un `.cshtml` hay que
    recompilar y reiniciar. CSS y JS se sirven directamente (basta recargar el navegador).
  - Antes de compilar hay que parar la web en marcha que use `bin\Debug`: la DLL queda bloqueada.
- **No solución necesita el DSN de ODBC `BQCOL`** (driver Simba de BigQuery, DSN de usuario, con
  la cuenta de Google del usuario): el mismo que usa ranking-mvc. Si falta, la página se queda en
  «Preparando los datos» y enseña el error del último intento. La primera vez (sin caché) tarda
  ~1 min en traer los datos.
- **Pruebas**: `dotnet test "CDM Auditorias Calidad.sln"` (con la web parada).
- **Publicación** (la del 5180, en marcha con la versión actual desde el 02-10-2026):
  1. Cerrar la ventana «CDM Auditorias Calidad (5180)».
  2. `publicar.cmd` → compila en Release en `publicacion\app` y, si no existe, copia el `.env`
     del proyecto a `publicacion\.env`.
  3. `arrancar.cmd` → abre una ventana que sirve en `0.0.0.0:5180` →
     `http://10.148.223.143:5180` (ranking-mvc ya usa el 5173).
  - Desde una consola, lanzar los `.cmd` con la ruta completa
    (`cmd /c "C:\Proyectos\CDM Auditorias Calidad\publicar.cmd"`); con el nombre solo, en este
    entorno, cmd no los encuentra. Doble clic también vale.
- **Capturas sin ventana** (para revisar el aspecto): Edge sin cabeza,
  `msedge --headless=new --force-prefers-reduced-motion --blink-settings=preferredColorScheme=1 --window-size=1680,1000 --screenshot=salida.png http://localhost:5180/general`
  (`preferredColorScheme=0` para el tema oscuro). Sin `--force-prefers-reduced-motion` la
  captura sale a mitad de las animaciones de entrada (y con `--virtual-time-budget` se cuelga).
  No baja de ~500 px de ancho; para móvil, mejor el panel del navegador en modo móvil. Si el
  panel del navegador está oculto mide 0 px: darle tamaño (`resize_window`) antes de medir.
- **GitHub**: repositorio privado `https://github.com/brianestivenreyjcol-rgb/CDMAuditorias`
  (rama `main`; identidad de git configurada solo en este repositorio). No se suben `.env`,
  `publicacion\`, `capturas\`, `bin`/`obj`/`.vs`, `*.user` ni la caché del PBI
  (`.pbi/cache.abf`, que lleva datos). **Si el repo pasa a público, quitar antes lo interno**
  (IP de servidores en este MD y en `env.ejemplo`, el PBIP y la consulta).

---

## 6. Verificación hecha

- **01-10-2026**: cifras de las tarjetas cuadradas con SQL directo sobre la misma consulta
  (General, sin filtros, datos 01-08 → 01-10): total 15.148 (▲106,0 % frente a 7.355 del 01-08
  al 01-09), semana 883 (28-09 → 01-10) frente a 1.017 (21-09 → 24-09) = ▼13,2 %, mes 28 frente
  a 166 = ▼83,1 %, nota 50,0 % frente a 47,8 % = ▲2,2 pp.
- Excel del sector «CO Atención Jazztel»: 1.549 filas = la barra del gráfico.
- Interacción en el navegador: clic en sector, vista Mes, Atrás, borrar filtros, «Actualizar»
  (recarga y conserva filtros). Sin errores de consola.
- Publicación probada en local (Production, puerto 5181 solo en localhost) y luego parada.
- **02-10-2026, guía de estilos**: capturas en claro y oscuro (portada, General, Formación a
  1366 px); botón de tema (auto → claro → oscuro → auto), fichas con guía vertical, tabla
  coloreada y ordenable, «Más filtros» con contador; sin desbordes a 375 px (portada, General,
  Formación) y sin errores de consola. Revisado que no hay colores escritos en las vistas, ni
  `<style>`/`<script>` en ellas, ni emojis, ni negritas de más de 600.
- **02-10-2026, «Total agentes»** cuadrado con SQL directo (General, sin filtros, 01-08 → 02-10):
  1.720 agentes en nómina, 1.487 con auditoría (86,45 %).
- **02-10-2026, filtros**: a 1440 × 900, el desplegable de Mes queda por encima de las
  tarjetas (medido con `elementFromPoint` en tres puntos que pisan el contenido) y el de Cargo
  (en «Más filtros») se abre hacia arriba dentro de la pantalla. Marcar «Septiembre» filtra al
  momento (15.340 → 7.933) con el desplegable abierto y el foco en la casilla; marcar otra da
  «Varias selecciones (2)»; «Quitar selección» vuelve a todo; Escape cierra.
- **02-10-2026, período anterior**: con `?mes=2026-09`, septiembre 7.933 frente a agosto 7.189 =
  +10,35 % y nota 51,54 % frente a 48,30 % = +3,25 pp, igual que con SQL directo; sin filtros,
  «Sin período anterior con datos».
- 30 pruebas unitarias en verde (fechas DAX, período anterior, tarjetas, «Total agentes»,
  filtros en cascada, top 10, vistas, detalle, URL).
- **05-10-2026, CDM No solución** (copia temporal en el 5190, ya parada; producción sin tocar):
  - El cubo se trajo de BigQuery desde esta web (dos tramos, ~1 min, 60 MB) y la página pasó sola de
    «Preparando los datos» al informe.
  - Últimos 30 días (05-09 → 04-10): 15,00 % de no solución, 15.612 no solucionadas de 104.060
    encuestadas, 364.793 llamadas, cruce con nómina 99,98 %. El CSV general trae 15.612 filas, igual
    que la tarjeta; el de un impedimento y los de equipos y tipologías también descargan.
  - Marcar «YOIGO» en Marca filtra al momento (sinAccesoInternet: 6.018 → 1.679 encuestadas) con el
    desplegable abierto; el buscador de Equipos y las filas desplegables funcionan.
  - Capturas en claro y oscuro de las 4 pestañas y la portada; sin desbordes a 375 px; sin errores de
    consola. El informe General de Auditorías sigue igual (se tocaron piezas compartidas).
  - «Copiar ID» no se pudo pulsar con un clic real (el panel del navegador no pintaba); usa lo mismo
    que ranking-mvc (portapapeles y, por HTTP con IP, `execCommand('copy')`).
  - 240 pruebas en verde (las de Auditorías y las de No solución traídas de ranking-mvc, que
    comparan con la salida del Python de referencia).

---

## 7. Pendientes y decisiones abiertas

- **Publicar la causa de cada no solución** (05-10-2026, tarde): el 5180 sirve la primera versión de
  No solución (publicada por el usuario a las 12:43). Al publicar, el cubo pasa a v3 y se vuelve a
  traer (~1 min). Revisar con el usuario el umbral de la rúbrica (1 de 7) y, de vez en cuando, las
  etiquetas que siguen en «Otro» (`ClasificadorEtiquetas`).

- **CDM No solución en el 5180**: el usuario publicó la primera versión el 05-10-2026 a las 12:43
  (caché en `publicacion\datos`). Para lo siguiente: cerrar la ventana «CDM Auditorias Calidad (5180)»,
  `publicar.cmd`, `arrancar.cmd`.
- **No solución depende del DSN `BQCOL` de usuario** (la cuenta de Google del usuario), igual que
  en ranking-mvc. En la torre nueva hay que volver a crearlo. Para entregar la web haría falta una
  cuenta de servicio de BigQuery.
- **Dos copias de No solución** (aquí y en ranking-mvc): un arreglo en una no llega a la otra. Si
  ranking-mvc deja de usarse, esta pasa a ser la única.

- **Producción (5180)**: al día con la versión actual desde el 02-10-2026 (el usuario pidió una
  sola dirección que funcione: `http://localhost:5180/general`). Para los próximos cambios:
  cerrar la ventana «CDM Auditorias Calidad (5180)», `publicar.cmd` y otra vez `arrancar.cmd`.
  Un `publicar.cmd` con la web en marcha falla al copiar la DLL (y deja copiados algunos
  ficheros sueltos antes de fallar).
- **Credenciales personales**: la web usa el login de SQL del usuario (el de `DB_2` de
  ranking-mvc). Para entregarla, pedir una cuenta de servicio con SELECT en
  `Reporting.WO.AuditoriasWhatsapp/Jazztel/Orange`,
  `ModulosIceberg.Calidad.PlantillaCalidadUnificada` y
  `Planificacion.Nomina.(NominaAntiguedad, Nomina_User_Avaya, PD_Usuarios)`.
- **Acceso**: no hay inicio de sesión. Decidir si hace falta (ranking-mvc tiene uno).
- **SOLARIS**: la guía es de la plataforma SOLARIS · GAIA. Si esta web pasa a formar parte de
  ella, falta su logotipo (`_MarcaSolaris`) en la portada y quizá el login de SOLARIS.
- **Histórico: 3 meses atrás + el mes en curso (pedido del usuario el 02-10-2026)**.
  - ICEBERG: el usuario cambió él mismo en `Consultas/Auditorias.sql` el filtro de `- 2` a
    `- 3`: hoy, desde el 01/07 (hay datos desde el 04/07). **Activo** en desarrollo y en
    producción desde el 02-10-2026: 20.350 auditorías del 04/07 al 02/10, julio = 5.009 (solo
    ICEBERG), igual que con SQL directo.
    - Por qué al principio no salía: la web leía la copia de `bin\…\Consultas`, que la
      compilación no había refrescado. Ahora lee la de la carpeta de la aplicación en cada
      carga y el `.csproj` copia las consultas siempre (`Always`).
    - En producción (la versión anterior, que aún lee de su carpeta de binarios) se sustituyó
      solo `publicacion\app\Consultas\Auditorias.sql` por el nuevo y se pulsó «Actualizar»; no
      se reinició nada. Para volver a la anterior (`- 2`), está en git:
      `git show 2caa2bd:"CDM Auditorias Calidad/Consultas/Auditorias.sql"`.
  - WEB (WhatsApp, Jazztel, Orange): las tablas de origen empiezan el 01/08/2026, no hay julio.
    **El usuario va a pedir que las tablas WEB traigan 4 meses y avisará**: no investigar más
    hasta entonces. La consulta no filtra fechas en WEB, así que en cuanto el origen tenga
    julio, saldrá; si luego trae más de 4 meses, habría que poner a WEB la misma ventana.
  - Visto de paso (sin usar): `Reporting.WO.Auditoria_Grupo1/3/5` y `Auditoria_YGMMKRTV` tienen
    auditorías de julio, pero son otros formularios, no están en el PBI y `Grupo3` llega hasta
    el 08/09 (se solaparía con las tablas nuevas). La vista `WO.vw_auditorias` está rota (usa
    `Auditoria_Grupo4`, que no existe).
- Cuidado al comparar agosto con julio mientras WEB no traiga julio: julio solo tiene ICEBERG,
  así que la variación de agosto frente a julio sale inflada (con el filtro Base = ICEBERG es
  justa).
- Ideas no hechas: exportar también legajo e ID de llamada en el Excel.

---

## 8. Bitácora

(Lo más reciente al final.)

- **01-10-2026** — Análisis del PBI completo (sección 2). Probada la conexión de lectura con
  la cuenta de DB_2 de ranking-mvc: lee las 4 tablas de origen y la consulta completa (~2 s).
- **01-10-2026** — Web construida sobre la plantilla MVC del usuario: portada, General,
  Formación & Calidad, filtros, 4 gráficos, Excel, recarga periódica, pruebas, scripts de
  publicación.
- **02-10-2026** — Aspecto rehecho con la guía de estilos SOLARIS · GAIA (sección 3 bis): tema
  claro/oscuro, cabecera de 72 px, pestañas, panel de 268 px con «Más filtros», tira de
  indicadores con icono, barras con la nota en pareja, tabla ranking ordenable con semáforo,
  fichas, animaciones y portada ejecutiva.
- **02-10-2026** — Proyecto subido a GitHub (repositorio privado; el usuario lo pasó de público
  a privado antes de subir).
- **02-10-2026** — Portada solo con «General». Tarjeta «Agentes auditados» sustituida por
  «Total agentes» (nómina frente a auditados; nueva consulta `Nomina.sql`, sección 2.2 bis).
- **02-10-2026** — El usuario arrancó producción en el 5180 (versión anterior; sin actualizar).
- **02-10-2026** — Filtros: los desplegables quedaban detrás de las tarjetas (arreglado con
  `z-index` en el panel pegajoso); ahora filtran al marcar y siguen abiertos; se abren hacia
  arriba si no caben; sin animaciones de entrada al filtrar. Documentación repasada entera.
- **02-10-2026** — La variación de «Total auditorías» y de la nota compara con el mismo tiempo
  justo antes (el DATEADD -1 MONTH del PBI daba +102 % con el rango completo).
- **02-10-2026** — Julio: ICEBERG con 3 meses atrás + el mes en curso (`- 3`, cambio del usuario)
  activo en desarrollo y producción; las consultas se leen de la carpeta de la aplicación en cada
  carga. WEB sin julio en origen: el usuario pedirá que traiga 4 meses y avisará. El usuario
  quitó la carpeta `Power bi\` del proyecto (subido el borrado; sigue en el historial).
- **02-10-2026** — El buscador de los desplegables filtra mientras se escribe (antes ocultaba con
  `[hidden]` pero el CSS lo anulaba).
- **02-10-2026** — Una sola dirección: el 5180 (también el perfil de Visual Studio); producción
  publicada con la versión actual y comprobada (`localhost` y la IP del equipo, julio en el
  filtro: 5.530 auditorías, todas ICEBERG; datos del 01/07 al 02/10, 20.874 auditorías).
- **05-10-2026** — **CDM No solución** traído de ranking-mvc a `/nosolucion` (sección 3 ter): motor de
  BigQuery, caché y cálculos copiados con sus pruebas; controlador, modelos y vistas rehechos con las
  piezas de este proyecto; tarjeta en la portada. Probado en una copia temporal (5190) con datos
  reales; producción (5180) sin tocar, a la espera del usuario.
- **05-10-2026** — `docs/guia-de-estilos.md` actualizada (el usuario la volvió a pasar, idéntica): las
  secciones 1–8 siguen siendo las de SOLARIS y la nueva sección 9 recoge cómo se aplica aquí, las
  variables añadidas y las piezas de Auditorías y de No solución. La misma copia quedó en su carpeta
  de Descargas.
- **05-10-2026** — Sin acceso a internet: **una causa por llamada** (atención, proceso, las dos, cliente,
  sin causa) con las reglas del proceso de soporte de YGMM, CSV de cada llamada con su causa, tarjeta
  «¿Hay llamadas repetidas?» (no hay: lo que sumaba de más eran llamadas con varios impedimentos) y
  etiquetas nuevas de DataOrb clasificadas por palabras clave. Cubo v3. 290 pruebas. Probado en el 5190
  con datos reales; sin publicar.
