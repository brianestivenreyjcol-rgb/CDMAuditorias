# CDM Auditorías Calidad — contexto para continuar el trabajo

> Lo lee la IA al empezar cada sesión: es corto a propósito. El detalle está en `docs/contexto/`,
> un fichero por tema: **léelo solo cuando vayas a tocar ese tema**. Al cambiar algo, actualiza aquí
> el estado o los pendientes si cambian, y el detalle en su fichero; lo hecho va a
> `docs/contexto/bitacora.md` (una entrada corta, lo más reciente al final).
> Idioma del usuario: **español**, en todo (lo que ve, los avisos intermedios, código y comentarios).

## Qué es y dónde está

- El Power BI «CDM Auditorías Calidad» pasado a web ASP.NET Core MVC (.NET 8, Razor): **General** y
  **Formación & Calidad**. Desde el 05-10-2026, también **CDM No solución** (`/nosolucion`), traído de
  ranking-mvc (son dos copias independientes del motor).
- Proyecto: `C:\Proyectos\CDM Auditorias Calidad\` (`CDM Auditorias Calidad.sln`: web + pruebas).
- GitHub privado `brianestivenreyjcol-rgb/CDMAuditorias`, rama `main` (commit y push tras cada tanda,
  sin `.env`, `publicacion\`, capturas ni cachés). Si pasara a público, quitar antes lo interno.
- Aspecto: `docs/guia-de-estilos.md` (SOLARIS · GAIA + sección 9, lo de esta web). Para el front está
  la skill y el agente **`frontend-solaris`** (copias para compartir en `docs/compartir/`); si la guía
  cambia, recopiarla en la skill y regenerar el `.skill`.

## Datos

- Auditorías: SQL Server `10.148.226.40\REPORTING`, `Consultas/Auditorias.sql` (la del PBI) y
  `Consultas/Nomina.sql`; en memoria, recarga cada 30 min; las consultas se leen en cada carga
  (editar el `.sql` + «Actualizar»). Ventana: 3 meses atrás + el mes en curso (ICEBERG con `- 3`).
  Las tablas WEB empiezan el 01/08: **el usuario avisará** cuando traigan 4 meses; no investigar.
- No solución: BigQuery por ODBC (DSN de usuario `BQCOL`), cubo de 90 días en disco (`App_Data` en
  desarrollo, `publicacion\datos` en producción), se renueva solo cada 12 h. Formato del cubo: **v3**.
- Credenciales en `CDM Auditorias Calidad\.env` y `publicacion\.env` (login personal del usuario;
  nunca decirlas en el chat).

## Producción y pruebas (una sola dirección: el 5180)

- `http://10.148.223.143:5180` (compañeros) = `http://localhost:5180`: ventana «CDM Auditorias Calidad
  (5180)» que abre `arrancar.cmd` sobre `publicacion\app`. Al día desde el **06-10-2026**.
- **Publicar** (solo si el usuario lo pide): cerrar esa ventana → `cmd /c "C:\Proyectos\CDM Auditorias
  Calidad\publicar.cmd"` → `arrancar.cmd` con ruta completa. Si cambia el formato del cubo de No
  solución, copiar antes la caché de `App_Data` a `publicacion\datos` para no esperar a BigQuery.
- **Probar** en una copia temporal en otro puerto (p. ej. 5190: `ASPNETCORE_URLS=http://localhost:5190
  dotnet run --no-build --no-launch-profile`) y **pararla al acabar**. Parar la web antes de compilar
  (la DLL queda bloqueada). Pruebas: `dotnet test "CDM Auditorias Calidad.sln"` (312 en verde).
- Capturas: Edge sin ventana con `--force-prefers-reduced-motion` (o `scripts/capturas.py` de la skill).
  Si el usuario dice «sin capturas», verificar con texto y medidas: las imágenes gastan muchos tokens.

## Decisiones del usuario que no hay que deshacer

