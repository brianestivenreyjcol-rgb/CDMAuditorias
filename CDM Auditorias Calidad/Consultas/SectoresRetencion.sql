-- % Retención de YGMM por mes y sector, en BigQuery (pedido del usuario el 07-10-2026). Cada sector la mide como su Excel de
-- ranking (carpeta de Oscar):
--   · CO Anticipación OUT YGMM  → «Blindaje / Cierres» (V2 - Ranking CO Anticipacion YGMM):
--       Cierres   = tipificaciones de TIPIS_ALL de la campaña «Emision Anticipa» cuya RETENCION no es «N/A».
--       Blindajes = clientes con alguna venta de la campaña RS_RET_Anticipa (bonos con permanencia, descuentos de la lista,
--                   terminales, TV activa, líneas activas y OTT), contando cada cliente una vez al mes (la consulta del usuario).
--   · CO Retención YGMM → «Retenido con herramienta RCH Servicio Fijo» (RCHF/BF, V2 - Ranking CO Retención YGMM):
--       Tipi BF   = tipificaciones de TIPIS_ALL (sin Anticipa) de la campaña «BF» (baja fijo).
--       Rete_N_F  = ventas con permanencia de RS_RET_Bajas Nivel 1 (terminales entregados o en vuelo, bonos con permanencia y
--                   descuentos con permanencia o de la lista) cuya tipificación del mismo día, agente y cliente («retenido con
--                   herramienta») es de la campaña «BF» o «BE».
-- Todo de la plataforma «JAZZPLAT BOGOTA RETENCIÓN». El sector es el del agente ese día en la nómina (NominaBogota), cruzando como los
-- Excel: el sfid de la tipificación y el dealer_code de la venta son la parte del correo de Genesys antes de la «@» en mayúsculas; el
-- seller de las ventas de Anticipa, el correo entero. La anticipación se mide en los sectores de Anticipación y el RCH fijo en los de
-- Retención YGMM. Los Excel además filtran a mano el equipo y quitan algunos días: aquí va el sector entero.
-- Ventana: 3 meses atrás + el mes en curso, hasta ayer.
WITH
nomina AS (
  SELECT DATE(Fecha) AS dia, LOWER(TRIM(Genesys)) AS correo, UPPER(SPLIT(TRIM(Genesys), '@')[OFFSET(0)]) AS usuario, ANY_VALUE(Sector) AS Sector
  FROM `mo-vendor-management-reporting.JZZBOGOTA.NominaBogota`
  WHERE DATE(Fecha) BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
    AND Genesys IS NOT NULL AND TRIM(Genesys) != ''
  GROUP BY 1, 2, 3
),
tipis AS (
  SELECT day, UPPER(sfid) AS sfid, customer_id, campaign, RETENCION
  FROM `mo-customer-ops-reporting.COMMERCIAL.TIPIS_ALL`
  WHERE day BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
    AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN'
),

