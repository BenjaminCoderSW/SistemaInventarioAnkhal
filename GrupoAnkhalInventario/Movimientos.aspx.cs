using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario
{
    public partial class Movimientos : System.Web.UI.Page
    {
        // ══ Infraestructura ══════════════════════════════════════════════════
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ══ ViewModel ════════════════════════════════════════════════════════
        public class MovimientoVM
        {
            public int      MovimientoID  { get; set; }
            public DateTime Fecha         { get; set; }
            public string   TipoClave     { get; set; }   // ENTRADA, TRANSFERENCIA…
            public string   Tipo          { get; set; }   // Nombre legible
            public string   TipoItem      { get; set; }   // Material | Producto
            public string   ItemNombre    { get; set; }
            public string   BaseOrigen    { get; set; }
            public string   BaseDestino   { get; set; }
            public decimal  Cantidad      { get; set; }
            public decimal  Costo         { get; set; }
            public decimal  Total         { get; set; }   // Cantidad × Costo
            public string   RegistradoPor { get; set; }
            public string   Observaciones { get; set; }
        }

        // ══ Page_Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarCatalogos();
                InjectJsData();
                CargarDashboard();
                CargarGrid();
            }
        }

        // ══ Catálogos ════════════════════════════════════════════════════════
        private void CargarCatalogos()
        {
            using (var db = NuevoDb(false))
            {
                var bases = db.Bases
                    .Where(b => b.Activo)
                    .OrderBy(b => b.Nombre)
                    .Select(b => new { b.BaseID, b.Nombre })
                    .ToList();

                // Filtro barra superior
                ddlFiltrBase.Items.Clear();
                ddlFiltrBase.Items.Add(new ListItem("-- Todas --", ""));
                foreach (var b in bases)
                    ddlFiltrBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // Modal: Base Origen
                ddlBaseOrigen.Items.Clear();
                ddlBaseOrigen.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in bases)
                    ddlBaseOrigen.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // Modal: Base Destino
                ddlBaseDestino.Items.Clear();
                ddlBaseDestino.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in bases)
                    ddlBaseDestino.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // ddlItem: materiales por defecto (el JS lo reemplaza al cambiar el radio)
                CargarDropdownItems(db, "Material");
            }
        }

        private void CargarDropdownItems(InventarioAnkhalDBDataContext db, string tipoItem)
        {
            ddlItem.Items.Clear();
            ddlItem.Items.Add(new ListItem("-- Seleccione un item --", ""));

            if (tipoItem == "Producto")
            {
                var prods = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Descripcion)
                    .Select(p => new { p.ProductoID, p.Descripcion })
                    .ToList();
                foreach (var p in prods)
                    ddlItem.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));
            }
            else
            {
                var mats = db.Materiales
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Descripcion)
                    .Select(m => new { m.MaterialID, m.Descripcion, m.Unidad })
                    .ToList();
                foreach (var m in mats)
                {
                    string texto = string.IsNullOrEmpty(m.Unidad)
                        ? m.Descripcion
                        : string.Format("{0} ({1})", m.Descripcion, m.Unidad);
                    ddlItem.Items.Add(new ListItem(texto, m.MaterialID.ToString()));
                }
            }
        }

        // ══ Inyección JS — materiales y productos ════════════════════════════
        private void InjectJsData()
        {
            using (var db = NuevoDb(false))
            {
                var mats = db.Materiales
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Descripcion)
                    .Select(m => new { id = m.MaterialID, nombre = m.Descripcion,
                                       unidad = m.Unidad, costo = m.PrecioUnitario })
                    .ToList();

                var prods = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Descripcion)
                    .Select(p => new { id = p.ProductoID, nombre = p.Descripcion,
                                       unidad = "", costo = p.PrecioVenta })
                    .ToList();

                litJsData.Text = string.Format(
                    "<script>window._materialesData={0}; window._productosData={1};</script>",
                    _json.Serialize(mats), _json.Serialize(prods));
            }
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                DateTime hoy    = DateTime.Today;
                DateTime manana = hoy.AddDays(1);

                var movHoy = (from mv in db.Movimientos
                              join tm in db.TiposMovimiento
                                  on mv.TipoMovimientoID equals tm.TipoMovimientoID
                              where mv.FechaMovimiento >= hoy && mv.FechaMovimiento < manana
                              select new { tm.Clave, mv.Cantidad, mv.Costo }).ToList();

                lblTotalHoy.Text  = movHoy.Count.ToString();
                lblEntradas.Text  = movHoy.Count(m => m.Clave == "ENTRADA").ToString();
                lblTraspasos.Text = movHoy.Count(m => m.Clave == "TRANSFERENCIA").ToString();
                lblAjustes.Text   = movHoy.Count(m =>
                    m.Clave == "AJUSTE_POS" || m.Clave == "AJUSTE_NEG").ToString();
                lblMermas.Text    = movHoy.Count(m => m.Clave == "MERMA").ToString();

                // AJUSTE_NEG y MERMA restan; TRANSFERENCIA tiene costo=0 y no afecta
                decimal valorTotal = movHoy.Sum(m =>
                    (m.Clave == "AJUSTE_NEG" || m.Clave == "MERMA")
                    ? -(m.Cantidad * m.Costo)
                    :  (m.Cantidad * m.Costo));
                lblValorHoy.Text = valorTotal.ToString("$#,##0.00");
            }
        }

        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                // ── Paso 1: Filtros sobre la tabla base ───────────────────────
                IQueryable<Modelo.Movimientos> movQ = db.Movimientos;

                if (!string.IsNullOrEmpty(ddlFiltrTipo.SelectedValue))
                {
                    int id = int.Parse(ddlFiltrTipo.SelectedValue);
                    movQ = movQ.Where(mv => mv.TipoMovimientoID == id);
                }
                if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
                {
                    int id = int.Parse(ddlFiltrBase.SelectedValue);
                    movQ = movQ.Where(mv => mv.BaseOrigenID == id || mv.BaseDestinoID == id);
                }
                if (!string.IsNullOrEmpty(ddlFiltrItem.SelectedValue))
                {
                    string ti = ddlFiltrItem.SelectedValue;
                    movQ = movQ.Where(mv => mv.TipoItem == ti);
                }
                if (!string.IsNullOrEmpty(txtFechaDesde.Text))
                {
                    DateTime d = DateTime.Parse(txtFechaDesde.Text);
                    movQ = movQ.Where(mv => mv.FechaMovimiento >= d);
                }
                if (!string.IsNullOrEmpty(txtFechaHasta.Text))
                {
                    DateTime h = DateTime.Parse(txtFechaHasta.Text).AddDays(1);
                    movQ = movQ.Where(mv => mv.FechaMovimiento < h);
                }

                int total   = movQ.Count();
                int pageIdx = gvMovimientos.PageIndex;
                int pageSz  = gvMovimientos.PageSize;
                gvMovimientos.VirtualItemCount = total;

                lblResultados.Text = total == 0
                    ? "Sin movimientos para los filtros aplicados."
                    : string.Format("{0} movimiento(s) encontrado(s).", total);

                if (total == 0)
                {
                    gvMovimientos.DataSource = new List<MovimientoVM>();
                    gvMovimientos.DataBind();
                    return;
                }

                // ── Paso 2: Obtener IDs de la página (ordenado desc por fecha) ─
                var ids = movQ
                    .OrderByDescending(mv => mv.FechaMovimiento)
                    .Select(mv => mv.MovimientoID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                // ── Paso 3: Traer datos con LEFT JOINs solo para esos IDs ──────
                var raw = (from mv in db.Movimientos
                           where ids.Contains(mv.MovimientoID)
                           join tm  in db.TiposMovimiento
                               on mv.TipoMovimientoID equals tm.TipoMovimientoID
                           join mat in db.Materiales
                               on mv.MaterialID equals (int?)mat.MaterialID into matG
                           from mat in matG.DefaultIfEmpty()
                           join prd in db.Productos
                               on mv.ProductoID equals (int?)prd.ProductoID into prdG
                           from prd in prdG.DefaultIfEmpty()
                           join bo in db.Bases
                               on mv.BaseOrigenID  equals (int?)bo.BaseID into boG
                           from bo in boG.DefaultIfEmpty()
                           join bd in db.Bases
                               on mv.BaseDestinoID equals (int?)bd.BaseID into bdG
                           from bd in bdG.DefaultIfEmpty()
                           select new
                           {
                               mv.MovimientoID,
                               Fecha         = mv.FechaMovimiento,
                               TipoClave     = tm.Clave,
                               TipoNombre    = tm.Nombre,
                               TipoItem      = mv.TipoItem,
                               MatNombre     = mat.Descripcion,
                               PrdNombre     = prd.Descripcion,
                               BaseOrig      = bo.Nombre,
                               BaseDest      = bd.Nombre,
                               mv.Cantidad,
                               mv.Costo,
                               mv.RegistradoPorID,
                               mv.Observaciones
                           }).ToList();

                // ── Paso 4: Nombres de usuarios (lookup separado, robustez cross-DB) ──
                Dictionary<int, string> nombresUsuario = new Dictionary<int, string>();
                try
                {
                    var uids = raw.Select(r => r.RegistradoPorID).Distinct().ToList();
                    nombresUsuario = db.DatosUsuario
                        .Where(du => uids.Contains(du.ClaveID))
                        .ToDictionary(
                            du => du.ClaveID,
                            du => (du.Nombre + " " + du.ApellidoPaterno).Trim());
                }
                catch { /* Si DatosUsuario no responde, mostramos el ClaveID */ }

                // ── Paso 5: Proyectar a ViewModel respetando el orden de los IDs ─
                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.MovimientoID == id))
                    .Where(r => r != null)
                    .Select(r => new MovimientoVM
                    {
                        MovimientoID  = r.MovimientoID,
                        Fecha         = r.Fecha,
                        TipoClave     = r.TipoClave  ?? "",
                        Tipo          = r.TipoNombre ?? "",
                        TipoItem      = r.TipoItem   ?? "",
                        ItemNombre    = r.TipoItem == "Material" ? (r.MatNombre ?? "")
                                      : (r.PrdNombre ?? ""),
                        BaseOrigen    = r.BaseOrig  ?? "",
                        BaseDestino   = r.BaseDest  ?? "",
                        Cantidad      = r.Cantidad,
                        Costo         = r.Costo,
                        Total         = r.Cantidad * r.Costo,
                        RegistradoPor = nombresUsuario.ContainsKey(r.RegistradoPorID)
                                        ? nombresUsuario[r.RegistradoPorID]
                                        : r.RegistradoPorID.ToString(),
                        Observaciones = r.Observaciones ?? ""
                    }).ToList();

                gvMovimientos.DataSource = pagina;
                gvMovimientos.DataBind();
            }
        }

        // ══ Eventos de filtros y grid ═════════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvMovimientos.PageIndex = 0;
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltrTipo.SelectedIndex = 0;
            ddlFiltrBase.SelectedIndex = 0;
            ddlFiltrItem.SelectedIndex = 0;
            txtFechaDesde.Text         = "";
            txtFechaHasta.Text         = "";
            gvMovimientos.PageIndex    = 0;
            CargarGrid();
        }

        protected void gvMovimientos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMovimientos.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // No-op: ya no hace postback desde JS, se deja para no romper compilación
        protected void btnCargarItems_Click(object sender, EventArgs e) { }

        // ══ Guardar movimiento ════════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // ── Validar tipo de movimiento ────────────────────────────────────
            if (string.IsNullOrEmpty(ddlTipoMovimiento.SelectedValue))
            {
                SetMsg("warning", "Campo requerido",
                    "Seleccione el tipo de movimiento.", "modalNuevo");
                return;
            }
            int tipoMovID = int.Parse(ddlTipoMovimiento.SelectedValue);

            // ── Validar item ──────────────────────────────────────────────────
            if (string.IsNullOrEmpty(ddlItem.SelectedValue))
            {
                SetMsg("warning", "Campo requerido",
                    "Seleccione un item.", "modalNuevo");
                return;
            }
            string tipoItem = hdnTipoItemSeleccionado.Value;  // "Material" | "Producto"
            int    itemID   = int.Parse(ddlItem.SelectedValue);

            // ── Validar cantidad y costo ──────────────────────────────────────
            decimal cantidad;
            if (!decimal.TryParse(txtCantidad.Text, out cantidad) || cantidad <= 0)
            {
                SetMsg("warning", "Campo inválido",
                    "La cantidad debe ser mayor a cero.", "modalNuevo");
                return;
            }
            decimal costo;
            if (!decimal.TryParse(txtCosto.Text, out costo) || costo < 0)
            {
                SetMsg("warning", "Campo inválido",
                    "El costo unitario no puede ser negativo.", "modalNuevo");
                return;
            }
            // Las transferencias entre bases no tienen costo (son internas)
            if (tipoMovID == 3) costo = 0m;

            // ── Determinar qué bases son requeridas ───────────────────────────
            // ENTRADA(1)      → solo base DESTINO
            // TRANSFERENCIA(3)→ base ORIGEN + base DESTINO
            // MERMA(5)        → solo base ORIGEN
            // AJUSTE_POS(6)   → solo base DESTINO
            // AJUSTE_NEG(7)   → solo base ORIGEN
            bool requiereOrigen  = tipoMovID == 3 || tipoMovID == 5 || tipoMovID == 7;
            bool requiereDestino = tipoMovID == 1 || tipoMovID == 3 || tipoMovID == 6;

            int? baseOrigenID  = null;
            int? baseDestinoID = null;

            if (requiereOrigen)
            {
                if (string.IsNullOrEmpty(ddlBaseOrigen.SelectedValue))
                {
                    SetMsg("warning", "Campo requerido",
                        "Seleccione la base de origen.", "modalNuevo");
                    return;
                }
                baseOrigenID = int.Parse(ddlBaseOrigen.SelectedValue);
            }
            if (requiereDestino)
            {
                if (string.IsNullOrEmpty(ddlBaseDestino.SelectedValue))
                {
                    SetMsg("warning", "Campo requerido",
                        "Seleccione la base de destino.", "modalNuevo");
                    return;
                }
                baseDestinoID = int.Parse(ddlBaseDestino.SelectedValue);
            }
            if (tipoMovID == 3 &&
                baseOrigenID.HasValue && baseDestinoID.HasValue &&
                baseOrigenID.Value == baseDestinoID.Value)
            {
                SetMsg("warning", "Bases inválidas",
                    "La base de origen y destino no pueden ser la misma.", "modalNuevo");
                return;
            }

            string obs     = txtObservaciones.Text.Trim();
            int    claveID = Convert.ToInt32(Session["ClaveID"]);

            // ── Ejecutar en un solo contexto (atómico con SubmitChanges) ──────
            using (var db = NuevoDb(true))
            {
                bool restaStock = tipoMovID == 3 || tipoMovID == 5 || tipoMovID == 7;

                // ── Validación y actualización de stock por tipo de item ───────
                {
                    // Material o Producto
                    if (restaStock && !ValidarStockSuficiente(db, tipoItem, itemID, baseOrigenID, cantidad))
                        return;

                    var mov = new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoMovID,
                        TipoItem         = tipoItem,
                        MaterialID       = tipoItem == "Material" ? (int?)itemID : null,
                        ProductoID       = tipoItem == "Producto" ? (int?)itemID : null,
                        BaseOrigenID     = baseOrigenID,
                        BaseDestinoID    = baseDestinoID,
                        Cantidad         = cantidad,
                        Costo            = costo,
                        Observaciones    = string.IsNullOrEmpty(obs) ? null : obs,
                        RegistradoPorID  = claveID,
                        FechaMovimiento  = DateTime.Now
                    };
                    db.Movimientos.InsertOnSubmit(mov);

                    switch (tipoMovID)
                    {
                        case 1: UpsertStock(db, tipoItem, itemID, baseDestinoID, +cantidad); break;
                        case 3: UpsertStock(db, tipoItem, itemID, baseOrigenID,  -cantidad);
                                UpsertStock(db, tipoItem, itemID, baseDestinoID, +cantidad); break;
                        case 5: UpsertStock(db, tipoItem, itemID, baseOrigenID,  -cantidad); break;
                        case 6: UpsertStock(db, tipoItem, itemID, baseDestinoID, +cantidad); break;
                        case 7: UpsertStock(db, tipoItem, itemID, baseOrigenID,  -cantidad); break;
                    }
                }

                db.SubmitChanges();
            }

            LimpiarModal();
            SetMsg("success", "Movimiento registrado", "El movimiento se guardó correctamente.");
            CargarDashboard();
            CargarGrid();
        }

        // ══ Helpers de negocio ═══════════════════════════════════════════════

        /// <summary>
        /// Upsert de stock: si existe el registro, suma delta; si no, lo crea.
        /// Delta negativo = resta. Solo crea el registro si delta > 0.
        /// </summary>
        private void UpsertStock(InventarioAnkhalDBDataContext db,
            string tipoItem, int itemID, int? baseID, decimal delta)
        {
            if (!baseID.HasValue) return;

            if (tipoItem == "Material")
            {
                var s = db.StockMateriales
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.MaterialID == itemID);

                if (s == null)
                {
                    db.StockMateriales.InsertOnSubmit(new StockMateriales
                    {
                        BaseID           = baseID.Value,
                        MaterialID       = itemID,
                        CantidadActual   = delta,
                        FechaUltimaModif = DateTime.Now
                    });
                }
                else
                {
                    s.CantidadActual   += delta;
                    s.FechaUltimaModif  = DateTime.Now;
                }
            }
            else if (tipoItem == "Producto") // CantidadBuenas es int
            {
                var s = db.StockProductos
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.ProductoID == itemID);

                int deltaInt = (int)Math.Round(delta, MidpointRounding.AwayFromZero);

                if (s == null)
                {
                    db.StockProductos.InsertOnSubmit(new StockProductos
                    {
                        BaseID           = baseID.Value,
                        ProductoID       = itemID,
                        CantidadBuenas   = deltaInt > 0 ? deltaInt : 0,
                        CantidadRechazo  = 0,
                        FechaUltimaModif = DateTime.Now
                    });
                }
                else
                {
                    s.CantidadBuenas   += deltaInt;
                    s.FechaUltimaModif  = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Verifica stock suficiente antes de restar.
        /// Si no hay, llama SetMsg con warning y retorna false.
        /// </summary>
        private bool ValidarStockSuficiente(InventarioAnkhalDBDataContext db,
            string tipoItem, int itemID, int? baseID, decimal cantidad)
        {
            if (!baseID.HasValue) return true;

            decimal actual     = 0;
            string  nombreItem = "";

            if (tipoItem == "Material")
            {
                actual = db.StockMateriales
                    .Where(x => x.BaseID == baseID.Value && x.MaterialID == itemID)
                    .Select(x => x.CantidadActual)
                    .FirstOrDefault();

                nombreItem = db.Materiales
                    .Where(m => m.MaterialID == itemID)
                    .Select(m => m.Descripcion)
                    .FirstOrDefault() ?? "Material";
            }
            else if (tipoItem == "Producto")
            {
                actual = db.StockProductos
                    .Where(x => x.BaseID == baseID.Value && x.ProductoID == itemID)
                    .Select(x => (decimal)x.CantidadBuenas)
                    .FirstOrDefault();

                nombreItem = db.Productos
                    .Where(p => p.ProductoID == itemID)
                    .Select(p => p.Descripcion)
                    .FirstOrDefault() ?? "Producto";
            }
            if (actual < cantidad)
            {
                SetMsg("warning", "Stock insuficiente",
                    string.Format("{0}: stock actual {1:N2}. No es posible restar {2:N2}.",
                        nombreItem, actual, cantidad),
                    "modalNuevo");
                return false;
            }
            return true;
        }

        // ══ Helper para el badge de tipo en el GridView ═══════════════════════
        public string GetBadgeTipo(object clave)
        {
            switch ((clave ?? "").ToString().ToUpper())
            {
                case "ENTRADA":       return "badge badge-entrada";
                case "SALIDA":        return "badge badge-salida";
                case "TRANSFERENCIA": return "badge badge-transferencia";
                case "CONSUMO":       return "badge badge-consumo";
                case "MERMA":         return "badge badge-merma";
                case "AJUSTE_POS":    return "badge badge-ajuste-pos";
                case "AJUSTE_NEG":    return "badge badge-ajuste-neg";
                default:              return "badge badge-secondary";
            }
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void LimpiarModal()
        {
            ddlTipoMovimiento.SelectedIndex = 0;
            hdnTipoItemSeleccionado.Value   = "Material";
            ddlItem.SelectedIndex           = 0;
            ddlBaseOrigen.SelectedIndex     = 0;
            ddlBaseDestino.SelectedIndex    = 0;
            txtCantidad.Text                = "";
            txtCosto.Text                   = "";
            txtObservaciones.Text           = "";
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }
    }
}
