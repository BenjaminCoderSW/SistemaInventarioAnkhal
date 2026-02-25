using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class Materiales : Page
    {
        public InventarioAnkhalDBDataContext db = new InventarioAnkhalDBDataContext(
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString);

        // ── DTO para el GridView ──────────────────────────────
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
        }

        public class StockBaseVM
        {
            public string BaseNombre { get; set; }
            public string BaseCodigo { get; set; }
            public decimal Cantidad { get; set; }
            public string NivelCss { get; set; }
        }

        // ─────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UsuarioID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarTipos();
                CargarMateriales();
            }
        }

        // ── Catálogo tipos ────────────────────────────────────
        private void CargarTipos()
        {
            var tipos = db.TiposMaterial
                          .Where(t => t.Activo)
                          .OrderBy(t => t.Nombre)
                          .ToList();

            // ddlTipo (nuevo)
            ddlTipo.Items.Clear();
            ddlTipo.Items.Add(new ListItem("-- Seleccione --", ""));
            foreach (var t in tipos)
                ddlTipo.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));

            // ddlTipoEdit (editar)
            ddlTipoEdit.Items.Clear();
            ddlTipoEdit.Items.Add(new ListItem("-- Seleccione --", ""));
            foreach (var t in tipos)
                ddlTipoEdit.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));

            // ddlFiltrTipo (filtro)
            ddlFiltrTipo.Items.Clear();
            ddlFiltrTipo.Items.Add(new ListItem("-- Todos --", ""));
            foreach (var t in tipos)
                ddlFiltrTipo.Items.Add(new ListItem(t.Nombre, t.TipoMaterialID.ToString()));
        }

        // ── Carga principal ───────────────────────────────────
        private void CargarMateriales()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filNivel = ddlFiltrNivel.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;

            // JOIN Materiales + TiposMaterial + StockMateriales (left) + Bases (left)
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
                    m.Activo
                };

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(m =>
                    m.Codigo.ToLower().Contains(buscar) ||
                    m.Nombre.ToLower().Contains(buscar));

            if (!string.IsNullOrEmpty(filTipo))
                query = query.Where(m => m.TipoMaterialID.ToString() == filTipo);

            if (filEst == "1") query = query.Where(m => m.Activo == true);
            else if (filEst == "0") query = query.Where(m => m.Activo == false);

            var lista = query.OrderBy(m => m.Codigo).ToList();

            // Stock por base (cargamos todo de una)
            var stockBases = (from sm in db.StockMateriales
                              join b in db.Bases on sm.BaseID equals b.BaseID
                              select new { sm.MaterialID, b.Nombre, b.Codigo, sm.CantidadActual })
                             .ToList();

            // Construir VMs
            var vms = new List<MaterialVM>();
            foreach (var m in lista)
            {
                var bases = stockBases
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

                // Filtro por nivel
                if (!string.IsNullOrEmpty(filNivel) && nivel != filNivel) continue;

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
                    StockBases = bases
                });
            }

            // Dashboard counters (sobre lista completa sin filtro de nivel)
            ActualizarDashboard(vms, filNivel);

            lblResultados.Text = vms.Count == 1
                ? "1 registro encontrado."
                : vms.Count + " registros encontrados.";

            gvMateriales.DataSource = vms;
            gvMateriales.DataBind();
        }

        private void ActualizarDashboard(List<MaterialVM> vms, string filNivel)
        {
            // Contar sobre la lista ya filtrada (sin filtro nivel) 
            // para mostrar totales relevantes
            lblTotal.Text = vms.Count.ToString();
            lblCritico.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "critico").ToString();
            lblBajo.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "bajo").ToString();
            lblOptimo.Text = vms.Count(m => GetNivel(m.StockGlobal, m.StockCritico, m.StockMinimo, m.StockOptimo) == "optimo").ToString();
        }

        // ── RowDataBound: inyectar fila acordeón ──────────────
        protected void gvMateriales_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var vm = (MaterialVM)e.Row.DataItem;
            if (vm == null || vm.StockBases == null || vm.StockBases.Count == 0) return;

            // Construir HTML del acordeón
            var sb = new System.Text.StringBuilder();
            sb.Append("<tr id=\"acc_" + vm.MaterialID + "\" style=\"display:none;\">");
            sb.Append("<td colspan=\"" + gvMateriales.Columns.Count + "\" style=\"padding:0;background:#eef3fa;\">");
            sb.Append("<div class=\"bases-accordion\">");
            sb.Append("<strong style='color:#003366'><i class='fas fa-warehouse'></i> Stock por base/planta</strong>");
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
                sb.Append("<td>" + b.Cantidad + " " + System.Web.HttpUtility.HtmlEncode(vm.Unidad) + "</td>");
                sb.Append("<td><span class='nivel-badge " + b.NivelCss + "'>" + icon + " " + GetNivelTexto(b.NivelCss) + "</span></td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></div></td></tr>");

            // Insertar la fila inmediatamente después de la fila actual
            var literal = new LiteralControl(sb.ToString());
            e.Row.Cells[0].Controls.Add(new LiteralControl(""));
            // Agregar como control extra al final de la fila — usamos un Panel invisible
            // Método: agregar la fila acordeón vía RegisterStartupScript acumulado
            ScriptManager.RegisterStartupScript(this, GetType(),
                "acc_" + vm.MaterialID,
                "document.getElementById('acc_" + vm.MaterialID + "') || " +
                "document.querySelector('#" + gvMateriales.ClientID + " tr[data-id=\"" + vm.MaterialID + "\"]');",
                true);

            // El enfoque más limpio en WebForms: inyectar la fila acordeón
            // directamente como literal en la última celda (oculta)
            e.Row.Attributes["data-id"] = vm.MaterialID.ToString();

            // Añadimos la fila acordeón insertando HTML crudo después de esta fila
            // mediante un control literal en la última celda
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
                    // celda fantasma para cerrar el TR que WebForms va a cerrar solo
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

        // ── Paginación ────────────────────────────────────────
        protected void gvMateriales_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMateriales.PageIndex = e.NewPageIndex;
            CargarMateriales();
        }

        // ── Buscar / Limpiar ──────────────────────────────────
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

        // ── GUARDAR NUEVO ─────────────────────────────────────
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCampos(txtCodigo.Text, txtNombre.Text,
                               ddlTipo.SelectedValue, txtUnidad.Text,
                               txtPrecio.Text, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

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

        // ── GUARDAR EDICIÓN ───────────────────────────────────
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnMaterialID.Value)) return;

            if (!ValidarCampos(txtCodigoEdit.Text, txtNombreEdit.Text,
                               ddlTipoEdit.SelectedValue, txtUnidadEdit.Text,
                               txtPrecioEdit.Text, "modalEditar")) return;

            int matID = int.Parse(hdnMaterialID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string nombreTrim = txtNombreEdit.Text.Trim();

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

                db.SubmitChanges();
                CargarMateriales();
                SetMsg("success", "¡Actualizado!", "El material fue actualizado correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al editar material: " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo actualizar el material.", "modalEditar");
            }
        }

        // ── Toggle ────────────────────────────────────────────
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleMaterialID.Value)) return;
            int matID = int.Parse(hdnToggleMaterialID.Value);
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

        // ── Helpers nivel (públicos para usar en .aspx) ───────
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
            return ""; // color se pone inline
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

        // ── Validaciones servidor ─────────────────────────────
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