-- ============================================================
-- Abonos parciales a CuentasPorPagar — Grupo Ankhal
-- Ejecutar en SQL Server Management Studio sobre InventarioAnkhalDB
-- PASO 1: ejecutar este script (idempotente, se puede re-ejecutar)
-- PASO 2: actualizar el DBML (ver instrucciones al final)
-- ============================================================

-- ── 1. Agregar 'PARCIAL' como estado válido de CuentasPorPagar ──
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CxP_Estado')
    ALTER TABLE [dbo].[CuentasPorPagar] DROP CONSTRAINT [CK_CxP_Estado];
GO

ALTER TABLE [dbo].[CuentasPorPagar]
    ADD CONSTRAINT CK_CxP_Estado CHECK ([Estado] IN ('PENDIENTE','PARCIAL','PAGADA','CANCELADA'));
GO

-- ── 2. Crear tabla AbonosCuentasPorPagar ─────────────────────
-- Abreviatura de convención: AbonoCxP. Relación 1-a-muchos por
-- diseño (varios abonos por cuenta) — sin índice único en CuentaPorPagarID.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AbonosCuentasPorPagar')
BEGIN
    CREATE TABLE [dbo].[AbonosCuentasPorPagar] (
        [AbonoID]           INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_AbonoCxP PRIMARY KEY,
        [CuentaPorPagarID]  INT             NOT NULL,
        [MontoAbono]        DECIMAL(18,2)   NOT NULL,
        [FechaAbono]        DATETIME2(7)    NOT NULL,
        [ReferenciaPago]    VARCHAR(100)    NULL,
        [Observaciones]     VARCHAR(500)    NULL,
        [Estado]            VARCHAR(20)     NOT NULL CONSTRAINT DF_AbonoCxP_Estado DEFAULT 'ACTIVO',
        [RegistradoPorID]   INT             NOT NULL,
        [FechaRegistro]     DATETIME2(7)    NOT NULL,
        CONSTRAINT CK_AbonoCxP_Estado      CHECK ([Estado] IN ('ACTIVO','CANCELADO')),
        CONSTRAINT CK_AbonoCxP_Monto       CHECK ([MontoAbono] > 0),
        CONSTRAINT FK_AbonoCxP_CuentaPorPagar FOREIGN KEY ([CuentaPorPagarID]) REFERENCES [dbo].[CuentasPorPagar]([CuentaPorPagarID]),
        CONSTRAINT FK_AbonoCxP_Registrado  FOREIGN KEY ([RegistradoPorID])     REFERENCES [dbo].[Usuario]([ClaveID])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AbonoCxP_CuentaPorPagarID')
    CREATE INDEX IX_AbonoCxP_CuentaPorPagarID
        ON [dbo].[AbonosCuentasPorPagar] ([CuentaPorPagarID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AbonoCxP_Estado')
    CREATE INDEX IX_AbonoCxP_Estado
        ON [dbo].[AbonosCuentasPorPagar] ([Estado]);
GO

-- ── 3. Crear tabla CancelacionesAbonosCxP ────────────────────
-- Abreviatura: CancAbonoCxP. Auditoría de cancelación de abonos:
-- el abono original NUNCA se borra ni se edita; cancelar crea este
-- registro referenciándolo (quién, cuándo, motivo). 1:1 garantizado
-- por el índice único UX_CancAbonoCxP_AbonoID.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CancelacionesAbonosCxP')
BEGIN
    CREATE TABLE [dbo].[CancelacionesAbonosCxP] (
        [CancelacionID]     INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_CancAbonoCxP PRIMARY KEY,
        [AbonoID]           INT             NOT NULL,
        [Motivo]            VARCHAR(500)    NOT NULL,
        [CanceladoPorID]    INT             NOT NULL,
        [FechaCancelacion]  DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_CancAbonoCxP_Abono        FOREIGN KEY ([AbonoID])        REFERENCES [dbo].[AbonosCuentasPorPagar]([AbonoID]),
        CONSTRAINT FK_CancAbonoCxP_CanceladoPor FOREIGN KEY ([CanceladoPorID]) REFERENCES [dbo].[Usuario]([ClaveID])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CancAbonoCxP_AbonoID')
    CREATE UNIQUE INDEX UX_CancAbonoCxP_AbonoID
        ON [dbo].[CancelacionesAbonosCxP] ([AbonoID]);
GO

-- ── 4. Migración retroactiva idempotente para cuentas ya PAGADA ──
-- Por qué importa: sin esto, "Ver Abonos" de una cuenta vieja pagada
-- se vería vacío aunque la cuenta sí esté pagada — inconsistente con
-- la UI y con la suma de abonos activos, que ahora es la fuente de
-- verdad del saldo pendiente.
INSERT INTO [dbo].[AbonosCuentasPorPagar]
    (CuentaPorPagarID, MontoAbono, FechaAbono, ReferenciaPago, Observaciones, Estado, RegistradoPorID, FechaRegistro)
SELECT
    c.CuentaPorPagarID,
    c.MontoTotal,
    ISNULL(c.FechaPago, c.FechaRegistro),
    c.ReferenciaPago,
    'Abono generado por migración (pago previo a la función de abonos parciales).',
    'ACTIVO',
    ISNULL(c.PagadaPorID, c.RegistradoPorID),
    ISNULL(c.FechaPago, c.FechaRegistro)
FROM [dbo].[CuentasPorPagar] c
WHERE c.Estado = 'PAGADA'
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[AbonosCuentasPorPagar] a WHERE a.CuentaPorPagarID = c.CuentaPorPagarID
  );
GO

-- ============================================================
-- DESPUÉS de ejecutar este script, actualizar el DBML:
--   1. Abrir Modelo/InventarioAnkhalDB.dbml en Visual Studio
--   2. Arrastrar AbonosCuentasPorPagar y CancelacionesAbonosCxP
--      desde el Server Explorer al diseñador
--   3. Guardar — el archivo .designer.cs se regenera automáticamente
--   (Mientras no haya acceso a la BD real desde Visual Studio, estas
--   clases se agregaron A MANO al .dbml/.designer.cs — compilar para
--   confirmar que el código escrito a mano es válido, y la próxima vez
--   que se tenga acceso real, re-arrastrar las tablas para comparar)
-- ============================================================
