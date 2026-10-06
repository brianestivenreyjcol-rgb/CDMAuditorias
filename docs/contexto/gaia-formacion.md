# GAIA Formación: análisis del Power BI y plan de migración

> Parte del contexto de CDM Auditorías Calidad (resumen en `CONTEXTO_IA.md`). Léelo cuando toques la
> vista GAIA Formación. Escrito el 06-10-2026, antes de empezar a programar.

## 1. Qué es

El Power BI `Power bi\GAIA Formación.pbip` (lo añadió el usuario el 06-10-2026) sigue a los agentes
nuevos durante su formación: las llamadas que hacen en sus días de **preconexión** y
**aseguramiento**, con las métricas de DataOrb (adherencia al estilo, rellamada, no solución, ventas,
motivos, «españolización» del vocabulario…). Se quiere como **vista nueva** de esta web.

## 2. De dónde salen los datos

| Tabla del PBI | Origen | Qué trae |
|---|---|---|
| `Nomina` | Excel `\\172.16.232.102\Proyecto Ranking\2.GAIA\Formación\Nomina Iniciales General.xlsx`, hoja **Oleadas** | Un agente por fila: Sector, Supervisor, Coordinador, Personal, Cargo, Legajo, `1/2/3 Preconexion`, `Aseguramiento 1…6`, Formador, Oleada, Login, `ID_Agente ` (con espacio al final), Ventas, CDA. El PBI lo despivota: una fila por agente y fecha con su «Tipo de Conexión». |
| `DatosBQ` | BigQuery (DSN `BQCOL`), `mo-vendor-management-reporting.JZZBOGOTA.ALL_dataorb_to_mo_insight_all_data_flattened_GAMMA` | Una fila por llamada, ~70 columnas de DataOrb, traducidas al español en Power Query. |
| `Encuestas` | BigQuery, `mm-cockpit-reporting.atc.REP_ALL_IVR_LLAMADAS_TIPIS` (YOIGO y MASMOVIL, colas JAZZBOG, encuesta pedida) | Se cruza con `DatosBQ` por `conversation_id + "_" + agent_step_order` = `externalConversationId`. |
| `Actualización de llamadas` | BigQuery, la misma GAMMA | Primera y última fecha por marca (la portada «Actualización BD»). |
| `Españolización` | BigQuery, `mo-customer-ops-reporting.smartops_prod.ALL_dataorb_to_mo_insight_all_data_flattened_GAMMA` | Lee la transcripción (`messages`) del agente y marca 41 palabras: 18 de España («vale», «coger», «móvil»…) y 23 de Colombia («qué pena», «celular», «de pronto»…). |

**Cómo se actualiza hoy:** `Generador.py` (en la misma carpeta compartida) lee el Excel y escribe
`Consulta.sql` con dos listas a mano: todas las fechas del Excel y todos los `ID_Agente`. Esas listas
se pegan en la consulta del PBI (`DatosBQ` y `Españolización`). `Encuestas` tiene otra lista de
fechas a mano que no se ha vuelto a pegar desde junio.

**En la web:** la web lee el Excel directamente, arma ella el filtro y consulta BigQuery. Con
**guardar el Excel** basta: no hay que tocar SQL ni ejecutar el generador.

## 3. Lo que hay que saber antes de copiar el PBI (problemas encontrados)

1. **Filtro cruzado: 8 veces más llamadas de las que tocan.** El PBI pide «cualquier fecha del Excel»
   × «cualquier agente del Excel», así que trae llamadas de un agente en días de formación de *otros*.
   Medido el 06-10-2026 con el Excel de ese día (356 agentes con ID, 97 fechas, 2.408 pares
   agente-día): **110.718** llamadas con el filtro del PBI frente a **13.461** con el filtro exacto
   (cada agente solo en sus días); 307 agentes tienen llamadas. El PBI solo recorta a lo exacto
   cuando se filtra por un campo de la nómina (la relación es por la llave `fecha + id`); sin
   filtros, los totales cuentan las 110.718. **Decisión del usuario** (sección 6).
