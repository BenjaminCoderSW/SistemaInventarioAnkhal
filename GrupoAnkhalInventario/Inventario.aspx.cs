using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class Inventario : Page
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
                SoloConExistencia = chkSoloExistencia.Checked
            };
        }

        // ── Carga principal ───────────────────────────────────────────────────
        private void CargarTodo()
        {
            var f = BuildFiltros();
            var svc = new InventarioService(_connStr);

            // Materiales
            if (f.TipoFiltro == "" || f.TipoFiltro == "MAT")
            {
                CargarMateriales(f, svc);
                pnlMateriales.Visible = true;
            }
            else
            {
                gvMateriales.DataSource = new List<MaterialInvVM>();
                gvMateriales.DataBind();
                pnlMateriales.Visible = false;
            }

            // Productos
            if (f.TipoFiltro == "" || f.TipoFiltro == "PROD")
            {
                CargarProductos(f, svc);
                pnlProductos.Visible = true;
            }
            else
            {
                gvProductos.DataSource = new List<ProductoInvVM>();
                gvProductos.DataBind();
                pnlProductos.Visible = false;
            }

            // Resumen por base
            var resumen = svc.ObtenerResumenPorBase(f);
            ViewState["ResumenTotales"] = new decimal[]
            {
                resumen.Sum(r => r.ValorMateriales),
                resumen.Sum(r => r.ValorBuenos),
                resumen.Sum(r => r.ValorRechazo),
                resumen.Sum(r => r.ValorMerma)
            };
            gvResumen.DataSource = resumen;
            gvResumen.DataBind();

            // Cards de valor total
            var kpis = svc.ObtenerKpis(f);
            lblValorMateriales.Text = kpis.ValorMateriales.ToString("N2");
            lblValorBuenos.Text = kpis.ValorBuenos.ToString("N2");
            lblValorRechazo.Text = kpis.ValorRechazo.ToString("N2");
            lblValorMermaMP.Text = kpis.ValorMerma.ToString("N2");
            lblValorTotal.Text = kpis.ValorTotal.ToString("N2");
        }

        // ── Materiales con paginación ─────────────────────────────────────────
        private void CargarMateriales(FiltrosInventario f, InventarioService svc = null)
        {
            if (svc == null) svc = new InventarioService(_connStr);
            var mats = svc.ObtenerMateriales(f);

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
            var prods = svc.ObtenerProductos(f);

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
            chkSoloExistencia.Checked = false;
            gvMateriales.PageIndex = 0;
            gvProductos.PageIndex = 0;
            CargarTodo();
        }

        // ══ ROW DATA BOUND ════════════════════════════════════════════════════

        protected void gvMateriales_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var vm = e.Row.DataItem as MaterialInvVM;
            if (vm == null || vm.StockBases == null || vm.StockBases.Count == 0) return;

            int lastCell = e.Row.Cells.Count - 1;
            e.Row.Cells[lastCell].Controls.Add(
                new LiteralControl(
                    "</td></tr>" +
                    "<tr id=\"accM_" + vm.MaterialID + "\" style=\"display:none;\">" +
                    "<td colspan=\"" + gvMateriales.Columns.Count + "\" style=\"padding:0;background:#eef3fa;\">" +
                    "<div class=\"bases-accordion\">" +
                    "<strong style='color:#003366'><i class='fas fa-warehouse'></i> Stock por base/planta</strong>" +
                    BuildMatBasesTable(vm) +
                    "</div></td>" +
                    "<td style='display:none'>"
                )
            );
        }

        private string BuildMatBasesTable(MaterialInvVM vm)
        {
            var sb = new StringBuilder();
            sb.Append("<table class='table table-sm mb-0 mt-1'>");
            sb.Append("<thead><tr><th>Base</th><th>Código</th><th>Cantidad</th><th>Nivel</th><th>Valor ($)</th><th style='color:#922b21'>Merma</th><th style='color:#922b21'>Valor Merma ($)</th></tr></thead><tbody>");

            foreach (var b in vm.StockBases)
            {
                string nivelCss = GetNivelCss(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                string icon = GetNivelIcon(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                string nivelTxt = GetNivelTextoCorto(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                decimal valor = b.Cantidad * vm.PrecioUnitario;
                decimal valorMerma = b.MermaBase * vm.PrecioUnitario;

                sb.Append("<tr>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><strong>" + b.Cantidad.ToString("N2") + "</strong> " + HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td><span class='nivel-badge " + nivelCss + "'>" + icon + " " + nivelTxt + "</span></td>");
                sb.Append("<td class='text-right'>" + valor.ToString("C2") + "</td>");
                sb.Append("<td style='color:#922b21; text-align:right;'>" + b.MermaBase.ToString("N2") + " " + HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td style='color:#922b21; text-align:right; font-weight:600;'>" + valorMerma.ToString("C2") + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        protected void gvProductos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var vm = e.Row.DataItem as ProductoInvVM;
            if (vm == null || vm.StockBases == null || vm.StockBases.Count == 0) return;

            int lastCell = e.Row.Cells.Count - 1;
            e.Row.Cells[lastCell].Controls.Add(
                new LiteralControl(
                    "</td></tr>" +
                    "<tr id=\"accP_" + vm.ProductoID + "\" style=\"display:none;\">" +
                    "<td colspan=\"" + gvProductos.Columns.Count + "\" style=\"padding:0;background:#eef3fa;\">" +
                    "<div class=\"bases-accordion\">" +
                    "<strong style='color:#003366'><i class='fas fa-warehouse'></i> Stock por base/planta</strong>" +
                    BuildProdBasesTable(vm) +
                    "</div></td>" +
                    "<td style='display:none'>"
                )
            );
        }

        private string BuildProdBasesTable(ProductoInvVM vm)
        {
            var sb = new StringBuilder();
            sb.Append("<table class='table table-sm mb-0 mt-1'>");
            sb.Append("<thead><tr><th>Base</th><th>Código</th><th>Buenos</th><th>Rechazo</th><th>Total</th><th>Valor Buenos ($)</th><th>Valor Rechazo ($)</th></tr></thead><tbody>");

            foreach (var b in vm.StockBases)
            {
                decimal valBuenos = b.Buenos * vm.PrecioVenta;
                decimal valRechazo = b.Rechazo * (vm.PrecioVenta * 0.5m);

                sb.Append("<tr>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><span class='badge badge-success'>" + b.Buenos + "</span></td>");
                sb.Append("<td><span class='badge badge-warning'>" + b.Rechazo + "</span></td>");
                sb.Append("<td><strong>" + (b.Buenos + b.Rechazo) + "</strong></td>");
                sb.Append("<td class='text-right text-success'>" + valBuenos.ToString("C2") + "</td>");
                sb.Append("<td class='text-right text-warning'>" + valRechazo.ToString("C2") + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        protected void gvResumen_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.Footer)
                return;

            var totales = ViewState["ResumenTotales"] as decimal[];
            if (totales == null || totales.Length < 4) return;

            e.Row.Cells[0].Text = "<strong>TOTAL</strong>";
            e.Row.Cells[1].Text = totales[0].ToString("C2");
            e.Row.Cells[2].Text = totales[1].ToString("C2");
            e.Row.Cells[3].Text = totales[2].ToString("C2");
            e.Row.Cells[4].Text = "<span style='color:#922b21'>" + totales[3].ToString("C2") + "</span>";
            e.Row.Cells[5].Text = "<strong>" + (totales[0] + totales[1] + totales[2] + totales[3]).ToString("C2") + "</strong>";
        }

        // ══ EXPORTAR EXCEL ════════════════════════════════════════════════════

        protected void btnExportarExcel_Click(object sender, EventArgs e)
        {
            var f = BuildFiltros();
            var svc = new InventarioService(_connStr);

            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'></head><body>");
            sb.Append("<h2>Inventario General — Grupo ANKHAL</h2>");
            sb.Append("<p>Fecha: " + AppHelper.Ahora.ToString("dd/MM/yyyy HH:mm") + "</p>");

            if (f.TipoFiltro == "" || f.TipoFiltro == "MAT")
            {
                sb.Append("<h3>Materiales</h3>");
                sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                sb.Append("<tr style='background:#003366;color:white'><th>Código</th><th>Descripción</th><th>Tipo</th><th>Unidad</th><th>Stock Global</th><th>Nivel</th><th>Precio Unit.</th><th>Valor ($)</th><th>Merma</th><th>Valor Merma ($)</th></tr>");

                foreach (var m in svc.ObtenerMateriales(f))
                {
                    string nivel = GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo);
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Unidad) + "</td>");
                    sb.Append("<td>" + m.StockGlobal.ToString("N2") + "</td>");
                    sb.Append("<td>" + nivel + "</td>");
                    sb.Append("<td>" + m.PrecioUnitario.ToString("C2") + "</td>");
                    sb.Append("<td>" + (m.StockGlobal * m.PrecioUnitario).ToString("C2") + "</td>");
                    sb.Append("<td>" + m.MermaGlobal.ToString("N2") + "</td>");
                    sb.Append("<td>" + (m.MermaGlobal * m.PrecioUnitario).ToString("C2") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table><br/>");
            }

            if (f.TipoFiltro == "" || f.TipoFiltro == "PROD")
            {
                sb.Append("<h3>Productos</h3>");
                sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                sb.Append("<tr style='background:#003366;color:white'><th>Código</th><th>Descripción</th><th>Tipo</th><th>Buenos</th><th>Rechazo</th><th>Total</th><th>Precio Venta</th><th>Valor Buenos ($)</th><th>Valor Rechazo ($)</th></tr>");

                foreach (var p in svc.ObtenerProductos(f))
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.TipoNombre) + "</td>");
                    sb.Append("<td>" + p.TotalBuenos + "</td>");
                    sb.Append("<td>" + p.TotalRechazo + "</td>");
                    sb.Append("<td>" + (p.TotalBuenos + p.TotalRechazo) + "</td>");
                    sb.Append("<td>" + p.PrecioVenta.ToString("C2") + "</td>");
                    sb.Append("<td>" + (p.TotalBuenos * p.PrecioVenta).ToString("C2") + "</td>");
                    sb.Append("<td>" + (p.TotalRechazo * p.PrecioVenta * 0.5m).ToString("C2") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("</body></html>");

            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment;filename=Inventario_" + AppHelper.Ahora.ToString("yyyyMMdd") + ".xls");
            Response.ContentEncoding = Encoding.UTF8;
            Response.Write(sb.ToString());
            Response.End();
        }

        // ══ IMPRIMIR (abre handler en nueva ventana) ══════════════════════════

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            string baseParam = string.IsNullOrEmpty(ddlBase.SelectedValue) ? "" : ddlBase.SelectedValue;
            string tipoParam = ddlTipoItem.SelectedValue;
            string buscarMat = txtBuscarMateriales.Text.Trim();
            string buscarProd = txtBuscarProductos.Text.Trim();
            string soloExist = chkSoloExistencia.Checked ? "1" : "";
            string url = string.Format(
                "ImprimirInventario.ashx?base={0}&tipo={1}&buscarMat={2}&buscarProd={3}&soloExistencia={4}",
                baseParam, tipoParam,
                Uri.EscapeDataString(buscarMat),
                Uri.EscapeDataString(buscarProd),
                soloExist);
            ScriptManager.RegisterStartupScript(this, GetType(), "printInv",
                "window.open('" + url + "','_blank','width=1000,height=750,scrollbars=yes');", true);
        }

        // ══ HELPERS DE NIVEL ══════════════════════════════════════════════════

        public string GetNivel(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            if (stock == 0)        return "sin";
            if (stock < minimo)    return "critico";
            if (stock <= maximo)   return "optimo";
            return "exceso";
        }

        public string GetNivelCss(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "nivel-critico";
                case "exceso":  return "nivel-exceso";
                case "optimo":  return "nivel-optimo";
                default:        return "nivel-sin";
            }
        }

        public string GetNivelIcon(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "🔴";
                case "exceso":  return "🟡";
                case "optimo":  return "🟢";
                default:        return "⚪";
            }
        }

        public string GetNivelTextoCorto(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "Crítico";
                case "exceso":  return "Exceso";
                case "optimo":  return "Óptimo";
                default:        return "Sin stock";
            }
        }

        private void SetMsg(string icon, string title, string text)
        {
            var obj = new { icon, title, text, modal = "" };
            hdnMensajePendiente.Value = new JavaScriptSerializer().Serialize(obj);
        }
    }
}
