using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class Productos : Page
    {
        // ── Cadena de conexión centralizada ───────────────────────────────────
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ── Paleta de colores (dots) para el desglose de tipos ───────────────
        private static readonly string[] _dotColors =
        {
            "#9b59b6", // morado
            "#27ae60", // verde
            "#e67e22", // naranja
            "#c0392b", // rojo
            "#17a2b8", // teal
            "#d4ac0d", // dorado
            "#2980b9", // azul
            "#6c3483", // morado oscuro
        };

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
            public string ClaveSAT { get; set; }
            public string ClaveUnidadSAT { get; set; }
            public decimal? TasaIVA { get; set; }
            public bool Activo { get; set; }
            public int TotalComponentes { get; set; }
            public int StockGlobal { get; set; }
            public List<StockBaseVM> StockBases { get; set; }
            public System.Data.Linq.Binary RowVersion { get; set; }
        }

        public class StockBaseVM
        {
            public string BaseNombre { get; set; }
            public string BaseCodigo { get; set; }
            public int Cantidad { get; set; }
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
                ViewState["IdsPagina"] = idsPagina;

                var compCounts = db.ProductoMateriales
                    .Where(pm => idsPagina.Contains(pm.ProductoID) && pm.Activo)
                    .GroupBy(pm => pm.ProductoID)
                    .Select(g => new { ProductoID = g.Key, Total = g.Count() })
                    .ToList();

                // Stock por base para los productos de esta página
                var stocksPagina = db.StockProductos
                    .Where(s => idsPagina.Contains(s.ProductoID) && s.CantidadBuenas > 0)
                    .Join(db.Bases, s => s.BaseID, b => b.BaseID,
                          (s, b) => new { s.ProductoID, b.Nombre, b.Codigo, s.CantidadBuenas })
                    .ToList();

                var lista = pagina.Select(x => new ProductoVM
                {
                    ProductoID = x.p.ProductoID,
                    Codigo = x.p.Codigo,
                    Descripcion = x.p.Descripcion,
                    TipoProductoID = x.p.TipoProductoID,
                    TipoNombre = x.tp.Nombre,
                    TipoClave = x.tp.Clave,
                    PrecioVenta = x.p.PrecioVenta,
                    ClaveSAT = x.p.ClaveSAT,
                    ClaveUnidadSAT = x.p.ClaveUnidadSAT,
                    TasaIVA = x.p.TasaIVA,
                    Activo = x.p.Activo,
                    RowVersion = x.p.RowVersion,
                    TotalComponentes = compCounts
                        .FirstOrDefault(c => c.ProductoID == x.p.ProductoID)?.Total ?? 0,
                    StockBases = stocksPagina
                        .Where(s => s.ProductoID == x.p.ProductoID)
                        .Select(s => new StockBaseVM
                        {
                            BaseNombre = s.Nombre,
                            BaseCodigo = s.Codigo,
                            Cantidad = s.CantidadBuenas
                        }).ToList(),
                    StockGlobal = stocksPagina
                        .Where(s => s.ProductoID == x.p.ProductoID)
                        .Sum(s => s.CantidadBuenas)
                }).ToList();

                // Dashboard: conteo por tipo (todos los productos, sin filtro de página)
                var dashboard = (from p in db.Productos
                                 join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                                 group tp.Clave by tp.Clave into g
                                 select new { Clave = g.Key, Total = g.Count() })
                                .ToList();

                lblTotal.Text = totalRegistros.ToString();

                // Generar cards dinámicos por tipo
                var tipos = db.TiposProducto
                    .Where(t => t.Activo)
                    .OrderBy(t => t.Nombre)
                    .Select(t => new { t.Nombre, t.Clave })
                    .ToList();

                var sbRows = new System.Text.StringBuilder();
                for (int i = 0; i < tipos.Count; i++)
                {
                    var t = tipos[i];
                    int count = dashboard.FirstOrDefault(d => d.Clave == t.Clave)?.Total ?? 0;
                    string dot = _dotColors[i % _dotColors.Length];
                    string badgeClass = count == 0 ? "tipo-badge cero" : "tipo-badge";
                    sbRows.AppendFormat(
                        "<div class='tipo-row'>" +
                        "<span class='tipo-dot' style='background:{0};'></span>" +
                        "<span class='tipo-nombre'>{1}</span>" +
                        "<span class='{2}'>{3}</span>" +
                        "</div>",
                        dot,
                        System.Web.HttpUtility.HtmlEncode(t.Nombre),
                        badgeClass,
                        count);
                }
                litTiposCards.Text = sbRows.ToString();

                gvProductos.VirtualItemCount = totalRegistros;
                gvProductos.DataSource = lista;
                gvProductos.DataBind();

                InjectJsData(db, lista);
            }
        }

        // ── Helper: conversiones por material ─────────────────────────────────
        private Dictionary<string, object> BuildConversionesMat(InventarioAnkhalDBDataContext db,
            IEnumerable<int> materialIDs = null)
        {
            // Carga unidades base de materiales
            var matBaseQ = db.Materiales
                .Where(m => m.Activo == true)
                .Select(m => new { m.MaterialID, m.UnidadMedidaID });
            if (materialIDs != null)
                matBaseQ = matBaseQ.Where(m => materialIDs.Contains(m.MaterialID));
            var matBase = matBaseQ.ToList();

            // Carga conversiones activas con nombre de unidad origen
            var convQ = (from c in db.ConversionesMaterial
                         where c.Activo
                         join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                         select new
                         {
                             c.MaterialID,
                             c.ConversionID,
                             c.Factor,
                             uNombre = u.Nombre,
                             uClave = u.Clave
                         });
            if (materialIDs != null)
                convQ = convQ.Where(c => materialIDs.Contains(c.MaterialID));
            var convs = convQ.ToList();

            // Unidades base (para el primer option)
            var unidadesBase = (from m in db.Materiales
                                where m.UnidadMedidaID != null
                                join u in db.UnidadesMedida on m.UnidadMedidaID equals u.UnidadMedidaID
                                select new { m.MaterialID, u.Nombre, u.Clave }).ToList();

            var result = new Dictionary<string, object>();
            foreach (var mb in matBase)
            {
                var ub = unidadesBase.FirstOrDefault(u => u.MaterialID == mb.MaterialID);
                var opts = new List<object>();
                // Primera opción: unidad base (val = 0 → sin conversión, factor = 1)
                opts.Add(new
                {
                    val = 0,
                    txt = ub != null ? string.Format("{0} (base)", ub.Nombre) : "(unidad base)",
                    factor = 1m
                });
                // Opciones de conversión
                foreach (var c in convs.Where(c => c.MaterialID == mb.MaterialID))
                {
                    opts.Add(new
                    {
                        val = c.ConversionID,
                        txt = string.Format("{0} ({1})", c.uNombre, c.uClave),
                        factor = c.Factor
                    });
                }
                result[mb.MaterialID.ToString()] = opts;
            }
            return result;
        }

        // ── Inyectar datos JS ─────────────────────────────────────────────────
        private void InjectJsData(InventarioAnkhalDBDataContext db, List<ProductoVM> lista)
        {
            var idsPagina = lista.Select(p => p.ProductoID).ToList();
            var pms = db.ProductoMateriales
                .Where(pm => idsPagina.Contains(pm.ProductoID) && pm.Activo)
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
                    materialCodigo = mat.Codigo,
                    materialNombre = mat.Descripcion,
                    unidad = mat.Unidad,
                    cantConsumo = pm.CantidadConsumo,
                    cantConsumoCap = pm.CantConsumoCapturada ?? pm.CantidadConsumo,
                    conversionID = pm.ConversionID ?? 0,
                    notas = pm.Notas ?? ""
                });
            }

            var conversionesMat = BuildConversionesMat(db, matIds);

            var paraClone = db.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Codigo)
                .Select(p => new {
                    productoID = p.ProductoID,
                    codigo = p.Codigo,
                    descripcion = p.Descripcion,
                    totalComponentes = p.ProductoMateriales.Count(pm => pm.Activo)
                })
                .ToList();

            litJsData.Text = string.Format(
                "<script>window._componentesData = {0}; window._conversionesMat = {1}; window._productosParaClone = {2};</script>",
                _json.Serialize(dict), _json.Serialize(conversionesMat), _json.Serialize(paraClone));
        }

        // Sobrecarga para postbacks donde no hay lista disponible
        private void InjectJsData()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var idsPag = ViewState["IdsPagina"] as List<int> ?? new List<int>();
                var pms = db.ProductoMateriales
                    .Where(pm => idsPag.Contains(pm.ProductoID) && pm.Activo)
                    .ToList();

                var pmsMatIds = pms.Select(pm => pm.MaterialID).Distinct().ToList();
                var mats = db.Materiales
                    .Where(m => pmsMatIds.Contains(m.MaterialID))
                    .Select(m => new { id = m.MaterialID, nombre = m.Descripcion, unidad = m.Unidad, codigo = m.Codigo })
                    .ToList();

                var dict = new Dictionary<string, object>();
                foreach (var pm in pms)
                {
                    var mat = mats.FirstOrDefault(m => m.id == pm.MaterialID);
                    if (mat == null) continue;
                    string key = pm.ProductoID.ToString();
                    if (!dict.ContainsKey(key))
                        dict[key] = new List<object>();
                    ((List<object>)dict[key]).Add(new
                    {
                        pmID = pm.ProductoMaterialID,
                        materialID = pm.MaterialID,
                        materialCodigo = mat.codigo,
                        materialNombre = mat.nombre,
                        unidad = mat.unidad,
                        cantConsumo = pm.CantidadConsumo,
                        cantConsumoCap = pm.CantConsumoCapturada ?? pm.CantidadConsumo,
                        conversionID = pm.ConversionID ?? 0,
                        notas = pm.Notas ?? ""
                    });
                }

                var conversionesMat = BuildConversionesMat(db, pmsMatIds);

                var paraClone = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Codigo)
                    .Select(p => new {
                        productoID = p.ProductoID,
                        codigo = p.Codigo,
                        descripcion = p.Descripcion,
                        totalComponentes = p.ProductoMateriales.Count(pm => pm.Activo)
                    })
                    .ToList();

                litJsData.Text = string.Format(
                    "<script>window._componentesData = {0}; window._conversionesMat = {1}; window._productosParaClone = {2};</script>",
                    _json.Serialize(dict), _json.Serialize(conversionesMat), _json.Serialize(paraClone));
            }
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════
        protected void gvProductos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProductos.PageIndex = e.NewPageIndex;
            CargarProductos();
        }

        // ── RowDataBound: inyectar fila acordeón de bases ─────────────────────
        protected void gvProductos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var vm = (ProductoVM)e.Row.DataItem;
            if (vm == null || vm.StockBases == null || vm.StockBases.Count == 0) return;

            e.Row.Attributes["data-id"] = vm.ProductoID.ToString();
            int lastCell = e.Row.Cells.Count - 1;
            e.Row.Cells[lastCell].Controls.Add(
                new LiteralControl(
                    "</td></tr>" +
                    "<tr id=\"acc_" + vm.ProductoID + "\" style=\"display:none;\">" +
                    "<td colspan=\"" + gvProductos.Columns.Count + "\" style=\"padding:0;background:#eef3fa;\">" +
                    "<div class=\"bases-accordion\">" +
                    "<strong style='color:#003366'><i class='fas fa-warehouse'></i> Stock por base/planta</strong>" +
                    BuildBasesTableProducto(vm) +
                    "</div></td>" +
                    "<td style='display:none'>"
                )
            );
        }

        private string BuildBasesTableProducto(ProductoVM vm)
        {
            if (vm.StockBases == null || vm.StockBases.Count == 0)
                return "<p class='text-muted mt-1 mb-0'>Sin stock registrado en ninguna base.</p>";

            var sb = new System.Text.StringBuilder();
            sb.Append("<table class='table table-sm mb-0 mt-1'>");
            sb.Append("<thead><tr><th>Base</th><th>Código</th><th>Piezas en stock</th></tr></thead><tbody>");
            foreach (var b in vm.StockBases)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseNombre) + "</td>");
                sb.Append("<td>" + System.Web.HttpUtility.HtmlEncode(b.BaseCodigo) + "</td>");
                sb.Append("<td><strong>" + b.Cantidad + "</strong></td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
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
                        ClaveSAT = string.IsNullOrWhiteSpace(txtClaveSAT.Text) ? null : txtClaveSAT.Text.Trim().ToUpper(),
                        ClaveUnidadSAT = string.IsNullOrWhiteSpace(txtClaveUnidadSAT.Text) ? null : txtClaveUnidadSAT.Text.Trim().ToUpper(),
                        TasaIVA = string.IsNullOrWhiteSpace(txtTasaIVA.Text) ? (decimal?)null : ParseDec(txtTasaIVA.Text),
                        Activo = true,
                        FechaAlta = AppHelper.Ahora,                          // ← corrige 0001-01-01
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    };
                    db.Productos.InsertOnSubmit(nuevo);
                    db.SubmitChanges();

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
                    prod.ClaveSAT = string.IsNullOrWhiteSpace(txtClaveSATEdit.Text) ? null : txtClaveSATEdit.Text.Trim().ToUpper();
                    prod.ClaveUnidadSAT = string.IsNullOrWhiteSpace(txtClaveUnidadSATEdit.Text) ? null : txtClaveUnidadSATEdit.Text.Trim().ToUpper();
                    prod.TasaIVA = string.IsNullOrWhiteSpace(txtTasaIVAEdit.Text) ? (decimal?)null : ParseDec(txtTasaIVAEdit.Text);
                    prod.FechaModif = AppHelper.Ahora;
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
                    // Leer conversión seleccionada y calcular factor
                    int convID = ParseInt(hdnCompConversionID.Value);
                    decimal factor = 1m;
                    int? convIDNullable = null;
                    if (convID > 0)
                    {
                        var conv = db.ConversionesMaterial.FirstOrDefault(c => c.ConversionID == convID);
                        if (conv != null)
                        {
                            factor = conv.Factor;
                            convIDNullable = convID;
                        }
                    }

                    switch (accion)
                    {
                        case "INSERT":
                            int matID = ParseInt(hdnCompMaterialID.Value);
                            decimal consumoCap = ParseDec(hdnCompCantConsumo.Value);
                            decimal consumoBase = consumoCap * factor;

                            var nuevo = new GrupoAnkhalInventario.Modelo.ProductoMateriales
                            {
                                ProductoID = prodID,
                                MaterialID = matID,
                                CantidadConsumo = consumoBase,
                                CantConsumoCapturada = consumoCap,
                                ConversionID = convIDNullable,
                                Notas = hdnCompNotas.Value,
                                Activo = true
                            };
                            db.ProductoMateriales.InsertOnSubmit(nuevo);
                            db.SubmitChanges();
                            SetMsg("success", "¡Agregado!", "Componente agregado correctamente.", null, true);
                            break;

                        case "UPDATE":
                            int pmID = ParseInt(hdnCompPMID.Value);
                            var pm2 = db.ProductoMateriales.FirstOrDefault(x => x.ProductoMaterialID == pmID);
                            if (pm2 == null) break;
                            decimal consumo2Cap = ParseDec(hdnCompCantConsumo.Value);
                            decimal consumo2Base = consumo2Cap * factor;
                            pm2.CantidadConsumo = consumo2Base;
                            pm2.CantConsumoCapturada = consumo2Cap;
                            pm2.ConversionID = convIDNullable;
                            pm2.Notas = hdnCompNotas.Value;
                            db.SubmitChanges();
                            SetMsg("success", "¡Actualizado!", "Componente actualizado.", null, true);
                            break;

                        case "DELETE":
                            int pmDel = ParseInt(hdnCompPMID.Value);
                            var pmD = db.ProductoMateriales.FirstOrDefault(x => x.ProductoMaterialID == pmDel);
                            if (pmD != null)
                            {
                                pmD.Activo = false;
                                db.SubmitChanges();
                            }
                            SetMsg("success", "¡Eliminado!", "Componente eliminado.", null, true);
                            break;

                        case "CLONE":
                            int origenID = ParseInt(hdnProductoCloneID.Value);
                            if (origenID == 0 || origenID == prodID) break;

                            var fuente = db.ProductoMateriales
                                .Where(pm => pm.ProductoID == origenID && pm.Activo)
                                .ToList();

                            int agregados = 0, omitidos = 0;
                            foreach (var src in fuente)
                            {
                                bool yaExiste = db.ProductoMateriales
                                    .Any(pm => pm.ProductoID == prodID &&
                                               pm.MaterialID == src.MaterialID && pm.Activo);
                                if (yaExiste) { omitidos++; continue; }

                                db.ProductoMateriales.InsertOnSubmit(new GrupoAnkhalInventario.Modelo.ProductoMateriales
                                {
                                    ProductoID = prodID,
                                    MaterialID = src.MaterialID,
                                    CantidadConsumo = src.CantidadConsumo,
                                    CantConsumoCapturada = src.CantConsumoCapturada,
                                    ConversionID = src.ConversionID,
                                    Notas = src.Notas,
                                    Activo = true
                                });
                                agregados++;
                            }

                            if (agregados > 0) db.SubmitChanges();

                            string cloneMsg;
                            if (agregados == 0)
                                cloneMsg = "Todos los componentes ya existen en este producto.";
                            else if (omitidos == 0)
                                cloneMsg = agregados + " componente(s) copiados correctamente.";
                            else
                                cloneMsg = agregados + " componente(s) copiados. " + omitidos + " ya existían y se omitieron.";

                            SetMsg(agregados > 0 ? "success" : "info",
                                   agregados > 0 ? "¡Copia completada!" : "Sin cambios",
                                   cloneMsg, null, true);
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
                    p.FechaModif = AppHelper.Ahora;
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

        // ══ WebMethod: búsqueda AJAX de materiales ════════════════════════════
        [WebMethod(EnableSession = true)]
        public static object BuscarMateriales(string query)
        {
            if (System.Web.HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado.");

            query = (query ?? "").Trim();
            if (query.Length < 2) return new object[0];

            var ci = System.Globalization.CultureInfo.InvariantCulture;

            using (var db = new InventarioAnkhalDBDataContext(
                ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString))
            {
                db.ObjectTrackingEnabled = false;

                var mats = db.Materiales
                    .Where(m => m.Activo == true &&
                        (m.Descripcion.Contains(query) || m.Codigo.Contains(query)))
                    .OrderBy(m => m.Codigo)
                    .Take(15)
                    .ToList();

                if (!mats.Any()) return new object[0];

                var matIDs = mats.Select(m => m.MaterialID).ToList();
                var unidades = db.UnidadesMedida.ToDictionary(u => u.UnidadMedidaID, u => u.Clave);
                var convs = (from c in db.ConversionesMaterial
                             where c.Activo && matIDs.Contains(c.MaterialID)
                             join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                             select new { c.MaterialID, c.ConversionID, c.Factor, u.Clave, u.Nombre })
                               .ToList();

                var resultado = new List<object>();
                foreach (var m in mats)
                {
                    string claveBase = (m.UnidadMedidaID.HasValue && unidades.ContainsKey(m.UnidadMedidaID.Value))
                        ? unidades[m.UnidadMedidaID.Value] : (m.Unidad ?? "");

                    var opciones = new List<object>();
                    if (m.UnidadMedidaID.HasValue)
                        opciones.Add(new { valor = "base:" + m.UnidadMedidaID.Value, texto = claveBase + " (base)", factor = 1.0 });

                    foreach (var c in convs.Where(x => x.MaterialID == m.MaterialID))
                    {
                        string textoConv = string.IsNullOrEmpty(c.Nombre) ? c.Clave : c.Nombre;
                        string factorStr = (c.Factor == Math.Floor(c.Factor))
                            ? ((long)c.Factor).ToString(ci) : c.Factor.ToString("G6", ci);
                        opciones.Add(new
                        {
                            valor = "conv:" + c.ConversionID,
                            texto = textoConv + " (= " + factorStr + " " + claveBase + ")",
                            factor = (double)c.Factor
                        });
                    }

                    resultado.Add(new
                    {
                        id = m.MaterialID,
                        nombre = "[" + m.Codigo + "] " + m.Descripcion,
                        conversiones = opciones
                    });
                }
                return resultado;
            }
        }
    }
}