using GrupoAnkhalInventario.Modelo;
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
            public string Nombre { get; set; }
            public int TipoMaterialID { get; set; }
            public string TipoNombre { get; set; }
            public string Subtipo { get; set; }
            public string Unidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal StockCritico { get; set; }
            public decimal StockMinimo { get; set; }
            public decimal StockOptimo { get; set; }
            public decimal StockGlobal { get; set; }
            public bool Activo { get; set; }
            // Detalle por base: List<(BaseNombre, Cantidad)>
            public List<StockBaseVM> StockBases { get; set; }
            public System.Data.Linq.Binary RowVersion { get; set; }
        }

        public class StockBaseVM
        {
            public string BaseNombre { get; set; }
            public string BaseCodigo { get; set; }
            public decimal Cantidad { get; set; }
            public string NivelCss { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UsuarioID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarTipos();
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

        // ══ CARGA PRINCIPAL CON PAGINACIÓN EN SQL ════════════════════════════
        private void CargarMateriales()
        {
            string buscar = (txtBuscar.Text ?? "").Trim();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filNivel = ddlFiltrNivel.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvMateriales.PageIndex;
            int pageSz = gvMateriales.PageSize;

            // ── CAMBIO: using en lugar del DataContext público ──
            using (var db = NuevoDb(tracking: false))
            {
                // ── JOIN Materiales + TiposMaterial ──────────────────────────
                var query =
                    from m in db.Materiales
                    join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                    select new
                    {
                        m.MaterialID,
                        m.Codigo,
                        m.Nombre,
                        m.TipoMaterialID,
                        TipoNombre = tp.Nombre,
                        m.Subtipo,
                        m.Unidad,
                        m.PrecioUnitario,
                        m.StockCritico,
                        m.StockMinimo,
                        m.StockOptimo,
                        m.Activo,
                        m.RowVersion
                    };

                // ── Filtros ──────────────────────────────────────────────────
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(m =>
                        m.Codigo.Contains(buscar) ||
                        m.Nombre.Contains(buscar));

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
                                               b.Nombre,
                                               b.Codigo,
                                               sm.CantidadActual
                                           }).ToList();

                    // Construir VMs con nivel calculado y aplicar filtro de nivel
                    var vmsFiltradas = new List<MaterialVM>();
                    foreach (var m in listaCompleta)
                    {
                        var bases = stockTodasBases
                            .Where(s => s.MaterialID == m.MaterialID)
                            .Select(s => new StockBaseVM
                            {
                                BaseNombre = s.Nombre,
                                BaseCodigo = s.Codigo,
                                Cantidad = s.CantidadActual,
                                NivelCss = GetNivelCss(s.CantidadActual, m.StockCritico, m.StockMinimo, m.StockOptimo)
                            }).ToList();

                        decimal global = bases.Sum(s => s.Cantidad);
                        string nivel = GetNivel(global, m.StockCritico, m.StockMinimo, m.StockOptimo);
                        if (nivel != filNivel) continue;

                        vmsFiltradas.Add(new MaterialVM
                        {
                            MaterialID = m.MaterialID,
                            Codigo = m.Codigo,
                            Nombre = m.Nombre,
                            TipoMaterialID = m.TipoMaterialID,
                            TipoNombre = m.TipoNombre,
                            Subtipo = m.Subtipo,
                            Unidad = m.Unidad,
                            PrecioUnitario = m.PrecioUnitario,
                            StockCritico = m.StockCritico,
                            StockMinimo = m.StockMinimo,
                            StockOptimo = m.StockOptimo,
                            StockGlobal = global,
                            Activo = m.Activo,
                            StockBases = bases,
                            RowVersion = m.RowVersion
                        });
                    }

                    int totalConNivel = vmsFiltradas.Count;
                    ViewState["TotalRegistros"] = totalConNivel;
                    gvMateriales.VirtualItemCount = totalConNivel;

                    // Paginar en memoria sobre la lista ya filtrada por nivel
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
                    // Esta es la ruta más común y la más eficiente.
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
                                           b.Nombre,
                                           b.Codigo,
                                           sm.CantidadActual
                                       }).ToList();

                    vms = new List<MaterialVM>();
                    foreach (var m in pagina)
                    {
                        var bases = stockPagina
                            .Where(s => s.MaterialID == m.MaterialID)
                            .Select(s => new StockBaseVM
                            {
                                BaseNombre = s.Nombre,
                                BaseCodigo = s.Codigo,
                                Cantidad = s.CantidadActual,
                                NivelCss = GetNivelCss(s.CantidadActual, m.StockCritico, m.StockMinimo, m.StockOptimo)
                            }).ToList();

                        decimal global = bases.Sum(s => s.Cantidad);

                        vms.Add(new MaterialVM
                        {
                            MaterialID = m.MaterialID,
                            Codigo = m.Codigo,
                            Nombre = m.Nombre,
                            TipoMaterialID = m.TipoMaterialID,
                            TipoNombre = m.TipoNombre,
                            Subtipo = m.Subtipo,
                            Unidad = m.Unidad,
                            PrecioUnitario = m.PrecioUnitario,
                            StockCritico = m.StockCritico,
                            StockMinimo = m.StockMinimo,
                            StockOptimo = m.StockOptimo,
                            StockGlobal = global,
                            Activo = m.Activo,
                            StockBases = bases,
                            RowVersion = m.RowVersion
                        });
                    }

                    var todosStocks = (from m2 in db.Materiales
                                       join sm in db.StockMateriales on m2.MaterialID equals sm.MaterialID into smg
                                       select new
                                       {
                                           m2.MaterialID,
                                           m2.StockCritico,
                                           m2.StockMinimo,
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

        // ── Dashboard: cuenta niveles sobre lista de VMs (ruta con filtro de nivel) ──
        private void ActualizarDashboard(List<MaterialVM> vms)
        {
            lblTotal.Text = vms.Count.ToString();
            lblCritico.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "critico").ToString();
            lblBajo.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "bajo").ToString();
            lblOptimo.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "optimo").ToString();
        }

        // ── Dashboard: cuenta niveles desde query ligera (ruta sin filtro de nivel) ──
        private void ActualizarDashboardDesdeQuery(
            IEnumerable<dynamic> lista)
        {
            // Usamos un tipo anónimo con los campos necesarios
            int total = 0, critico = 0, bajo = 0, optimo = 0;
            foreach (dynamic item in lista)
            {
                total++;
                string nivel = GetNivel((decimal)item.StockGlobal, (decimal)item.StockCritico,
                                        (decimal)item.StockMinimo, (decimal)item.StockOptimo);
                if (nivel == "critico") critico++;
                else if (nivel == "bajo") bajo++;
                else if (nivel == "optimo") optimo++;
            }
            lblTotal.Text = total.ToString();
            lblCritico.Text = critico.ToString();
            lblBajo.Text = bajo.ToString();
            lblOptimo.Text = optimo.ToString();
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
            sb.Append("<table class='table table-sm mb-0 mt-1'>");
            sb.Append("<thead><tr><th>Base</th><th>Código</th><th>Cantidad</th><th>Nivel</th></tr></thead><tbody>");

            foreach (var b in vm.StockBases)
            {
                string icon = b.NivelCss == "nivel-critico" ? "🔴"
                            : b.NivelCss == "nivel-bajo" ? "🟡"
                            : b.NivelCss == "nivel-optimo" ? "🟢" : "⚪";

                sb.Append("<tr>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><strong>" + b.Cantidad + "</strong> " + System.Web.HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td><span class='nivel-badge " + b.NivelCss + "'>" + icon + " " + GetNivelTexto(b.NivelCss) + "</span></td>");
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
            if (!ValidarCampos(txtCodigo.Text, txtNombre.Text,
                               ddlTipo.SelectedValue, txtUnidad.Text,
                               txtPrecio.Text, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Materiales.Any(m => m.Codigo == codigoUpper))
                { SetMsg("error", "Código duplicado", "Ya existe un material con el código '" + codigoUpper + "'.", "modalNuevo"); return; }

                if (db.Materiales.Any(m => m.Nombre.ToLower() == nombreTrim.ToLower()))
                { SetMsg("error", "Nombre duplicado", "Ya existe un material con el nombre '" + nombreTrim + "'.", "modalNuevo"); return; }

                decimal critico = ParseDec(txtStockCritico.Text);
                decimal minimo = ParseDec(txtStockMinimo.Text);
                decimal optimo = ParseDec(txtStockOptimo.Text);

                if (critico > minimo || minimo > optimo)
                { SetMsg("warning", "Niveles inválidos", "Debe cumplirse: Crítico ≤ Mínimo ≤ Óptimo.", "modalNuevo"); return; }

                try
                {
                    var nuevo = new GrupoAnkhalInventario.Modelo.Materiales
                    {
                        Codigo = codigoUpper,
                        Nombre = nombreTrim,
                        TipoMaterialID = int.Parse(ddlTipo.SelectedValue),
                        Subtipo = txtSubtipo.Text.Trim(),
                        Unidad = txtUnidad.Text.Trim(),
                        PrecioUnitario = ParseDec(txtPrecio.Text),
                        StockCritico = critico,
                        StockMinimo = minimo,
                        StockOptimo = optimo,
                        Activo = true,
                        UsuarioAltaID = Convert.ToInt32(Session["UsuarioID"])
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

            if (!ValidarCampos(txtCodigoEdit.Text, txtNombreEdit.Text,
                               ddlTipoEdit.SelectedValue, txtUnidadEdit.Text,
                               txtPrecioEdit.Text, "modalEditar")) return;

            int matID = int.Parse(hdnMaterialID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string nombreTrim = txtNombreEdit.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Materiales.Any(m => m.Codigo == codigoUpper && m.MaterialID != matID))
                { SetMsg("error", "Código duplicado", "Ya existe otro material con el código '" + codigoUpper + "'.", "modalEditar"); return; }

                if (db.Materiales.Any(m => m.Nombre.ToLower() == nombreTrim.ToLower() && m.MaterialID != matID))
                { SetMsg("error", "Nombre duplicado", "Ya existe otro material con el nombre '" + nombreTrim + "'.", "modalEditar"); return; }

                decimal critico = ParseDec(txtStockCriticoEdit.Text);
                decimal minimo = ParseDec(txtStockMinimoEdit.Text);
                decimal optimo = ParseDec(txtStockOptimoEdit.Text);

                if (critico > minimo || minimo > optimo)
                { SetMsg("warning", "Niveles inválidos", "Debe cumplirse: Crítico ≤ Mínimo ≤ Óptimo.", "modalEditar"); return; }

                try
                {
                    var mat = db.Materiales.FirstOrDefault(m => m.MaterialID == matID);
                    if (mat == null) { SetMsg("error", "Error", "No se encontró el material."); return; }

                    byte[] rowVersionOriginal = null;
                    if (!string.IsNullOrEmpty(hdnRowVersion.Value))
                        rowVersionOriginal = Convert.FromBase64String(hdnRowVersion.Value);

                    if (rowVersionOriginal != null &&
                        mat.RowVersion != null &&
                        !rowVersionOriginal.SequenceEqual(mat.RowVersion.ToArray()))
                    {
                        SetMsg("warning",
                            "Registro modificado",
                            "Otro usuario acaba de modificar este material justo ahora. " +
                            "Salte y vuelve a entrar a Materiales para ver los datos actuales y poder editar.",
                            "modalEditar");
                        return;
                    }

                    mat.Codigo = codigoUpper;
                    mat.Nombre = nombreTrim;
                    mat.TipoMaterialID = int.Parse(ddlTipoEdit.SelectedValue);
                    mat.Subtipo = txtSubtipoEdit.Text.Trim();
                    mat.Unidad = txtUnidadEdit.Text.Trim();
                    mat.PrecioUnitario = ParseDec(txtPrecioEdit.Text);
                    mat.StockCritico = critico;
                    mat.StockMinimo = minimo;
                    mat.StockOptimo = optimo;
                    mat.FechaModif = DateTime.Now;
                    mat.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);

                    // FailOnFirstConflict como segunda red de seguridad
                    db.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);

                    CargarMateriales();
                    SetMsg("success", "¡Actualizado!", "El material fue actualizado correctamente.");
                }
                catch (System.Data.Linq.ChangeConflictException)
                {
                    // ── CAMBIO: captura explícita del conflicto de concurrencia de LINQ ──
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
                    m.FechaModif = DateTime.Now;
                    m.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);
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
        public string GetNivel(decimal stock, decimal critico, decimal minimo, decimal optimo)
        {
            if (stock < critico) return "critico";
            if (stock < minimo) return "bajo";
            if (stock >= optimo) return "optimo";
            return "sin";
        }

        public string GetNivelCss(decimal stock, decimal critico, decimal minimo, decimal optimo)
        {
            switch (GetNivel(stock, critico, minimo, optimo))
            {
                case "critico": return "nivel-critico";
                case "bajo": return "nivel-bajo";
                case "optimo": return "nivel-optimo";
                default: return "nivel-sin";
            }
        }

        public string GetNivelIcon(decimal stock, decimal critico, decimal minimo, decimal optimo)
        {
            switch (GetNivel(stock, critico, minimo, optimo))
            {
                case "critico": return "🔴";
                case "bajo": return "🟡";
                case "optimo": return "🟢";
                default: return "⚪";
            }
        }

        public string GetBarCss(decimal stock, decimal critico, decimal minimo, decimal optimo)
        {
            return "";
        }

        public string GetBarColor(decimal stock, decimal critico, decimal minimo, decimal optimo)
        {
            switch (GetNivel(stock, critico, minimo, optimo))
            {
                case "critico": return "#e74c3c";
                case "bajo": return "#e67e22";
                case "optimo": return "#27ae60";
                default: return "#bdc3c7";
            }
        }

        public int GetBarPct(decimal stock, decimal optimo)
        {
            if (optimo <= 0) return 0;
            int pct = (int)Math.Round(stock / optimo * 100);
            return Math.Min(pct, 100);
        }

        private string GetNivelTexto(string css)
        {
            switch (css)
            {
                case "nivel-critico": return "Crítico";
                case "nivel-bajo": return "Bajo";
                case "nivel-optimo": return "Óptimo";
                default: return "Sin stock";
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
        private bool ValidarCampos(string cod, string nom, string tipo, string uni, string pre, string modal)
        {
            if (string.IsNullOrWhiteSpace(cod) || cod.Trim().Length < 2)
            { SetMsg("warning", "Código inválido", "El código es obligatorio y debe tener al menos 2 caracteres.", modal); return false; }
            if (string.IsNullOrWhiteSpace(nom) || nom.Trim().Length < 3)
            { SetMsg("warning", "Nombre inválido", "El nombre es obligatorio y debe tener al menos 3 caracteres.", modal); return false; }
            if (string.IsNullOrWhiteSpace(tipo))
            { SetMsg("warning", "Tipo obligatorio", "Debe seleccionar el tipo de material.", modal); return false; }
            if (string.IsNullOrWhiteSpace(uni))
            { SetMsg("warning", "Unidad obligatoria", "La unidad de medida es obligatoria.", modal); return false; }
            return true;
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = new JavaScriptSerializer().Serialize(obj);
        }

        private void LimpiarNuevo()
        {
            txtCodigo.Text = "";
            txtNombre.Text = "";
            ddlTipo.SelectedIndex = 0;
            txtSubtipo.Text = "";
            txtUnidad.Text = "";
            txtPrecio.Text = "0";
            txtStockCritico.Text = "0";
            txtStockMinimo.Text = "0";
            txtStockOptimo.Text = "0";
        }

        private decimal ParseDec(string v)
        {
            decimal r;
            return decimal.TryParse(v, out r) && r >= 0 ? r : 0;
        }
    }
}