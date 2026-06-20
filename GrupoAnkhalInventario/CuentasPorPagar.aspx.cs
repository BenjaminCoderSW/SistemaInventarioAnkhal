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
            public string Estado { get; set; }
            public string EstadoVisual { get; set; }
            public string BadgeClass { get; set; }
            public bool PuedeRegistrarPago { get; set; }
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
                    q = q.Where(c => c.Estado == "PENDIENTE");
                else if (filtroEstado == "VENCIDA")
                    q = q.Where(c => c.Estado == "PENDIENTE" && c.FechaVencimiento < hoy);
                else if (filtroEstado == "PORVENCER")
                    q = q.Where(c => c.Estado == "PENDIENTE"
                                  && c.FechaVencimiento >= hoy
                                  && c.FechaVencimiento <= hoy.AddDays(7));
                else if (filtroEstado == "PENDIENTE")
                    q = q.Where(c => c.Estado == "PENDIENTE" && c.FechaVencimiento > hoy.AddDays(7));
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

                var datos = q.Select(c => new { c.MontoTotal, c.FechaVencimiento, c.Estado }).ToList();

                // Sobre el conjunto filtrado, calcular solo los pendientes (excluir pagadas del monto)
                decimal totalPendiente = datos
                    .Where(c => c.Estado == "PENDIENTE")
                    .Sum(c => (decimal?)c.MontoTotal) ?? 0m;
                int notasPendientes = datos.Count(c => c.Estado == "PENDIENTE" && c.FechaVencimiento > hoy.AddDays(7));
                int vencidas = datos.Count(c => c.Estado == "PENDIENTE" && c.FechaVencimiento < hoy);
                int porVencer = datos.Count(c => c.Estado == "PENDIENTE"
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
                    q = q.Where(c => c.Estado == "PENDIENTE");
                else if (filtroEstado == "VENCIDA")
                    q = q.Where(c => c.Estado == "PENDIENTE" && c.FechaVencimiento < hoy);
                else if (filtroEstado == "PORVENCER")
                    q = q.Where(c => c.Estado == "PENDIENTE"
                                  && c.FechaVencimiento >= hoy
                                  && c.FechaVencimiento <= hoy.AddDays(7));
                else if (filtroEstado == "PENDIENTE")
                    q = q.Where(c => c.Estado == "PENDIENTE" && c.FechaVencimiento > hoy.AddDays(7));
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
                    else
                    {
                        estadoVisual = "PENDIENTE"; badgeClass = "badge-primary";
                    }

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
                        Estado = r.Estado,
                        EstadoVisual = estadoVisual,
                        BadgeClass = badgeClass,
                        PuedeRegistrarPago = puedeRegistrarPago && r.Estado == "PENDIENTE"
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

        // ══ Helper para botón Registrar Pago en GridView ═════════════════════
        public string GetBtnPago(object cxpIDObj, object puedeObj, object provObj,
                                  object notaObj, object montoObj)
        {
            bool puede = puedeObj is bool b && b;
            if (!puede)
                return "<button type='button' class='btn btn-sm btn-outline-success' disabled title='Sin permiso'>" +
                       "<i class='fas fa-check'></i></button>";

            int cxpID = Convert.ToInt32(cxpIDObj);
            string proveedor = System.Web.HttpUtility.JavaScriptStringEncode(provObj?.ToString() ?? "");
            string nota = System.Web.HttpUtility.JavaScriptStringEncode(notaObj?.ToString() ?? "");
            string monto = ((decimal)montoObj).ToString("C2");

            return string.Format(
                "<button type='button' class='btn btn-sm btn-success' title='Registrar Pago' " +
                "onclick=\"abrirPago({0}, '{1}', '{2}', '{3}')\">" +
                "<i class='fas fa-check'></i></button>",
                cxpID, proveedor, nota, monto);
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text)
        {
            var obj = new { icon, title, text };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        // ══ WebMethods ═══════════════════════════════════════════════════════

        [WebMethod(EnableSession = true), ScriptMethod]
        public static object RegistrarPago(int cuentaPorPagarID, string referencia)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                return new { ok = false, msg = "Sesión expirada. Recargue la página." };

            int claveID = (int)HttpContext.Current.Session["ClaveID"];
            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var cxp = db.CuentasPorPagar
                    .SingleOrDefault(c => c.CuentaPorPagarID == cuentaPorPagarID);

                if (cxp == null)
                    return new { ok = false, msg = "Nota no encontrada." };

                if (cxp.Estado == "PAGADA")
                    return new { ok = false, msg = "Esta nota ya fue registrada como pagada." };

                cxp.Estado = "PAGADA";
                cxp.FechaPago = AppHelper.Ahora;
                cxp.ReferenciaPago = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
                cxp.PagadaPorID = claveID;
                db.SubmitChanges();

                return new { ok = true };
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
