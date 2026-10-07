-- Rellamada 72 h de Jazztel, Orange y WhatsApp JZZ por mes y sector, en SQL Server (YGMM sale de BigQuery:
-- SectoresYgmm.sql). Es la consulta del usuario (07-10-2026) con el mes añadido y, de Orange, también
-- CO Técnico Convergente Orange (lo pidió el usuario el mismo día). El sector sale del skill.
--   % Rellamada = LlamadaMala / TotalLlamadas
-- Ojo: Pd_R72_Orange.Fecha es datetime y el login está en español: un literal '2026-09-01' se leería como
-- 9 de enero. Por eso las fechas van en variables DATE (o como '20260901').
SET NOCOUNT ON;
DECLARE @FechaIni DATE = DATEADD(MONTH, -3, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));  -- 3 meses atrás + el actual
DECLARE @FechaFin DATE = DATEADD(DAY, -1, CAST(GETDATE() AS date));                                -- hasta ayer

;WITH BASE_GENERAL AS (   -- Jazztel: todos los sectores FRONT de Bogotá
    SELECT FORMAT(rll.fecha, 'yyyy-MM') AS Mes,
           sk.Sector,
           'JAZZTEL' AS Marca,
           SUM(TRY_CAST(rll.llamadas  AS INT)) AS Base,
           SUM(TRY_CAST(rll.llam_mala AS INT)) AS Casos
    FROM [Indicadores].[Front].[PD_JAZZTEL_FCR_72h] AS rll
    LEFT JOIN [Planificacion].[dbo].[Skill_Actualizados] AS sk
           ON rll.Skill - 50000 = sk.Skill
    WHERE rll.fecha BETWEEN @FechaIni AND @FechaFin
      AND sk.[Tipo Servicio] = 'FRONT'
      AND sk.Site = 'Bogotá'
    GROUP BY FORMAT(rll.fecha, 'yyyy-MM'), sk.Sector
),
BASE_ORANGE AS (
    SELECT FORMAT(pd.Fecha, 'yyyy-MM') AS Mes,
           sk.Sector,
           'ORANGE' AS Marca,
           SUM(TRY_CAST(pd.llamadas  AS INT)) AS Base,
           SUM(TRY_CAST(pd.llam_mala AS INT)) AS Casos
    FROM [Indicadores].[Front].[Pd_R72_Orange] AS pd
    LEFT JOIN [Planificacion].[dbo].[Skill_Actualizados] AS sk
           ON pd.Skill = sk.Skill
    WHERE pd.Fecha >= @FechaIni AND pd.Fecha < DATEADD(DAY, 1, @FechaFin)
      AND sk.[Tipo Servicio] = 'FRONT'
      AND sk.Site = 'Bogotá'
      AND sk.Sector IN ('CO Masivo Orange', 'CO Técnico Convergente Orange')
    GROUP BY FORMAT(pd.Fecha, 'yyyy-MM'), sk.Sector
),
BASE_WHATSAPP AS (       -- WhatsApp de Jazztel: recontacto en 72 h
    SELECT FORMAT(FE_FIN_TIME, 'yyyy-MM') AS Mes,
           SectorNOL AS Sector,
           'JAZZTEL' AS Marca,
           COUNT(CAST(ReContacto AS INT)) AS Base,
           SUM(CAST(ReContacto AS INT))   AS Casos
    FROM [Indicadores].[WhatsApp].[VW_Recontacto]
    WHERE FE_FIN_TIME BETWEEN @FechaIni AND @FechaFin
      AND SectorNOL = 'CO Whatsapp JZZ'
    GROUP BY FORMAT(FE_FIN_TIME, 'yyyy-MM'), SectorNOL
)
SELECT Mes, Sector, Marca, Base, Casos FROM BASE_GENERAL
UNION ALL SELECT Mes, Sector, Marca, Base, Casos FROM BASE_ORANGE
UNION ALL SELECT Mes, Sector, Marca, Base, Casos FROM BASE_WHATSAPP;