-- ------------------------------------------------------------------ Anticipación: blindajes / cierres
cierres AS (
  SELECT FORMAT_DATE('%Y-%m', t.day) AS Mes, n.Sector, COUNT(*) AS cuenta
  FROM tipis t JOIN nomina n ON n.dia = t.day AND n.usuario = t.sfid
  WHERE t.campaign = 'Emision Anticipa' AND IFNULL(t.RETENCION, '') <> 'N/A'
  GROUP BY Mes, n.Sector
),
ventas_anticipa AS (
  SELECT customer_id, CAST(init_date AS DATE) AS dia, LOWER(seller) AS seller
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_BONOS`
  WHERE dealer_campaign_name = 'RS_RET_Anticipa' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN' AND PERMANENCIA = 'CON PERMANENCIA'
  UNION ALL
  SELECT customer_id, CAST(init_date_ts AS DATE), LOWER(seller)
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_DESCUENTOS`
  WHERE dealer_campaign_name = 'RS_RET_Anticipa' AND Plataforma = 'JAZZPLAT BOGOTA RETENCIÓN' AND brand_ds IN ('YOIGO', 'MASMOVIL')
    AND campaignname IN (
      'DESCUENTO 15€ FIBRA ADICIONAL', 'Descuento 50% 12 meses x-sell', 'R YG dto Convergente 25% 12 M con perma',
      'R YG dto Convergente 20% 12 M con perma', 'R YG dto Convergente 30% 12 M con perma', 'R YG dto Convergente 10% 12 M con perma',
      'R Descuento Convergente 12€ 12 meses con Perma', 'R YG dto Convergente 40% 12 M con perma', 'Descuento 3€ 12 meses AgileTV c/p',
      'R YG dto Convergente 50% 12 M con perma', 'Descuento 3€ 12 meses Yoigo TV c/p', 'Descuento Fibra adicional', 'R YG dto Móvil 10% 12 M',
      'R YG dto Convergente 5% 12 M con perma', 'R Descuento Convergente 5€ 12 meses con Perma', 'Descuento MASMOVIL TV 3€ 12 Meses',
      'YG_PERMA_TERMINAL_200_24M', 'R Descuento Convergente 8€ 12 meses con Perma', 'YG_PERMA_TERMINAL_100_24M',
      'R YG dto Convergente 15% 12 M con perma', 'Descuento AGILE TV 3€ 12 Meses', 'R Descuento Convergente 14€ 12 meses con Perma',
      'R Descuento Convergente 10€ 12 meses con Perma', 'R Descuento Convergente 17€ 12 meses', 'MM_PERMA_TERMINAL_200_24M',
      'YG_PERMA_TERMINAL_50_24M', 'R Descuento Internet 2P 12,10€ x 12 meses', 'R Descuento Móvil 5€ 12 meses con Perma',
      'MM_PERMA_TERMINAL_100_24M', 'MM_PERMA_TERMINAL_50_24M', 'R Descuento Móvil 2€ 12 meses con Perma', 'R Descuento Internet 12€ 12 meses',
      'R YG dto Convergente 60% 12 M con perma', 'Descuento 30% 12 meses x-sell', 'R Descuento Internet 2P 7,10€ x 12 meses',
      'R Descuento Convergente 22€ 12 meses', 'R Descuento Convergente 2,5€ 12 meses con Perma', 'Dto Fibra + Móvil 10€ 12 meses con Perma',
      'Descuento Solo Internet 7€ 12 meses_C con Perma', 'R YG dto Móvil 20% 12 M', 'Dto 3€x12m YOIGO TV + Orange TV Libre c/p',
      'R YG dto Móvil 30% 12 M', 'R Descuento Servicio Ciberayuda 12 meses', 'Dto Fibra + Móvil 15€ 12 meses con Perma',
      'YG_PERMA_TERMINAL_250_12M', 'Descuento Alarma 20% 12 meses', 'MM_DTO SOLO INTERNET_5E DURANTE 12M_C', 'R YG dto Móvil 40% 12 M',
      'MM_PERMA_TERMINAL_250_12M', 'R YG dto Convergente GB Infinitos 5€ 12 M')
  UNION ALL
  SELECT customer_id, CAST(Activation_date AS DATE), LOWER(seller)
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_TERMINALES`
  WHERE dealer_campaign_name = 'RS_RET_Anticipa' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN'
  UNION ALL
  SELECT customer_id, PARSE_DATE('%Y%m%d', CAST(order_day_id AS STRING)), LOWER(seller_ds)
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_TV`
  WHERE campaign_name = 'RS_RET_Anticipa' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN' AND Estado = 'Activa'
  UNION ALL
  SELECT customer_id, DATE(fecha_venta), LOWER(seller)
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_VENTAS`
  WHERE dealer_campaign_name = 'RS_RET_Anticipa' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN' AND ESTADO = 'ACTIVO'
  UNION ALL
  SELECT customer_id, DATE(order_date), LOWER(seller)
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_VENTAS_OTT`
  WHERE dealer_campaign_name = 'RS_RET_Anticipa' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN'
),
blindajes AS (
  -- Cada cliente cuenta una vez al mes (el «blindaje = contar = 1» de la consulta del Excel), para el agente de su primera venta.
  SELECT FORMAT_DATE('%Y-%m', v.dia) AS Mes, n.Sector, COUNT(*) AS cuenta
  FROM (
    SELECT * FROM ventas_anticipa
    WHERE dia BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
    QUALIFY ROW_NUMBER() OVER (PARTITION BY customer_id, FORMAT_DATE('%Y-%m', dia) ORDER BY dia) = 1
  ) v
  JOIN nomina n ON n.dia = v.dia AND n.correo = v.seller
  GROUP BY Mes, n.Sector
),

