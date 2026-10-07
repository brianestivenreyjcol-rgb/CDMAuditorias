# KPI de los servicios: productividad, reitero y retención (investigación, 07-10-2026)

> Parte del contexto de CDM Auditorías Calidad. Pedido del usuario: añadir a «por sector» la Productividad, el % Reitero y el %
> de retención donde apliquen, sacando el cálculo de los Excel de ranking (`Y:\INCENTIVOS JAZZPLAT\2026\09. SEPTIEMBRE\01. RANKING`).
> **Aún no está en la web.** El usuario eligió empezar por lo que tiene base de datos (productividad de WhatsApp y reitero).

Cada sector calcula cada KPI a su manera; muchas bases son Excel pegados a mano en `Z:\Ranking\…` o `Y:\Reporting\…`.

**Aviso de seguridad**: las conexiones de los Excel llevan guardada en texto plano la contraseña de SQL de otra persona. No se usa
ni se copia; el usuario debería avisar para que la cambie.

## Productividad = cerradas ÷ horas (cada sector con su fuente)

| Sector | Cerradas | Horas | Metas 0 / 100 / 150 % (peso) |
|---|---|---|---|
| CO Whatsapp JZZ | `Indicadores.WhatsApp.WhatsappProductividad[conv  cerradas]` (hoja «Product») | CMS `Indicadores.dbo.PD_Cms_Int[Tiempo con personal]/3600`, **skill 723** | 4,7 / 5 / 7,5 (5 %) |
| CO WhatsApp Retención | Datamining `Asistencia_planificada[GestionesCerradas]` | hoja «Product» col. L | 4,3 / 4,6 / 4,9 (13 %) |
| CO WhatsApp Técnico | — | — | 0,2 / 3 / 4,5 |
| Infancia Convergente (CDA) | «Product» S | «Product» K (t Avaya) | — |
| CO WhatsApp YGMM | «Datos rutos» U | CMS | — |
| Atención Jazztel | solo **Blending** | | 2,4 / 2,76 / 3 / 3,8 (30 %) |
| Atención YGMM | solo **Experto**: casos gestionados ÷ asignados | | |

CO Whatsapp JZZ, agosto: universo del Excel (agentes con cargo «Agente» y sector JZZ a fin de mes; conversaciones por extensión
Avaya o login `PD_Usuarios.CRM`; horas del skill 723) → 36.749 ÷ 7.779,9 = **4,72** frente a 4,79 del Excel (57 agentes en el Excel,
58 aquí). Con el sector de cada día y solo Avaya salía 4,56.

## % Reitero

- Es sobre todo de técnicos y back office: CO BO Jazztel («Reitero FTTH / Móvil Apertura», hojas «PDC Ag» con % Reitero 24/48/72/128 H
  y «R72TC»), Supervisor Experto, Agendamiento («Q Reitero 7D BBOO»). Esas hojas se pegan (sin conexión en el Excel).
- En SQL: `Indicadores.Front.SchamanAgencia` (`REITERA_EN_1H/24H/72H` por sesión de Schaman) y
  `BusinessIntelligence.dbo.ExpedientesCerradosCancelados` (`RELLAMADA72` por expediente, con sector/agente). Falta cuadrar cuál usa cada Excel.
- En Atención YGMM «Schaman» es el **% de uso correcto de Schaman**, no reitero. En CO Retención Jazztel el «Q Reitero 7D» de su consulta
  está **anulado** (NULL).

## % Retención

- CO Retención (Jazztel) y CO WhatsApp Retención: «Retención N» fijo / móvil (y N+1 del mes anterior) de
  `Y:\Reporting\Diego\Retención N\Retención N _Push+Caída_Web Orange AGO.xlsx` (hojas «RETENCION MesAct» y «Retencion N+1 MesAnt»).
- CO Retención YGMM y Anticipación: «Retenido con herramienta FIJO / MOVIL (RCH)», de tipificaciones y consultas a BigQuery (`BQCOL`).
