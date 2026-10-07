# Rellamada y No solución por sector (`/sectores`, 07-10-2026)

> Parte del contexto de CDM Auditorías Calidad: el resumen está en `CONTEXTO_IA.md` (raíz del proyecto).

La cifra **mensual** de rellamada 72 h y de no solución de cada sector de Bogotá. El usuario dio las consultas el
07-10-2026 y pidió: **de BigQuery solo YGMM**; **Jazztel, Orange y WhatsApp, de SQL Server** («de GAMMA no se saca
nada»). No mezclar con CDM No solución (`/nosolucion`), que sale de GAMMA y es otra cosa.

## Fuentes (las tres consultas en `Consultas/`, se leen en cada carga)

| Fichero | Dónde | Qué trae |
|---|---|---|
| `SectoresYgmm.sql` | BigQuery (DSN `BQCOL`) | `mo-customer-ops-reporting.CALLS_CHATS.ALL_LLAMADAS_EJECUTIVO_BRUTOS_RELLAMADA` (la del PBI de Atención YGMM): entrantes, `agent_agency = 'JAZZBOG'`, YOIGO y MASMOVIL. Los dos indicadores en una consulta |
| `SectoresRellamada.sql` | SQL Server (el `.env` de Auditorías) | La consulta del usuario con el mes: `Indicadores.Front.PD_JAZZTEL_FCR_72h` (skill − 50000), `Pd_R72_Orange` (solo CO Masivo Orange) y `Indicadores.WhatsApp.VW_Recontacto` (CO Whatsapp JZZ) |
| `SectoresNoSolucion.sql` | SQL Server | `Indicadores.Front.PD_Encuesta2` (Jazztel; el skill cruza **directo**, sin el − 50000) y `Pd_Encuesta_Orange2` (CO Masivo Orange) |

- Sector de YGMM por servicio: Comercial, Fidelización y General → CO Atención YGMM; Retención → CO Retención YGMM;
  Averías → CO Técnico MasMovil. **Lo asigné yo** (el usuario solo dio el filtro de Atención): si dice otra cosa, cambiar el `CASE`.
- Sector de Jazztel y Orange: `Planificacion.dbo.Skill_Actualizados` (`[Tipo Servicio] = 'FRONT'`, `Site = 'Bogotá'`).
- `agent_agency = 'JAZZBOG'` equivale al cruce con la nómina (`NominaBogota.Genesys` = `agente`): en septiembre solo cambiaban 4 llamadas.
- Fórmulas: rellamada = `SUM(redial_customer_operations_72h_kpi) / COUNT(…)` (la medida DAX del usuario) y `llam_mala / llamadas`;
  no solución = `SUM(no_solucion_kpi) / COUNT(…)` (`no_solucion_kpi` = 1 si `answer_2` = 2, 0 si 1) y `CuentaNO / (CuentaSI + CuentaNO)`.
- **Fechas en SQL**: `Pd_R72_Orange.Fecha` y `Pd_Encuesta_Orange2.Fecha` son `datetime` y el login está en español: `'2026-09-01'` se lee
  como 9 de enero. Las consultas usan variables `DATE`; a mano, `'20260901'`.
- **WhatsApp en No solución: no está.** `Indicadores.Front.EncuestaSolucion` trae `CantidadSiSolucion` siempre a 0. Pendiente de que el
  usuario dé la fuente buena. **CO Técnico Convergente Orange** tampoco sale (el usuario solo puso Masivo Orange); sí tiene encuestas.
- SQL Server no tiene la rellamada 72 h de YGMM al día (`Indicadores.Front.DatosBrutosYGMMV` acaba el 06-07-2026, `YYGMMV.dbo.DatosBrutosYGMMV`
  el 14-01-2026 y la vista `YYGMMV.dbo.DATOS_BRUTOS` está rota): por eso YGMM sale de BigQuery.

Cifras comprobadas el 07-10-2026 contra las consultas sueltas (septiembre): Atención YGMM 22,28 % rellamada y 17,58 % no solución; Atención
Jazztel 21,04 % y 9,90 %; Masivo Orange 18,94 % y 16,64 %; total rellamada 22,12 %. Agosto de Jazztel cuadra al número con la consulta original del usuario.

## Código

- `Servicios/Sectores/`: `FuenteSectores` (las tres consultas; si una falla, las demás se enseñan con un aviso), `ServicioSectores` (caché
  `App_Data\cache_sectores.json` / `publicacion\datos\cache_sectores.json`, versión 1, se renueva cada 6 h y con «Actualizar ahora»;
  `RevisionSectores` mira cada 30 min), `CalculadoraSectores` (meses, mes cerrado o en curso, totales, filtros en cascada, tabla) y
  `DatosSectores` (filas e `IndicadorSectores`).
- `Controllers/SectoresController.cs`: `/sectores` (rellamada), `/sectores/nosolucion`, `/sectores/csv`, POST `/sectores/actualizar`.
- `Models/Sectores/PaginaSectores.cs` y `Views/Sectores/` (`_LayoutSectores`, `_InformeSectores`, `_PanelSectores`, `Indicador`).
- Opciones: sección `Sectores` de appsettings (`Odbc`, `RutaCache`, `RefrescoHoras`, `SegundosConsulta`).
- Pruebas: `SectoresTests.cs`.

## Pantalla

- Pestañas Rellamada 72 h / No solución y, a la derecha, el **mes de referencia** (`?mes=`; por defecto, el último cerrado: en rellamada un mes
  se cierra 3 días después de acabar).
- Panel: Marca (YOIGO · MASMOVIL, Jazztel, Orange) y Sector, en cascada; CSV con los dos indicadores. El color sigue a la marca.
- Tira de 4 (`.resumen.en-4`): % del mes con la diferencia frente al anterior (más es peor), base, casos y el sector más alto.
- Evolución mensual (`_LineasGaia`, **solo meses cerrados**: el mes en curso hundía la línea), «Por marca» (solo con 2 o más marcas) y barras por
  sector del mes con el color de su marca (`_BarrasNs` de No solución + `.relleno-serie`).
- Tabla sector × mes (`table.ranking[data-mapa]`, semáforo por columna, variación en chip, base con barrita, TOTAL al pie, `.tabla-contenedor.alta`).
- Portada: cuarta tarjeta (`.modulos` a 4 columnas).
