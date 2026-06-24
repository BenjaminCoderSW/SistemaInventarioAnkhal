-- ============================================================
-- Cuentas por Cobrar (CxC) — Grupo Ankhal
-- Ejecutar en SQL Server Management Studio sobre InventarioAnkhalDB
-- PASO 1: ejecutar este script (idempotente, se puede re-ejecutar)
-- PASO 2: el .dbml/.designer.cs ya se actualizaron a mano en el código,
--         no se requiere abrir el diseñador de Visual Studio.
-- ============================================================

-- ── 1. EsCredito en Entregas y Ordenes ───────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Entregas') AND name = 'EsCredito')
    ALTER TABLE [dbo].[Entregas]
        ADD [EsCredito] BIT NOT NULL CONSTRAINT DF_Entregas_EsCredito DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ordenes') AND name = 'EsCredito')
    ALTER TABLE [dbo].[Ordenes]
        ADD [EsCredito] BIT NOT NULL CONSTRAINT DF_Ordenes_EsCredito DEFAULT (0);
GO

-- ── 2. Crear tabla CuentasPorCobrar ──────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CuentasPorCobrar')
BEGIN
    CREATE TABLE [dbo].[CuentasPorCobrar] (
        [CuentaPorCobrarID]     INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_CxC PRIMARY KEY,
        [EntregaID]             INT             NOT NULL,
        [ClienteID]             INT             NOT NULL,
        [NumeroFactura]         VARCHAR(50)     NULL,
        [FechaEntrega]          DATE            NOT NULL,
        [DiasCreditoAplicados]  INT             NOT NULL,
        [FechaVencimiento]      DATE            NOT NULL,
        [MontoTotal]            DECIMAL(18,2)   NOT NULL,
        [Estado]                VARCHAR(20)     NOT NULL CONSTRAINT DF_CxC_Estado DEFAULT 'PENDIENTE',
        [FechaCobro]            DATETIME2(7)    NULL,
        [ReferenciaCobro]       VARCHAR(100)    NULL,
        [Observaciones]         VARCHAR(500)    NULL,
        [CobradaPorID]          INT             NULL,
        [FechaRegistro]         DATETIME2(7)    NOT NULL,
        [RegistradoPorID]       INT             NOT NULL,
        CONSTRAINT CK_CxC_Estado     CHECK ([Estado] IN ('PENDIENTE','PARCIAL','PAGADA','CANCELADA')),
        CONSTRAINT CK_CxC_Monto      CHECK ([MontoTotal] > 0),
        CONSTRAINT FK_CxC_Entrega    FOREIGN KEY ([EntregaID])       REFERENCES [dbo].[Entregas]([EntregaID]),
        CONSTRAINT FK_CxC_Cliente    FOREIGN KEY ([ClienteID])       REFERENCES [dbo].[Clientes]([ClienteID]),
        CONSTRAINT FK_CxC_Registrado FOREIGN KEY ([RegistradoPorID]) REFERENCES [dbo].[Usuario]([ClaveID]),
        CONSTRAINT FK_CxC_Cobrada    FOREIGN KEY ([CobradaPorID])    REFERENCES [dbo].[Usuario]([ClaveID])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CxC_EntregaID')
    CREATE UNIQUE INDEX UX_CxC_EntregaID
        ON [dbo].[CuentasPorCobrar] ([EntregaID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CxC_ClienteID')
    CREATE INDEX IX_CxC_ClienteID
        ON [dbo].[CuentasPorCobrar] ([ClienteID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CxC_Estado')
    CREATE INDEX IX_CxC_Estado
        ON [dbo].[CuentasPorCobrar] ([Estado]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CxC_FechaVencimiento')
    CREATE INDEX IX_CxC_FechaVencimiento
        ON [dbo].[CuentasPorCobrar] ([FechaVencimiento])
        WHERE [Estado] IN ('PENDIENTE','PARCIAL');
GO

-- ── 3. Crear tabla AbonosCuentasPorCobrar ────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AbonosCuentasPorCobrar')
BEGIN
    CREATE TABLE [dbo].[AbonosCuentasPorCobrar] (
        [AbonoID]           INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_AbonoCxC PRIMARY KEY,
        [CuentaPorCobrarID] INT             NOT NULL,
        [MontoAbono]        DECIMAL(18,2)   NOT NULL,
        [FechaAbono]        DATETIME2(7)    NOT NULL,
        [ReferenciaPago]    VARCHAR(100)    NULL,
        [Observaciones]     VARCHAR(500)    NULL,
        [Estado]            VARCHAR(20)     NOT NULL CONSTRAINT DF_AbonoCxC_Estado DEFAULT 'ACTIVO',
        [RegistradoPorID]   INT             NOT NULL,
        [FechaRegistro]     DATETIME2(7)    NOT NULL,
        CONSTRAINT CK_AbonoCxC_Estado      CHECK ([Estado] IN ('ACTIVO','CANCELADO')),
        CONSTRAINT CK_AbonoCxC_Monto       CHECK ([MontoAbono] > 0),
        CONSTRAINT FK_AbonoCxC_CuentaPorCobrar FOREIGN KEY ([CuentaPorCobrarID]) REFERENCES [dbo].[CuentasPorCobrar]([CuentaPorCobrarID]),
        CONSTRAINT FK_AbonoCxC_Registrado  FOREIGN KEY ([RegistradoPorID])      REFERENCES [dbo].[Usuario]([ClaveID])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AbonoCxC_CuentaPorCobrarID')
    CREATE INDEX IX_AbonoCxC_CuentaPorCobrarID
        ON [dbo].[AbonosCuentasPorCobrar] ([CuentaPorCobrarID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AbonoCxC_Estado')
    CREATE INDEX IX_AbonoCxC_Estado
        ON [dbo].[AbonosCuentasPorCobrar] ([Estado]);
GO

-- ── 4. Crear tabla CancelacionesAbonosCxC ────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CancelacionesAbonosCxC')
BEGIN
    CREATE TABLE [dbo].[CancelacionesAbonosCxC] (
        [CancelacionID]     INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_CancAbonoCxC PRIMARY KEY,
        [AbonoID]           INT             NOT NULL,
        [Motivo]            VARCHAR(500)    NOT NULL,
        [CanceladoPorID]    INT             NOT NULL,
        [FechaCancelacion]  DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_CancAbonoCxC_Abono        FOREIGN KEY ([AbonoID])        REFERENCES [dbo].[AbonosCuentasPorCobrar]([AbonoID]),
        CONSTRAINT FK_CancAbonoCxC_CanceladoPor FOREIGN KEY ([CanceladoPorID]) REFERENCES [dbo].[Usuario]([ClaveID])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CancAbonoCxC_AbonoID')
    CREATE UNIQUE INDEX UX_CancAbonoCxC_AbonoID
        ON [dbo].[CancelacionesAbonosCxC] ([AbonoID]);
GO

-- ── 5. Retroaplicar DiasCreditoAplicados a CuentasPorPagar (tabla existente) ──
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CuentasPorPagar') AND name = 'DiasCreditoAplicados')
    ALTER TABLE [dbo].[CuentasPorPagar]
        ADD [DiasCreditoAplicados] INT NULL;
GO

-- Backfill determinista: el valor original ya quedó implícito en
-- FechaVencimiento = FechaRecepcion + DiasCredito al momento de insertar.
UPDATE [dbo].[CuentasPorPagar]
   SET [DiasCreditoAplicados] = DATEDIFF(DAY, [FechaRecepcion], [FechaVencimiento])
 WHERE [DiasCreditoAplicados] IS NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CuentasPorPagar') AND name = 'DiasCreditoAplicados' AND is_nullable = 1
)
    ALTER TABLE [dbo].[CuentasPorPagar]
        ALTER COLUMN [DiasCreditoAplicados] INT NOT NULL;
GO

-- Sin INSERT de migración retroactiva para CxC: el módulo aplica
-- solo hacia adelante, desde la próxima entrega que se registre.
