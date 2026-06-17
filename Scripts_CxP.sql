-- ============================================================
-- CuentasPorPagar — Grupo Ankhal
-- Ejecutar en SQL Server Management Studio sobre InventarioAnkhalDB
-- PASO 1: ejecutar este script
-- PASO 2: actualizar el DBML (ver instrucciones al final)
-- ============================================================

-- ── 1. Agregar NumeroNota a LotesMovimiento ──────────────────
ALTER TABLE [dbo].[LotesMovimiento]
    ADD [NumeroNota] VARCHAR(50) NULL;
GO

-- ── 2. Crear tabla CuentasPorPagar ───────────────────────────
CREATE TABLE [dbo].[CuentasPorPagar] (
    [CuentaPorPagarID]  INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_CxP PRIMARY KEY,
    [LoteID]            INT             NOT NULL,
    [ProveedorID]       INT             NOT NULL,
    [NumeroNota]        VARCHAR(50)     NULL,
    [FechaRecepcion]    DATE            NOT NULL,
    [FechaVencimiento]  DATE            NOT NULL,
    [MontoTotal]        DECIMAL(18,2)   NOT NULL,
    [Estado]            VARCHAR(20)     NOT NULL CONSTRAINT DF_CxP_Estado DEFAULT 'PENDIENTE',
    [FechaPago]         DATETIME2(7)    NULL,
    [ReferenciaPago]    VARCHAR(100)    NULL,
    [Observaciones]     VARCHAR(500)    NULL,
    [PagadaPorID]       INT             NULL,
    [FechaRegistro]     DATETIME2(7)    NOT NULL,
    [RegistradoPorID]   INT             NOT NULL,
    CONSTRAINT CK_CxP_Estado     CHECK ([Estado] IN ('PENDIENTE','PAGADA','CANCELADA')),
    CONSTRAINT CK_CxP_Monto      CHECK ([MontoTotal] > 0),
    CONSTRAINT FK_CxP_Lote       FOREIGN KEY ([LoteID])          REFERENCES [dbo].[LotesMovimiento]([LoteID]),
    CONSTRAINT FK_CxP_Proveedor  FOREIGN KEY ([ProveedorID])     REFERENCES [dbo].[Proveedores]([ProveedorID]),
    CONSTRAINT FK_CxP_Registrado FOREIGN KEY ([RegistradoPorID]) REFERENCES [dbo].[Usuario]([ClaveID]),
    CONSTRAINT FK_CxP_Pagado     FOREIGN KEY ([PagadaPorID])     REFERENCES [dbo].[Usuario]([ClaveID])
);
GO

CREATE UNIQUE INDEX UX_CxP_LoteID
    ON [dbo].[CuentasPorPagar] ([LoteID]);

CREATE INDEX IX_CxP_ProveedorID
    ON [dbo].[CuentasPorPagar] ([ProveedorID]);

CREATE INDEX IX_CxP_Estado
    ON [dbo].[CuentasPorPagar] ([Estado]);

CREATE INDEX IX_CxP_FechaVencimiento
    ON [dbo].[CuentasPorPagar] ([FechaVencimiento])
    WHERE [Estado] = 'PENDIENTE';
GO

-- ============================================================
-- DESPUÉS de ejecutar este script, actualizar el DBML:
--   1. Abrir Modelo/InventarioAnkhalDB.dbml en Visual Studio
--   2. Eliminar la tabla LotesMovimiento del diseñador
--   3. Re-arrastrarla desde el Server Explorer (recoge NumeroNota)
--   4. Arrastrar la nueva tabla CuentasPorPagar al diseñador
--   5. Guardar — el archivo .designer.cs se regenera automáticamente
-- ============================================================
