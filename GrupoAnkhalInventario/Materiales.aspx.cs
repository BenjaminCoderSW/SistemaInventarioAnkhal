using GrupoAnkhalInventario.Helpers;
﻿using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class Materiales : Page
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
        public class MaterialVM
        {
            public int MaterialID { get; set; }
            public string Codigo { get; set; }
            public string Descripcion { get; set; }   // antes: Nombre
            public int TipoMaterialID { get; set; }
            public string TipoNombre { get; set; }
            public string Subtipo { get; set; }
            public string Unidad { get; set; }
            public int? UnidadMedidaID { get; set; }
            public string UnidadNombre { get; set; }
            public string UnidadClave { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal StockMinimo { get; set; }   // antes: StockCritico
            public decimal StockMaximo { get; set; }   // antes: StockMinimo
            public decimal StockOptimo { get; set; }
            public decimal StockGlobal { get; set; }
            public bool Activo { get; set; }
            public List<StockBaseVM> StockBases { get; set; }
            public System.Data.Linq.Binary RowVersion { get; set; }
            public int? ProveedorPrincipalID { get; set; }
        }

        public class StockBaseVM
        {
            public int    BaseID           { get; set; }
            public string BaseNombre       { get; set; }
            public string BaseCodigo       { get; set; }
            public decimal Cantidad        { get; set; }
            public string NivelCss         { get; set; }
            // Umbrales efectivos para esta base (null = usa globales del material)
            public decimal? NivelMinimo    { get; set; }
            public decimal? NivelOptimo    { get; set; }
            public decimal? NivelMaximo    { get; set; }
            public bool TieneNivelPropio   { get; set; }
        }

        private class NivelBaseDto
        {
            public int     MaterialID  { get; set; }
            public int     BaseID      { get; set; }
            public decimal StockMinimo { get; set; }
            public decimal StockOptimo { get; set; }
            public decimal StockMaximo { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarTipos();
                CargarUnidades();
                CargarProveedores();
                CargarMateriales();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvMateriales.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ── Catálogo tipos ────────────────────────────────────────────────────
        private void CargarTipos()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var tipos = db.TiposMaterial
                              .Where(t => t.Activo)
                              .OrderBy(t => t.Nombre)
                              .ToList();

                ddlTipo.Items.Clear();
                ddlTipo.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var t in tipos)
                    ddlTipo.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));

                ddlTipoEdit.Items.Clear();
                ddlTipoEdit.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var t in tipos)
                    ddlTipoEdit.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));

                ddlFiltrTipo.Items.Clear();
                ddlFiltrTipo.Items.Add(new ListItem("-- Todos --", ""));
                foreach (var t in tipos)
                    ddlFiltrTipo.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));
            }
        }

        // ── Catálogo proveedores ──────────────────────────────────────────────
        private void CargarProveedores()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var provs = db.Proveedores
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Nombre)
                    .Select(p => new { p.ProveedorID, p.Nombre })
                    .ToList();

                ddlProveedorPrincipal.Items.Clear();
                ddlProveedorPrincipal.Items.Add(new ListItem("-- Sin proveedor --", ""));
                ddlProveedorPrincipalEdit.Items.Clear();
                ddlProveedorPrincipalEdit.Items.Add(new ListItem("-- Sin proveedor --", ""));

                foreach (var p in provs)
                {
                    ddlProveedorPrincipal.Items.Add(new ListItem(p.Nombre, p.ProveedorID.ToString()));
                    ddlProveedorPrincipalEdit.Items.Add(new ListItem(p.Nombre, p.ProveedorID.ToString()));
                }
            }
        }

        // ── Catálogo unidades de medida ───────────────────────────────────────
        private void CargarUnidades()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var unidades = db.UnidadesMedida
                                 .Where(u => u.Activo)
                                 .OrderBy(u => u.Nombre)
                                 .ToList();

                ddlUnidad.Items.Clear();
                ddlUnidad.Items.Add(new ListItem("-- Seleccione --", "0"));
                ddlUnidadEdit.Items.Clear();
                ddlUnidadEdit.Items.Add(new ListItem("-- Seleccione --", "0"));

                foreach (var u in unidades)
                {
                    string texto = u.Nombre + " (" + u.Clave + ")";
                    string valor = u.UnidadMedidaID.ToString();
                    ddlUnidad.Items.Add(new ListItem(texto, valor));
                    ddlUnidadEdit.Items.Add(new ListItem(texto, valor));
                }
            }
        }

        // ══ CARGA PRINCIPAL CON PAGINACIÓN EN SQL ════════════════════════════
        private void CargarMateriales()
        {
            string buscar = (txtBuscar.Text ?? "").Trim();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filNivel = ddlFiltrNivel.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvMateriales.PageIndex;
            int pageSz = gvMateriales.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var dicUnidades = db.UnidadesMedida
                    .ToDictionary(u => u.UnidadMedidaID);

                // ── JOIN Materiales + TiposMaterial ──────────────────────────
                var query =
                    from m in db.Materiales
                    join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                    select new
                    {
                        m.MaterialID,
                        m.Codigo,
                        Descripcion = m.Descripcion,
                        m.TipoMaterialID,
                        TipoNombre = tp.Nombre,
                        m.Subtipo,
                        m.Unidad,
                        m.UnidadMedidaID,
                        m.PrecioUnitario,
                        StockMinimo = m.StockMinimo,
                        StockMaximo = m.StockMaximo,
                        m.StockOptimo,
                        m.Activo,
                        m.RowVersion,
                        m.ProveedorPrincipalID
                    };

                // ── Filtros ──────────────────────────────────────────────────
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(m =>
                        m.Codigo.Contains(buscar) ||
                        m.Descripcion.Contains(buscar));

                if (!string.IsNullOrEmpty(filTipo))
                {
                    int tipoID = int.Parse(filTipo);
                    query = query.Where(m => m.TipoMaterialID == tipoID);
                }

                if (filEst == "1") query = query.Where(m => m.Activo == true);
                else if (filEst == "0") query = query.Where(m => m.Activo == false);

                query = query.OrderBy(m => m.Codigo);

                int totalSinNivel = query.Count();

                List<MaterialVM> vms;

                if (!string.IsNullOrEmpty(filNivel))
                {
                    var listaCompleta = query.ToList();
                    var materialIDsCompleta = listaCompleta.Select(m => m.MaterialID).ToList();

                    var stockTodasBases = (from sm in db.StockMateriales
                                           join b in db.Bases on sm.BaseID equals b.BaseID
                                           where materialIDsCompleta.Contains(sm.MaterialID)
                                           select new
                                           {
                                               sm.MaterialID,
                                               sm.BaseID,
                                               b.Nombre,
                                               b.Codigo,
                                               sm.CantidadActual
                                           }).ToList();

                    var nivelesCompleta = CargarNivelesBase(materialIDsCompleta);

                    var vmsFiltradas = new List<MaterialVM>();
                    foreach (var m in listaCompleta)
                    {
                        var bases = stockTodasBases
                            .Where(s => s.MaterialID == m.MaterialID)
                            .Select(s =>
                            {
                                var nb = nivelesCompleta.FirstOrDefault(
                                    x => x.MaterialID == m.MaterialID && x.BaseID == s.BaseID);
                                bool tienePropio = nb != null;
                                decimal minimo = tienePropio ? nb.StockMinimo : m.StockMinimo;
                                decimal optimo = tienePropio ? nb.StockOptimo : m.StockOptimo;
                                decimal maximo = tienePropio ? nb.StockMaximo : m.StockMaximo;
                                return new StockBaseVM
                                {
                                    BaseID          = s.BaseID,
                                    BaseNombre      = s.Nombre,
                                    BaseCodigo      = s.Codigo,
                                    Cantidad        = s.CantidadActual,
                                    NivelCss        = GetNivelCss(s.CantidadActual, minimo, maximo, optimo),
                                    NivelMinimo     = tienePropio ? (decimal?)nb.StockMinimo : null,
                                    NivelOptimo     = tienePropio ? (decimal?)nb.StockOptimo : null,
                                    NivelMaximo     = tienePropio ? (decimal?)nb.StockMaximo : null,
                                    TieneNivelPropio = tienePropio
                                };
                            }).ToList();

                        decimal global = bases.Sum(s => s.Cantidad);
                        string nivel = GetNivel(global, m.StockMinimo, m.StockMaximo, m.StockOptimo);
                        if (nivel != filNivel) continue;

                        vmsFiltradas.Add(new MaterialVM
                        {
                            MaterialID = m.MaterialID,
                            Codigo = m.Codigo,
                            Descripcion = m.Descripcion,
                            TipoMaterialID = m.TipoMaterialID,
                            TipoNombre = m.TipoNombre,
                            Subtipo = m.Subtipo,
                            Unidad = m.Unidad,
                            UnidadMedidaID = m.UnidadMedidaID,
                            UnidadNombre = (m.UnidadMedidaID.HasValue && dicUnidades.ContainsKey(m.UnidadMedidaID.Value))
                                ? dicUnidades[m.UnidadMedidaID.Value].Nombre
                                : m.Unidad,
                            UnidadClave = (m.UnidadMedidaID.HasValue && dicUnidades.ContainsKey(m.UnidadMedidaID.Value))
                                ? dicUnidades[m.UnidadMedidaID.Value].Clave
                                : m.Unidad,
                            PrecioUnitario = m.PrecioUnitario,
                            StockMinimo = m.StockMinimo,
                            StockMaximo = m.StockMaximo,
                            StockOptimo = m.StockOptimo,
                            StockGlobal = global,
                            Activo = m.Activo,
                            StockBases = bases,
                            RowVersion = m.RowVersion,
                            ProveedorPrincipalID = m.ProveedorPrincipalID
                        });
                    }

                    int totalConNivel = vmsFiltradas.Count;
                    ViewState["TotalRegistros"] = totalConNivel;
                    gvMateriales.VirtualItemCount = totalConNivel;

                    vms = vmsFiltradas
                        .Skip(pageIdx * pageSz)
                        .Take(pageSz)
                        .ToList();

                    ActualizarDashboard(vmsFiltradas);

                    lblResultados.Text = totalConNivel == 1
                        ? "1 registro encontrado."
                        : totalConNivel + " registros encontrados.";
                }
                else
                {
                    // ── Sin filtro de nivel: paginación pura en SQL ──────────
                    ViewState["TotalRegistros"] = totalSinNivel;
                    gvMateriales.VirtualItemCount = totalSinNivel;

                    var pagina = query
                        .Skip(pageIdx * pageSz)
                        .Take(pageSz)
                        .ToList();

                    var materialIDsPagina = pagina.Select(m => m.MaterialID).ToList();

                    var stockPagina = (from sm in db.StockMateriales
                                       join b in db.Bases on sm.BaseID equals b.BaseID
                                       where materialIDsPagina.Contains(sm.MaterialID)
                                       select new
                                       {
                                           sm.MaterialID,
                                           sm.BaseID,
                                           b.Nombre,
                                           b.Codigo,
                                           sm.CantidadActual
                                       }).ToList();

                    var nivelesPagina = CargarNivelesBase(materialIDsPagina);

                    vms = new List<MaterialVM>();
                    foreach (var m in pagina)
                    {
                        var bases = stockPagina
                            .Where(s => s.MaterialID == m.MaterialID)
                            .Select(s =>
                            {
                                var nb = nivelesPagina.FirstOrDefault(
                                    x => x.MaterialID == m.MaterialID && x.BaseID == s.BaseID);
                                bool tienePropio = nb != null;
                                decimal minimo = tienePropio ? nb.StockMinimo : m.StockMinimo;
                                decimal optimo = tienePropio ? nb.StockOptimo : m.StockOptimo;
                                decimal maximo = tienePropio ? nb.StockMaximo : m.StockMaximo;
                                return new StockBaseVM
                                {
                                    BaseID          = s.BaseID,
                                    BaseNombre      = s.Nombre,
                                    BaseCodigo      = s.Codigo,
                                    Cantidad        = s.CantidadActual,
                                    NivelCss        = GetNivelCss(s.CantidadActual, minimo, maximo, optimo),
                                    NivelMinimo     = tienePropio ? (decimal?)nb.StockMinimo : null,
                                    NivelOptimo     = tienePropio ? (decimal?)nb.StockOptimo : null,
                                    NivelMaximo     = tienePropio ? (decimal?)nb.StockMaximo : null,
                                    TieneNivelPropio = tienePropio
                                };
                            }).ToList();

                        decimal global = bases.Sum(s => s.Cantidad);

                        vms.Add(new MaterialVM
                        {
                            MaterialID = m.MaterialID,
                            Codigo = m.Codigo,
                            Descripcion = m.Descripcion,
                            TipoMaterialID = m.TipoMaterialID,
                            TipoNombre = m.TipoNombre,
                            Subtipo = m.Subtipo,
                            Unidad = m.Unidad,
                            UnidadMedidaID = m.UnidadMedidaID,
                            UnidadNombre = (m.UnidadMedidaID.HasValue && dicUnidades.ContainsKey(m.UnidadMedidaID.Value))
                                ? dicUnidades[m.UnidadMedidaID.Value].Nombre
                                : m.Unidad,
                            UnidadClave = (m.UnidadMedidaID.HasValue && dicUnidades.ContainsKey(m.UnidadMedidaID.Value))
                                ? dicUnidades[m.UnidadMedidaID.Value].Clave
                                : m.Unidad,
                            PrecioUnitario = m.PrecioUnitario,
                            StockMinimo = m.StockMinimo,
                            StockMaximo = m.StockMaximo,
                            StockOptimo = m.StockOptimo,
                            StockGlobal = global,
                            Activo = m.Activo,
                            StockBases = bases,
                            RowVersion = m.RowVersion,
                            ProveedorPrincipalID = m.ProveedorPrincipalID
                        });
                    }

                    var todosStocks = (from m2 in db.Materiales
                                       join sm in db.StockMateriales on m2.MaterialID equals sm.MaterialID into smg
                                       select new
                                       {
                                           m2.MaterialID,
                                           StockMinimo = m2.StockMinimo,
                                           StockMaximo = m2.StockMaximo,
                                           m2.StockOptimo,
                                           StockGlobal = (decimal?)smg.Sum(s => s.CantidadActual) ?? 0m
                                       }).ToList();

                    ActualizarDashboardDesdeQuery(todosStocks);

                    lblResultados.Text = totalSinNivel == 1
                        ? "1 registro encontrado."
                        : totalSinNivel + " registros encontrados.";
                }

                gvMateriales.DataSource = vms;
                gvMateriales.DataBind();
            }
        }

        // ── Dashboard: cuenta niveles sobre lista de VMs ─────────────────────
        private void ActualizarDashboard(List<MaterialVM> vms)
        {
            lblTotal.Text   = vms.Count.ToString();
            lblCritico.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo) == "critico").ToString();
            lblBajo.Text    = vms.Count(m => GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo) == "exceso").ToString();
            lblOptimo.Text  = vms.Count(m => GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo) == "optimo").ToString();
            lblSin.Text     = vms.Count(m => GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo) == "sin").ToString();
        }

        // ── Dashboard: desde query ligera ────────────────────────────────────
        private void ActualizarDashboardDesdeQuery(IEnumerable<dynamic> lista)
        {
            int total = 0, critico = 0, exceso = 0, optimo = 0, sin = 0;
            foreach (dynamic item in lista)
            {
                total++;
                string nivel = GetNivel(
                    (decimal)item.StockGlobal,
                    (decimal)item.StockMinimo,
                    (decimal)item.StockMaximo,
                    (decimal)item.StockOptimo);
                if      (nivel == "critico") critico++;
                else if (nivel == "exceso")  exceso++;
                else if (nivel == "optimo")  optimo++;
                else if (nivel == "sin")     sin++;
            }
            lblTotal.Text   = total.ToString();
            lblCritico.Text = critico.ToString();
            lblBajo.Text    = exceso.ToString();
            lblOptimo.Text  = optimo.ToString();
            lblSin.Text     = sin.ToString();
        }

        // ── RowDataBound: inyectar fila acordeón de bases ────────────────────
        protected void gvMateriales_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var vm = (MaterialVM)e.Row.DataItem;
            if (vm == null || vm.StockBases == null || vm.StockBases.Count == 0) return;

            e.Row.Attributes["data-id"] = vm.MaterialID.ToString();

            int lastCell = e.Row.Cells.Count - 1;
            e.Row.Cells[lastCell].Controls.Add(
                new LiteralControl(
                    "</td></tr>" +
                    "<tr id=\"acc_" + vm.MaterialID + "\" style=\"display:none;\">" +
                    "<td colspan=\"" + gvMateriales.Columns.Count + "\" style=\"padding:0;background:#eef3fa;\">" +
                    "<div class=\"bases-accordion\">" +
                    "<strong style='color:#003366'><i class='fas fa-warehouse'></i> Stock por base/planta</strong>" +
                    BuildBasesTable(vm) +
                    "</div></td>" +
                    "<td style='display:none'>"
                )
            );
        }

        private string BuildBasesTable(MaterialVM vm)
        {
            if (vm.StockBases == null || vm.StockBases.Count == 0)
                return "<p class='text-muted mt-1 mb-0'>Sin registros de stock en ninguna base.</p>";

            var sb = new System.Text.StringBuilder();
            sb.Append("<table class='table table-sm mb-0 mt-1 table-niveles-base'>");
            sb.Append("<thead><tr>" +
                      "<th>Base</th>" +
                      "<th>Código</th>" +
                      "<th>Cantidad</th>" +
                      "<th>Nivel</th>" +
                      "<th>Mín / Ópt / Máx</th>" +
                      "<th>Configurar</th>" +
                      "</tr></thead><tbody>");

            foreach (var b in vm.StockBases)
            {
                string icon = b.NivelCss == "nivel-critico" ? "🔴"
                            : b.NivelCss == "nivel-exceso"  ? "🟡"
                            : b.NivelCss == "nivel-optimo"  ? "🟢" : "⚪";

                // Valores efectivos a mostrar
                decimal dispMin = b.NivelMinimo ?? vm.StockMinimo;
                decimal dispOpt = b.NivelOptimo ?? vm.StockOptimo;
                decimal dispMax = b.NivelMaximo ?? vm.StockMaximo;
                string etiqueta = b.TieneNivelPropio
                    ? ""
                    : " <small class='text-muted'>(global)</small>";

                string nivelesHtml = string.Format(
                    "<span class='text-danger' title='Mínimo'>🔴 {0:N2}</span> / " +
                    "<span class='text-success' title='Óptimo'>🟢 {1:N2}</span> / " +
                    "<span style='color:#d35400' title='Máximo'>🟡 {2:N2}</span>{3}",
                    dispMin, dispOpt, dispMax, etiqueta);

                // Botón editar: pasa valores con InvariantCulture para evitar coma/punto
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                string btnEditar = string.Format(
                    "<button type='button' class='btn btn-xs btn-outline-primary btn-editar-nivel' " +
                    "onclick=\"abrirEditorNivel({0},{1},'{2}',{3},{4},{5},{6},{7},{8})\"> " +
                    "<i class='fas fa-sliders-h'></i> Editar</button>",
                    vm.MaterialID,
                    b.BaseID,
                    System.Web.HttpUtility.JavaScriptStringEncode(b.BaseNombre),
                    dispMin.ToString(ic),
                    dispOpt.ToString(ic),
                    dispMax.ToString(ic),
                    b.TieneNivelPropio ? "true" : "false",
                    vm.StockMinimo.ToString(ic),
                    vm.StockMaximo.ToString(ic));

                sb.Append("<tr>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><strong>" + b.Cantidad.ToString("N2") + "</strong> " +
                          System.Web.HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td><span class='nivel-badge " + b.NivelCss + "'>" +
                          icon + " " + GetNivelTexto(b.NivelCss) + "</span></td>");
                sb.Append("<td style='white-space:nowrap;font-size:.78rem;'>" + nivelesHtml + "</td>");
                sb.Append("<td>" + btnEditar + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════
        protected void gvMateriales_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMateriales.PageIndex = e.NewPageIndex;
            CargarMateriales();
        }

        // ══ BUSCAR / LIMPIAR ══════════════════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvMateriales.PageIndex = 0;
            CargarMateriales();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrTipo.SelectedIndex = 0;
            ddlFiltrNivel.SelectedIndex = 0;
            ddlFiltrEstado.SelectedIndex = 0;
            hdnNivelFiltro.Value = "";
            gvMateriales.PageIndex = 0;
            CargarMateriales();
        }

        // ══ GUARDAR NUEVO ═════════════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCampos(txtCodigo.Text, txtDescripcion.Text,
                               ddlTipo.SelectedValue, ddlUnidad.SelectedValue,
                               txtPrecio.Text, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string descripTrim = txtDescripcion.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Materiales.Any(m => m.Codigo == codigoUpper))
                { SetMsg("error", "Código duplicado", "Ya existe un material con el código '" + codigoUpper + "'.", "modalNuevo"); return; }

                if (db.Materiales.Any(m => m.Descripcion.ToLower() == descripTrim.ToLower()))
                { SetMsg("error", "Descripción duplicada", "Ya existe un material con esa descripción.", "modalNuevo"); return; }

                decimal minimo = ParseDec(txtStockMinimo.Text);
                decimal maximo = ParseDec(txtStockMaximo.Text);
                decimal optimo = ParseDec(txtStockOptimo.Text);

                if (minimo < 0 || optimo < 0 || maximo < 0)
                { SetMsg("warning", "Niveles inválidos", "Los niveles de stock no pueden ser negativos.", "modalNuevo"); return; }
                if (minimo >= optimo)
                { SetMsg("warning", "Niveles inválidos", "El Mínimo debe ser menor al Óptimo.", "modalNuevo"); return; }
                if (optimo >= maximo)
                { SetMsg("warning", "Niveles inválidos", "El Óptimo debe ser menor al Máximo.", "modalNuevo"); return; }

                try
                {
                    var nuevo = new GrupoAnkhalInventario.Modelo.Materiales
                    {
                        Codigo = codigoUpper,
                        Descripcion = descripTrim,
                        TipoMaterialID = int.Parse(ddlTipo.SelectedValue),
                        Subtipo = txtSubtipo.Text.Trim(),
                        UnidadMedidaID = int.Parse(ddlUnidad.SelectedValue),
                        Unidad = ddlUnidad.SelectedItem.Text,
                        PrecioUnitario = ParseDec(txtPrecio.Text),
                        StockMinimo = minimo,
                        StockMaximo = maximo,
                        StockOptimo = optimo,
                        ProveedorPrincipalID = string.IsNullOrEmpty(ddlProveedorPrincipal.SelectedValue)
                            ? (int?)null : int.Parse(ddlProveedorPrincipal.SelectedValue),
                        Activo = true,
                        FechaAlta = AppHelper.Ahora,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    };
                    db.Materiales.InsertOnSubmit(nuevo);
                    db.SubmitChanges();

                    LimpiarNuevo();
                    CargarMateriales();
                    SetMsg("success", "¡Guardado!", "El material fue creado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al guardar material: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo guardar el material.", "modalNuevo");
                }
            }
        }

        // ══ GUARDAR EDICIÓN CON CONTROL DE CONCURRENCIA ══════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnMaterialID.Value)) return;

            if (!ValidarCampos(txtCodigoEdit.Text, txtDescripcionEdit.Text,
                               ddlTipoEdit.SelectedValue, ddlUnidadEdit.SelectedValue,
                               txtPrecioEdit.Text, "modalEditar")) return;

            int matID = int.Parse(hdnMaterialID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string descripTrim = txtDescripcionEdit.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Materiales.Any(m => m.Codigo == codigoUpper && m.MaterialID != matID))
                { SetMsg("error", "Código duplicado", "Ya existe otro material con el código '" + codigoUpper + "'.", "modalEditar"); return; }

                if (db.Materiales.Any(m => m.Descripcion.ToLower() == descripTrim.ToLower() && m.MaterialID != matID))
                { SetMsg("error", "Descripción duplicada", "Ya existe otro material con esa descripción.", "modalEditar"); return; }

                decimal minimo = ParseDec(txtStockMinimoEdit.Text);
                decimal maximo = ParseDec(txtStockMaximoEdit.Text);
                decimal optimo = ParseDec(txtStockOptimoEdit.Text);

                if (minimo < 0 || optimo < 0 || maximo < 0)
                { SetMsg("warning", "Niveles inválidos", "Los niveles de stock no pueden ser negativos.", "modalEditar"); return; }
                if (minimo >= optimo)
                { SetMsg("warning", "Niveles inválidos", "El Mínimo debe ser menor al Óptimo.", "modalEditar"); return; }
                if (optimo >= maximo)
                { SetMsg("warning", "Niveles inválidos", "El Óptimo debe ser menor al Máximo.", "modalEditar"); return; }

                try
                {
                    var mat = db.Materiales.FirstOrDefault(m => m.MaterialID == matID);
                    if (mat == null) { SetMsg("error", "Error", "No se encontró el material."); return; }

                    // ── Control de concurrencia ──────────────────────────────
                    byte[] rowVersionOriginal = null;
                    if (!string.IsNullOrEmpty(hdnRowVersion.Value))
                        rowVersionOriginal = Convert.FromBase64String(hdnRowVersion.Value);

                    if (rowVersionOriginal != null &&
                        mat.RowVersion != null &&
                        !rowVersionOriginal.SequenceEqual(mat.RowVersion.ToArray()))
                    {
                        SetMsg("warning",
                            "Registro modificado",
                            "Otro usuario acaba de modificar este material. " +
                            "Salte y vuelve a entrar a Materiales para ver los datos actuales y poder editar.",
                            "modalEditar");
                        return;
                    }

                    mat.Codigo = codigoUpper;
                    mat.Descripcion = descripTrim;
                    mat.TipoMaterialID = int.Parse(ddlTipoEdit.SelectedValue);
                    mat.Subtipo = txtSubtipoEdit.Text.Trim();
                    mat.UnidadMedidaID = int.Parse(ddlUnidadEdit.SelectedValue);
                    mat.Unidad = ddlUnidadEdit.SelectedItem.Text;
                    mat.PrecioUnitario = ParseDec(txtPrecioEdit.Text);
                    mat.StockMinimo = minimo;
                    mat.StockMaximo = maximo;
                    mat.StockOptimo = optimo;
                    mat.ProveedorPrincipalID = string.IsNullOrEmpty(ddlProveedorPrincipalEdit.SelectedValue)
                        ? (int?)null : int.Parse(ddlProveedorPrincipalEdit.SelectedValue);
                    mat.FechaModif = AppHelper.Ahora;
                    mat.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);

                    db.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);

                    CargarMateriales();
                    CargarConversiones(matID);
                    SetMsg("success", "¡Actualizado!", "El material fue actualizado correctamente.", "modalEditar");
                }
                catch (System.Data.Linq.ChangeConflictException)
                {
                    SetMsg("warning",
                        "Conflicto de edición",
                        "Otro usuario guardó cambios en este material al mismo tiempo. " +
                        "Recarga el registro para ver los datos más recientes.",
                        "modalEditar");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al editar material: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo actualizar el material.", "modalEditar");
                }
            }
        }

        // ══ GUARDAR NIVELES POR BASE ══════════════════════════════════════════
        protected void btnGuardarNivelBase_Click(object sender, EventArgs e)
        {
            int materialID, baseID;
            if (!int.TryParse(hdnNivelBaseMaterialID.Value, out materialID) || materialID <= 0) return;
            if (!int.TryParse(hdnNivelBaseBaseID.Value,     out baseID)     || baseID     <= 0) return;

            decimal minimo = ParseDec(hdnNivelBaseMinimo.Value);
            decimal optimo = ParseDec(hdnNivelBaseOptimo.Value);
            decimal maximo = ParseDec(hdnNivelBaseMaximo.Value);

            if (minimo < 0 || optimo < 0 || maximo < 0)
            { SetMsg("warning", "Niveles inválidos", "Los niveles no pueden ser negativos."); return; }
            if (minimo >= optimo)
            { SetMsg("warning", "Niveles inválidos", "El Mínimo debe ser menor al Óptimo."); return; }
            if (optimo >= maximo)
            { SetMsg("warning", "Niveles inválidos", "El Óptimo debe ser menor al Máximo."); return; }

            // Verificar que el par (MaterialID, BaseID) existe en StockMateriales
            using (var db = NuevoDb(tracking: false))
            {
                bool existe = db.StockMateriales.Any(
                    s => s.MaterialID == materialID && s.BaseID == baseID);
                if (!existe)
                { SetMsg("error", "Combinación inválida",
                         "No existe registro de stock para ese material en esa base."); return; }
            }

            const string sql = @"
                MERGE dbo.NivelesMaterialBase AS target
                USING (SELECT @matID AS MaterialID, @baseID AS BaseID) AS source
                    ON target.MaterialID = source.MaterialID AND target.BaseID = source.BaseID
                WHEN MATCHED THEN
                    UPDATE SET StockMinimo    = @minimo,
                               StockOptimo    = @optimo,
                               StockMaximo    = @maximo,
                               FechaModif     = @ahora,
                               UsuarioModifID = @usuID
                WHEN NOT MATCHED THEN
                    INSERT (MaterialID, BaseID, StockMinimo, StockOptimo, StockMaximo,
                            FechaModif, UsuarioModifID)
                    VALUES (@matID, @baseID, @minimo, @optimo, @maximo, @ahora, @usuID);";

            try
            {
                using (var cn = new System.Data.SqlClient.SqlConnection(_connStr))
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@matID",  materialID);
                    cmd.Parameters.AddWithValue("@baseID", baseID);
                    cmd.Parameters.AddWithValue("@minimo", minimo);
                    cmd.Parameters.AddWithValue("@optimo", optimo);
                    cmd.Parameters.AddWithValue("@maximo", maximo);
                    cmd.Parameters.AddWithValue("@ahora",  AppHelper.Ahora);
                    cmd.Parameters.AddWithValue("@usuID",  Convert.ToInt32(Session["ClaveID"]));
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                CargarMateriales();
                SetMsg("success", "¡Guardado!", "Los niveles por base fueron configurados correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar nivel base: " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudieron guardar los niveles por base.");
            }
        }

        // ══ TOGGLE ════════════════════════════════════════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleMaterialID.Value)) return;
            int matID = int.Parse(hdnToggleMaterialID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    var m = db.Materiales.FirstOrDefault(x => x.MaterialID == matID);
                    if (m == null) return;
                    m.Activo = !m.Activo;
                    m.FechaModif = AppHelper.Ahora;
                    m.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);
                    db.SubmitChanges();

                    string estado = m.Activo ? "activado" : "desactivado";
                    CargarMateriales();
                    SetMsg("success", "¡Listo!", "El material fue " + estado + " correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error toggle: " + ex.Message);
                    SetMsg("error", "Error", "No se pudo cambiar el estatus del material.");
                }
            }
        }

        // ══ HELPERS DE NIVEL (públicos para usar en .aspx) ═══════════════════
        /// <summary>
        /// Calcula el nivel semáforo usando los nuevos nombres de columna.
        /// minimo  = antes StockCritico  (🔴 si stock menor a este)
        /// maximo  = antes StockMinimo   (🟡 si stock entre minimo y maximo)
        /// optimo  = sin cambio          (🟢 si stock mayor o igual a este)
        /// </summary>
        public string GetNivel(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            if (stock == 0)        return "sin";
            if (stock < minimo)    return "critico";   // Rojo: bajo el mínimo
            if (stock <= maximo)   return "optimo";    // Verde: zona saludable
            return "exceso";                           // Amarillo: sobre el máximo
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

        public string GetBarCss(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            return "";
        }

        public string GetBarColor(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            switch (GetNivel(stock, minimo, maximo, optimo))
            {
                case "critico": return "#e74c3c";
                case "exceso":  return "#e67e22";
                case "optimo":  return "#27ae60";
                default:        return "#bdc3c7";
            }
        }

        public int GetBarPct(decimal stock, decimal maximo)
        {
            if (maximo <= 0) return 0;
            int pct = (int)Math.Round(stock / maximo * 100);
            return Math.Min(pct, 100);
        }

        private string GetNivelTexto(string css)
        {
            switch (css)
            {
                case "nivel-critico": return "Bajo mínimo";
                case "nivel-optimo":  return "Nivel saludable";
                case "nivel-exceso":  return "Exceso de inventario";
                default:              return "Sin stock";
            }
        }

        public string RowVersionBase64(object rowVersion)
        {
            if (rowVersion == null) return "";
            if (rowVersion is System.Data.Linq.Binary)
                return Convert.ToBase64String(((System.Data.Linq.Binary)rowVersion).ToArray());
            if (rowVersion is byte[])
                return Convert.ToBase64String((byte[])rowVersion);
            return "";
        }

        // ══ VALIDACIONES SERVIDOR ═════════════════════════════════════════════
        private bool ValidarCampos(string cod, string desc, string tipo, string uni, string pre, string modal)
        {
            if (string.IsNullOrWhiteSpace(cod) || cod.Trim().Length < 2)
            { SetMsg("warning", "Código inválido", "El código es obligatorio y debe tener al menos 2 caracteres.", modal); return false; }
            if (string.IsNullOrWhiteSpace(desc) || desc.Trim().Length < 3)
            { SetMsg("warning", "Descripción inválida", "La descripción es obligatoria y debe tener al menos 3 caracteres.", modal); return false; }
            if (string.IsNullOrWhiteSpace(tipo))
            { SetMsg("warning", "Tipo obligatorio", "Debe seleccionar el tipo de material.", modal); return false; }
            if (string.IsNullOrWhiteSpace(uni) || uni == "0")
            { SetMsg("warning", "Unidad obligatoria", "Debe seleccionar la unidad de medida.", modal); return false; }
            return true;
        }

        // ══ HELPER: cargar umbrales por base desde NivelesMaterialBase ══════════
        private List<NivelBaseDto> CargarNivelesBase(List<int> materialIDs)
        {
            var result = new List<NivelBaseDto>();
            if (materialIDs == null || materialIDs.Count == 0) return result;

            var paramNames = materialIDs.Select((id, i) => "@m" + i).ToList();
            string sql = "SELECT MaterialID, BaseID, StockMinimo, StockOptimo, StockMaximo " +
                         "FROM dbo.NivelesMaterialBase " +
                         "WHERE MaterialID IN (" + string.Join(",", paramNames) + ")";

            using (var cn = new System.Data.SqlClient.SqlConnection(_connStr))
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, cn))
            {
                for (int i = 0; i < materialIDs.Count; i++)
                    cmd.Parameters.AddWithValue("@m" + i, materialIDs[i]);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        result.Add(new NivelBaseDto
                        {
                            MaterialID  = rdr.GetInt32(0),
                            BaseID      = rdr.GetInt32(1),
                            StockMinimo = rdr.GetDecimal(2),
                            StockOptimo = rdr.GetDecimal(3),
                            StockMaximo = rdr.GetDecimal(4)
                        });
                    }
                }
            }
            return result;
        }

        // ══ Conversiones de unidad ════════════════════════════════════════════

        /// <summary>
        /// Postback oculto disparado desde abrirModalEditar JS para cargar conversiones.
        /// </summary>
        protected void btnCargarConversiones_Click(object sender, EventArgs e)
        {
            int materialID;
            if (int.TryParse(hdnConvMaterialID.Value, out materialID) && materialID > 0)
                CargarConversiones(materialID);

            // Recargar el grid para que RowDataBound inyecte los divs acc_XXX.
            // Sin esto, después del postback los botones "Ver bases" no encuentran
            // sus elementos en el DOM y no hacen nada.
            CargarMateriales();

            // Re-abrir el modal (mismo patrón que Produccion.aspx)
            ClientScript.RegisterStartupScript(GetType(), "abrirModalEditar",
                "window.addEventListener('load',function(){$('#modalEditar').modal('show');});", true);
        }

        /// <summary>
        /// Carga el GridView de conversiones para el material indicado.
        /// También llena ddlUnidadOrigenConv excluyendo la unidad base del material.
        /// </summary>
        private void CargarConversiones(int materialID)
        {
            hdnConvMaterialID.Value = materialID.ToString();

            using (var db = NuevoDb(false))
            {
                // Obtener la unidad base del material
                var mat = db.Materiales.FirstOrDefault(m => m.MaterialID == materialID);
                int? unidadBaseID = mat?.UnidadMedidaID;

                // Lista de conversiones activas
                var convs = (from c in db.ConversionesMaterial
                             where c.MaterialID == materialID && c.Activo
                             join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                             select new
                             {
                                 c.ConversionID,
                                 UnidadNombre = u.Nombre + " (" + u.Clave + ")",
                                 c.Factor,
                                 c.Descripcion
                             }).ToList();

                gvConversiones.DataSource = convs;
                gvConversiones.DataBind();

                // Llenar dropdown excluyendo la unidad base
                var todasUnidades = db.UnidadesMedida
                    .Where(u => u.Activo && (!unidadBaseID.HasValue || u.UnidadMedidaID != unidadBaseID.Value))
                    .OrderBy(u => u.Nombre)
                    .ToList();

                // También excluir las que ya tienen conversión activa
                var yaUsadas = convs.Select(c => c.ConversionID).ToList();
                var yaUsadasIDs = db.ConversionesMaterial
                    .Where(c => c.MaterialID == materialID && c.Activo)
                    .Select(c => c.UnidadOrigenID)
                    .ToList();

                ddlUnidadOrigenConv.Items.Clear();
                ddlUnidadOrigenConv.Items.Add(new ListItem("-- Seleccione unidad --", "0"));
                foreach (var u in todasUnidades.Where(u => !yaUsadasIDs.Contains(u.UnidadMedidaID)))
                    ddlUnidadOrigenConv.Items.Add(new ListItem(u.Nombre + " (" + u.Clave + ")", u.UnidadMedidaID.ToString()));
            }
        }

        protected void btnAgregarConversion_Click(object sender, EventArgs e)
        {
            int materialID;
            if (!int.TryParse(hdnConvMaterialID.Value, out materialID) || materialID == 0)
            {
                SetMsg("warning", "Sin material", "Guarde primero el material antes de agregar conversiones.", "modalEditar");
                return;
            }

            // Validación 1: unidad origen seleccionada
            int unidadOrigenID;
            if (!int.TryParse(ddlUnidadOrigenConv.SelectedValue, out unidadOrigenID) || unidadOrigenID == 0)
            {
                SetMsg("warning", "Campo requerido", "Seleccione la unidad de origen.", "modalEditar");
                return;
            }

            // Validación 2: factor > 0
            decimal factor;
            if (!decimal.TryParse(txtFactorConv.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out factor) || factor <= 0)
            {
                SetMsg("warning", "Factor inválido", "El factor debe ser mayor a cero.", "modalEditar");
                return;
            }

            using (var db = NuevoDb(true))
            {
                var mat = db.Materiales.FirstOrDefault(m => m.MaterialID == materialID);

                // Validación 3: no puede ser la misma unidad base del material
                if (mat != null && mat.UnidadMedidaID.HasValue && unidadOrigenID == mat.UnidadMedidaID.Value)
                {
                    SetMsg("warning", "Unidad inválida",
                        "No puedes usar la misma unidad base del material como conversión.", "modalEditar");
                    return;
                }

                // Validación 4: buscar conversión existente (activa o inactiva)
                var existente = db.ConversionesMaterial.FirstOrDefault(c =>
                    c.MaterialID == materialID && c.UnidadOrigenID == unidadOrigenID);

                if (existente != null && existente.Activo)
                {
                    SetMsg("warning", "Conversión duplicada",
                        "Ya existe una conversión activa para esa unidad en este material.", "modalEditar");
                    return;
                }

                string desc = txtDescConversion.Text.Trim();
                if (existente != null)
                {
                    // Reactivar fila inactiva — la unique constraint no filtra por Activo
                    existente.Activo        = true;
                    existente.Factor        = factor;
                    existente.Descripcion   = string.IsNullOrEmpty(desc) ? null : desc;
                    existente.FechaAlta     = AppHelper.Ahora;
                    existente.UsuarioAltaID = Convert.ToInt32(Session["ClaveID"]);
                }
                else
                {
                    db.ConversionesMaterial.InsertOnSubmit(new Modelo.ConversionesMaterial
                    {
                        MaterialID    = materialID,
                        UnidadOrigenID = unidadOrigenID,
                        Factor        = factor,
                        Descripcion   = string.IsNullOrEmpty(desc) ? null : desc,
                        Activo        = true,
                        FechaAlta     = AppHelper.Ahora,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    });
                }
                db.SubmitChanges();
            }

            // Limpiar campos de agregar y recargar
            txtFactorConv.Text = "";
            txtDescConversion.Text = "";
            CargarConversiones(materialID);
            SetMsg("success", "Conversión agregada", "La conversión se registró correctamente.", "modalEditar");
        }

        protected void gvConversiones_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EliminarConv") return;

            int convID;
            if (!int.TryParse(e.CommandArgument.ToString(), out convID)) return;

            int materialID;
            int.TryParse(hdnConvMaterialID.Value, out materialID);

            using (var db = NuevoDb(true))
            {
                var conv = db.ConversionesMaterial.FirstOrDefault(c => c.ConversionID == convID);
                if (conv != null)
                {
                    conv.Activo = false; // Soft delete — nunca DELETE (preserva historial)
                    db.SubmitChanges();
                }
            }

            CargarConversiones(materialID);
            SetMsg("success", "Conversión eliminada", "La conversión se desactivó correctamente.", "modalEditar");
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = new JavaScriptSerializer().Serialize(obj);
        }

        private void LimpiarNuevo()
        {
            txtCodigo.Text = "";
            txtDescripcion.Text = "";
            ddlTipo.SelectedIndex = 0;
            txtSubtipo.Text = "";
            ddlUnidad.SelectedIndex = 0;
            ddlProveedorPrincipal.SelectedIndex = 0;
            txtPrecio.Text = "0";
            txtStockMinimo.Text = "0";
            txtStockMaximo.Text = "0";
            txtStockOptimo.Text = "0";
        }

        private decimal ParseDec(string v)
        {
            decimal r;
            return decimal.TryParse(v, out r) && r >= 0 ? r : 0;
        }
    }
}