-- Rellamada 72 h y No solución de YGMM (YOIGO y MASMOVIL) por mes y sector, en BigQuery.
-- Es la tabla corporativa del PBI de Atención YGMM; solo trae YOIGO y MASMOVIL (Jazztel, Orange y
-- WhatsApp salen de SQL Server: SectoresRellamada.sql y SectoresNoSolucion.sql).
--   % Rellamada 72 h = SUM(redial_customer_operations_72h_kpi) / COUNT(redial_customer_operations_72h_kpi)
--   % No solución    = SUM(no_solucion_kpi) / COUNT(no_solucion_kpi)   (no_solucion_kpi = 1 si answer_2 = 2, 0 si answer_2 = 1)
-- COUNT solo cuenta las llamadas con dato: las que aún no tienen rellamada calculada o no tienen encuesta no cuentan.
-- La tabla trae todos los proveedores: agent_agency = 'JAZZBOG' deja solo Bogotá (comprobado contra la nómina
-- por Genesys el 07-10-2026: en septiembre solo cambiaban 4 llamadas). El sector sale del servicio.
-- Ventana: 3 meses atrás + el mes en curso, hasta ayer.
SELECT
  FORMAT_DATE('%Y-%m', day) AS Mes,
  CASE
    WHEN UPPER(agent_service_norm) IN ('COMERCIAL','FIDELIZACION','GENERAL') THEN 'CO Atención YGMM'
    WHEN UPPER(agent_service_norm) = 'RETENCION' THEN 'CO Retención YGMM'
    WHEN UPPER(agent_service_norm) = 'AVERIAS'   THEN 'CO Técnico MasMovil'
  END AS Sector,
  'YGMM' AS Marca,
  COUNT(redial_customer_operations_72h_kpi) AS BaseRellamada,
  SUM(redial_customer_operations_72h_kpi)   AS CasosRellamada,
  COUNT(no_solucion_kpi)                    AS BaseNoSolucion,
  SUM(no_solucion_kpi)                      AS CasosNoSolucion
FROM `mo-customer-ops-reporting.CALLS_CHATS.ALL_LLAMADAS_EJECUTIVO_BRUTOS_RELLAMADA`
WHERE direction = 'INBOUND'
  AND agent_agency = 'JAZZBOG'
  AND UPPER(agent_brand_norm) IN ('MASMOVIL','YOIGO')
  AND UPPER(agent_service_norm) IN ('COMERCIAL','FIDELIZACION','GENERAL','RETENCION','AVERIAS')
  AND day >= DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH)
  AND day <= DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
GROUP BY Mes, Sector
