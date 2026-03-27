using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
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

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ── DTOs ─────────────────────────────────────────────────────────────

        public class MaterialInvVM
        {
            public int MaterialID { get; set; }
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public string TipoNombre { get; set; }
            public string Unidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal StockMinimo { get; set; }
            public decimal StockMaximo { get; set; }
            public decimal StockOptimo { get; set; }
            public decimal StockGlobal { get; set; }
            public List<StockBaseInvVM> StockBases { get; set; }
        }

        public class StockBaseInvVM
        {
            public int BaseID { get; set; }
            public string BaseNombre { get; set; }
            public string BaseCodigo { get; set; }
            public decimal Cantidad { get; set; }
        }

        public class ProductoInvVM
        {
            public int ProductoID { get; set; }
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public string TipoNombre { get; set; }
            public decimal PrecioVenta { get; set; }
            public int TotalBuenos { get; set; }
            public int TotalRechazo { get; set; }
            public List<StockProdBaseVM> StockBases { get; set; }
        }

        public class StockProdBaseVM
        {
            public int BaseID { get; set; }
            public string BaseNombre { get; set; }
            public string BaseCodigo { get; set; }
            public int Buenos { get; set; }
            public int Rechazo { get; set; }
        }

        public class ResumenBaseVM
        {
            public int BaseID { get; set; }
            public string BaseNombre { get; set; }
            public decimal ValorMateriales { get; set; }
            public decimal ValorBuenos { get; set; }
            public decimal ValorRechazo { get; set; }
        }

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

        // ── Carga principal ───────────────────────────────────────────────────
        private void CargarTodo()
        {
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            int? baseFiltraID = string.IsNullOrEmpty(ddlBase.SelectedValue)
                ? (int?)null
                : int.Parse(ddlBase.SelectedValue);
            string tipoFiltro = ddlTipoItem.SelectedValue; // "", "MAT", "PROD"

            using (var db = NuevoDb(tracking: false))
            {
                // ── MATERIALES ────────────────────────────────────────────────
                if (tipoFiltro == "" || tipoFiltro == "MAT")
                {
                    CargarMateriales(db, basesUsuario, baseFiltraID);
                    pnlMateriales.Visible = true;
                }
                else
                {
                    gvMateriales.DataSource = new List<MaterialInvVM>();
                    gvMateriales.DataBind();
                    pnlMateriales.Visible = false;
                }

                // ── PRODUCTOS ─────────────────────────────────────────────────
                if (tipoFiltro == "" || tipoFiltro == "PROD")
                {
                    CargarProductos(db, basesUsuario, baseFiltraID);
                    pnlProductos.Visible = true;
                }
                else
                {
                    gvProductos.DataSource = new List<ProductoInvVM>();
                    gvProductos.DataBind();
                    pnlProductos.Visible = false;
                }

                // ── RESUMEN POR BASE ──────────────────────────────────────────
                CargarResumen(db, basesUsuario, baseFiltraID);

                // ── CARDS ─────────────────────────────────────────────────────
                ActualizarCards(db, basesUsuario, baseFiltraID);
            }
        }

        // ── Materiales con paginación ─────────────────────────────────────────
        private void CargarMateriales(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            int pageIdx = gvMateriales.PageIndex;
            int pageSz = gvMateriales.PageSize;

            var queryMat = from m in db.Materiales
                           join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                           where m.Activo
                           select new
                           {
                               m.MaterialID,
                               m.Codigo,
                               Descripcion = m.Descripcion,
                               TipoNombre = tp.Nombre,
                               m.Unidad,
                               m.PrecioUnitario,
                               StockMinimo = m.StockMinimo,
                               StockMaximo = m.StockMaximo,
                               m.StockOptimo
                           };

            queryMat = queryMat.OrderBy(m => m.Codigo);
            var listaMat = queryMat.ToList();

            // Stock filtrado por base del usuario y/o base seleccionada
            var stockQuery = from sm in db.StockMateriales
                             join b in db.Bases on sm.BaseID equals b.BaseID
                             where b.Activo
                             select new { sm.MaterialID, b.BaseID, b.Nombre, b.Codigo, sm.CantidadActual };

            if (basesUsuario != null)
                stockQuery = stockQuery.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue)
                stockQuery = stockQuery.Where(s => s.BaseID == baseFiltraID.Value);

            var stockTodos = stockQuery.ToList();

            // Construir VMs con stock global filtrado
            var vms = new List<MaterialInvVM>();
            foreach (var m in listaMat)
            {
                var basesStock = stockTodos
                    .Where(s => s.MaterialID == m.MaterialID)
                    .Select(s => new StockBaseInvVM
                    {
                        BaseID = s.BaseID,
                        BaseNombre = s.Nombre,
                        BaseCodigo = s.Codigo,
                        Cantidad = s.CantidadActual
                    }).ToList();

                decimal global = basesStock.Sum(s => s.Cantidad);

                // Solo incluir si tiene stock (o si no hay filtro de base)
                if (!baseFiltraID.HasValue || global > 0 || basesStock.Any())
                {
                    vms.Add(new MaterialInvVM
                    {
                        MaterialID = m.MaterialID,
                        Codigo = m.Codigo,
                        Descripcion = m.Descripcion,
                        TipoNombre = m.TipoNombre,
                        Unidad = m.Unidad,
                        PrecioUnitario = m.PrecioUnitario,
                        StockMinimo = m.StockMinimo,
                        StockMaximo = m.StockMaximo,
                        StockOptimo = m.StockOptimo,
                        StockGlobal = global,
                        StockBases = basesStock
                    });
                }
            }

            int total = vms.Count;
            ViewState["TotalMat"] = total;
            gvMateriales.VirtualItemCount = total;
            lblTotalMateriales.Text = total.ToString();

            gvMateriales.DataSource = vms.Skip(pageIdx * pageSz).Take(pageSz).ToList();
            gvMateriales.DataBind();

            // Guardar lista completa para Excel
            ViewState["MatVMs"] = null; // no guardar en ViewState por peso; se recalcula al exportar
        }

        // ── Productos con paginación ──────────────────────────────────────────
        private void CargarProductos(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            int pageIdx = gvProductos.PageIndex;
            int pageSz = gvProductos.PageSize;

            var queryProd = from p in db.Productos
                            join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                            where p.Activo
                            select new
                            {
                                p.ProductoID,
                                p.Codigo,
                                Descripcion = p.Descripcion,
                                TipoNombre = tp.Nombre,
                                p.PrecioVenta
                            };

            queryProd = queryProd.OrderBy(p => p.Codigo);
            var listaProd = queryProd.ToList();

            var stockProdQuery = from sp in db.StockProductos
                                 join b in db.Bases on sp.BaseID equals b.BaseID
                                 where b.Activo
                                 select new { sp.ProductoID, b.BaseID, b.Nombre, b.Codigo, sp.CantidadBuenas, sp.CantidadRechazo };

            if (basesUsuario != null)
                stockProdQuery = stockProdQuery.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue)
                stockProdQuery = stockProdQuery.Where(s => s.BaseID == baseFiltraID.Value);

            var stockProdTodos = stockProdQuery.ToList();

            var vms = new List<ProductoInvVM>();
            foreach (var p in listaProd)
            {
                var basesStock = stockProdTodos
                    .Where(s => s.ProductoID == p.ProductoID)
                    .Select(s => new StockProdBaseVM
                    {
                        BaseID = s.BaseID,
                        BaseNombre = s.Nombre,
                        BaseCodigo = s.Codigo,
                        Buenos = s.CantidadBuenas,
                        Rechazo = s.CantidadRechazo
                    }).ToList();

                int totalBuenos = basesStock.Sum(s => s.Buenos);
                int totalRechazo = basesStock.Sum(s => s.Rechazo);

                if (!baseFiltraID.HasValue || basesStock.Any())
                {
                    vms.Add(new ProductoInvVM
                    {
                        ProductoID = p.ProductoID,
                        Codigo = p.Codigo,
                        Descripcion = p.Descripcion,
                        TipoNombre = p.TipoNombre,
                        PrecioVenta = p.PrecioVenta,
                        TotalBuenos = totalBuenos,
                        TotalRechazo = totalRechazo,
                        StockBases = basesStock
                    });
                }
            }

            int total = vms.Count;
            ViewState["TotalProd"] = total;
            gvProductos.VirtualItemCount = total;
            lblTotalProductos.Text = total.ToString();

            gvProductos.DataSource = vms.Skip(pageIdx * pageSz).Take(pageSz).ToList();
            gvProductos.DataBind();
        }

        // ── Resumen por base ──────────────────────────────────────────────────
        private void CargarResumen(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            var basesQuery = db.Bases.Where(b => b.Activo).AsQueryable();

            if (basesUsuario != null)
                basesQuery = basesQuery.Where(b => basesUsuario.Contains(b.BaseID));
            if (baseFiltraID.HasValue)
                basesQuery = basesQuery.Where(b => b.BaseID == baseFiltraID.Value);

            var bases = basesQuery.OrderBy(b => b.Nombre).ToList();

            var stockMats = (from sm in db.StockMateriales
                             join m in db.Materiales on sm.MaterialID equals m.MaterialID
                             select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario }).ToList();

            var stockProds = (from sp in db.StockProductos
                              join p in db.Productos on sp.ProductoID equals p.ProductoID
                              select new
                              {
                                  sp.BaseID,
                                  ValorBuenos = sp.CantidadBuenas * p.PrecioVenta,
                                  ValorRechazo = sp.CantidadRechazo * (p.PrecioVenta * 0.5m)
                              }).ToList();

            var resumen = new List<ResumenBaseVM>();
            foreach (var b in bases)
            {
                decimal valMat = stockMats.Where(s => s.BaseID == b.BaseID).Sum(s => s.Valor);
                decimal valBuenos = stockProds.Where(s => s.BaseID == b.BaseID).Sum(s => s.ValorBuenos);
                decimal valRechazo = stockProds.Where(s => s.BaseID == b.BaseID).Sum(s => s.ValorRechazo);

                resumen.Add(new ResumenBaseVM
                {
                    BaseID = b.BaseID,
                    BaseNombre = b.Nombre,
                    ValorMateriales = valMat,
                    ValorBuenos = valBuenos,
                    ValorRechazo = valRechazo
                });
            }

            // Guardar totales para footer
            ViewState["ResumenTotales"] = new decimal[]
            {
                resumen.Sum(r => r.ValorMateriales),
                resumen.Sum(r => r.ValorBuenos),
                resumen.Sum(r => r.ValorRechazo)
            };

            gvResumen.DataSource = resumen;
            gvResumen.DataBind();
        }

        // ── Cards de valor total ──────────────────────────────────────────────
        private void ActualizarCards(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            var smQuery = from sm in db.StockMateriales
                          join m in db.Materiales on sm.MaterialID equals m.MaterialID
                          join b in db.Bases on sm.BaseID equals b.BaseID
                          where b.Activo
                          select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario };

            if (basesUsuario != null) smQuery = smQuery.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue) smQuery = smQuery.Where(s => s.BaseID == baseFiltraID.Value);

            decimal valMat = smQuery.Sum(s => (decimal?)s.Valor) ?? 0m;

            var spQuery = from sp in db.StockProductos
                          join p in db.Productos on sp.ProductoID equals p.ProductoID
                          join b in db.Bases on sp.BaseID equals b.BaseID
                          where b.Activo
                          select new
                          {
                              sp.BaseID,
                              ValBuenos = sp.CantidadBuenas * p.PrecioVenta,
                              ValRechazo = sp.CantidadRechazo * (p.PrecioVenta * 0.5m)
                          };

            if (basesUsuario != null) spQuery = spQuery.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue) spQuery = spQuery.Where(s => s.BaseID == baseFiltraID.Value);

            decimal valBuenos = spQuery.Sum(s => (decimal?)s.ValBuenos) ?? 0m;
            decimal valRechazo = spQuery.Sum(s => (decimal?)s.ValRechazo) ?? 0m;

            lblValorMateriales.Text = valMat.ToString("N2");
            lblValorBuenos.Text = valBuenos.ToString("N2");
            lblValorRechazo.Text = valRechazo.ToString("N2");
            lblValorTotal.Text = (valMat + valBuenos + valRechazo).ToString("N2");
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════

        protected void gvMateriales_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMateriales.PageIndex = e.NewPageIndex;
            using (var db = NuevoDb(tracking: false))
            {
                var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
                int? baseFiltraID = string.IsNullOrEmpty(ddlBase.SelectedValue) ? (int?)null : int.Parse(ddlBase.SelectedValue);
                CargarMateriales(db, basesUsuario, baseFiltraID);
            }
        }

        protected void gvProductos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProductos.PageIndex = e.NewPageIndex;
            using (var db = NuevoDb(tracking: false))
            {
                var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
                int? baseFiltraID = string.IsNullOrEmpty(ddlBase.SelectedValue) ? (int?)null : int.Parse(ddlBase.SelectedValue);
                CargarProductos(db, basesUsuario, baseFiltraID);
            }
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

            // Inyectar fila acordeón debajo de la fila actual
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
            sb.Append("<thead><tr><th>Base</th><th>Código</th><th>Cantidad</th><th>Nivel</th><th>Valor ($)</th></tr></thead><tbody>");

            foreach (var b in vm.StockBases)
            {
                string nivelCss = GetNivelCss(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                string icon = GetNivelIcon(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                string nivelTxt = GetNivelTextoCorto(b.Cantidad, vm.StockMinimo, vm.StockMaximo, vm.StockOptimo);
                decimal valor = b.Cantidad * vm.PrecioUnitario;

                sb.Append("<tr>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><strong>" + b.Cantidad.ToString("N2") + "</strong> " + HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td><span class='nivel-badge " + nivelCss + "'>" + icon + " " + nivelTxt + "</span></td>");
                sb.Append("<td class='text-right'>" + valor.ToString("C2") + "</td>");
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
            if (totales == null || totales.Length < 3) return;

            e.Row.Cells[0].Text = "<strong>TOTAL</strong>";
            e.Row.Cells[1].Text = totales[0].ToString("C2");
            e.Row.Cells[2].Text = totales[1].ToString("C2");
            e.Row.Cells[3].Text = totales[2].ToString("C2");
            e.Row.Cells[4].Text = "<strong>" + (totales[0] + totales[1] + totales[2]).ToString("C2") + "</strong>";
        }

        // ══ EXPORTAR EXCEL ════════════════════════════════════════════════════

        protected void btnExportarExcel_Click(object sender, EventArgs e)
        {
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            int? baseFiltraID = string.IsNullOrEmpty(ddlBase.SelectedValue) ? (int?)null : int.Parse(ddlBase.SelectedValue);
            string tipoFiltro = ddlTipoItem.SelectedValue;

            using (var db = NuevoDb(tracking: false))
            {
                var sb = new StringBuilder();
                sb.Append("<html><head><meta charset='utf-8'></head><body>");
                sb.Append("<h2>Inventario General — Grupo ANKHAL</h2>");
                sb.Append("<p>Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</p>");

                if (tipoFiltro == "" || tipoFiltro == "MAT")
                {
                    sb.Append("<h3>Materiales</h3>");
                    sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                    sb.Append("<tr style='background:#003366;color:white'><th>Código</th><th>Descripción</th><th>Tipo</th><th>Unidad</th><th>Stock Global</th><th>Nivel</th><th>Precio Unit.</th><th>Valor ($)</th></tr>");

                    var mats = ObtenerTodosMateriales(db, basesUsuario, baseFiltraID);
                    foreach (var m in mats)
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
                        sb.Append("</tr>");
                    }
                    sb.Append("</table><br/>");
                }

                if (tipoFiltro == "" || tipoFiltro == "PROD")
                {
                    sb.Append("<h3>Productos</h3>");
                    sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
                    sb.Append("<tr style='background:#003366;color:white'><th>Código</th><th>Descripción</th><th>Tipo</th><th>Buenos</th><th>Rechazo</th><th>Total</th><th>Precio Venta</th><th>Valor Buenos ($)</th><th>Valor Rechazo ($)</th></tr>");

                    var prods = ObtenerTodosProductos(db, basesUsuario, baseFiltraID);
                    foreach (var p in prods)
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
                Response.AddHeader("Content-Disposition", "attachment;filename=Inventario_" + DateTime.Now.ToString("yyyyMMdd") + ".xls");
                Response.ContentEncoding = Encoding.UTF8;
                Response.Write(sb.ToString());
                Response.End();
            }
        }

        // ══ IMPRIMIR (abre handler en nueva ventana) ══════════════════════════

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            string baseParam = string.IsNullOrEmpty(ddlBase.SelectedValue) ? "" : ddlBase.SelectedValue;
            string tipoParam = ddlTipoItem.SelectedValue;
            string url = string.Format("ImprimirInventario.ashx?base={0}&tipo={1}", baseParam, tipoParam);
            ScriptManager.RegisterStartupScript(this, GetType(), "printInv",
                "window.open('" + url + "','_blank','width=1000,height=750,scrollbars=yes');", true);
        }

        // ══ HELPERS AUXILIARES (sin paginación, para exportar) ════════════════

        private List<MaterialInvVM> ObtenerTodosMateriales(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            var mats = (from m in db.Materiales
                        join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                        where m.Activo
                        orderby m.Codigo
                        select new
                        {
                            m.MaterialID, m.Codigo,
                            Descripcion = m.Descripcion,
                            TipoNombre = tp.Nombre,
                            m.Unidad, m.PrecioUnitario,
                            StockMinimo = m.StockMinimo,
                            StockMaximo = m.StockMaximo,
                            m.StockOptimo
                        }).ToList();

            var stockQ = from sm in db.StockMateriales
                         join b in db.Bases on sm.BaseID equals b.BaseID
                         where b.Activo
                         select new { sm.MaterialID, b.BaseID, b.Nombre, b.Codigo, sm.CantidadActual };
            if (basesUsuario != null) stockQ = stockQ.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue) stockQ = stockQ.Where(s => s.BaseID == baseFiltraID.Value);
            var stock = stockQ.ToList();

            var result = new List<MaterialInvVM>();
            foreach (var m in mats)
            {
                var bases = stock.Where(s => s.MaterialID == m.MaterialID)
                    .Select(s => new StockBaseInvVM { BaseID = s.BaseID, BaseNombre = s.Nombre, BaseCodigo = s.Codigo, Cantidad = s.CantidadActual }).ToList();
                result.Add(new MaterialInvVM
                {
                    MaterialID = m.MaterialID, Codigo = m.Codigo, Descripcion = m.Descripcion,
                    TipoNombre = m.TipoNombre, Unidad = m.Unidad, PrecioUnitario = m.PrecioUnitario,
                    StockMinimo = m.StockMinimo, StockMaximo = m.StockMaximo, StockOptimo = m.StockOptimo,
                    StockGlobal = bases.Sum(s => s.Cantidad), StockBases = bases
                });
            }
            return result;
        }

        private List<ProductoInvVM> ObtenerTodosProductos(InventarioAnkhalDBDataContext db, List<int> basesUsuario, int? baseFiltraID)
        {
            var prods = (from p in db.Productos
                         join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                         where p.Activo
                         orderby p.Codigo
                         select new { p.ProductoID, p.Codigo, Descripcion = p.Descripcion, TipoNombre = tp.Nombre, p.PrecioVenta }).ToList();

            var spQ = from sp in db.StockProductos
                      join b in db.Bases on sp.BaseID equals b.BaseID
                      where b.Activo
                      select new { sp.ProductoID, b.BaseID, b.Nombre, b.Codigo, sp.CantidadBuenas, sp.CantidadRechazo };
            if (basesUsuario != null) spQ = spQ.Where(s => basesUsuario.Contains(s.BaseID));
            if (baseFiltraID.HasValue) spQ = spQ.Where(s => s.BaseID == baseFiltraID.Value);
            var sp2 = spQ.ToList();

            var result = new List<ProductoInvVM>();
            foreach (var p in prods)
            {
                var bases = sp2.Where(s => s.ProductoID == p.ProductoID)
                    .Select(s => new StockProdBaseVM { BaseID = s.BaseID, BaseNombre = s.Nombre, BaseCodigo = s.Codigo, Buenos = s.CantidadBuenas, Rechazo = s.CantidadRechazo }).ToList();
                result.Add(new ProductoInvVM
                {
                    ProductoID = p.ProductoID, Codigo = p.Codigo, Descripcion = p.Descripcion,
                    TipoNombre = p.TipoNombre, PrecioVenta = p.PrecioVenta,
                    TotalBuenos = bases.Sum(s => s.Buenos), TotalRechazo = bases.Sum(s => s.Rechazo),
                    StockBases = bases
                });
            }
            return result;
        }

        // ══ HELPERS DE NIVEL (reutilizados de Materiales.aspx) ═══════════════

        public string GetNivel(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            if (stock == 0)     return "sin";
            if (stock < minimo) return "critico";
            if (stock < maximo) return "bajo";
            return "optimo";
        }

        public string GetNivelCss(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "nivel-critico";
                case "bajo":    return "nivel-bajo";
                case "optimo":  return "nivel-optimo";
                default:        return "nivel-sin";
            }
        }

        public string GetNivelIcon(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "🔴";
                case "bajo":    return "🟡";
                case "optimo":  return "🟢";
                default:        return "⚪";
            }
        }

        public string GetNivelTextoCorto(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "Crítico";
                case "bajo":    return "Bajo";
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
