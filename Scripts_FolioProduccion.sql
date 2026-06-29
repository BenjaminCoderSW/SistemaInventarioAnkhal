-- ============================================================
-- Folio propio para Produccion (PROD-YYYYMMDD-NNN)
-- Correr manualmente en SSMS. Primero en desarrollo, validar,
-- y despues repetir en produccion (somee.com).
-- ============================================================

-- 1. Agregar columna (nullable temporalmente para poder rellenar registros existentes)
ALTER TABLE dbo.Produccion ADD Folio VARCHAR(30) NULL;
GO

-- 2. Rellenar folios para registros que ya existen. No pretende reconstruir el
--    historico real de numeracion -- solo deja la columna consistente para poder
--    aplicar NOT NULL + UNIQUE sin chocar con filas vacias/duplicadas.
;WITH Numerado AS (
    SELECT ProduccionID, Fecha,
           ROW_NUMBER() OVER (PARTITION BY Fecha ORDER BY ProduccionID) AS Seq
    FROM dbo.Produccion
)
UPDATE p
SET p.Folio = 'PROD-' + CONVERT(VARCHAR(8), n.Fecha, 112) + '-' + RIGHT('000' + CAST(n.Seq AS VARCHAR(3)), 3)
FROM dbo.Produccion p
JOIN Numerado n ON n.ProduccionID = p.ProduccionID;
GO

-- 3. Hacer la columna obligatoria y unica, igual que Entregas.Folio / Ordenes.Folio
ALTER TABLE dbo.Produccion ALTER COLUMN Folio VARCHAR(30) NOT NULL;
GO
ALTER TABLE dbo.Produccion ADD CONSTRAINT UQ_Produccion_Folio UNIQUE (Folio);
GO
