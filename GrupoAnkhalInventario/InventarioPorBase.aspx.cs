using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class InventarioPorBase : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ─────────────────────────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarDropdownBase();
                CargarTodo();
            }
            else
            {
                if (ViewState["TotalMat"] != null)
                    gvMateriales.VirtualItemCount = (int)ViewState["TotalMat"];
                if (ViewState["TotalProd"] != null)
                    gvProductos.VirtualItemCount = (int)ViewState["TotalProd"];
            }
        }

        // ── Dropdown de bases ─────────────────────────────────────────────────
        private void CargarDropdownBase()
        {
            var bases = AppHelper.ObtenerBasesActivasParaUsuario(Session);
            ddlBase.Items.Clear();
            ddlBase.Items.Add(new ListItem("-- Todas --", ""));
            foreach (var b in bases)
                ddlBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));
        }

        // ── Constructor de filtros desde los controles UI ─────────────────────
        private FiltrosInventario BuildFiltros()
        {
            return new FiltrosInventario
            {
                BasesUsuario = AppHelper.ObtenerBasesUsuario(Session),
                BaseFiltraID = string.IsNullOrEmpty(ddlBase.SelectedValue)
                    ? (int?)null : int.Parse(ddlBase.SelectedValue),
                TipoFiltro = ddlTipoItem.SelectedValue,
                BuscarMat = txtBuscarMateriales.Text.Trim(),
                BuscarProd = txtBuscarProductos.Text.Trim(),
                SoloConExistencia = true
            };
        }

        // ── Carga principal ───────────────────────────────────────────────────
        private void CargarTodo()
        {
            var f = BuildFiltros();
            var svc = new InventarioService(_connStr);

            if (f.TipoFiltro == "" || f.TipoFiltro == "MAT")
            {
                CargarMateriales(f, svc);
                pnlMateriales.Visible = true;
            }
            else
            {
                gvMateriales.DataSource = new List<DetalleBaseMatVM>();
                gvMateriales.DataBind();
                pnlMateriales.Visible = false;
            }

            if (f.TipoFiltro == "" || f.TipoFiltro == "PROD")
            {
                CargarProductos(f, svc);
                pnlProductos.Visible = true;
            }
            else
            {
                gvProductos.DataSource = new List<DetalleBaseProdVM>();
                gvProductos.DataBind();
                pnlProductos.Visible = false;
            }
        }

        // ── Materiales con paginación ─────────────────────────────────────────
        private void CargarMateriales(FiltrosInventario f, InventarioService svc = null)
        {
            if (svc == null) svc = new InventarioService(_connStr);
            var mats = svc.ObtenerMaterialesPorBase(f);

            int total = mats.Count;
            ViewState["TotalMat"] = total;
            gvMateriales.VirtualItemCount = total;
            lblTotalMateriales.Text = total.ToString();

            int pageIdx = gvMateriales.PageIndex;
            int pageSz = gvMateriales.PageSize;
            gvMateriales.DataSource = mats.Skip(pageIdx * pageSz).Take(pageSz).ToList();
            gvMateriales.DataBind();
        }

        // ── Productos con paginación ──────────────────────────────────────────
        private void CargarProductos(FiltrosInventario f, InventarioService svc = null)
        {
            if (svc == null) svc = new InventarioService(_connStr);
            var prods = svc.ObtenerProductosPorBase(f);

            int total = prods.Count;
            ViewState["TotalProd"] = total;
            gvProductos.VirtualItemCount = total;
            lblTotalProductos.Text = total.ToString();

            int pageIdx = gvProductos.PageIndex;
            int pageSz = gvProductos.PageSize;
            gvProductos.DataSource = prods.Skip(pageIdx * pageSz).Take(pageSz).ToList();
            gvProductos.DataBind();
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════

        protected void gvMateriales_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMateriales.PageIndex = e.NewPageIndex;
            CargarMateriales(BuildFiltros());
        }

        protected void gvProductos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProductos.PageIndex = e.NewPageIndex;
            CargarProductos(BuildFiltros());
        }

        // ══ BOTONES ═══════════════════════════════════════════════════════════

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            gvMateriales.PageIndex = 0;
            gvProductos.PageIndex = 0;
            CargarTodo();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlBase.SelectedIndex = 0;
            ddlTipoItem.SelectedIndex = 0;
            txtBuscarMateriales.Text = "";
            txtBuscarProductos.Text = "";
            gvMateriales.PageIndex = 0;
            gvProductos.PageIndex = 0;
            CargarTodo();
        }

        // ══ EXPORTAR EXCEL ════════════════════════════════════════════════════

        protected void btnExportarExcel_Click(object sender, EventArgs e)
        {
            var f = BuildFiltros();
            var svc = new InventarioService(_connStr);
            string baseNombre = svc.ObtenerNombreBase(f.BaseFiltraID);

            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'></head><body>");
            sb.Append("<h2>Inventario por Base / Planta — Grupo ANKHAL</h2>");
            sb.Append("<p>Base: " + HttpUtility.HtmlEncode(baseNombre) + " | Fecha: " + AppHelper.Ahora.ToString("dd/MM/yyyy HH:mm") + "</p>");

            if (f.TipoFiltro == "" || f.TipoFiltro == "MAT")
            {
                sb.Append("<h3>Materiales por Base</h3>");
                sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                sb.Append("<tr style='background:#003366;color:white'>" +
                          "<th>Código</th><th>Descripción</th><th>Tipo</th><th>Base</th>" +
                          "<th>Cantidad</th><th>Unidad</th><th>Precio Unit.</th><th>Valor ($)</th></tr>");
                foreach (var m in svc.ObtenerMaterialesPorBase(f))
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.BaseNombre) + "</td>");
                    sb.Append("<td>" + m.Cantidad.ToString("N2") + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Unidad) + "</td>");
                    sb.Append("<td>" + m.PrecioUnitario.ToString("C2") + "</td>");
                    sb.Append("<td>" + m.ValorItem.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table><br/>");
            }

            if (f.TipoFiltro == "" || f.TipoFiltro == "PROD")
            {
                sb.Append("<h3>Productos por Base</h3>");
                sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                sb.Append("<tr style='background:#003366;color:white'>" +
                          "<th>Código</th><th>Descripción</th><th>Tipo</th><th>Base</th>" +
                          "<th>Buenos</th><th>Rechazo</th><th>Total</th>" +
                          "<th>Precio Venta</th><th>Valor Buenos ($)</th><th>Valor Rechazo ($)</th></tr>");
                foreach (var p in svc.ObtenerProductosPorBase(f))
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.BaseNombre) + "</td>");
                    sb.Append("<td>" + p.Buenos + "</td>");
                    sb.Append("<td>" + p.Rechazo + "</td>");
                    sb.Append("<td>" + (p.Buenos + p.Rechazo) + "</td>");
                    sb.Append("<td>" + p.PrecioVenta.ToString("C2") + "</td>");
                    sb.Append("<td>" + p.ValorBuenos.ToString("C2") + "</td>");
                    sb.Append("<td>" + p.ValorRechazo.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("</body></html>");

            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition",
                "attachment;filename=InventarioPorBase_" + AppHelper.Ahora.ToString("yyyyMMdd") + ".xls");
            Response.ContentEncoding = Encoding.UTF8;
            Response.Write(sb.ToString());
            Response.End();
        }

        // ══ IMPRIMIR ══════════════════════════════════════════════════════════

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            string baseParam = string.IsNullOrEmpty(ddlBase.SelectedValue) ? "" : ddlBase.SelectedValue;
            string tipoParam = ddlTipoItem.SelectedValue;
            string buscarMat = txtBuscarMateriales.Text.Trim();
            string buscarProd = txtBuscarProductos.Text.Trim();
            string soloExist = "1";
            string url = string.Format(
                "ImprimirInventarioPorBase.ashx?base={0}&tipo={1}&buscarMat={2}&buscarProd={3}&soloExistencia={4}",
                baseParam, tipoParam,
                Uri.EscapeDataString(buscarMat),
                Uri.EscapeDataString(buscarProd),
                soloExist);
            ScriptManager.RegisterStartupScript(this, GetType(), "printInvBase",
                "window.open('" + url + "','_blank','width=1000,height=750,scrollbars=yes');", true);
        }
    }
}
