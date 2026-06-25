-- ============================================================
-- Bitácora de Límites de Crédito (Clientes y Proveedores) — Grupo Ankhal
-- Ejecutar en SQL Server Management Studio sobre InventarioAnkhalDB
-- PASO 1: ejecutar este script (idempotente, se puede re-ejecutar)
-- PASO 2: no se requiere abrir el diseñador de Visual Studio — la tabla
--         se consulta por SQL crudo (db.ExecuteCommand/ExecuteQuery), no
--         se agrega al .dbml/.designer.cs.
-- ============================================================

-- ── 1. Crear tabla BitacoraLimitesCredito ────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BitacoraLimitesCredito')
BEGIN
    CREATE TABLE [dbo].[BitacoraLimitesCredito] (
        [BitacoraID]        INT             IDENTITY(1,1)   NOT NULL CONSTRAINT PK_BitacoraLimitesCredito PRIMARY KEY,
        [Fecha]             DATETIME2(7)    NOT NULL,
        [UsuarioID]         INT             NOT NULL,
        [TipoEntidad]       VARCHAR(10)     NOT NULL,
        [ClienteID]         INT             NULL,
        [ProveedorID]       INT             NULL,
        [EntregaID]         INT             NULL,
        [LoteID]            INT             NULL,
        [MontoOperacion]    DECIMAL(18,2)   NOT NULL,
        [LimiteCredito]     DECIMAL(18,2)   NOT NULL,
        [SaldoActual]       DECIMAL(18,2)   NOT NULL,
        [Excedente]         DECIMAL(18,2)   NOT NULL,
        [Motivo]            VARCHAR(500)    NULL,
        CONSTRAINT CK_BitacoraLimitesCredito_Tipo CHECK ([TipoEntidad] IN ('CLIENTE','PROVEEDOR')),
        -- EntregaID/LoteID deliberadamente SIN FK: es una tabla de auditoría histórica que no
        -- debe romperse si algún día se corrige o borra una entrega/lote operativo.
        CONSTRAINT FK_BitacoraLimitesCredito_Usuario     FOREIGN KEY ([UsuarioID])   REFERENCES [dbo].[Usuario]([ClaveID]),
        CONSTRAINT FK_BitacoraLimitesCredito_Clientes    FOREIGN KEY ([ClienteID])   REFERENCES [dbo].[Clientes]([ClienteID]),
        CONSTRAINT FK_BitacoraLimitesCredito_Proveedores FOREIGN KEY ([ProveedorID]) REFERENCES [dbo].[Proveedores]([ProveedorID])
    );
END
GO

-- ── 2. Índices para consultas futuras tipo ──────────────────
--     "todas las veces que el cliente/proveedor X se pasó del límite"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BitacoraLimitesCredito_Fecha')
    CREATE INDEX IX_BitacoraLimitesCredito_Fecha
        ON [dbo].[BitacoraLimitesCredito] ([Fecha]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BitacoraLimitesCredito_Cliente')
    CREATE INDEX IX_BitacoraLimitesCredito_Cliente
        ON [dbo].[BitacoraLimitesCredito] ([ClienteID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BitacoraLimitesCredito_Proveedor')
    CREATE INDEX IX_BitacoraLimitesCredito_Proveedor
        ON [dbo].[BitacoraLimitesCredito] ([ProveedorID]);
GO