-- ------------------------------------------------------------------ Retención YGMM: RCH servicio fijo (RCHF / BF)
tipi_bf AS (
  SELECT FORMAT_DATE('%Y-%m', t.day) AS Mes, n.Sector, COUNT(*) AS cuenta
  FROM tipis t JOIN nomina n ON n.dia = t.day AND n.usuario = t.sfid
  WHERE t.campaign = 'BF'
  GROUP BY Mes, n.Sector
),
tipis_llave AS (
  -- Una tipificación «retenido con herramienta» por día, agente y cliente (el Table.Distinct por LLAVE del Excel).
  SELECT CONCAT(CAST(day AS STRING), sfid, customer_id) AS llave, ANY_VALUE(campaign) AS campaign
  FROM tipis
  WHERE campaign <> 'Emision Anticipa'
    AND RETENCION IN ('Retenido con herramienta', 'RETENIDO CON HERRAMIENTA', 'Retenido CON herramienta')
  GROUP BY llave
),
descuentos AS (
  SELECT
    COALESCE(CAST(TP.day AS DATE), CAST(DC.init_date_ts AS DATE)) AS fecha,
    DC.customer_id,
    UPPER(DC.dealer_code) AS dealer_code,
    DC.flag_permanencia = 1 OR DC.campaignname IN (
      'DESCUENTO 15€ FIBRA ADICIONAL', 'Descuento Fibra adicional', 'Descuento 30% 12 meses x-sell', 'Descuento 50% 12 meses x-sell',
      'R YG dto Convergente 25% 12 M con perma', 'R YG dto Convergente 20% 12 M con perma', 'R YG dto Convergente 30% 12 M con perma',
      'R YG dto Convergente 10% 12 M con perma', 'R Descuento Convergente 12€ 12 meses con Perma', 'R YG dto Convergente 40% 12 M con perma',
      'Descuento 3€ 12 meses AgileTV c/p', 'R YG dto Convergente 50% 12 M con perma', 'Descuento 3€ 12 meses Yoigo TV c/p',
      'R YG dto Móvil 10% 12 M', 'R YG dto Convergente 5% 12 M con perma', 'R Descuento Convergente 5€ 12 meses con Perma',
      'Descuento MASMOVIL TV 3€ 12 Meses', 'YG_PERMA_TERMINAL_200_24M', 'R Descuento Convergente 8€ 12 meses con Perma',
      'YG_PERMA_TERMINAL_100_24M', 'R YG dto Convergente 15% 12 M con perma', 'Descuento AGILE TV 3€ 12 Meses',
      'R Descuento Convergente 14€ 12 meses con Perma', 'R Descuento Convergente 10€ 12 meses con Perma', 'R Descuento Convergente 17€ 12 meses',
      'MM_PERMA_TERMINAL_200_24M', 'YG_PERMA_TERMINAL_50_24M', 'R Descuento Internet 2P 12,10€ x 12 meses',
      'R Descuento Móvil 5€ 12 meses con Perma', 'MM_PERMA_TERMINAL_100_24M', 'MM_PERMA_TERMINAL_50_24M',
      'R Descuento Móvil 2€ 12 meses con Perma', 'R Descuento Internet 12€ 12 meses', 'R YG dto Convergente 60% 12 M con perma',
      'R Descuento Internet 2P 7,10€ x 12 meses', 'R Descuento Convergente 22€ 12 meses', 'R Descuento Convergente 2,5€ 12 meses con Perma',
      'Dto Fibra + Móvil 10€ 12 meses con Perma', 'Descuento Solo Internet 7€ 12 meses_C con Perma', 'R YG dto Móvil 20% 12 M',
      'Dto 3€x12m YOIGO TV + Orange TV Libre c/p', 'R YG dto Móvil 30% 12 M', 'R Descuento Servicio Ciberayuda 12 meses',
      'Dto Fibra + Móvil 15€ 12 meses con Perma', 'YG_PERMA_TERMINAL_250_12M', 'Descuento Alarma 20% 12 meses',
      'MM_DTO SOLO INTERNET_5E DURANTE 12M_C', 'R YG dto Móvil 40% 12 M', 'MM_PERMA_TERMINAL_250_12M', 'R YG dto Convergente GB Infinitos 5€ 12 M'
    ) AS permanencia
  FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_DESCUENTOS` AS DC
  LEFT JOIN `mo-customer-ops-reporting.COMMERCIAL.TIPIS_ALL` AS TP
    ON TP.customer_id = DC.customer_id
   AND DATE(TP.day) BETWEEN DATE_SUB(DATE(DC.init_date_ts), INTERVAL 1 DAY) AND DATE_ADD(DATE(DC.init_date_ts), INTERVAL 2 DAY)
   AND TP.campaign <> 'Emision Anticipa'
  WHERE DC.Plataforma = 'JAZZPLAT BOGOTA RETENCIÓN'
    AND DATE(DC.init_date_ts) BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
    AND DC.Servicio <> 'EMISION-RES'
  QUALIFY ROW_NUMBER() OVER (PARTITION BY DC.customer_id, DC.init_date_ts ORDER BY ABS(DATE_DIFF(DATE(TP.day), DATE(DC.init_date_ts), DAY))) = 1
),
ventas_bajas AS (
  SELECT DISTINCT fecha, customer_id, dealer_code FROM (
    SELECT CAST(order_date AS DATE) AS fecha, customer_id, UPPER(dealer_code) AS dealer_code
    FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_TERMINALES`
    WHERE CAST(Activation_date AS DATE) BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
      AND Plataforma LIKE '%JAZZPLAT BOGOTA RETENCIÓN%' AND dealer_campaign_name = 'RS_RET_Bajas Nivel 1'
      AND status_ds IN ('ENTREGADO', 'EN VUELO')
    UNION ALL
    SELECT CAST(init_date AS DATE), customer_id, UPPER(dealer_code)
    FROM `mo-customer-ops-reporting.COMMERCIAL.REP_YGMM_BONOS`
    WHERE CAST(init_date AS DATE) BETWEEN DATE_SUB(DATE_TRUNC(CURRENT_DATE(), MONTH), INTERVAL 3 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)
      AND dealer_campaign_name = 'RS_RET_Bajas Nivel 1' AND plataforma = 'JAZZPLAT BOGOTA RETENCIÓN' AND permanencia = 'CON PERMANENCIA'
      AND dealer_code IS NOT NULL
    UNION ALL
    SELECT fecha, customer_id, dealer_code FROM descuentos WHERE permanencia
  )
),
rete_n_f AS (
  SELECT FORMAT_DATE('%Y-%m', v.fecha) AS Mes, n.Sector, COUNT(*) AS cuenta
  FROM ventas_bajas v
  JOIN tipis_llave t ON t.llave = CONCAT(CAST(v.fecha AS STRING), v.dealer_code, v.customer_id)
  JOIN nomina n ON n.dia = v.fecha AND n.usuario = v.dealer_code
  WHERE t.campaign IN ('BF', 'BE')
  GROUP BY Mes, n.Sector
)

SELECT c.Mes, c.Sector, 'YGMM' AS Marca, c.cuenta AS Base, IFNULL(b.cuenta, 0) AS Casos
FROM cierres c LEFT JOIN blindajes b USING (Mes, Sector)
WHERE c.Sector LIKE '%Anticipación%'
UNION ALL
SELECT t.Mes, t.Sector, 'YGMM', t.cuenta, IFNULL(r.cuenta, 0)
FROM tipi_bf t LEFT JOIN rete_n_f r USING (Mes, Sector)
WHERE t.Sector LIKE '%Retención YGMM%'
