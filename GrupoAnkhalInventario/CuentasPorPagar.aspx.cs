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
    public partial class CuentasPorPagar : System.Web.UI.Page
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
        public class CxPRow
        {
            public int CuentaPorPagarID { get; set; }
            public int LoteID { get; set; }
            public string FolioLote { get; set; }
            public string NumeroNota { get; set; }
            public string ProveedorNombre { get; set; }
            public DateTime FechaRecepcion { get; set; }
            public DateTime FechaVencimiento { get; set; }
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
                CargarFiltroProveedores();
                CargarResumen();
                CargarGrid();
            }
        }

        // ══ Catálogos de filtros ══════════════════════════════════════════════
        private void CargarFiltroProveedores()
        {
            using (var db = NuevoDb(false))
            {
                ddlFiltroProveedor.Items.Clear();
                ddlFiltroProveedor.Items.Add(new ListItem("-- Todos --", ""));
                var provs = db.Proveedores
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Nombre)
                    .Select(p => new { p.ProveedorID, p.Nombre })
                    .ToList();
                foreach (var p in provs)
                    ddlFiltroProveedor.Items.Add(new ListItem(p.Nombre, p.ProveedorID.ToString()));
            }
        }

        // ══ Cards de resumen ══════════════════════════════════════════════════
        private void CargarResumen()
        {
            using (var db = NuevoDb(false))
            {
                DateTime hoy = AppHelper.Hoy;

                // Mismos filtros que CargarGrid para que los cards coincidan con lo visible
                IQueryable<Modelo.CuentasPorPagar> q = db.CuentasPorPagar.AsQueryable();

                if (!string.IsNullOrEmpty(ddlFiltroProveedor.SelectedValue))
                {
                    int provID = int.Parse(ddlFiltroProveedor.SelectedValue);
                    q = q.Where(c => c.ProveedorID == provID);
                }

                string filtroEstado = ddlFiltroEstado.SelectedValue;
                if (filtroEstado == "SINPAGAR")
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
                    q = q.Where(c => c.FechaRecepcion >= fd);
                }
                if (!string.IsNullOrEmpty(txtFiltroFechaHasta.Text))
                {
                    DateTime fh = DateTime.Parse(txtFiltroFechaHasta.Text).AddDays(1);
                    q = q.Where(c => c.FechaRecepcion < fh);
                }

                var datos = q.Select(c => new { c.CuentaPorPagarID, c.MontoTotal, c.FechaVencimiento, c.Estado }).ToList();

                var idsResumen = datos.Select(c => c.CuentaPorPagarID).ToList();
                var abonosResumen = db.AbonosCuentasPorPagar
                    .Where(a => idsResumen.Contains(a.CuentaPorPagarID) && a.Estado == "ACTIVO")
                    .GroupBy(a => a.CuentaPorPagarID)
                    .Select(g => new { CuentaPorPagarID = g.Key, Total = g.Sum(a => a.MontoAbono) })
                    .ToDictionary(x => x.CuentaPorPagarID, x => x.Total);

                // Sobre el conjunto filtrado, calcular el saldo pendiente real (excluye pagadas)
                decimal totalPendiente = datos
                    .Where(c => c.Estado == "PENDIENTE" || c.Estado == "PARCIAL")
                    .Sum(c => c.MontoTotal - (abonosResumen.TryGetValue(c.CuentaPorPagarID, out var ab) ? ab : 0m));
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

                IQueryable<Modelo.CuentasPorPagar> q = db.CuentasPorPagar.AsQueryable();

                // Filtro proveedor
                if (!string.IsNullOrEmpty(ddlFiltroProveedor.SelectedValue))
                {
                    int provID = int.Parse(ddlFiltroProveedor.SelectedValue);
                    q = q.Where(c => c.ProveedorID == provID);
                }

                // Filtro estado
                string filtroEstado = ddlFiltroEstado.SelectedValue;
                if (filtroEstado == "SINPAGAR")
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

                // Filtro rango de fechas (fecha de recepción)
                if (!string.IsNullOrEmpty(txtFiltroFechaDesde.Text))
                {
                    DateTime fd = DateTime.Parse(txtFiltroFechaDesde.Text);
                    q = q.Where(c => c.FechaRecepcion >= fd);
                }
                if (!string.IsNullOrEmpty(txtFiltroFechaHasta.Text))
                {
                    DateTime fh = DateTime.Parse(txtFiltroFechaHasta.Text).AddDays(1);
                    q = q.Where(c => c.FechaRecepcion < fh);
                }

                int total = q.Count();
                int pageIdx = gvCxP.PageIndex;
                int pageSz = gvCxP.PageSize;
                gvCxP.VirtualItemCount = total;

                lblResultados.Text = total == 0
                    ? "Sin registros para los filtros aplicados."
                    : string.Format("{0} registro(s) encontrado(s).", total);

                if (total == 0)
                {
                    gvCxP.DataSource = new List<CxPRow>();
                    gvCxP.DataBind();
                    return;
                }

                var ids = q
                    .OrderBy(c => c.Estado == "PAGADA" ? 1 : 0)       // pendientes primero
                    .ThenBy(c => c.FechaVencimiento)                   // más urgentes arriba
                    .Select(c => c.CuentaPorPagarID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                var raw = (from c in db.CuentasPorPagar
                           where ids.Contains(c.CuentaPorPagarID)
                           join lm in db.LotesMovimiento on c.LoteID equals lm.LoteID
                           join prov in db.Proveedores on c.ProveedorID equals prov.ProveedorID
                           select new
                           {
                               c.CuentaPorPagarID,
                               c.LoteID,
                               c.NumeroNota,
                               c.FechaRecepcion,
                               c.FechaVencimiento,
                               c.MontoTotal,
                               c.Estado,
                               FolioLote = lm.Folio,
                               ProveedorNombre = prov.Nombre
                           }).ToList();

                string rol = Session["Rol"]?.ToString() ?? "";
                bool puedeRegistrarPago = (rol == "Administrador" || rol == "Compras");

                var totalesAbonados = db.AbonosCuentasPorPagar
                    .Where(a => ids.Contains(a.CuentaPorPagarID) && a.Estado == "ACTIVO")
                    .GroupBy(a => a.CuentaPorPagarID)
                    .Select(g => new { CuentaPorPagarID = g.Key, Total = g.Sum(a => a.MontoAbono) })
                    .ToDictionary(x => x.CuentaPorPagarID, x => x.Total);

                var pagina = new List<CxPRow>();
                foreach (var id in ids)
                {
                    var r = raw.FirstOrDefault(x => x.CuentaPorPagarID == id);
                    if (r == null) continue;

                    string estadoVisual, badgeClass;
                    if (r.Estado == "PAGADA")
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

                    pagina.Add(new CxPRow
                    {
                        CuentaPorPagarID = r.CuentaPorPagarID,
                        LoteID = r.LoteID,
                        FolioLote = r.FolioLote ?? "",
                        NumeroNota = r.NumeroNota ?? "",
                        ProveedorNombre = r.ProveedorNombre ?? "",
                        FechaRecepcion = r.FechaRecepcion,
                        FechaVencimiento = r.FechaVencimiento,
                        MontoTotal = r.MontoTotal,
                        SaldoPendiente = r.MontoTotal - abonado,
                        Estado = r.Estado,
                        EstadoVisual = estadoVisual,
                        BadgeClass = badgeClass,
                        PuedeAbonar = puedeRegistrarPago && (r.Estado == "PENDIENTE" || r.Estado == "PARCIAL")
                    });
                }

                gvCxP.DataSource = pagina;
                gvCxP.DataBind();
            }
        }

        // ══ Eventos de filtros y grid ═════════════════════════════════════════
        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            gvCxP.PageIndex = 0;
            CargarResumen();
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltroProveedor.SelectedIndex = 0;
            ddlFiltroEstado.SelectedIndex = 0;
            txtFiltroFechaDesde.Text = "";
            txtFiltroFechaHasta.Text = "";
            gvCxP.PageIndex = 0;
            CargarResumen();
            CargarGrid();
        }

        protected void gvCxP_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvCxP.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // ══ Helper para botones Abonar / Ver Abonos en GridView ══════════════
        public string GetBotonesAccion(object cxpIDObj, object puedeObj, object provObj,
                                        object notaObj, object saldoObj)
        {
            int cxpID = Convert.ToInt32(cxpIDObj);
            string proveedor = System.Web.HttpUtility.JavaScriptStringEncode(provObj?.ToString() ?? "");
            string nota = System.Web.HttpUtility.JavaScriptStringEncode(notaObj?.ToString() ?? "");
            string saldo = ((decimal)saldoObj).ToString("C2");
            bool puede = puedeObj is bool b && b;

            string btnAbonar = puede
                ? string.Format(
                    "<button type='button' class='btn btn-sm btn-success mr-1' title='Abonar' " +
                    "onclick=\"abrirAbono({0}, '{1}', '{2}', '{3}')\">" +
                    "<i class='fas fa-money-bill-wave'></i></button>",
                    cxpID, proveedor, nota, saldo)
                : "<button type='button' class='btn btn-sm btn-outline-success mr-1' disabled title='Sin permiso'>" +
                  "<i class='fas fa-money-bill-wave'></i></button>";

            string btnVerAbonos = string.Format(
                "<button type='button' class='btn btn-sm btn-outline-primary' title='Ver Abonos' " +
                "onclick=\"abrirHistorialAbonos({0}, '{1}', '{2}')\">" +
                "<i class='fas fa-list'></i></button>",
                cxpID, proveedor, nota);

            return btnAbonar + btnVerAbonos;
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text)
        {
            var obj = new { icon, title, text };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        // ══ WebMethods ═══════════════════════════════════════════════════════

        // Recalcula Estado/FechaPago/ReferenciaPago/PagadaPorID de la cabecera a partir
        // de la suma de abonos ACTIVOs. Fuente de verdad real: AbonosCuentasPorPagar.
        private static void RecalcularEstadoCuenta(InventarioAnkhalDBDataContext db, int cuentaPorPagarID)
        {
            var cxp = db.CuentasPorPagar.First(c => c.CuentaPorPagarID == cuentaPorPagarID);
            if (cxp.Estado == "CANCELADA") return; // estado reservado, sin lógica activa hoy

            decimal totalAbonado = db.AbonosCuentasPorPagar
                .Where(a => a.CuentaPorPagarID == cuentaPorPagarID && a.Estado == "ACTIVO")
                .Sum(a => (decimal?)a.MontoAbono) ?? 0m;

            if (totalAbonado <= 0m)
            {
                cxp.Estado = "PENDIENTE";
                cxp.FechaPago = null;
                cxp.ReferenciaPago = null;
                cxp.PagadaPorID = null;
            }
            else if (totalAbonado < cxp.MontoTotal)
            {
                cxp.Estado = "PARCIAL";
                cxp.FechaPago = null;
                cxp.ReferenciaPago = null;
                cxp.PagadaPorID = null;
            }
            else
            {
                cxp.Estado = "PAGADA";
                var ultimo = db.AbonosCuentasPorPagar
                    .Where(a => a.CuentaPorPagarID == cuentaPorPagarID && a.Estado == "ACTIVO")
                    .OrderByDescending(a => a.FechaAbono).ThenByDescending(a => a.AbonoID)
                    .First();
                cxp.FechaPago = ultimo.FechaAbono;
                cxp.ReferenciaPago = ultimo.ReferenciaPago;
                cxp.PagadaPorID = ultimo.RegistradoPorID;
            }
        }

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object RegistrarAbono(int cuentaPorPagarID, decimal monto, string referencia, string observaciones)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                return new { ok = false, msg = "Sesión expirada. Recargue la página." };

            string rol = HttpContext.Current.Session["Rol"]?.ToString() ?? "";
            if (rol != "Administrador" && rol != "Compras")
                return new { ok = false, msg = "No tiene permiso para registrar abonos." };

            if (monto <= 0)
                return new { ok = false, msg = "El monto del abono debe ser mayor a cero." };

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
                        // Lock de fila — evita condiciones de carrera entre abonos concurrentes
                        db.ExecuteQuery<int>(
                            "SELECT CuentaPorPagarID FROM dbo.CuentasPorPagar WITH (UPDLOCK, HOLDLOCK) WHERE CuentaPorPagarID = {0}",
                            cuentaPorPagarID).FirstOrDefault();

                        var cxp = db.CuentasPorPagar.SingleOrDefault(c => c.CuentaPorPagarID == cuentaPorPagarID);
                        if (cxp == null)
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Nota no encontrada." };
                        }
                        if (cxp.Estado == "PAGADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Esta nota ya está liquidada." };
                        }
                        if (cxp.Estado == "CANCELADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Esta nota está cancelada." };
                        }

                        decimal totalAbonado = db.AbonosCuentasPorPagar
                            .Where(a => a.CuentaPorPagarID == cuentaPorPagarID && a.Estado == "ACTIVO")
                            .Sum(a => (decimal?)a.MontoAbono) ?? 0m;
                        decimal saldoPendiente = cxp.MontoTotal - totalAbonado;

                        if (monto > saldoPendiente)
                        {
                            tx.Rollback();
                            return new { ok = false, msg = string.Format("El monto excede el saldo pendiente ({0:C2}).", saldoPendiente) };
                        }

                        db.AbonosCuentasPorPagar.InsertOnSubmit(new AbonosCuentasPorPagar
                        {
                            CuentaPorPagarID = cuentaPorPagarID,
                            MontoAbono = monto,
                            FechaAbono = AppHelper.Ahora,
                            ReferenciaPago = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim(),
                            Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim(),
                            Estado = "ACTIVO",
                            RegistradoPorID = claveID,
                            FechaRegistro = AppHelper.Ahora
                        });
                        db.SubmitChanges();

                        RecalcularEstadoCuenta(db, cuentaPorPagarID);
                        db.SubmitChanges();

                        tx.Commit();
                        return new { ok = true };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new { ok = false, msg = "Error al registrar el abono: " + ex.Message };
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
            if (rol != "Administrador" && rol != "Compras")
                return new { ok = false, msg = "No tiene permiso para cancelar abonos." };

            if (string.IsNullOrWhiteSpace(motivo))
                return new { ok = false, msg = "Debe indicar un motivo de cancelación." };

            int claveID = (int)HttpContext.Current.Session["ClaveID"];
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var abonoPrev = db.AbonosCuentasPorPagar.SingleOrDefault(a => a.AbonoID == abonoID);
                if (abonoPrev == null)
                    return new { ok = false, msg = "Abono no encontrado." };
                int cuentaPorPagarID = abonoPrev.CuentaPorPagarID;

                db.Connection.Open();
                using (var tx = db.Connection.BeginTransaction())
                {
                    db.Transaction = tx;
                    try
                    {
                        db.ExecuteQuery<int>(
                            "SELECT CuentaPorPagarID FROM dbo.CuentasPorPagar WITH (UPDLOCK, HOLDLOCK) WHERE CuentaPorPagarID = {0}",
                            cuentaPorPagarID).FirstOrDefault();

                        var abono = db.AbonosCuentasPorPagar.Single(a => a.AbonoID == abonoID);
                        if (abono.Estado != "ACTIVO")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "Este abono ya fue cancelado." };
                        }

                        var cxp = db.CuentasPorPagar.Single(c => c.CuentaPorPagarID == cuentaPorPagarID);
                        if (cxp.Estado == "CANCELADA")
                        {
                            tx.Rollback();
                            return new { ok = false, msg = "La cuenta está cancelada; no se puede modificar." };
                        }

                        db.CancelacionesAbonosCxP.InsertOnSubmit(new CancelacionesAbonosCxP
                        {
                            AbonoID = abonoID,
                            Motivo = motivo.Trim(),
                            CanceladoPorID = claveID,
                            FechaCancelacion = AppHelper.Ahora
                        });
                        abono.Estado = "CANCELADO";
                        db.SubmitChanges();

                        RecalcularEstadoCuenta(db, cuentaPorPagarID);
                        db.SubmitChanges();

                        tx.Commit();
                        return new { ok = true };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new { ok = false, msg = "Error al cancelar el abono: " + ex.Message };
                    }
                }
            }
        }

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object ObtenerAbonos(int cuentaPorPagarID)
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                string rol = HttpContext.Current.Session["Rol"]?.ToString() ?? "";
                bool puedeCancelar = (rol == "Administrador" || rol == "Compras");

                var abonos = db.AbonosCuentasPorPagar
                    .Where(a => a.CuentaPorPagarID == cuentaPorPagarID)
                    .OrderByDescending(a => a.FechaAbono)
                    .ToList();
                var abonoIds = abonos.Select(a => a.AbonoID).ToList();
                var cancelaciones = db.CancelacionesAbonosCxP
                    .Where(c => abonoIds.Contains(c.AbonoID))
                    .ToList();

                // Resolver nombres: mismo patrón que Movimientos.aspx.cs (ClaveID → DatosUsuario
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
        public static object ObtenerDetalleLote(int loteID)
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var raw = (from mv in db.Movimientos
                           where mv.LoteID == loteID
                           join mat in db.Materiales
                               on mv.MaterialID equals (int?)mat.MaterialID into matG
                           from mat in matG.DefaultIfEmpty()
                           join prd in db.Productos
                               on mv.ProductoID equals (int?)prd.ProductoID into prdG
                           from prd in prdG.DefaultIfEmpty()
                           select new
                           {
                               mv.TipoItem,
                               MatDesc = mat.Descripcion,
                               MatCod = mat.Codigo,
                               PrdDesc = prd.Descripcion,
                               PrdCod = prd.Codigo,
                               mv.Cantidad,
                               mv.Costo
                           }).ToList();

                return raw.Select(r => new
                {
                    nombre = (r.TipoItem == "Material" || r.TipoItem == "MermaMaterial")
                                 ? "[" + (r.MatCod ?? "") + "] " + (r.MatDesc ?? "")
                                 : "[" + (r.PrdCod ?? "") + "] " + (r.PrdDesc ?? ""),
                    cantidad = r.Cantidad,
                    costo = r.Costo,
                    subtotal = r.Cantidad * r.Costo
                }).ToList();
            }
        }
    }
}
