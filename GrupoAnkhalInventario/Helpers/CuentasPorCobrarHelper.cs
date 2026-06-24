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
    }
}
