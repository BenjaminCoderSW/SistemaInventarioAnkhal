using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using GrupoAnkhalInventario.Services;

namespace GrupoAnkhalInventario
{
    public partial class CuentasPorCobrar : System.Web.UI.Page
    {
        // ══ Infraestructura ══════════════════════════════════════════════════
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private readonly System.Web.Script.Serialization.JavaScriptSerializer _json =
            new System.Web.Script.Serialization.JavaScriptSerializer();

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ══ ViewModel ════════════════════════════════════════════════════════
        public class CxCRow
        {
            public int CuentaPorCobrarID { get; set; }
            public int EntregaID { get; set; }
            public string FolioEntrega { get; set; }
            public string NumeroFactura { get; set; }
            public string ClienteNombre { get; set; }
            public DateTime FechaEntrega { get; set; }
            public DateTime FechaVencimiento { get; set; }
            public int DiasCreditoAplicados { get; set; }
            public decimal MontoTotal { get; set; }
            public decimal SaldoPendiente { get; set; }
            public string Estado { get; set; }
            public string EstadoVisual { get; set; }
            public string BadgeClass { get; set; }
            public bool PuedeAbonar { get; set; }
        }

        // ══ Page_Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarFiltroClientes();
                CargarResumen();
                CargarGrid();
            }
        }

        // ══ Catálogos de filtros ══════════════════════════════════════════════
        private void CargarFiltroClientes()
        {
            using (var db = NuevoDb(false))
            {
                ddlFiltroCliente.Items.Clear();
                ddlFiltroCliente.Items.Add(new ListItem("-- Todos --", ""));
                var clis = db.Clientes
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Nombre)
                    .Select(c => new { c.ClienteID, c.Nombre })
                    .ToList();
                foreach (var c in clis)
                    ddlFiltroCliente.Items.Add(new ListItem(c.Nombre, c.ClienteID.ToString()));
            }
        }

        // ══ Cards de resumen ══════════════════════════════════════════════════
        private void CargarResumen()
        {
            using (var db = NuevoDb(false))
            {
                DateTime hoy = AppHelper.Hoy;

                IQueryable<Modelo.CuentasPorCobrar> q = db.CuentasPorCobrar.AsQueryable();

                if (!string.IsNullOrEmpty(ddlFiltroCliente.SelectedValue))
                {
                    int cliID = int.Parse(ddlFiltroCliente.SelectedValue);
                    q = q.Where(c => c.ClienteID == cliID);
                }

                string filtroEstado = ddlFiltroEstado.SelectedValue;
                if (filtroEstado == "SINCOBRAR")
                    q = q.Where(c => c.Estado == "PENDIENTE" || c.Estado == "PARCIAL");
                else if (filtroEstado == "VENCIDA")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento < hoy);
                else if (filtroEstado == "PORVENCER")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL")
                                  && c.FechaVencimiento >= hoy
                                  && c.FechaVencimiento <= hoy.AddDays(7));
                else if (filtroEstado == "PENDIENTE")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento > hoy.AddDays(7));
                else if (filtroEstado == "PARCIAL")
                    q = q.Where(c => c.Estado == "PARCIAL");
                else if (filtroEstado == "PAGADA")
                    q = q.Where(c => c.Estado == "PAGADA");
                // filtroEstado == "" = todas, sin filtro adicional

                if (!string.IsNullOrEmpty(txtFiltroFechaDesde.Text))
                {
                    DateTime fd = DateTime.Parse(txtFiltroFechaDesde.Text);
                    q = q.Where(c => c.FechaEntrega >= fd);
                }
                if (!string.IsNullOrEmpty(txtFiltroFechaHasta.Text))
                {
                    DateTime fh = DateTime.Parse(txtFiltroFechaHasta.Text).AddDays(1);
                    q = q.Where(c => c.FechaEntrega < fh);
                }

                string buscar = (txtFiltroBusqueda.Text ?? "").Trim().ToLower();
                if (!string.IsNullOrEmpty(buscar))
                {
                    q = q.Where(c =>
                        (c.NumeroFactura != null && c.NumeroFactura.ToLower().Contains(buscar)) ||
                        c.Entregas.Folio.ToLower().Contains(buscar));
                }

                var datos = q.Select(c => new { c.CuentaPorCobrarID, c.MontoTotal, c.FechaVencimiento, c.Estado }).ToList();

                var idsResumen = datos.Select(c => c.CuentaPorCobrarID).ToList();
                var abonosResumen = db.AbonosCuentasPorCobrar
                    .Where(a => idsResumen.Contains(a.CuentaPorCobrarID) && a.Estado == "ACTIVO")
                    .GroupBy(a => a.CuentaPorCobrarID)
                    .Select(g => new { CuentaPorCobrarID = g.Key, Total = g.Sum(a => a.MontoAbono) })
                    .ToDictionary(x => x.CuentaPorCobrarID, x => x.Total);

                // Sobre el conjunto filtrado, calcular el saldo pendiente real (excluye pagadas)
                decimal totalPendiente = datos
                    .Where(c => c.Estado == "PENDIENTE" || c.Estado == "PARCIAL")
                    .Sum(c => c.MontoTotal - (abonosResumen.TryGetValue(c.CuentaPorCobrarID, out var ab) ? ab : 0m));
                int notasPendientes = datos.Count(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento > hoy.AddDays(7));
                int vencidas = datos.Count(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento < hoy);
                int porVencer = datos.Count(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL")
                                              && c.FechaVencimiento >= hoy
                                              && c.FechaVencimiento <= hoy.AddDays(7));

                lblTotalPendiente.Text = totalPendiente.ToString("C2");
                lblNotasPendientes.Text = notasPendientes.ToString();
                lblNotasVencidas.Text = vencidas.ToString();
                lblPorVencer.Text = porVencer.ToString();
            }
        }

        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                DateTime hoy = AppHelper.Hoy;

                IQueryable<Modelo.CuentasPorCobrar> q = db.CuentasPorCobrar.AsQueryable();

                // Filtro cliente
                if (!string.IsNullOrEmpty(ddlFiltroCliente.SelectedValue))
                {
                    int cliID = int.Parse(ddlFiltroCliente.SelectedValue);
                    q = q.Where(c => c.ClienteID == cliID);
                }

                // Filtro estado
                string filtroEstado = ddlFiltroEstado.SelectedValue;
                if (filtroEstado == "SINCOBRAR")
                    q = q.Where(c => c.Estado == "PENDIENTE" || c.Estado == "PARCIAL");
                else if (filtroEstado == "VENCIDA")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento < hoy);
                else if (filtroEstado == "PORVENCER")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL")
                                  && c.FechaVencimiento >= hoy
                                  && c.FechaVencimiento <= hoy.AddDays(7));
                else if (filtroEstado == "PENDIENTE")
                    q = q.Where(c => (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL") && c.FechaVencimiento > hoy.AddDays(7));
                else if (filtroEstado == "PARCIAL")
                    q = q.Where(c => c.Estado == "PARCIAL");
                else if (filtroEstado == "PAGADA")
                    q = q.Where(c => c.Estado == "PAGADA");
                // filtroEstado == "" = todas, sin filtro adicional

                // Filtro rango de fechas (fecha de entrega)
                if (!string.IsNullOrEmpty(txtFiltroFechaDesde.Text))
                {
                    DateTime fd = DateTime.Parse(txtFiltroFechaDesde.Text);
                    q = q.Where(c => c.FechaEntrega >= fd);
                }
                if (!string.IsNullOrEmpty(txtFiltroFechaHasta.Text))
                {
                    DateTime fh = DateTime.Parse(txtFiltroFechaHasta.Text).AddDays(1);
                    q = q.Where(c => c.FechaEntrega < fh);
                }

                // Filtro de búsqueda por Folio de Entrega o No. de Factura
                string buscar = (txtFiltroBusqueda.Text ?? "").Trim().ToLower();
                if (!string.IsNullOrEmpty(buscar))
                {
                    q = q.Where(c =>
                        (c.NumeroFactura != null && c.NumeroFactura.ToLower().Contains(buscar)) ||
                        c.Entregas.Folio.ToLower().Contains(buscar));
                }

                int total = q.Count();
                int pageIdx = gvCxC.PageIndex;
                int pageSz = gvCxC.PageSize;
                gvCxC.VirtualItemCount = total;

                lblResultados.Text = total == 0
                    ? "Sin registros para los filtros aplicados."
                    : string.Format("{0} registro(s) encontrado(s).", total);

                if (total == 0)
                {
                    gvCxC.DataSource = new List<CxCRow>();
                    gvCxC.DataBind();
                    return;
                }

                var ids = q
                    .OrderBy(c => c.Estado == "PAGADA" ? 1 : 0)       // pendientes primero
                    .ThenBy(c => c.FechaVencimiento)                   // más urgentes arriba
                    .Select(c => c.CuentaPorCobrarID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                var raw = (from c in db.CuentasPorCobrar
                           where ids.Contains(c.CuentaPorCobrarID)
                           join ent in db.Entregas on c.EntregaID equals ent.EntregaID
                           join cli in db.Clientes on c.ClienteID equals cli.ClienteID
                           select new
                           {
                               c.CuentaPorCobrarID,
                               c.EntregaID,
                               c.NumeroFactura,
                               c.FechaEntrega,
                               c.FechaVencimiento,
                               c.DiasCreditoAplicados,
                               c.MontoTotal,
                               c.Estado,
                               FolioEntrega = ent.Folio,
                               ClienteNombre = cli.Nombre
                           }).ToList();

                string rol = Session["Rol"]?.ToString() ?? "";
                bool puedeRegistrarCobro = (rol == "Administrador" || rol == "Ventas" || rol == "Jefe de planta");

                var totalesAbonados = db.AbonosCuentasPorCobrar
                    .Where(a => ids.Contains(a.CuentaPorCobrarID) && a.Estado == "ACTIVO")
                    .GroupBy(a => a.CuentaPorCobrarID)
                    .Select(g => new { CuentaPorCobrarID = g.Key, Total = g.Sum(a => a.MontoAbono) })
                    .ToDictionary(x => x.CuentaPorCobrarID, x => x.Total);

                var pagina = new List<CxCRow>();
                foreach (var id in ids)
                {
                    var r = raw.FirstOrDefault(x => x.CuentaPorCobrarID == id);
                    if (r == null) continue;

                    string estadoVisual, badgeClass;
                    if (r.Estado == "CANCELADA")
                    {
                        estadoVisual = "CANCELADA"; badgeClass = "badge-secondary";
                    }
                    else if (r.Estado == "PAGADA")
                    {
                        estadoVisual = "PAGADA"; badgeClass = "badge-success";
                    }
                    else if (r.FechaVencimiento < hoy)
                    {
                        estadoVisual = "VENCIDA"; badgeClass = "badge-danger";
                    }
                    else if (r.FechaVencimiento <= hoy.AddDays(7))
                    {
                        estadoVisual = "POR VENCER"; badgeClass = "badge-warning";
                    }
                    else if (r.Estado == "PARCIAL")
                    {
                        estadoVisual = "PARCIAL"; badgeClass = "badge-info";
                    }
                    else
                    {
                        estadoVisual = "PENDIENTE"; badgeClass = "badge-primary";
                    }

                    decimal abonado = totalesAbonados.TryGetValue(id, out var ab) ? ab : 0m;

                    pagina.Add(new CxCRow
                    {
                        CuentaPorCobrarID = r.CuentaPorCobrarID,
                        EntregaID = r.EntregaID,
                        FolioEntrega = r.FolioEntrega ?? "",
                        NumeroFactura = r.NumeroFactura ?? "",
                        ClienteNombre = r.ClienteNombre ?? "",
                        FechaEntrega = r.FechaEntrega,
                        FechaVencimiento = r.FechaVencimiento,
                        DiasCreditoAplicados = r.DiasCreditoAplicados,
                        MontoTotal = r.MontoTotal,
                        SaldoPendiente = r.MontoTotal - abonado,
                        Estado = r.Estado,
                        EstadoVisual = estadoVisual,
                        BadgeClass = badgeClass,
                        PuedeAbonar = puedeRegistrarCobro && (r.Estado == "PENDIENTE" || r.Estado == "PARCIAL")
                    });
                }

                gvCxC.DataSource = pagina;
                gvCxC.DataBind();
            }
        }

        // ══ Eventos de filtros y grid ═════════════════════════════════════════
        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            gvCxC.PageIndex = 0;
            CargarResumen();
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtFiltroBusqueda.Text = "";
            ddlFiltroCliente.SelectedIndex = 0;
            ddlFiltroEstado.SelectedIndex = 0;
            txtFiltroFechaDesde.Text = "";
            txtFiltroFechaHasta.Text = "";
            gvCxC.PageIndex = 0;
            CargarResumen();
            CargarGrid();
        }

        protected void gvCxC_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvCxC.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // ══ Helper para botones Abonar / Ver Abonos en GridView ══════════════
        public string GetBotonesAccion(object cxcIDObj, object puedeObj, object clienteObj,
                                        object facturaObj, object saldoObj)
        {
            int cxcID = Convert.ToInt32(cxcIDObj);
            string cliente = System.Web.HttpUtility.JavaScriptStringEncode(clienteObj?.ToString() ?? "");
            string factura = System.Web.HttpUtility.JavaScriptStringEncode(facturaObj?.ToString() ?? "");
            string saldo = ((decimal)saldoObj).ToString("C2");
            bool puede = puedeObj is bool b && b;

            string btnAbonar = puede
                ? string.Format(
                    "<button type='button' class='btn btn-sm btn-success mr-1' title='Registrar Cobro' " +
                    "onclick=\"abrirAbono({0}, '{1}', '{2}', '{3}')\">" +
                    "<i class='fas fa-money-bill-wave'></i></button>",
                    cxcID, cliente, factura, saldo)
                : "<button type='button' class='btn btn-sm btn-outline-success mr-1' disabled title='Sin permiso'>" +
                  "<i class='fas fa-money-bill-wave'></i></button>";

            string btnVerAbonos = string.Format(
                "<button type='button' class='btn btn-sm btn-outline-primary' title='Ver Cobros' " +
                "onclick=\"abrirHistorialAbonos({0}, '{1}', '{2}')\">" +
                "<i class='fas fa-list'></i></button>",
                cxcID, cliente, factura);

            return btnAbonar + btnVerAbonos;
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text)
        {
            var obj = new { icon, title, text };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        // ══ WebMethods ═══════════════════════════════════════════════════════

        // Recalcula Estado/FechaCobro/ReferenciaCobro/CobradaPorID de la cabecera a partir
        // de la suma de abonos ACTIVOs. Fuente de verdad real: AbonosCuentasPorCobrar.
        private static void RecalcularEstadoCuentaCxC(InventarioAnkhalDBDataContext db, int cuentaPorCobrarID)
        {
            var cxc = db.CuentasPorCobrar.First(c => c.CuentaPorCobrarID == cuentaPorCobrarID);
            if (cxc.Estado == "CANCELADA") return; // estado reservado, sin lógica activa hoy

            decimal totalAbonado = db.AbonosCuentasPorCobrar
                .Where(a => a.CuentaPorCobrarID == cuentaPorCobrarID && a.Estado == "ACTIVO")
                .Sum(a => (decimal?)a.MontoAbono) ?? 0m;

            if (totalAbonado <= 0m)
            {
                cxc.Estado = "PENDIENTE";
                cxc.FechaCobro = null;
                cxc.ReferenciaCobro = null;
                cxc.CobradaPorID = null;
            }
            else if (totalAbonado < cxc.MontoTotal)
            {
                cxc.Estado = "PARCIAL";
                cxc.FechaCobro = null;
                cxc.ReferenciaCobro = null;
                cxc.CobradaPorID = null;
            }
            else
            {
                cxc.Estado = "PAGADA";
                var ultimo = db.AbonosCuentasPorCobrar
                    .Where(a => a.CuentaPorCobrarID == cuentaPorCobrarID && a.Estado == "ACTIVO")
                    .OrderByDescending(a => a.FechaAbono).ThenByDescending(a => a.AbonoID)
                    .First();
                cxc.FechaCobro = ultimo.FechaAbono;
                cxc.ReferenciaCobro = ultimo.ReferenciaPago;
                cxc.CobradaPorID = ultimo.RegistradoPorID;
            }
        }

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object RegistrarAbono(int cuentaPorCobrarID, decimal monto, string referencia, string observaciones)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                return new { ok = false, msg = "Sesión expirada. Recargue la página." };

            string rol = HttpContext.Current.Session["Rol"]?.ToString() ?? "";
            if (rol != "Administrador" && rol != "Ventas" && rol != "Jefe de planta")
                return new { ok = false, msg = "No tiene permiso para registrar cobros." };

            if (monto <= 0)
                return new { ok = false, msg = "El monto del cobro debe ser mayor a cero." };

            int claveID = (int)HttpContext.Current.Session["ClaveID"];
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                db.Connection.Open();
                using (var tx = db.Connection.BeginTransaction())
                {
                    db.Transaction = tx;
                    try
                    {
                        // Lock de fila — evita condiciones de carrera entre cobros concurrentes
                        db.ExecuteQuery<int>(
                            "SELECT CuentaPorCobrarID FROM dbo.CuentasPorCobrar WITH (UPDLOCK, HOLDLOCK) WHERE CuentaPorCobrarID = {0}",
                            cuentaPorCobrarID).FirstOrDefault();

                        var cxc = db.CuentasPorCobrar.SingleOrDefault(c => c.CuentaPorCobrarID == cuentaPorCobrarID);
                        if (cxc == null)
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Cuenta no encontrada." };
                        }
                        if (cxc.Estado == "PAGADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Esta cuenta ya está liquidada." };
                        }
                        if (cxc.Estado == "CANCELADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Esta cuenta está cancelada." };
                        }

                        decimal totalAbonado = db.AbonosCuentasPorCobrar
                            .Where(a => a.CuentaPorCobrarID == cuentaPorCobrarID && a.Estado == "ACTIVO")
                            .Sum(a => (decimal?)a.MontoAbono) ?? 0m;
                        decimal saldoPendiente = cxc.MontoTotal - totalAbonado;

                        if (monto > saldoPendiente)
                        {
                            tx.Rollback();
                            return new { ok = false, msg = string.Format("El monto excede el saldo pendiente ({0:C2}).", saldoPendiente) };
                        }

                        db.AbonosCuentasPorCobrar.InsertOnSubmit(new AbonosCuentasPorCobrar
                        {
                            CuentaPorCobrarID = cuentaPorCobrarID,
                            MontoAbono = monto,
                            FechaAbono = AppHelper.Ahora,
                            ReferenciaPago = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim(),
                            Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim(),
                            Estado = "ACTIVO",
                            RegistradoPorID = claveID,
                            FechaRegistro = AppHelper.Ahora
                        });
                        db.SubmitChanges();

                        RecalcularEstadoCuentaCxC(db, cuentaPorCobrarID);
                        db.SubmitChanges();

                        tx.Commit();
                        return new { ok = true };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new { ok = false, msg = "Error al registrar el cobro: " + ex.Message };
                    }
                }
            }
        }

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object CancelarAbono(int abonoID, string motivo)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                return new { ok = false, msg = "Sesión expirada. Recargue la página." };

            string rol = HttpContext.Current.Session["Rol"]?.ToString() ?? "";
            if (rol != "Administrador" && rol != "Ventas" && rol != "Jefe de planta")
                return new { ok = false, msg = "No tiene permiso para cancelar cobros." };

            if (string.IsNullOrWhiteSpace(motivo))
                return new { ok = false, msg = "Debe indicar un motivo de cancelación." };

            int claveID = (int)HttpContext.Current.Session["ClaveID"];
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var abonoPrev = db.AbonosCuentasPorCobrar.SingleOrDefault(a => a.AbonoID == abonoID);
                if (abonoPrev == null)
                    return new { ok = false, msg = "Cobro no encontrado." };
                int cuentaPorCobrarID = abonoPrev.CuentaPorCobrarID;

                db.Connection.Open();
                using (var tx = db.Connection.BeginTransaction())
                {
                    db.Transaction = tx;
                    try
                    {
                        db.ExecuteQuery<int>(
                            "SELECT CuentaPorCobrarID FROM dbo.CuentasPorCobrar WITH (UPDLOCK, HOLDLOCK) WHERE CuentaPorCobrarID = {0}",
                            cuentaPorCobrarID).FirstOrDefault();

                        var abono = db.AbonosCuentasPorCobrar.Single(a => a.AbonoID == abonoID);
                        if (abono.Estado != "ACTIVO")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Este cobro ya fue cancelado." };
                        }

                        var cxc = db.CuentasPorCobrar.Single(c => c.CuentaPorCobrarID == cuentaPorCobrarID);
                        if (cxc.Estado == "CANCELADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "La cuenta está cancelada; no se puede modificar." };
                        }

                        db.CancelacionesAbonosCxC.InsertOnSubmit(new CancelacionesAbonosCxC
                        {
                            AbonoID = abonoID,
                            Motivo = motivo.Trim(),
                            CanceladoPorID = claveID,
                            FechaCancelacion = AppHelper.Ahora
                        });
                        abono.Estado = "CANCELADO";
                        db.SubmitChanges();

                        RecalcularEstadoCuentaCxC(db, cuentaPorCobrarID);
                        db.SubmitChanges();

                        tx.Commit();
                        return new { ok = true };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new { ok = false, msg = "Error al cancelar el cobro: " + ex.Message };
                    }
                }
            }
        }

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object ObtenerAbonos(int cuentaPorCobrarID)
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                string rol = HttpContext.Current.Session["Rol"]?.ToString() ?? "";
                bool puedeCancelar = (rol == "Administrador" || rol == "Ventas" || rol == "Jefe de planta");

                var abonos = db.AbonosCuentasPorCobrar
                    .Where(a => a.CuentaPorCobrarID == cuentaPorCobrarID)
                    .OrderByDescending(a => a.FechaAbono)
                    .ToList();
                var abonoIds = abonos.Select(a => a.AbonoID).ToList();
                var cancelaciones = db.CancelacionesAbonosCxC
                    .Where(c => abonoIds.Contains(c.AbonoID))
                    .ToList();

                // Resolver nombres: mismo patrón que CuentasPorPagar.aspx.cs (ClaveID → DatosUsuario
                // → UsuarioID → UsuarioService API externa, con fallback si falla).
                var claveIds = abonos.Select(a => a.RegistradoPorID)
                    .Concat(cancelaciones.Select(c => c.CanceladoPorID))
                    .Distinct().ToList();
                Dictionary<int, string> nombres = new Dictionary<int, string>();
                try
                {
                    var claveToUsuario = db.DatosUsuario
                        .Where(du => claveIds.Contains(du.ClaveID))
                        .Select(du => new { du.ClaveID, du.UsuarioID })
                        .ToList();
                    var usuarioIds = claveToUsuario.Where(x => x.UsuarioID.HasValue)
                        .Select(x => x.UsuarioID.Value).ToList();
                    var apiNombres = UsuarioService.ObtenerEmpleadosBulk(usuarioIds)
                        .ToDictionary(e => e.IdUsuario, e => e.NombreCompleto);
                    nombres = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => x.UsuarioID.HasValue && apiNombres.ContainsKey(x.UsuarioID.Value)
                             ? apiNombres[x.UsuarioID.Value]
                             : $"Usuario {x.ClaveID}");
                }
                catch { /* si falla la API, fallback abajo con $"Usuario {ClaveID}" */ }

                return abonos.Select(a =>
                {
                    var canc = cancelaciones.FirstOrDefault(c => c.AbonoID == a.AbonoID);
                    return new
                    {
                        abonoID = a.AbonoID,
                        monto = a.MontoAbono,
                        fecha = a.FechaAbono.ToString("dd/MM/yyyy HH:mm"),
                        referencia = a.ReferenciaPago,
                        observaciones = a.Observaciones,
                        estado = a.Estado,
                        registradoPor = nombres.ContainsKey(a.RegistradoPorID) ? nombres[a.RegistradoPorID] : $"Usuario {a.RegistradoPorID}",
                        puedeCancelar = puedeCancelar && a.Estado == "ACTIVO",
                        canceladoPor = canc != null
                            ? (nombres.ContainsKey(canc.CanceladoPorID) ? nombres[canc.CanceladoPorID] : $"Usuario {canc.CanceladoPorID}")
                            : null,
                        fechaCancelacion = canc != null ? canc.FechaCancelacion.ToString("dd/MM/yyyy HH:mm") : null,
                        motivoCancelacion = canc?.Motivo
                    };
                }).ToList();
            }
        }

        [WebMethod]
        public static object ObtenerDetalleEntrega(int entregaID)
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var raw = (from de in db.DetalleEntregas
                           where de.EntregaID == entregaID
                           join mat in db.Materiales
                               on de.MaterialID equals (int?)mat.MaterialID into matG
                           from mat in matG.DefaultIfEmpty()
                           join prd in db.Productos
                               on de.ProductoID equals (int?)prd.ProductoID into prdG
                           from prd in prdG.DefaultIfEmpty()
                           select new
                           {
                               de.TipoItem,
                               MatDesc = mat.Descripcion,
                               MatCod = mat.Codigo,
                               PrdDesc = prd.Descripcion,
                               PrdCod = prd.Codigo,
                               de.Cantidad,
                               de.PrecioUnitario
                           }).ToList();

                return raw.Select(r => new
                {
                    nombre = (r.TipoItem == "MATERIAL")
                                 ? "[" + (r.MatCod ?? "") + "] " + (r.MatDesc ?? "")
                                 : "[" + (r.PrdCod ?? "") + "] " + (r.PrdDesc ?? ""),
                    cantidad = r.Cantidad,
                    precio = r.PrecioUnitario,
                    subtotal = r.Cantidad * r.PrecioUnitario
                }).ToList();
            }
        }
    }
}
