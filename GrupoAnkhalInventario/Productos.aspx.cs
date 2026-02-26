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
    public partial class Productos : Page
    {
        private InventarioAnkhalDBDataContext db = new InventarioAnkhalDBDataContext(
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString);

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ── DTOs ─────────────────────────────────────────────
        public class ProductoVM
        {
            public int ProductoID { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public int TipoProductoID { get; set; }
            public string TipoNombre { get; set; }
            public string TipoClave { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioVenta { get; set; }
            public bool Activo { get; set; }
            public int TotalComponentes { get; set; }
        }

        public class ComponenteVM
        {
            public int pmID { get; set; }
            public int materialID { get; set; }
            public string materialNombre { get; set; }
            public string unidad { get; set; }
            public decimal cantMin { get; set; }
            public decimal cantMax { get; set; }
            public string notas { get; set; }
        }

        public class MaterialSelectVM
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string unidad { get; set; }
        }

        public class CompNuevoVM
        {
            public string materialID { get; set; }
            public decimal cantMin { get; set; }
            public decimal cantMax { get; set; }
            public string notas { get; set; }
        }

        // ── Datos embebidos en la página para el JS ───────────
        private List<ProductoVM> _productosCache;

        // ─────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UsuarioID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarTipos();
                CargarProductos(); // CargarProductos ya llama InjectJsData internamente
            }
            else
            {
                // En postbacks solo reinyectar datos JS.
                // CargarTipos() no se llama aquí para no resetear SelectedValue.
                InjectJsData();
            }
        }

        // ── Tipos ─────────────────────────────────────────────
        private void CargarTipos()
        {
            // Solo repoblar si están vacíos (primera carga).
            // En postback NO limpiar: limpiar resetea SelectedValue antes de que
            // los event handlers lo lean, causando falsos errores de validación.
            if (ddlTipo.Items.Count > 0) return;

            var tipos = db.TiposProducto.Where(t => t.Activo).OrderBy(t => t.Nombre).ToList();

            foreach (var ddl in new[] { ddlTipo, ddlTipoEdit })
            {
                ddl.Items.Clear();
                ddl.Items.Add(new System.Web.UI.WebControls.ListItem("-- Seleccione --", ""));
                foreach (var t in tipos)
                    ddl.Items.Add(new System.Web.UI.WebControls.ListItem(t.Nombre, t.TipoProductoID.ToString()));
            }

            ddlFiltrTipo.Items.Clear();
            ddlFiltrTipo.Items.Add(new System.Web.UI.WebControls.ListItem("-- Todos --", ""));
            foreach (var t in tipos)
                ddlFiltrTipo.Items.Add(new System.Web.UI.WebControls.ListItem(t.Nombre, t.TipoProductoID.ToString()));
        }

        // ── Carga principal ───────────────────────────────────
        private void CargarProductos()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;

            var query =
                from p in db.Productos
                join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                select new { p, tp };

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(x =>
                    x.p.Codigo.ToLower().Contains(buscar) ||
                    x.p.Nombre.ToLower().Contains(buscar));

            if (!string.IsNullOrEmpty(filTipo))
                query = query.Where(x => x.p.TipoProductoID.ToString() == filTipo);

            if (filEst == "1") query = query.Where(x => x.p.Activo == true);
            else if (filEst == "0") query = query.Where(x => x.p.Activo == false);

            // Contar componentes por producto
            var compCounts = db.ProductoMateriales
                .GroupBy(pm => pm.ProductoID)
                .Select(g => new { ProductoID = g.Key, Total = g.Count() })
                .ToList();

            var lista = query.OrderBy(x => x.p.Codigo).ToList().Select(x => new ProductoVM
            {
                ProductoID = x.p.ProductoID,
                Codigo = x.p.Codigo,
                Nombre = x.p.Nombre,
                TipoProductoID = x.p.TipoProductoID,
                TipoNombre = x.tp.Nombre,
                TipoClave = x.tp.Clave,
                Descripcion = x.p.Descripcion,
                PrecioVenta = x.p.PrecioVenta,
                Activo = x.p.Activo,
                TotalComponentes = compCounts.FirstOrDefault(c => c.ProductoID == x.p.ProductoID)?.Total ?? 0
            }).ToList();

            _productosCache = lista;

            // Dashboard
            lblTotal.Text = lista.Count.ToString();
            lblTarimas.Text = lista.Count(p => p.TipoClave == "TARIMA").ToString();
            lblCajas.Text = lista.Count(p => p.TipoClave == "CAJA").ToString();
            lblAccesorios.Text = lista.Count(p => p.TipoClave == "ACCESORIO").ToString();

            lblResultados.Text = lista.Count == 1
                ? "1 registro encontrado."
                : lista.Count + " registros encontrados.";

            gvProductos.DataSource = lista;
            gvProductos.DataBind();

            // Inyectar datos JSON para el JavaScript del cliente
            InjectJsData();
        }


        // ── Inyectar datos para JS ────────────────────────────
        private void InjectJsData()
        {
            // Materiales activos para los selects de componentes
            var mats = db.Materiales
                .Where(m => m.Activo == true)
                .OrderBy(m => m.Nombre)
                .Select(m => new { id = m.MaterialID, nombre = m.Nombre, unidad = m.Unidad })
                .ToList();
            string matsJson = _json.Serialize(mats);

            // Componentes de todos los productos (dict productoID -> lista)
            var pms = db.ProductoMateriales.ToList();
            var mats2 = db.Materiales.ToList();
            var dict = new System.Collections.Generic.Dictionary<string, object>();
            foreach (var pm in pms)
            {
                var mat = mats2.FirstOrDefault(m => m.MaterialID == pm.MaterialID);
                if (mat == null) continue;
                string key = pm.ProductoID.ToString();
                if (!dict.ContainsKey(key))
                    dict[key] = new System.Collections.Generic.List<object>();
                ((System.Collections.Generic.List<object>)dict[key]).Add(new
                {
                    pmID = pm.ProductoMaterialID,
                    materialID = pm.MaterialID,
                    materialNombre = mat.Nombre,
                    unidad = mat.Unidad,
                    cantMin = pm.CantidadMin,
                    cantMax = pm.CantidadMax,
                    notas = pm.Notas ?? ""
                });
            }
            string compJson = _json.Serialize(dict);

            // Inyectar ANTES del bloque <script> de la página usando el Literal
            // Así las variables están disponibles cuando las funciones JS se definen
            litJsData.Text = string.Format(
                "<script>window._materialesData = {0}; window._componentesData = {1};</script>",
                matsJson, compJson);
        }

        // ── Paginación / Buscar / Limpiar ─────────────────────
        protected void gvProductos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProductos.PageIndex = e.NewPageIndex;
            CargarProductos();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvProductos.PageIndex = 0;
            CargarProductos();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrTipo.SelectedIndex = 0;
            ddlFiltrEstado.SelectedIndex = 0;
            gvProductos.PageIndex = 0;
            CargarProductos();
        }

        // ── GUARDAR NUEVO ─────────────────────────────────────
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCamposProd(txtCodigo.Text, txtNombre.Text,
                                   ddlTipo.SelectedValue, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

            if (db.Productos.Any(p => p.Codigo == codigoUpper))
            { SetMsg("error", "Código duplicado", "Ya existe un producto con el código '" + codigoUpper + "'.", "modalNuevo"); return; }

            if (db.Productos.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower()))
            { SetMsg("error", "Nombre duplicado", "Ya existe un producto con el nombre '" + nombreTrim + "'.", "modalNuevo"); return; }

            try
            {
                var nuevo = new GrupoAnkhalInventario.Modelo.Productos
                {
                    Codigo = codigoUpper,
                    Nombre = nombreTrim,
                    TipoProductoID = int.Parse(ddlTipo.SelectedValue),
                    Descripcion = txtDescripcion.Text.Trim(),
                    PrecioVenta = ParseDec(txtPrecio.Text),
                    Activo = true,
                    UsuarioAltaID = Convert.ToInt32(Session["UsuarioID"])
                };
                db.Productos.InsertOnSubmit(nuevo);
                db.SubmitChanges();

                // Guardar componentes
                string jsonComp = hdnComponentesNuevo.Value;
                if (!string.IsNullOrEmpty(jsonComp) && jsonComp != "[]")
                {
                    var comps = _json.Deserialize<List<CompNuevoVM>>(jsonComp);
                    foreach (var c in comps)
                    {
                        if (string.IsNullOrEmpty(c.materialID)) continue;
                        var pm = new GrupoAnkhalInventario.Modelo.ProductoMateriales
                        {
                            ProductoID = nuevo.ProductoID,
                            MaterialID = int.Parse(c.materialID),
                            CantidadMin = c.cantMin,
                            CantidadMax = c.cantMax >= c.cantMin ? c.cantMax : c.cantMin,
                            Notas = c.notas
                        };
                        db.ProductoMateriales.InsertOnSubmit(pm);
                    }
                    db.SubmitChanges();
                }

                LimpiarNuevo();
                CargarProductos();
                SetMsg("success", "¡Guardado!", "El producto fue creado correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error guardar producto: " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo guardar el producto.", "modalNuevo");
            }
        }

        // ── GUARDAR EDICIÓN ───────────────────────────────────
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProductoID.Value)) return;

            if (!ValidarCamposProd(txtCodigoEdit.Text, txtNombreEdit.Text,
                                   ddlTipoEdit.SelectedValue, "modalEditar")) return;

            int prodID = int.Parse(hdnProductoID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string nombreTrim = txtNombreEdit.Text.Trim();

            if (db.Productos.Any(p => p.Codigo == codigoUpper && p.ProductoID != prodID))
            { SetMsg("error", "Código duplicado", "Ya existe otro producto con ese código.", "modalEditar"); return; }

            if (db.Productos.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower() && p.ProductoID != prodID))
            { SetMsg("error", "Nombre duplicado", "Ya existe otro producto con ese nombre.", "modalEditar"); return; }

            try
            {
                var prod = db.Productos.FirstOrDefault(p => p.ProductoID == prodID);
                if (prod == null) { SetMsg("error", "Error", "No se encontró el producto."); return; }

                prod.Codigo = codigoUpper;
                prod.Nombre = nombreTrim;
                prod.TipoProductoID = int.Parse(ddlTipoEdit.SelectedValue);
                prod.Descripcion = txtDescripcionEdit.Text.Trim();
                prod.PrecioVenta = ParseDec(txtPrecioEdit.Text);
                prod.FechaModif = DateTime.Now;
                prod.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);

                db.SubmitChanges();
                CargarProductos();
                SetMsg("success", "¡Actualizado!", "El producto fue actualizado correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error editar producto: " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo actualizar el producto.", "modalEditar");
            }
        }

        // ── COMPONENTES: INSERT / UPDATE / DELETE ─────────────
        protected void btnGuardarComponentes_Click(object sender, EventArgs e)
        {
            string accion = hdnCompAccion.Value;
            int prodID = ParseInt(hdnCompProductoID.Value);

            try
            {
                switch (accion)
                {
                    case "INSERT":
                        int matID = ParseInt(hdnCompMaterialID.Value);
                        decimal cmi = ParseDec(hdnCompCantMin.Value);
                        decimal cma = ParseDec(hdnCompCantMax.Value);

                        // Validar duplicado
                        if (db.ProductoMateriales.Any(pm => pm.ProductoID == prodID && pm.MaterialID == matID))
                        { SetMsg("error", "Duplicado", "Ese material ya es componente de este producto.", null, true); break; }

                        var nuevo = new GrupoAnkhalInventario.Modelo.ProductoMateriales
                        {
                            ProductoID = prodID,
                            MaterialID = matID,
                            CantidadMin = cmi,
                            CantidadMax = cma >= cmi ? cma : cmi,
                            Notas = hdnCompNotas.Value
                        };
                        db.ProductoMateriales.InsertOnSubmit(nuevo);
                        db.SubmitChanges();
                        SetMsg("success", "¡Agregado!", "Componente agregado correctamente.", null, true);
                        break;

                    case "UPDATE":
                        int pmID = ParseInt(hdnCompPMID.Value);
                        var pm2 = db.ProductoMateriales.FirstOrDefault(x => x.ProductoMaterialID == pmID);
                        if (pm2 == null) break;
                        decimal cmi2 = ParseDec(hdnCompCantMin.Value);
                        decimal cma2 = ParseDec(hdnCompCantMax.Value);
                        pm2.CantidadMin = cmi2;
                        pm2.CantidadMax = cma2 >= cmi2 ? cma2 : cmi2;
                        pm2.Notas = hdnCompNotas.Value;
                        db.SubmitChanges();
                        SetMsg("success", "¡Actualizado!", "Componente actualizado.", null, true);
                        break;

                    case "DELETE":
                        int pmDel = ParseInt(hdnCompPMID.Value);
                        var pmD = db.ProductoMateriales.FirstOrDefault(x => x.ProductoMaterialID == pmDel);
                        if (pmD != null)
                        {
                            db.ProductoMateriales.DeleteOnSubmit(pmD);
                            db.SubmitChanges();
                        }
                        SetMsg("success", "¡Eliminado!", "Componente eliminado.", null, true);
                        break;
                }

                CargarProductos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error componente: " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo procesar la operación.", null, true);
            }
        }

        // ── Toggle ────────────────────────────────────────────
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleProductoID.Value)) return;
            int prodID = int.Parse(hdnToggleProductoID.Value);
            try
            {
                var p = db.Productos.FirstOrDefault(x => x.ProductoID == prodID);
                if (p == null) return;
                p.Activo = !p.Activo;
                p.FechaModif = DateTime.Now;
                p.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);
                db.SubmitChanges();
                string estado = p.Activo ? "activado" : "desactivado";
                CargarProductos();
                SetMsg("success", "¡Listo!", "El producto fue " + estado + " correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error toggle: " + ex.Message);
                SetMsg("error", "Error", "No se pudo cambiar el estatus.");
            }
        }

        // ── Helpers ───────────────────────────────────────────
        private bool ValidarCamposProd(string cod, string nom, string tipo, string modal)
        {
            if (string.IsNullOrWhiteSpace(cod) || cod.Trim().Length < 2)
            { SetMsg("warning", "Código inválido", "El código es obligatorio (mín. 2 caracteres).", modal); return false; }
            if (string.IsNullOrWhiteSpace(nom) || nom.Trim().Length < 3)
            { SetMsg("warning", "Nombre inválido", "El nombre es obligatorio (mín. 3 caracteres).", modal); return false; }
            if (string.IsNullOrWhiteSpace(tipo))
            { SetMsg("warning", "Tipo obligatorio", "Debe seleccionar el tipo de producto.", modal); return false; }
            return true;
        }

        private void SetMsg(string icon, string title, string text, string modal = null, bool reopenComp = false)
        {
            var obj = new { icon, title, text, modal = modal ?? "", reopenComp };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        private void LimpiarNuevo()
        {
            txtCodigo.Text = "";
            txtNombre.Text = "";
            ddlTipo.SelectedIndex = 0;
            txtDescripcion.Text = "";
            txtPrecio.Text = "0";
            hdnComponentesNuevo.Value = "[]";
        }

        private decimal ParseDec(string v)
        {
            decimal r;
            return decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out r) && r >= 0 ? r : 0;
        }

        private int ParseInt(string v)
        {
            int r;
            return int.TryParse(v, out r) ? r : 0;
        }
    }
}