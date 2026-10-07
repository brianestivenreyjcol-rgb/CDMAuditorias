-- No solución de Jazztel, Orange y WhatsApp JZZ por mes y sector, en SQL Server (YGMM sale de BigQuery: SectoresYgmm.sql).
-- Las tablas de llamadas son las equivalentes a las de rellamada (PD_Encuesta2 para Jazztel, Pd_Encuesta_Orange2 para Orange,
-- con CO Masivo Orange y CO Técnico Convergente Orange) y el sector sale del skill. En PD_Encuesta2 el skill cruza directo
-- (sin el «- 50000» de la de rellamada).
--   % No solución = CuentaNO / (CuentaSI + CuentaNO)
-- WhatsApp: la pregunta «¿se ha resuelto tu consulta?» de WhatsApp_Conversaciones (questionSolvedAsked = 'sí'), con las
-- respuestas «sí» y «no» (el texto libre no cuenta), del último agente humano de Jazzplat Bogotá. El sector sale de su extensión
-- Avaya (PD_Usuarios → legajo → nómina del día), que da lo mismo que VW_Recontacto (sep-2026: 79 de 1.281 frente a 80 de 1.288)
-- y es mucho más rápido. La pregunta empezó en septiembre de 2026 (antes hay un puñado de respuestas sueltas): se cuenta desde ahí.
-- Mismo alcance que la rellamada de WhatsApp: CO Whatsapp JZZ. EncuestaSolucion y CMDWhat no sirven (la primera trae
-- CantidadSiSolucion siempre a 0; en la segunda casi todas las encuestas quedan «Sin agente asignado»).
SET NOCOUNT ON;
DECLARE @FechaIni DATE = DATEADD(MONTH, -3, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));  -- 3 meses atrás + el actual
DECLARE @FechaFin DATE = DATEADD(DAY, -1, CAST(GETDATE() AS date));                                -- hasta ayer
DECLARE @HastaTexto CHAR(10) = CONVERT(CHAR(10), DATEADD(DAY, 1, @FechaFin), 120);
-- La pregunta de solución de WhatsApp empezó en septiembre de 2026.
DECLARE @DesdeWhatsApp CHAR(10) = CONVERT(CHAR(10), CASE WHEN @FechaIni < '20260901' THEN CAST('20260901' AS DATE) ELSE @FechaIni END, 120);

;WITH BASE_GENERAL AS (   -- Jazztel: todos los sectores FRONT de Bogotá
    SELECT FORMAT(e.Fecha, 'yyyy-MM') AS Mes,
           sk.Sector,
           'JAZZTEL' AS Marca,
           SUM(e.CuentaSI) + SUM(e.CuentaNO) AS Base,
           SUM(e.CuentaNO) AS Casos
    FROM [Indicadores].[Front].[PD_Encuesta2] AS e
    LEFT JOIN [Planificacion].[dbo].[Skill_Actualizados] AS sk
           ON TRY_CAST(e.SKILL AS INT) = sk.Skill
    WHERE e.Fecha BETWEEN @FechaIni AND @FechaFin
      AND sk.[Tipo Servicio] = 'FRONT'
      AND sk.Site = 'Bogotá'
    GROUP BY FORMAT(e.Fecha, 'yyyy-MM'), sk.Sector
),
BASE_ORANGE AS (
    SELECT FORMAT(o.Fecha, 'yyyy-MM') AS Mes,
           sk.Sector,
           'ORANGE' AS Marca,
           SUM(TRY_CAST(o.CuentaSI AS INT)) + SUM(TRY_CAST(o.CuentaNO AS INT)) AS Base,
           SUM(TRY_CAST(o.CuentaNO AS INT)) AS Casos
    FROM [Indicadores].[Front].[Pd_Encuesta_Orange2] AS o
    LEFT JOIN [Planificacion].[dbo].[Skill_Actualizados] AS sk
           ON o.skill = sk.Skill
    WHERE o.Fecha >= @FechaIni AND o.Fecha < DATEADD(DAY, 1, @FechaFin)
      AND sk.[Tipo Servicio] = 'FRONT'
      AND sk.Site = 'Bogotá'
      AND sk.Sector IN ('CO Masivo Orange', 'CO Técnico Convergente Orange')
    GROUP BY FORMAT(o.Fecha, 'yyyy-MM'), sk.Sector
),
CONVERSACIONES AS (   -- WhatsApp: ts_end es texto «2026-09-15 20:28:17»; se filtra como texto para que vaya rápido
    SELECT CAST(LEFT(ts_end, 10) AS DATE) AS Dia,
           TRY_CAST(messageAgentHumanLatestAvayaID AS BIGINT) AS Avaya,
           LOWER(LTRIM(RTRIM(questionSolvedAnswer))) AS Respuesta
    FROM [Indicadores].[WhatsApp].[WhatsApp_Conversaciones]
    WHERE ts_end >= @DesdeWhatsApp AND ts_end < @HastaTexto
      AND questionSolvedAsked = 'sí'
      AND messageAgentHumanLatestPlatform = 'Jazzplat Bogotá'
),
USUARIOS AS (         -- extensión Avaya -> legajo (la fila más reciente)
    SELECT CAST(Avaya AS BIGINT) AS Avaya, CAST(legajo AS INT) AS leg,
           ROW_NUMBER() OVER (PARTITION BY CAST(Avaya AS BIGINT) ORDER BY fecha DESC) AS rn
    FROM [Planificacion].[Nomina].[PD_Usuarios]
    WHERE Avaya IS NOT NULL AND Avaya > 0
),
BASE_WHATSAPP AS (
    SELECT FORMAT(c.Dia, 'yyyy-MM') AS Mes,
           n.sec_descrip AS Sector,
           'JAZZTEL' AS Marca,
           COUNT(*) AS Base,
           SUM(CASE WHEN c.Respuesta = 'no' THEN 1 ELSE 0 END) AS Casos
    FROM CONVERSACIONES AS c
    JOIN USUARIOS AS u ON u.Avaya = c.Avaya AND u.rn = 1
    JOIN [RecursosHumanos].[RRHH].[PANEL - DIARIO NOMINA] AS n ON n.leg = u.leg AND n.Fecha = c.Dia
    WHERE c.Respuesta IN ('sí', 'si', 'no')
      AND n.sec_descrip = 'CO Whatsapp JZZ'
    GROUP BY FORMAT(c.Dia, 'yyyy-MM'), n.sec_descrip
)
SELECT Mes, Sector, Marca, Base, Casos FROM BASE_GENERAL
UNION ALL SELECT Mes, Sector, Marca, Base, Casos FROM BASE_ORANGE
UNION ALL SELECT Mes, Sector, Marca, Base, Casos FROM BASE_WHATSAPP;
