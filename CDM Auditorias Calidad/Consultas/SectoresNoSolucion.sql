-- No solución de Jazztel y Orange por mes y sector, en SQL Server (YGMM sale de BigQuery: SectoresYgmm.sql).
-- Las tablas son las equivalentes a las de rellamada (PD_Encuesta2 para Jazztel, Pd_Encuesta_Orange2 para Orange)
-- y el sector sale del skill. En PD_Encuesta2 el skill cruza directo (sin el «- 50000» de la de rellamada).
--   % No solución = CuentaNO / (CuentaSI + CuentaNO)
-- WhatsApp no tiene aún una fuente fiable de encuesta de solución (EncuestaSolucion trae CantidadSiSolucion
-- siempre a 0) y no se incluye.
SET NOCOUNT ON;
DECLARE @FechaIni DATE = DATEADD(MONTH, -3, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));  -- 3 meses atrás + el actual
DECLARE @FechaFin DATE = DATEADD(DAY, -1, CAST(GETDATE() AS date));                                -- hasta ayer

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
      AND sk.Sector = 'CO Masivo Orange'
    GROUP BY FORMAT(o.Fecha, 'yyyy-MM'), sk.Sector
)
SELECT Mes, Sector, Marca, Base, Casos FROM BASE_GENERAL
UNION ALL SELECT Mes, Sector, Marca, Base, Casos FROM BASE_ORANGE;
