# Pesos y metas por sector (`/pesos`, 07-10-2026)

> Parte del contexto de CDM Auditorías Calidad: el resumen está en `CONTEXTO_IA.md` (raíz del proyecto).

Pedido del usuario: «sacar los pesos de los Excel… por mes leer el último archivo más actualizado desde V3 y sacar los pesos de
los objetivos por sector… con un botón que analice y valide». La tabla es como la del catálogo de KPI de Clan: sector, KPI, peso,
meta 0 %, (60 %), 100 % y 150 %.

## De dónde sale

- Carpeta `\\172.16.232.102\incentivos\INCENTIVOS JAZZPLAT` (la unidad `Y:` del usuario; `Pesos:RutaRaiz`). Un año por carpeta
  y un mes por subcarpeta (`09. SEPTIEMBRE`); **los datos son del mes anterior** (en septiembre se paga agosto): la vista enseña el
  mes de los datos. Dentro, `01. RANKING` con `01. PRECIERRE`, `02. V1`, `03. V2`, `04. VF` y una carpeta por responsable.
- De cada sector se lee **la versión más reciente**: la carpeta (VF > V2 > V1 > precierre), luego la marca del nombre (VF, V3, V2,
  V1) y, si empatan, la fecha. La clave del sector quita «Pre Cierre» (y la errata «Prre»), «Ranking», versión, mes, signos y el
  número de orden que algunos ponen delante («1.PreCierre…»). Se descartan temporales (`~$`), copias («… -.xlsb», «Original»,
  «Estilo», «excepción»), plantillas y consolidados.
- Los consolidados (`01. Consolidado de Incentivos … $$ V3`) traen sueldos e incentivos por persona: **no se usan**.
- **Los .xlsb no se pueden leer** (6–16 ficheros según el mes, de Sebastián y Debinson): salen como «no legible: guárdalo como
  .xlsm o .xlsx».

## Cómo se encuentra el bloque

`Servicios/Pesos/LectorCabeceras.cs` lee con OpenXML (SAX) solo las primeras 40 filas × 200 columnas de cada hoja, con el valor
guardado de cada celda (los ficheros pesan hasta 20 MB). `ExtractorPesos` busca la **cabecera de metas**: tres o más cifras seguidas
que empiezan en 0, incluyen el 1, crecen y no pasan de 2 (`0 | 1 | 1,5` o `0 | 0,6 | 1 | 1,5`; a veces en texto). No se busca el
rótulo porque cambia («Peso», «Ponderacion», « Peso», nada en Atención YGMM). La columna de la izquierda es el peso y la siguiente el
KPI; debajo, una fila por KPI hasta la primera sin nombre **o hasta otra cabecera** (Técnico Orange apila un bloque por skill). El
nivel sale del nombre de la hoja por palabras enteras (JS/Jefe, SP/Super, TL/Team, el resto Agente: «Ranking Agentes TLT» es Agente).
Los bloques «Indicador | Objetivo | Peso» de TL/SP son aceleradores y no entran (no tienen cabecera de metas).

## Validación (`PaginaPesos.Validar` y `ValidarSuma`)

- **Suma por hoja**: vale si cada bloque suma 100 % o si todos juntos suman 100 %; los bloques con todos los pesos a 0 no cuentan
  (nota «este mes no se aplica»).
- KPI con peso al que le falta alguna meta → revisar; si le faltan **todas** («Ponderación Equipo AG», «Promedio 3M») → nota: se
  calcula aparte. Metas que no van en un solo sentido → revisar. KPI sin peso → nota (requisito o llave).
- Primer análisis de octubre de 2026 (datos de septiembre): 27 ficheros, 19 leídos, 571 KPI con peso, 20 para revisar (la mitad,
  .xlsb), 55 notas.

## Código y uso

- `Servicios/Pesos/` (`LectorCabeceras`, `ExtractorPesos`, `ArchivosRanking`, `ServicioPesos`), `Models/Pesos/PaginaPesos.cs`,
  `Controllers/PesosController.cs` (`/pesos`, POST `/pesos/analizar`, `/pesos/csv`), `Views/Pesos/`. Pruebas: `PesosTests.cs`.
- Caché `App_Data\cache_pesos.json` / `publicacion\datos\cache_pesos.json` (versión 1), un análisis por mes. Un mes sin analizar se
  analiza solo al abrirlo; «Analizar y validar» lo repite (tarda un par de minutos; la página enseña el progreso y se recarga sola).
- Filtros: Sector, Nivel, Responsable y «Todo / Para revisar». Meses: los 6 últimos en la barra (`?mes=2026-09`, la carpeta).

## Cambios pedidos por el usuario (07-10-2026, tarde)

- **Solo agentes**: se enseñan solo los bloques de hojas de nivel Agente; los de TL, supervisor y jefe de servicio se leen (siguen en la
  caché) pero no salen. Un fichero sin bloques de agente sale como «Sin agentes» y con una nota. Ya no hay filtro de nivel.
- **Meses como la carpeta**: la barra enseña «09. SEPTIEMBRE» (la ficha y la nota dicen de qué mes son los datos).
- **Ruta para validar**: cada fila lleva «Copiar ruta» y la tabla de ficheros enseña la ruta relativa con «Copiar». La ruta se escribe con la
  unidad del usuario (`Pesos:RutaVisible` = `Y:\INCENTIVOS JAZZPLAT`) aunque se lea por la ruta de red; también va en el CSV.

