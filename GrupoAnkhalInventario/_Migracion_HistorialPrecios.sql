-- ============================================================
-- MIGRACIÓN: Catálogo de Precios por Proveedor + Historial
-- Base de datos: InventarioAnkhalDB
-- Fecha: 2026-05-27
-- ============================================================
-- INSTRUCCIONES:
--   1. Abrir SSMS y conectar a DESKTOP-A9E3FVS
--   2. Seleccionar la base InventarioAnkhalDB
--   3. Ejecutar este script completo (F5)
--   4. Confirmar que las tablas existen: SELECT * FROM ProveedorMateriales; SELECT * FROM HistorialPrecios;
--   5. En Visual Studio, abrir Modelo/InventarioAnkhalDB.dbml
--   6. En Server Explorer, arrastrar dbo.ProveedorMateriales y dbo.HistorialPrecios al canvas
--   7. Guardar el .dbml (el .designer.cs se regenera automáticamente)
--   8. Reconstruir la solución (Build > Rebuild Solution)
-- ============================================================

USE [InventarioAnkhalDB]
GO

-- ── Tabla 1: vínculo Proveedor ↔ Material (1 registro por par único) ───
CREATE TABLE dbo.ProveedorMateriales
(
    ProveedorMaterialID INT       NOT NULL IDENTITY(1,1)
        CONSTRAINT PK_ProveedorMateriales PRIMARY KEY,
    ProveedorID         INT       NOT NULL
        CONSTRAINT FK_PM_Proveedores FOREIGN KEY REFERENCES dbo.Proveedores(ProveedorID),
    MaterialID          INT       NOT NULL
        CONSTRAINT FK_PM_Materiales  FOREIGN KEY REFERENCES dbo.Materiales(MaterialID),
    Activo              BIT       NOT NULL CONSTRAINT DF_PM_Activo    DEFAULT (1),
    FechaAlta           DATETIME2 NOT NULL CONSTRAINT DF_PM_FechaAlta DEFAULT SYSUTCDATETIME(),
    UsuarioAltaID       INT       NULL,
    -- Un proveedor no puede tener el mismo material dos veces
    CONSTRAINT UQ_PM_ProveedorMaterial UNIQUE (ProveedorID, MaterialID)
);
GO

CREATE NONCLUSTERED INDEX IX_PM_ProveedorID
    ON dbo.ProveedorMateriales (ProveedorID) INCLUDE (MaterialID, Activo);
CREATE NONCLUSTERED INDEX IX_PM_MaterialID
    ON dbo.ProveedorMateriales (MaterialID);
GO

-- ── Tabla 2: historial de precios (append-only, sin FechaFin) ──────────
-- "Precio vigente" = TOP 1 ORDER BY FechaInicio DESC WHERE FechaInicio <= CAST(GETDATE() AS DATE)
CREATE TABLE dbo.HistorialPrecios
(
    HistorialPrecioID   INT           NOT NULL IDENTITY(1,1)
        CONSTRAINT PK_HistorialPrecios PRIMARY KEY,
    ProveedorMaterialID INT           NOT NULL
        CONSTRAINT FK_HP_ProveedorMateriales
            FOREIGN KEY REFERENCES dbo.ProveedorMateriales(ProveedorMaterialID),
    Precio              DECIMAL(12,4) NOT NULL,
    FechaInicio         DATE          NOT NULL,   -- Fecha de vigencia del precio
    Notas               NVARCHAR(300) NULL,
    RegistradoPorID     INT           NOT NULL,   -- ClaveID del usuario (Session["ClaveID"])
    FechaRegistro       DATETIME2     NOT NULL CONSTRAINT DF_HP_FechaRegistro DEFAULT SYSUTCDATETIME()
);
GO

-- Índice principal: dado un ProveedorMaterialID, encontrar el precio vigente rápidamente
CREATE NONCLUSTERED INDEX IX_HP_PMFecha
    ON dbo.HistorialPrecios (ProveedorMaterialID, FechaInicio DESC)
    INCLUDE (Precio);
GO

-- ── Verificación ────────────────────────────────────────────────────────
SELECT 'ProveedorMateriales creada' AS Resultado, COUNT(*) AS Registros FROM dbo.ProveedorMateriales
UNION ALL
SELECT 'HistorialPrecios creada',                  COUNT(*) FROM dbo.HistorialPrecios;
GO
