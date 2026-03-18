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
        // ── Cadena de conexión centralizada ───────────────────────────────────
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ── Helper: crea un DataContext nuevo ─────────────────────────────────
        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ── DTOs ─────────────────────────────────────────────────────────────
        public class ProductoVM
        {
            public int ProductoID { get; set; }
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public int TipoProductoID { get; set; }
            public string TipoNombre { get; set; }
            public string TipoClave { get; set; }
            public decimal PrecioVenta { get; set; }
            public bool Activo { get; set; }
            public int TotalComponentes { get; set; }
            public System.Data.Linq.Binary RowVersion { get; set; }
        }

        public class CompNuevoVM
        {
            public string materialID { get; set; }
            public decimal cantMin { get; set; }
            public decimal cantMax { get; set; }
            public string notas { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarTipos();
                CargarProductos();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvProductos.VirtualItemCount = (int)ViewState["TotalRegistros"];

                InjectJsData();
            }
        }

        // ── Catálogo de tipos ─────────────────────────────────────────────────
        private void CargarTipos()
        {
            if (ddlTipo.Items.Count > 0) return;

            using (var db = NuevoDb(tracking: false))
            {
                var tipos = db.TiposProducto
                              .Where(t => t.Activo)
                              .OrderBy(t => t.Nombre)
                              .ToList();

                foreach (var ddl in new[] { ddlTipo, ddlTipoEdit })
                {
                    ddl.Items.Clear();
                    ddl.Items.Add(new ListItem("-- Seleccione --", ""));
                    foreach (var t in tipos)
                        ddl.Items.Add(new ListItem(t.Nombre, t.TipoProductoID.ToString()));
                }

                ddlFiltrTipo.Items.Clear();
                ddlFiltrTipo.Items.Add(new ListItem("-- Todos --", ""));
                foreach (var t in tipos)
                    ddlFiltrTipo.Items.Add(new ListItem(t.Nombre, t.TipoProductoID.ToString()));
            }
        }

        // ══ CARGA PRINCIPAL CON PAGINACIÓN EN SQL ════════════════════════════
        private void CargarProductos()
        {
            string buscar = (txtBuscar.Text ?? "").Trim();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvProductos.PageIndex;
            int pageSz = gvProductos.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var query =
                    from p in db.Productos
                    join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                    select new { p, tp };

                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(x =>
                        x.p.Codigo.Contains(buscar) ||
                        x.p.Descripcion.Contains(buscar));   // antes: x.p.Nombre

                if (!string.IsNullOrEmpty(filTipo))
                {
                    int tipoID = int.Parse(filTipo);
                    query = query.Where(x => x.p.TipoProductoID == tipoID);
                }

                if (filEst == "1") query = query.Where(x => x.p.Activo == true);
                else if (filEst == "0") query = query.Where(x => x.p.Activo == false);

                query = query.OrderBy(x => x.p.Codigo);

                int totalRegistros = query.Count();

                lblResultados.Text = totalRegistros == 1
                    ? "1 registro encontrado."
                    : totalRegistros + " registros encontrados.";

                ViewState["TotalRegistros"] = totalRegistros;

                var pagina = query
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                var idsPagina = pagina.Select(x => x.p.ProductoID).ToList();

                var compCounts = db.ProductoMateriales
                    .Where(pm => idsPagina.Contains(pm.ProductoID))
                    .GroupBy(pm => pm.ProductoID)
                    .Select(g => new { ProductoID = g.Key, Total = g.Count() })
                    .ToList();

                var lista = pagina.Select(x => new ProductoVM
                {
                    ProductoID = x.p.ProductoID,
                    Codigo = x.p.Codigo,
                    Descripcion = x.p.Descripcion,        // antes: Nombre
                    TipoProductoID = x.p.TipoProductoID,
                    TipoNombre = x.tp.Nombre,
                    TipoClave = x.tp.Clave,
                    PrecioVenta = x.p.PrecioVenta,
                    Activo = x.p.Activo,
                    RowVersion = x.p.RowVersion,
                    TotalComponentes = compCounts
                        .FirstOrDefault(c => c.ProductoID == x.p.ProductoID)?.Total ?? 0
                }).ToList();

                var dashboard = (from p in db.Productos
                                 join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                                 group tp.Clave by tp.Clave into g
                                 select new { Clave = g.Key, Total = g.Count() })
                                .ToList();

                lblTotal.Text = totalRegistros.ToString();
                lblTarimas.Text = (dashboard.FirstOrDefault(d => d.Clave == "TARIMA")?.Total ?? 0).ToString();
                lblCajas.Text = (dashboard.FirstOrDefault(d => d.Clave == "CAJA")?.Total ?? 0).ToString();
                lblAccesorios.Text = (dashboard.FirstOrDefault(d => d.Clave == "ACCESORIO")?.Total ?? 0).ToString();

                gvProductos.VirtualItemCount = totalRegistros;
                gvProductos.DataSource = lista;
                gvProductos.DataBind();

                InjectJsData(db, lista);
            }
        }

        // ── Inyectar datos JS ─────────────────────────────────────────────────
        private void InjectJsData(InventarioAnkhalDBDataContext db, List<ProductoVM> lista)
        {
            var mats = db.Materiales
                .Where(m => m.Activo == true)
                .OrderBy(m => m.Descripcion)
                .Select(m => new { id = m.MaterialID, nombre = m.Descripcion, unidad = m.Unidad })
                .ToList();

            var idsPagina = lista.Select(p => p.ProductoID).ToList();
            var pms = db.ProductoMateriales
                .Where(pm => idsPagina.Contains(pm.ProductoID))
                .ToList();
            var matIds = pms.Select(pm => pm.MaterialID).Distinct().ToList();
            var mats2 = db.Materiales.Where(m => matIds.Contains(m.MaterialID)).ToList();

            var dict = new Dictionary<string, object>();
            foreach (var pm in pms)
            {
                var mat = mats2.FirstOrDefault(m => m.MaterialID == pm.MaterialID);
                if (mat == null) continue;
                string key = pm.ProductoID.ToString();
                if (!dict.ContainsKey(key))
                    dict[key] = new List<object>();
                ((List<object>)dict[key]).Add(new
                {
                    pmID = pm.ProductoMaterialID,
                    materialID = pm.MaterialID,
                    materialNombre = mat.Descripcion,
                    unidad = mat.Unidad,
                    cantMin = pm.CantidadMin,
                    cantMax = pm.CantidadMax,
                    notas = pm.Notas ?? ""
                });
            }

            litJsData.Text = string.Format(
                "<script>window._materialesData = {0}; window._componentesData = {1};</script>",
                _json.Serialize(mats), _json.Serialize(dict));
        }

        // Sobrecarga para postbacks donde no hay lista disponible
        private void InjectJsData()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var mats = db.Materiales
                    .Where(m => m.Activo == true)
                    .OrderBy(m => m.Descripcion)
                    .Select(m => new { id = m.MaterialID, nombre = m.Descripcion, unidad = m.Unidad })
                    .ToList();

                var pms = db.ProductoMateriales.ToList();
                var mats2 = db.Materiales.ToList();

                var dict = new Dictionary<string, object>();
                foreach (var pm in pms)
                {
                    var mat = mats2.FirstOrDefault(m => m.MaterialID == pm.MaterialID);
                    if (mat == null) continue;
                    string key = pm.ProductoID.ToString();
                    if (!dict.ContainsKey(key))
                        dict[key] = new List<object>();
                    ((List<object>)dict[key]).Add(new
                    {
                        pmID = pm.ProductoMaterialID,
                        materialID = pm.MaterialID,
                        materialNombre = mat.Descripcion,
                        unidad = mat.Unidad,
                        cantMin = pm.CantidadMin,
                        cantMax = pm.CantidadMax,
                        notas = pm.Notas ?? ""
                    });
                }

                litJsData.Text = string.Format(
                    "<script>window._materialesData = {0}; window._componentesData = {1};</script>",
                    _json.Serialize(mats), _json.Serialize(dict));
            }
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════
        protected void gvProductos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProductos.PageIndex = e.NewPageIndex;
            CargarProductos();
        }

        // ══ BUSCAR / LIMPIAR ══════════════════════════════════════════════════
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

        // ══ GUARDAR NUEVO PRODUCTO ════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCamposProd(txtCodigo.Text, txtDescripcion.Text,
                                   ddlTipo.SelectedValue, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string descripTrim = txtDescripcion.Text.Trim();   // antes: nombreTrim

            using (var db = NuevoDb())
            {
                if (db.Productos.Any(p => p.Codigo == codigoUpper))
                { SetMsg("error", "Código duplicado", "Ya existe un producto con el código '" + codigoUpper + "'.", "modalNuevo"); return; }

                if (db.Productos.Any(p => p.Descripcion.ToLower() == descripTrim.ToLower()))   // antes: p.Nombre
                { SetMsg("error", "Descripción duplicada", "Ya existe un producto con esa descripción.", "modalNuevo"); return; }

                try
                {
                    var nuevo = new GrupoAnkhalInventario.Modelo.Productos
                    {
                        Codigo = codigoUpper,
                        Descripcion = descripTrim,                           // antes: Nombre
                        TipoProductoID = int.Parse(ddlTipo.SelectedValue),
                        PrecioVenta = ParseDec(txtPrecio.Text),
                        Activo = true,
                        FechaAlta = DateTime.Now,                          // ← corrige 0001-01-01
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    };
                    db.Productos.InsertOnSubmit(nuevo);
                    db.SubmitChanges();

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
        }

        // ══ GUARDAR EDICIÓN CON CONTROL DE CONCURRENCIA ══════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProductoID.Value)) return;

            if (!ValidarCamposProd(txtCodigoEdit.Text, txtDescripcionEdit.Text,
                                   ddlTipoEdit.SelectedValue, "modalEditar")) return;

            int prodID = int.Parse(hdnProductoID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string descripTrim = txtDescripcionEdit.Text.Trim();   // antes: nombreTrim

            using (var db = NuevoDb())
            {
                if (db.Productos.Any(p => p.Codigo == codigoUpper && p.ProductoID != prodID))
                { SetMsg("error", "Código duplicado", "Ya existe otro producto con ese código.", "modalEditar"); return; }

                if (db.Productos.Any(p => p.Descripcion.ToLower() == descripTrim.ToLower() && p.ProductoID != prodID))   // antes: p.Nombre
                { SetMsg("error", "Descripción duplicada", "Ya existe otro producto con esa descripción.", "modalEditar"); return; }

                try
                {
                    var prod = db.Productos.FirstOrDefault(p => p.ProductoID == prodID);
                    if (prod == null) { SetMsg("error", "Error", "No se encontró el producto."); return; }

                    byte[] rowVersionOriginal = null;
                    if (!string.IsNullOrEmpty(hdnRowVersion.Value))
                        rowVersionOriginal = Convert.FromBase64String(hdnRowVersion.Value);

                    if (rowVersionOriginal != null &&
                        prod.RowVersion != null &&
                        !rowVersionOriginal.SequenceEqual(prod.RowVersion.ToArray()))
                    {
                        SetMsg("warning",
                            "Registro modificado",
                            "Otro usuario acaba de modificar este producto. " +
                            "Recarga la página para ver los datos actuales y vuelve a editar.",
                            "modalEditar");
                        return;
                    }

                    prod.Codigo = codigoUpper;
                    prod.Descripcion = descripTrim;           // antes: prod.Nombre
                    prod.TipoProductoID = int.Parse(ddlTipoEdit.SelectedValue);
                    prod.PrecioVenta = ParseDec(txtPrecioEdit.Text);
                    prod.FechaModif = DateTime.Now;
                    prod.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);

                    db.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);

                    CargarProductos();
                    SetMsg("success", "¡Actualizado!", "El producto fue actualizado correctamente.");
                }
                catch (System.Data.Linq.ChangeConflictException)
                {
                    SetMsg("warning",
                        "Conflicto de edición",
                        "Otro usuario guardó cambios en este producto al mismo tiempo. " +
                        "Recarga la página para ver los datos más recientes.",
                        "modalEditar");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error editar producto: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo actualizar el producto.", "modalEditar");
                }
            }
        }

        // ══ COMPONENTES: INSERT / UPDATE / DELETE ════════════════════════════
        protected void btnGuardarComponentes_Click(object sender, EventArgs e)
        {
            string accion = hdnCompAccion.Value;
            int prodID = ParseInt(hdnCompProductoID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    switch (accion)
                    {
                        case "INSERT":
                            int matID = ParseInt(hdnCompMaterialID.Value);
                            decimal cmi = ParseDec(hdnCompCantMin.Value);
                            decimal cma = ParseDec(hdnCompCantMax.Value);

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
        }

        // ══ TOGGLE ════════════════════════════════════════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleProductoID.Value)) return;
            int prodID = int.Parse(hdnToggleProductoID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    var p = db.Productos.FirstOrDefault(x => x.ProductoID == prodID);
                    if (p == null) return;
                    p.Activo = !p.Activo;
                    p.FechaModif = DateTime.Now;
                    p.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);
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
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        public string RowVersionBase64(object rowVersion)
        {
            if (rowVersion == null) return "";
            if (rowVersion is System.Data.Linq.Binary)
                return Convert.ToBase64String(((System.Data.Linq.Binary)rowVersion).ToArray());
            if (rowVersion is byte[])
                return Convert.ToBase64String((byte[])rowVersion);
            return "";
        }

        private bool ValidarCamposProd(string cod, string desc, string tipo, string modal)
        {
            if (string.IsNullOrWhiteSpace(cod) || cod.Trim().Length < 2)
            { SetMsg("warning", "Código inválido", "El código es obligatorio (mín. 2 caracteres).", modal); return false; }
            if (string.IsNullOrWhiteSpace(desc) || desc.Trim().Length < 3)
            { SetMsg("warning", "Descripción inválida", "La descripción es obligatoria (mín. 3 caracteres).", modal); return false; }
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
            txtDescripcion.Text = "";
            ddlTipo.SelectedIndex = 0;
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