- Variación de «Total auditorías» y de la nota: frente al mismo tiempo justo antes (no el DATEADD del PBI).
- Filtros que se aplican al marcar, con el desplegable abierto; portada con «General» y «CDM No solución».
- «Total agentes» = nómina (Agente, Agente en Capacitacion, Aprendiz Sena Etapa Productiva) vs. auditados.
- No solución, «Sin acceso a internet»: **una causa por llamada** (proceso, proceso con fallos de
  atención, atención, cliente, sin causa) con las reglas del proceso de soporte de YGMM
  (`C:\Proyectos\Soporte YGMM`); etiquetas nuevas de DataOrb por palabras clave; no hay llamadas
  repetidas (lo que sumaba de más eran llamadas con varios impedimentos).
- Gráficas: **cada punto con su valor en una pastilla** del color de su serie, sin «máx./mín./media»
  (06-10-2026, sustituye a las etiquetas de impacto). Tablas: **total al pie** (`tfoot`, «TOTAL», pegado
  abajo), nunca arriba. El color de la página **sigue a la marca filtrada**: Orange naranja, YOIGO/MASMOVIL
  morado, Jazztel `#FFD200`. Reglas en la skill frontend-solaris y en la guía (2.1, 4 y 9.4).
- **Aspecto = el del portal SOLARIS** (06-10-2026, guía 9.9): cabecera con migas, Imprimir / Presentar / Tema, pestañas en barra, panel
  de filtros plegable (`cdm-panel`), tarjetas con cabecera y pie, tablas con puesto, barrita de volumen y TOTAL al pie, gráficas con
  cada punto en pastilla (piezas en `Views/Shared`), medidor y anillos en GAIA → Estilo; el color de No solución también sigue a la marca.

## Pendientes

- Revisar con el usuario el umbral de la rúbrica para «atención» (hoy basta con fallar 2 de 7) y, de
  vez en cuando, las etiquetas de impedimento que siguen en «Otro».
- Cuenta de servicio para SQL y BigQuery (hoy van con el login y el DSN personales del usuario); en la
  torre nueva hay que recrear el DSN `BQCOL`.
- Decidir si hace falta inicio de sesión; si la web entra en SOLARIS, su logotipo y su login.
- Julio de WEB cuando avise el usuario (hasta entonces, agosto frente a julio sale inflado).
- **GAIA Formación** (`/gaia`, desde el 06-10-2026): PBI en `Power bi\GAIA Formación.pbip`; llamadas de
  BigQuery filtradas por el Excel de nómina de la compartida (filtro exacto agente + día, fallos del PBI
  corregidos; se recarga sola al guardar el Excel). Las 9 pestañas (Resumen, Ranking, Estilo,
  Rendimiento, Evolución, Comercial, Motivos, Españolización, Llamadas) hechas y probadas en el 5190;
  **sin publicar**. Todo en `docs/contexto/gaia-formacion.md`.
- Detalle de todo esto: `docs/contexto/pendientes.md`.

## Dónde está el detalle (`docs/contexto/`)

| Fichero | Léelo cuando… |
|---|---|
| `gaia-formacion.md` | toques la vista GAIA Formación (análisis de su PBI, Excel de nómina, plan por fases) |
| `power-bi.md` | toques medidas, fechas DAX o la consulta de auditorías (análisis del PBI y cómo se pasó cada medida) |
| `decisiones.md` | toques el informe de Auditorías: filtros, tarjetas, top 10, Excel, estilo aplicado |
| `no-solucion.md` | toques No solución: fuente, cubo, pestañas, causas atención/proceso, palabras clave |
| `estructura-y-ejecucion.md` | necesites saber qué hay en cada carpeta o cómo se ejecuta y publica con detalle |
| `verificacion.md` | quieras cuadrar cifras con lo que ya se comprobó contra SQL o BigQuery |
| `pendientes.md` | vayas a cerrar un pendiente (historia de cada uno) |
| `bitacora.md` | necesites saber qué se hizo y cuándo |