2. **«Abruptas» siempre vale 0**: la consulta pone `0 AS interactionFlags_abruptlyEnded`. Por eso
   «% Abruptas» y «Llamadas cortadas» salen siempre a 0 y la regla «si la llamada se cortó, el
   criterio es N/A» de la adherencia nunca se aplica. En GAMMA existe el campo de verdad.
3. **«% Envío encuestas» vacío desde julio**: la lista de fechas de `Encuestas` acaba el 17-06-2026.
4. **«Españolización»** tiene sus fechas e ID pegados a mano (los mismos que `DatosBQ`) y lee
   transcripciones completas: es la consulta más pesada.
5. **Denominadores raros**: `% Transferencia` divide entre `Transferencia = 1 || Rellamada72hr = 0`
   y `% Rellamada 24h` entre `Rellamada24hr = 1 || Rellamada72hr = 0` (lo lógico sería el mismo campo
   = 0). Preguntar antes de corregir.
6. `Rellamada24hr` es una columna calculada que busca otra llamada del mismo cliente en las 24 h
   anteriores **dentro de lo descargado**: cambia si cambia el filtro (punto 1).
7. **Campos rotos** en páginas copiadas de otro informe: `Team`, `Mes`, `Semana`, `PLATAFORMA`,
   `sec_descrip`, `AntiguedadLinea`, `Color de Letra por Marca`, `FECHA_LLAMADA`, `fecha_seg`.
   No existen en el modelo: esos segmentadores no hacen nada.
8. **El Excel**: 23 filas sin `ID_Agente` (no salen en BigQuery), 8 ID repetidos, `CDA` vacío,
   `Aseguramiento 4–6` solo en parte de las filas (el generador las usa; el PBI las despivota sin
   tipo). La segunda hoja (`Hoja1`) guarda **usuarios y contraseñas**: la web solo leerá `Oleadas`
   y el contenido de `Hoja1` no se copia ni se documenta.

## 4. Medidas (cómo pasarlas)

- **Adherencia al estilo** = Saludo 10 % + Soy claro y fiable 15 % + Solucionó 25 % + Resumió 20 %
  + Confirmó solución 20 % + Despedida 10 %. Cada criterio sale de la calificación de DataOrb
  (`yes` → 1, otro → 0, N/A si la llamada se cortó). Aparte, sin peso: Escuchó (habla < 80 %, se
  pisaron < 10 veces, reconoció al cliente) y Buscó información (silencio ≤ 50 % de la duración).
  Colores de la barra: ≤ 40 % naranja, ≤ 70 % rojo, verde por encima.
- **KPIs**: % Rellamada 72 h (`enh_Redial_72h` 1 / 1+0), % No solución (`enh_Resolution_request`
  2 / 1+2), % Transferencia, % Churn (riesgo «Alto» / total), % Ofrecimientos (intento de venta /
  llamadas), reactivo/proactivo (con o sin `inboundSalesLead`), alineados/no alineados, % Ventas
  (`enh_Has_Sales_Bool`), sentimiento final positivo/negativo, TMO (media de `duration`), llamadas
  < 60 s, 60–180 s, sin contexto, % resuelto/pendiente/no se puede cumplir, % envío de encuestas.
- Los «parámetros de campo» del PBI (elegir qué KPI se ve en el eje) pasan a un desplegable.
- Las traducciones de Power Query (riesgo, sentimiento, motivos, resultado de venta…) se pasan a un
  diccionario en C#, igual que en No solución.

## 5. Páginas del PBI (17; solo «GAIA» es visible, el resto por botones)

| Página | Qué enseña | Fase |
|---|---|---|
| GAIA (portada) | Botones a cada página y «Actualización BD» por marca | 2 |
| Base | Tabla de llamadas con todos los campos y filtros | 2 |
| Ranking | Agentes con todos los KPI («Ranking general» y «Tipológico») | 2 |
| Estilo / Estilo Tabla | Adherencia y sus criterios: donas, barra, tabla por agente y fecha | 2 |
| Rendimiento | TMO, cortas, sin contexto, rellamada por día/semana | 3 |
| Cronológico / Comparativo | KPI elegido por día; agentes en dispersión | 3 |
| Ventas / Detalle Comercial / Alineamientos | Ofrecimientos, ventas, resultado, alineación | 3 |
| Motivos de Contacto / Mapa de afectación | Motivos (zoom 1-2-3), obstáculos, KPI por motivo | 3 |
| Españolización (+ Novato, Aficionado, Experto) | Uso de palabras de España y Colombia por agente | 4 |

