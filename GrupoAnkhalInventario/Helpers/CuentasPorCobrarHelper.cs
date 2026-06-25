using System;
using System.Linq;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario.Helpers
{
    /// <summary>Se lanza al intentar cancelar una entrega cuya CxC ya tiene cobros (abonos) ACTIVOs.</summary>
    public class CxCConCobrosActivosException : Exception
    {
        public CxCConCobrosActivosException(string message) : base(message) { }
    }

    /// <summary>
    /// Punto único de generación/cancelación de Cuentas por Cobrar, llamado desde los
    /// distintos lugares de Entregas.aspx.cs y Ordenes.aspx.cs donde una Entrega
    /// transiciona hacia/desde el estado ENTREGADA.
    /// </summary>
    public static class CuentasPorCobrarHelper
    {
        /// <summary>
        /// Genera la CxC de una entrega si EsCredito=true, tiene ClienteID y aún no existe una
        /// CxC para ese EntregaID. No hace nada (silenciosamente) si no aplica. No hace commit;
        /// el llamador es responsable de su propia transacción.
        /// </summary>
        public static void GenerarSiAplica(InventarioAnkhalDBDataContext db, int entregaID, int claveIdRegistro)
        {
            var entrega = db.Entregas.FirstOrDefault(e => e.EntregaID == entregaID);
            if (entrega == null || !entrega.EsCredito || !entrega.ClienteID.HasValue)
                return;

            if (db.CuentasPorCobrar.Any(c => c.EntregaID == entregaID))
                return; // ya existe (defensivo, idempotente)

            decimal monto = db.DetalleEntregas
                .Where(d => d.EntregaID == entregaID)
                .Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0m;
            if (monto <= 0m)
                return;

            var cliente = db.Clientes.Single(c => c.ClienteID == entrega.ClienteID.Value);
            int dias = cliente.DiasCredito ?? 0;

            db.CuentasPorCobrar.InsertOnSubmit(new GrupoAnkhalInventario.Modelo.CuentasPorCobrar
            {
                EntregaID = entregaID,
                ClienteID = entrega.ClienteID.Value,
                NumeroFactura = entrega.NumeroFactura,
                FechaEntrega = entrega.FechaEntrega,
                DiasCreditoAplicados = dias,
                FechaVencimiento = entrega.FechaEntrega.AddDays(dias),
                MontoTotal = monto,
                Estado = "PENDIENTE",
                RegistradoPorID = claveIdRegistro,
                FechaRegistro = AppHelper.Ahora
            });
            db.SubmitChanges();
        }

        /// <summary>
        /// True si la CxC ligada a esta entrega (si existe y no está ya CANCELADA) tiene
        /// algún abono ACTIVO — es decir, si ya se le cobró algo al cliente.
        /// </summary>
        public static bool HayCobrosActivos(InventarioAnkhalDBDataContext db, int entregaID)
        {
            var cxc = db.CuentasPorCobrar.FirstOrDefault(c => c.EntregaID == entregaID);
            if (cxc == null || cxc.Estado == "CANCELADA")
                return false;

            return db.AbonosCuentasPorCobrar
                .Any(a => a.CuentaPorCobrarID == cxc.CuentaPorCobrarID && a.Estado == "ACTIVO");
        }

        /// <summary>
        /// Marca como CANCELADA la CxC ligada a esta entrega, si existe. Asume que ya se validó
        /// con HayCobrosActivos que no tiene cobros activos. No hace SubmitChanges; el llamador
        /// es responsable de su propia transacción.
        /// </summary>
        public static void CancelarCxCSiExiste(InventarioAnkhalDBDataContext db, int entregaID)
        {
            var cxc = db.CuentasPorCobrar.FirstOrDefault(c => c.EntregaID == entregaID);
            if (cxc == null || cxc.Estado == "CANCELADA")
                return;

            cxc.Estado = "CANCELADA";
        }

        /// <summary>Resultado de evaluar el límite de crédito de un cliente contra una operación nueva.</summary>
        public class InfoLimiteCredito
        {
            public int ClienteID { get; set; }
            public string NombreCliente { get; set; }
            public decimal Limite { get; set; }
            public decimal SaldoActual { get; set; }
            public decimal MontoNuevo { get; set; }
            public decimal SaldoProyectado { get; set; }
            public decimal Excedente { get; set; }
            public decimal PorcentajeActual     => Limite == 0 ? 0 : Math.Round(SaldoActual / Limite * 100, 0);
            public decimal PorcentajeProyectado => Limite == 0 ? 0 : Math.Round(SaldoProyectado / Limite * 100, 0);
        }

        /// <summary>
        /// Suma de SaldoPendiente (MontoTotal - abonos activos) de las CxC PENDIENTE/PARCIAL de un
        /// cliente. Consulta agregada en SQL — no trae filas a memoria.
        /// </summary>
        public static decimal ObtenerSaldoActual(InventarioAnkhalDBDataContext db, int clienteId)
        {
            decimal? total = db.ExecuteQuery<decimal?>(@"
                SELECT SUM(c.MontoTotal - ISNULL(ab.TotalAbonado, 0))
                FROM dbo.CuentasPorCobrar c
                LEFT JOIN (
                    SELECT CuentaPorCobrarID, SUM(MontoAbono) AS TotalAbonado
                    FROM dbo.AbonosCuentasPorCobrar
                    WHERE Estado = 'ACTIVO'
                    GROUP BY CuentaPorCobrarID
                ) ab ON ab.CuentaPorCobrarID = c.CuentaPorCobrarID
                WHERE c.ClienteID = {0} AND c.Estado IN ('PENDIENTE','PARCIAL')",
                clienteId).FirstOrDefault();
            return total ?? 0m;
        }

        /// <summary>
        /// Evalúa si sumar montoNuevo al saldo actual del cliente excede su LimiteCredito.
        /// Devuelve null tanto si el cliente no tiene límite configurado (sin límite) como si no
        /// se excede.
        /// </summary>
        public static InfoLimiteCredito EvaluarLimiteCredito(InventarioAnkhalDBDataContext db, int clienteId, decimal montoNuevo)
        {
            var cliente = db.Clientes.FirstOrDefault(c => c.ClienteID == clienteId);
            if (cliente == null || !cliente.LimiteCredito.HasValue)
                return null;

            decimal saldoActual = ObtenerSaldoActual(db, clienteId);
            decimal limite = cliente.LimiteCredito.Value;
            decimal saldoProyectado = saldoActual + montoNuevo;
            if (saldoProyectado <= limite)
                return null;

            return new InfoLimiteCredito
            {
                ClienteID = clienteId,
                NombreCliente = cliente.Nombre,
                Limite = limite,
                SaldoActual = saldoActual,
                MontoNuevo = montoNuevo,
                SaldoProyectado = saldoProyectado,
                Excedente = saldoProyectado - limite
            };
        }

        public static string FormatearMensaje(InfoLimiteCredito info)
        {
            return string.Format(
                "Cliente: {0}<br>Límite de crédito: {1:C}<br>Saldo actual: {2:C} ({3}%)<br>" +
                "Esta operación: {4:C}<br>Saldo proyectado: {5:C} ({6}%)<br>Excede por: <b>{7:C}</b>" +
                "<br><br>¿Desea continuar de todos modos?",
                info.NombreCliente, info.Limite, info.SaldoActual, info.PorcentajeActual,
                info.MontoNuevo, info.SaldoProyectado, info.PorcentajeProyectado, info.Excedente);
        }

        /// <summary>
        /// Registra en BitacoraLimitesCredito que un usuario decidió continuar pese a exceder el
        /// límite. Se llama SOLO cuando ya se confirmó la operación. `motivo` queda reservado para
        /// cuando se agregue un campo de justificación en el modal de confirmación.
        /// Usa ADO.NET directo (no db.ExecuteCommand) porque LINQ to SQL no infiere correctamente
        /// el tipo SQL de parámetros nulos (ni con null ni con DBNull.Value).
        /// </summary>
        public static void RegistrarBitacora(InventarioAnkhalDBDataContext db, InfoLimiteCredito info, int? entregaId, int usuarioId, string motivo = null)
        {
            using (var cmd = db.Connection.CreateCommand())
            {
                cmd.Transaction = db.Transaction;
                cmd.CommandText = @"
                    INSERT INTO dbo.BitacoraLimitesCredito
                        (Fecha, UsuarioID, TipoEntidad, ClienteID, ProveedorID, EntregaID, LoteID, MontoOperacion, LimiteCredito, SaldoActual, Excedente, Motivo)
                    VALUES (@fecha, @usuarioId, 'CLIENTE', @clienteId, NULL, @entregaId, NULL, @monto, @limite, @saldoActual, @excedente, @motivo)";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@fecha"; p1.Value = AppHelper.Ahora; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@usuarioId"; p2.Value = usuarioId; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.ParameterName = "@clienteId"; p3.Value = info.ClienteID; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.ParameterName = "@entregaId"; p4.Value = (object)entregaId ?? DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.ParameterName = "@monto"; p5.Value = info.MontoNuevo; cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.ParameterName = "@limite"; p6.Value = info.Limite; cmd.Parameters.Add(p6);
                var p7 = cmd.CreateParameter(); p7.ParameterName = "@saldoActual"; p7.Value = info.SaldoActual; cmd.Parameters.Add(p7);
                var p8 = cmd.CreateParameter(); p8.ParameterName = "@excedente"; p8.Value = info.Excedente; cmd.Parameters.Add(p8);
                var p9 = cmd.CreateParameter(); p9.ParameterName = "@motivo"; p9.Value = (object)motivo ?? DBNull.Value; cmd.Parameters.Add(p9);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