Filtros comunes: Fecha, Marca, Sector, Oleada, Formador, Supervisor, Agente y Tipo de conexión
(Preconexión 1-3, Aseguramiento 1-6).

## 6. Decisiones que tiene que tomar el usuario

1. **Filtro exacto** (cada agente en sus días; recomendado) **o el cruce del PBI** (sección 3, punto 1).
2. ¿Se **corrige** Abruptas (campo real), las encuestas (sin lista de fechas) y los denominadores
   de transferencia y rellamada 24 h, o se replica el PBI tal cual para cuadrar primero?
3. Dirección y nombre: propuesta `/gaia` («GAIA Formación») con su tarjeta en la portada.
4. Orden de páginas (propuesta en la sección 5).

## 7. Plan de trabajo

**Fase 0 — Decisiones y base (½ día).** Respuestas de la sección 6. Copia del Excel para pruebas en
`App_Data` (sin versionar). Anotar las cifras del PBI de un periodo para cuadrar (fase 5).

**Fase 1 — Datos (1–2 días).**
- `LectorNominaGaia` (ClosedXML, ya está en el proyecto): abre el Excel en solo lectura (o una copia
  temporal si está abierto en Excel), solo la hoja `Oleadas`, recorta espacios de los encabezados y
  devuelve los agentes y los pares (agente, día, tipo de conexión). Avisa de filas sin ID o repetidas.
- `Consultas/GaiaLlamadas.sql`, `GaiaEncuestas.sql`, `GaiaActualizacion.sql`, `GaiaEspanolizacion.sql`
  con un marcador `{{PARES}}` que la web sustituye por la lista del Excel (`UNNEST` de pares y
  `JOIN`, o el cruce si el usuario lo prefiere). Se leen en cada carga, como las de auditorías.
- Fuente BigQuery por ODBC reutilizando lo de No solución (DSN `BQCOL`).
- Caché en disco (`App_Data\cache_gaia.json` / `publicacion\datos`): se rehace **cuando cambia la
  fecha de modificación del Excel** (se comprueba cada pocos minutos), cada 12 h (la rellamada mira
  3 días hacia delante y JAZZTEL/ORANGE llegan con ~5 días de retraso) y con «Actualizar». La
  consulta tarda ~20 s; la web sirve la caché anterior mientras tanto.
- `appsettings.json`: sección `Gaia` con la ruta UNC del Excel (no `Z:`), la hoja y las horas.

**Fase 2 — Cálculos y primeras vistas (2–3 días).**
- `CalculadoraGaia` con las medidas de la sección 4 y pruebas unitarias (una por medida).
- `GaiaController` en `/gaia` con los filtros comunes (mismo panel que el resto de la web).
- Vistas: portada con KPIs y «Actualización BD», Ranking, Estilo (adherencia) y Base (tabla con
  descarga a Excel). Front con el agente `frontend-solaris` y la guía de estilos.

**Fase 3 — Resto de páginas (2–3 días).** Rendimiento, Cronológico/Comparativo, Ventas/Alineamientos,
Motivos y Mapa de afectación.

**Fase 4 — Españolización (1–2 días).** Consulta aparte (transcripciones) con su propia caché;
palabras en una lista configurable; niveles Novato/Aficionado/Experto por agente.

**Fase 5 — Cuadre y publicación (1 día).** Comparar con el PBI el mismo periodo y filtro (sabiendo
que el filtro exacto cambia los totales), probar en el 5190, documentar en `verificacion.md` y
publicar en el 5180 solo cuando el usuario lo pida.

## 8. Riesgos

- La web corre en el PC del usuario: necesita acceso a `\\172.16.232.102` y su DSN `BQCOL`
  (dependencias personales, igual que No solución).
- Si alguien cambia el nombre de la hoja o de una columna del Excel, la vista avisa y sigue con la
  última caché buena.
- Un agente sin `ID_Agente` en el Excel no sale: la vista lo listará como aviso.
