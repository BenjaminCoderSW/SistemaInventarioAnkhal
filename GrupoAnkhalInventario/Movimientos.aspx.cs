using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using GrupoAnkhalInventario.Services;

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

        // ══ Modelo para ítems del lote (deserializado desde JSON) ════════════
        private class ItemLoteModel
        {
            public string tipoItem { get; set; }
            public int itemId { get; set; }
            public string itemTexto { get; set; }
            public decimal cantidadCapturada { get; set; }
            public decimal costo { get; set; }
            public int unidadId { get; set; }
            public decimal factor { get; set; }
        }

        // ══ ViewModel ════════════════════════════════════════════════════════
        public class MovimientoVM
        {
            public int MovimientoID { get; set; }
            public DateTime Fecha { get; set; }
            public string TipoClave { get; set; }   // ENTRADA, TRANSFERENCIA…
            public string Tipo { get; set; }   // Nombre legible
            public string TipoItem { get; set; }   // Material | Producto
            public string ItemCodigo { get; set; }
            public string ItemNombre { get; set; }
            public string BaseOrigen { get; set; }
            public string BaseDestino { get; set; }
            public decimal Cantidad { get; set; }
            public string Unidad { get; set; }
            public decimal Costo { get; set; }
            public decimal Total { get; set; }   // Cantidad × Costo
            public decimal CantidadCapturada { get; set; }   // Lo que el usuario capturó
            public string UnidadCapturaNombre { get; set; }   // Nombre de la unidad capturada
            public bool TuvoConversion { get; set; }   // true si se aplicó factor ≠ 1
            public string RegistradoPor { get; set; }
            public string Observaciones { get; set; }
            public string FolioLote { get; set; }
            public string ProveedorNombre { get; set; }
        }

        // ══ Page_Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarCatalogos();
                CargarProveedores();
                // Por defecto mostrar solo hoy en grid y cards
                string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
                txtFechaDesde.Text = hoy;
                txtFechaHasta.Text = hoy;
                CargarDashboard();
                CargarGrid();
            }
        }

        // ══ Catálogos ════════════════════════════════════════════════════════
        private void CargarCatalogos()
        {
            using (var db = NuevoDb(false))
            {
                // Bases del usuario (filtradas por permisos)
                var basesUsuario = AppHelper.ObtenerBasesActivasParaUsuario(Session);

                // Todas las bases activas (para destino de transferencias)
                var todasLasBases = db.Bases
                    .Where(b => b.Activo)
                    .OrderBy(b => b.Nombre)
                    .Select(b => new { b.BaseID, b.Nombre })
                    .ToList();

                // Filtro barra superior — solo bases del usuario
                ddlFiltrBase.Items.Clear();
                ddlFiltrBase.Items.Add(new ListItem("-- Todas --", ""));
                foreach (var b in basesUsuario)
                    ddlFiltrBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // Modal: Base Origen — solo bases del usuario
                ddlBaseOrigen.Items.Clear();
                ddlBaseOrigen.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in basesUsuario)
                    ddlBaseOrigen.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // Modal: Base Destino — todas las bases (se puede transferir a cualquier base)
                ddlBaseDestino.Items.Clear();
                ddlBaseDestino.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in todasLasBases)
                    ddlBaseDestino.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

            }
        }

        private void CargarProveedores()
        {
            using (var db = NuevoDb(false))
            {
                ddlProveedor.Items.Clear();
                ddlProveedor.Items.Add(new ListItem("-- Sin proveedor --", ""));
                var provs = db.Proveedores
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Nombre)
                    .Select(p => new { p.ProveedorID, p.Nombre })
                    .ToList();
                foreach (var p in provs)
                    ddlProveedor.Items.Add(new ListItem(p.Nombre, p.ProveedorID.ToString()));
            }
        }

        // ══ Filtros compartidos entre Dashboard y Grid ═══════════════════════
        /// <summary>
        /// Aplica los controles de filtro del formulario a una query.
        /// Devuelve si se usó algún filtro de fecha (para decidir si el dashboard muestra "hoy" o "filtrado").
        /// </summary>
        private IQueryable<Modelo.Movimientos> AplicarFiltrosBusqueda(
            IQueryable<Modelo.Movimientos> q, out bool hayFiltroFecha)
        {
            hayFiltroFecha = false;

            // Restringir siempre por las bases del usuario (null = Administrador, ve todo)
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            if (basesUsuario != null)
                q = q.Where(mv => basesUsuario.Contains(mv.BaseOrigenID ?? 0) ||
                                  basesUsuario.Contains(mv.BaseDestinoID ?? 0));

            var selTipos = cblFiltrTipo.Items.Cast<ListItem>()
                .Where(li => li.Selected)
                .Select(li => int.Parse(li.Value))
                .ToList();
            if (selTipos.Any())
                q = q.Where(mv => selTipos.Contains(mv.TipoMovimientoID));
            if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
            {
                int id = int.Parse(ddlFiltrBase.SelectedValue);
                q = q.Where(mv => mv.BaseOrigenID == id || mv.BaseDestinoID == id);
            }
            if (!string.IsNullOrEmpty(ddlFiltrItem.SelectedValue))
            {
                string ti = ddlFiltrItem.SelectedValue;
                q = q.Where(mv => mv.TipoItem == ti);
            }
            if (!string.IsNullOrEmpty(txtFechaDesde.Text))
            {
                DateTime d = DateTime.Parse(txtFechaDesde.Text);
                q = q.Where(mv => mv.FechaMovimiento >= d);
                hayFiltroFecha = true;
            }
            if (!string.IsNullOrEmpty(txtFechaHasta.Text))
            {
                DateTime h = DateTime.Parse(txtFechaHasta.Text).AddDays(1);
                q = q.Where(mv => mv.FechaMovimiento < h);
                hayFiltroFecha = true;
            }
            return q;
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                bool hayFiltroFecha;
                var movQ = AplicarFiltrosBusqueda(db.Movimientos.AsQueryable(), out hayFiltroFecha);

                var movData = (from mv in movQ
                               join tm in db.TiposMovimiento
                                   on mv.TipoMovimientoID equals tm.TipoMovimientoID
                               select new { tm.Clave, mv.Cantidad, mv.Costo }).ToList();

                lblTotalHoy.Text = movData.Count.ToString();
                lblEntradas.Text = movData.Count(m => m.Clave == "ENTRADA").ToString();
                lblTraspasos.Text = movData.Count(m => m.Clave == "TRANSFERENCIA").ToString();
                lblAjustes.Text = movData.Count(m =>
                    m.Clave == "AJUSTE_POS" || m.Clave == "AJUSTE_NEG").ToString();
                lblMermas.Text = movData.Count(m => m.Clave == "MERMA").ToString();
                lblConsumos.Text = movData.Count(m => m.Clave == "CONSUMO").ToString();
                lblSalidas.Text = movData.Count(m => m.Clave == "SALIDA").ToString();

                // AJUSTE_NEG y MERMA restan; TRANSFERENCIA tiene costo=0 y no afecta
                decimal valorTotal = movData.Sum(m =>
                    (m.Clave == "AJUSTE_NEG" || m.Clave == "MERMA")
                    ? -(m.Cantidad * m.Costo)
                    : (m.Cantidad * m.Costo));
                lblValorHoy.Text = valorTotal.ToString("$#,##0.00");

                // Actualizar etiquetas según contexto
                bool hayFiltroActivo = hayFiltroFecha ||
                    cblFiltrTipo.Items.Cast<ListItem>().Any(li => li.Selected) ||
                    !string.IsNullOrEmpty(ddlFiltrBase.SelectedValue) ||
                    !string.IsNullOrEmpty(ddlFiltrItem.SelectedValue);

                lblTituloTotal.Text = hayFiltroActivo ? "TOTAL FILTRADO" : "TOTAL";
                lblDescValor.Text = hayFiltroActivo
                    ? "Valor Total del Período / Filtro Aplicado"
                    : "Valor Total (Entradas + Ajustes − Mermas/Ajustes negativos)";
            }
        }

        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                // ── Paso 1: Filtros sobre la tabla base ───────────────────────
                bool hayFiltroFecha;
                IQueryable<Modelo.Movimientos> movQ =
                    AplicarFiltrosBusqueda(db.Movimientos.AsQueryable(), out hayFiltroFecha);

                int total = movQ.Count();
                int pageIdx = gvMovimientos.PageIndex;
                int pageSz = gvMovimientos.PageSize;
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
                           join tm in db.TiposMovimiento
                               on mv.TipoMovimientoID equals tm.TipoMovimientoID
                           join mat in db.Materiales
                               on mv.MaterialID equals (int?)mat.MaterialID into matG
                           from mat in matG.DefaultIfEmpty()
                           join prd in db.Productos
                               on mv.ProductoID equals (int?)prd.ProductoID into prdG
                           from prd in prdG.DefaultIfEmpty()
                           join bo in db.Bases
                               on mv.BaseOrigenID equals (int?)bo.BaseID into boG
                           from bo in boG.DefaultIfEmpty()
                           join bd in db.Bases
                               on mv.BaseDestinoID equals (int?)bd.BaseID into bdG
                           from bd in bdG.DefaultIfEmpty()
                           join lm in db.LotesMovimiento
                               on mv.LoteID equals (int?)lm.LoteID into lmG
                           from lm in lmG.DefaultIfEmpty()
                           join prov in db.Proveedores
                               on lm.ProveedorID equals (int?)prov.ProveedorID into provG
                           from prov in provG.DefaultIfEmpty()
                           select new
                           {
                               mv.MovimientoID,
                               Fecha = mv.FechaMovimiento,
                               TipoClave = tm.Clave,
                               TipoNombre = tm.Nombre,
                               TipoItem = mv.TipoItem,
                               MatCodigo = mat.Codigo,
                               MatNombre = mat.Descripcion,
                               MatUnidad = mat.Unidad,
                               PrdCodigo = prd.Codigo,
                               PrdNombre = prd.Descripcion,
                               BaseOrig = bo.Nombre,
                               BaseDest = bd.Nombre,
                               mv.Cantidad,
                               mv.CantidadCapturada,
                               mv.FactorAplicado,
                               UnidadCapturaID = mv.UnidadCapturaID,
                               mv.Costo,
                               mv.RegistradoPorID,
                               LoteObs = lm.Observaciones ?? mv.Observaciones,
                               FolioLote = lm.Folio,
                               ProveedorNombre = prov.Nombre
                           }).ToList();

                // ── Paso 4a: Nombres de unidades de captura ──────────────────────
                var ucIds = raw
                    .Where(r => r.UnidadCapturaID.HasValue)
                    .Select(r => r.UnidadCapturaID.Value)
                    .Distinct().ToList();
                var unidadesCaptura = ucIds.Count > 0
                    ? db.UnidadesMedida
                          .Where(u => ucIds.Contains(u.UnidadMedidaID))
                          .ToDictionary(u => u.UnidadMedidaID,
                                        u => u.Nombre + " (" + u.Clave + ")")
                    : new Dictionary<int, string>();

                // ── Paso 4: Nombres de usuarios via API de Asistencia ────────────
                Dictionary<int, string> nombresUsuario = new Dictionary<int, string>();
                try
                {
                    // ClaveID → UsuarioID (local, sin cross-DB)
                    var claveIds = raw.Select(r => r.RegistradoPorID).Distinct().ToList();
                    var claveToUsuario = db.DatosUsuario
                        .Where(du => claveIds.Contains(du.ClaveID))
                        .Select(du => new { du.ClaveID, du.UsuarioID })
                        .ToList();

                    // UsuarioID → NombreCompleto (API, cacheado)
                    var usuarioIds = claveToUsuario
                        .Where(x => x.UsuarioID.HasValue)
                        .Select(x => x.UsuarioID.Value)
                        .ToList();
                    var apiNombres = UsuarioService.ObtenerEmpleadosBulk(usuarioIds)
                        .ToDictionary(e => e.IdUsuario, e => e.NombreCompleto);

                    // Construir mapa final ClaveID → Nombre
                    nombresUsuario = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => x.UsuarioID.HasValue && apiNombres.ContainsKey(x.UsuarioID.Value)
                             ? apiNombres[x.UsuarioID.Value]
                             : $"Usuario {x.ClaveID}");
                }
                catch { /* Si falla la API mostramos el ClaveID */ }

                // ── Paso 5: Proyectar a ViewModel respetando el orden de los IDs ─
                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.MovimientoID == id))
                    .Where(r => r != null)
                    .Select(r => new MovimientoVM
                    {
                        MovimientoID = r.MovimientoID,
                        Fecha = r.Fecha,
                        TipoClave = r.TipoClave ?? "",
                        Tipo = r.TipoNombre ?? "",
                        TipoItem = r.TipoItem ?? "",
                        ItemCodigo = (r.TipoItem == "Material" || r.TipoItem == "MermaMaterial")
                                      ? (r.MatCodigo ?? "") : (r.PrdCodigo ?? ""),
                        ItemNombre = (r.TipoItem == "Material" || r.TipoItem == "MermaMaterial")
                                      ? (r.MatNombre ?? "") : (r.PrdNombre ?? ""),
                        BaseOrigen = r.BaseOrig ?? "",
                        BaseDestino = r.BaseDest ?? "",
                        Cantidad = r.Cantidad,
                        CantidadCapturada = r.CantidadCapturada ?? r.Cantidad,
                        UnidadCapturaNombre = r.UnidadCapturaID.HasValue &&
                                             unidadesCaptura.ContainsKey(r.UnidadCapturaID.Value)
                                             ? unidadesCaptura[r.UnidadCapturaID.Value] : "",
                        TuvoConversion = r.FactorAplicado.HasValue &&
                                             r.FactorAplicado.Value != 1m,
                        Unidad = (r.TipoItem == "Material" || r.TipoItem == "MermaMaterial")
                                        ? (r.MatUnidad ?? "")
                                        : "Unidad/es",
                        Costo = r.Costo,
                        Total = r.Cantidad * r.Costo,
                        RegistradoPor = nombresUsuario.ContainsKey(r.RegistradoPorID)
                                        ? nombresUsuario[r.RegistradoPorID]
                                        : r.RegistradoPorID.ToString(),
                        Observaciones = r.LoteObs ?? "",
                        FolioLote = r.FolioLote ?? "",
                        ProveedorNombre = r.ProveedorNombre ?? ""
                    }).ToList();

                gvMovimientos.DataSource = pagina;
                gvMovimientos.DataBind();
            }
        }

        // ══ Eventos de filtros y grid ═════════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvMovimientos.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            foreach (ListItem li in cblFiltrTipo.Items) li.Selected = false;
            ddlFiltrBase.SelectedIndex = 0;
            ddlFiltrItem.SelectedIndex = 0;
            string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
            txtFechaDesde.Text = hoy;
            txtFechaHasta.Text = hoy;
            gvMovimientos.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void gvMovimientos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMovimientos.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // No-op: ya no hace postback desde JS, se deja para no romper compilación
        protected void btnCargarItems_Click(object sender, EventArgs e) { }

        // ══ Guardar lote de movimientos ══════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // ── Leer ítems del JSON ───────────────────────────────────────────
            List<ItemLoteModel> items;
            try
            {
                var rawJson = hdnItemsJson.Value ?? "[]";
                items = _json.Deserialize<List<ItemLoteModel>>(rawJson);
            }
            catch
            {
                items = new List<ItemLoteModel>();
            }

            if (items == null || items.Count == 0)
            {
                SetMsg("warning", "Sin ítems",
                    "Agregue al menos un ítem antes de guardar.", "modalNuevo");
                return;
            }

            const int MaxItemsPorLote = 50;
            if (items.Count > MaxItemsPorLote)
            {
                SetMsg("warning", "Límite excedido",
                    $"Un lote no puede contener más de {MaxItemsPorLote} ítems.", "modalNuevo");
                return;
            }

            // ── Validar tipo de movimiento ────────────────────────────────────
            if (string.IsNullOrEmpty(ddlTipoMovimiento.SelectedValue))
            {
                SetMsg("warning", "Campo requerido",
                    "Seleccione el tipo de movimiento.", "modalNuevo");
                return;
            }
            int tipoMovID = int.Parse(ddlTipoMovimiento.SelectedValue);

            string claveTipo;
            using (var dbClave = NuevoDb(false))
            {
                claveTipo = dbClave.TiposMovimiento
                    .Where(t => t.TipoMovimientoID == tipoMovID)
                    .Select(t => t.Clave)
                    .FirstOrDefault();
            }
            if (string.IsNullOrEmpty(claveTipo))
                throw new InvalidOperationException(
                    $"Tipo de movimiento ID={tipoMovID} no reconocido en el catálogo.");

            // ── Determinar bases requeridas ───────────────────────────────────
            bool requiereOrigen = claveTipo == "TRANSFERENCIA" || claveTipo == "MERMA" || claveTipo == "AJUSTE_NEG";
            bool requiereDestino = claveTipo == "ENTRADA" || claveTipo == "TRANSFERENCIA" || claveTipo == "AJUSTE_POS";

            int? baseOrigenID = null;
            int? baseDestinoID = null;

            if (requiereOrigen)
            {
                if (string.IsNullOrEmpty(ddlBaseOrigen.SelectedValue))
                {
                    SetMsg("warning", "Campo requerido", "Seleccione la base de origen.", "modalNuevo");
                    return;
                }
                baseOrigenID = int.Parse(ddlBaseOrigen.SelectedValue);
            }
            if (requiereDestino)
            {
                if (string.IsNullOrEmpty(ddlBaseDestino.SelectedValue))
                {
                    SetMsg("warning", "Campo requerido", "Seleccione la base de destino.", "modalNuevo");
                    return;
                }
                baseDestinoID = int.Parse(ddlBaseDestino.SelectedValue);
            }
            if (claveTipo == "TRANSFERENCIA" &&
                baseOrigenID.HasValue && baseDestinoID.HasValue &&
                baseOrigenID.Value == baseDestinoID.Value)
            {
                SetMsg("warning", "Bases inválidas",
                    "La base de origen y destino no pueden ser la misma.", "modalNuevo");
                return;
            }

            string obs = txtObservaciones.Text.Trim();
            int claveID = Convert.ToInt32(Session["ClaveID"]);
            bool restaStock = claveTipo == "TRANSFERENCIA" || claveTipo == "MERMA" || claveTipo == "AJUSTE_NEG";

            using (var db = NuevoDb(true))
            {
                // Pre-validar stock de todos los ítems antes de abrir la transacción
                if (restaStock)
                {
                    foreach (var it in items)
                    {
                        decimal cantBase = it.cantidadCapturada * it.factor;
                        if (!ValidarStockSuficiente(db, it.tipoItem, it.itemId, baseOrigenID, cantBase))
                            return;
                    }
                }

                db.Connection.Open();
                using (var tx = db.Connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    db.Transaction = tx;
                    try
                    {
                        // ── 1. Crear LoteMovimiento con folio ─────────────────
                        string folio = GenerarFolioLote(db);
                        int? proveedorID = claveTipo == "ENTRADA" && !string.IsNullOrEmpty(ddlProveedor.SelectedValue)
                            ? (int?)int.Parse(ddlProveedor.SelectedValue)
                            : null;
                        string numeroNota = txtNumeroNota.Text.Trim();
                        var lote = new Modelo.LotesMovimiento
                        {
                            Folio = folio,
                            TipoMovimientoID = tipoMovID,
                            BaseOrigenID = baseOrigenID,
                            BaseDestinoID = baseDestinoID,
                            Observaciones = string.IsNullOrEmpty(obs) ? null : obs,
                            RegistradoPorID = claveID,
                            FechaLote = AppHelper.Ahora.Date,
                            FechaRegistro = AppHelper.Ahora,
                            ProveedorID = proveedorID,
                            NumeroNota = (claveTipo == "ENTRADA" && !string.IsNullOrEmpty(numeroNota))
                                         ? numeroNota : null
                        };
                        db.LotesMovimiento.InsertOnSubmit(lote);
                        db.SubmitChanges(); // obtener LoteID

                        // ── 2. Insertar movimiento + actualizar stock por ítem ─
                        foreach (var it in items)
                        {
                            var (cantBase, unidadCapturaID, factorAplicado) =
                                ResolverConversion(db, it);

                            var mov = new Modelo.Movimientos
                            {
                                TipoMovimientoID = tipoMovID,
                                TipoItem = it.tipoItem,
                                MaterialID = (it.tipoItem == "Material" || it.tipoItem == "MermaMaterial") ? (int?)it.itemId : null,
                                ProductoID = (it.tipoItem == "Producto" || it.tipoItem == "MermaProducto") ? (int?)it.itemId : null,
                                BaseOrigenID = baseOrigenID,
                                BaseDestinoID = baseDestinoID,
                                Cantidad = cantBase,
                                CantidadCapturada = it.cantidadCapturada,
                                UnidadCapturaID = unidadCapturaID,
                                FactorAplicado = factorAplicado,
                                Costo = it.costo,
                                LoteID = lote.LoteID,
                                Observaciones = null,
                                RegistradoPorID = claveID,
                                FechaMovimiento = lote.FechaRegistro
                            };
                            db.Movimientos.InsertOnSubmit(mov);

                            // Validación server-side: MermaMaterial/MermaProducto solo admiten
                            // TRANSFERENCIA, AJUSTE_POS y AJUSTE_NEG
                            if ((it.tipoItem == "MermaMaterial" || it.tipoItem == "MermaProducto") &&
                                claveTipo != "TRANSFERENCIA" && claveTipo != "AJUSTE_POS" && claveTipo != "AJUSTE_NEG")
                            {
                                tx.Rollback();
                                SetMsg("warning", "Combinación inválida",
                                    "Para ítems de merma solo se permiten: Transferencia, Ajuste positivo y Ajuste negativo.",
                                    "modalNuevo");
                                return;
                            }

                            switch (claveTipo)
                            {
                                case "ENTRADA":
                                    UpsertStock(db, it.tipoItem, it.itemId, baseDestinoID, +cantBase); break;
                                case "TRANSFERENCIA":
                                    if (it.tipoItem == "MermaMaterial")
                                    {
                                        UpsertStockMerma(db.Connection, tx, it.itemId, baseOrigenID.Value, -cantBase);
                                        UpsertStockMerma(db.Connection, tx, it.itemId, baseDestinoID.Value, +cantBase);
                                    }
                                    else if (it.tipoItem == "MermaProducto")
                                    {
                                        int d = (int)Math.Round(cantBase, MidpointRounding.AwayFromZero);
                                        UpsertRechazoProducto(db, it.itemId, baseOrigenID.Value, -d);
                                        UpsertRechazoProducto(db, it.itemId, baseDestinoID.Value, +d);
                                    }
                                    else
                                    {
                                        UpsertStock(db, it.tipoItem, it.itemId, baseOrigenID, -cantBase);
                                        UpsertStock(db, it.tipoItem, it.itemId, baseDestinoID, +cantBase);
                                    }
                                    break;
                                case "MERMA":
                                    UpsertStock(db, it.tipoItem, it.itemId, baseOrigenID, -cantBase);
                                    if (it.tipoItem == "Material")
                                        UpsertStockMerma(db.Connection, tx, it.itemId, baseOrigenID.Value, cantBase);
                                    else if (it.tipoItem == "Producto" && baseOrigenID.HasValue)
                                        UpsertRechazoProducto(db, it.itemId, baseOrigenID.Value,
                                            (int)Math.Round(cantBase, MidpointRounding.AwayFromZero));
                                    break;
                                case "AJUSTE_POS":
                                    if (it.tipoItem == "MermaMaterial")
                                        UpsertStockMerma(db.Connection, tx, it.itemId, baseDestinoID.Value, +cantBase);
                                    else if (it.tipoItem == "MermaProducto")
                                        UpsertRechazoProducto(db, it.itemId, baseDestinoID.Value,
                                            (int)Math.Round(cantBase, MidpointRounding.AwayFromZero));
                                    else
                                        UpsertStock(db, it.tipoItem, it.itemId, baseDestinoID, +cantBase);
                                    break;
                                case "AJUSTE_NEG":
                                    if (it.tipoItem == "MermaMaterial")
                                        UpsertStockMerma(db.Connection, tx, it.itemId, baseOrigenID.Value, -cantBase);
                                    else if (it.tipoItem == "MermaProducto")
                                        UpsertRechazoProducto(db, it.itemId, baseOrigenID.Value,
                                            -(int)Math.Round(cantBase, MidpointRounding.AwayFromZero));
                                    else
                                        UpsertStock(db, it.tipoItem, it.itemId, baseOrigenID, -cantBase);
                                    break;
                                default:
                                    throw new InvalidOperationException(
                                        $"Tipo de movimiento '{claveTipo}' sin lógica de stock definida.");
                            }
                        }

                        db.SubmitChanges();

                        // ── 3. Auto-crear CxP si es ENTRADA con proveedor ──────
                        if (claveTipo == "ENTRADA" && lote.ProveedorID.HasValue)
                        {
                            decimal monto = db.Movimientos
                                .Where(mv => mv.LoteID == lote.LoteID)
                                .Sum(mv => mv.Cantidad * mv.Costo);
                            if (monto > 0)
                            {
                                var prov = db.Proveedores
                                    .Single(p => p.ProveedorID == lote.ProveedorID.Value);
                                int dias = prov.DiasCredito ?? 0;
                                db.CuentasPorPagar.InsertOnSubmit(new Modelo.CuentasPorPagar
                                {
                                    LoteID = lote.LoteID,
                                    ProveedorID = lote.ProveedorID.Value,
                                    NumeroNota = lote.NumeroNota,
                                    FechaRecepcion = lote.FechaLote,
                                    FechaVencimiento = lote.FechaLote.AddDays(dias),
                                    DiasCreditoAplicados = dias,
                                    MontoTotal = monto,
                                    Estado = "PENDIENTE",
                                    RegistradoPorID = claveID,
                                    FechaRegistro = AppHelper.Ahora
                                });
                                db.SubmitChanges();
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            LimpiarModal();
            SetMsg("success", "Lote registrado",
                string.Format("Lote guardado con {0} movimiento(s).", items.Count));
            CargarDashboard();
            CargarGrid();
        }

        // ── Genera folio MOV-yyyyMMdd-### con UPDLOCK/HOLDLOCK ────────────────
        private string GenerarFolioLote(InventarioAnkhalDBDataContext db)
        {
            return AppHelper.GenerarFolio(db, "MOV");
        }

        // ── Resuelve la conversión de unidades para un ítem del lote ─────────
        // Retorna (cantidadBase, unidadCapturaID, factorAplicado)
        private (decimal cantBase, int? unidadCapturaID, decimal factorAplicado)
            ResolverConversion(InventarioAnkhalDBDataContext db, ItemLoteModel it)
        {
            if (it.tipoItem != "Material" && it.tipoItem != "MermaMaterial")
                return (it.cantidadCapturada, null, 1m);

            // El JS ya resolvió el factor; el unidadId puede ser el UnidadMedidaID (unidad base)
            // o un ConversionID. Si factor == 1 o unidadId == 0, es unidad base.
            if (it.factor == 1m || it.unidadId == 0)
            {
                var mat = db.Materiales.FirstOrDefault(m => m.MaterialID == it.itemId);
                return (it.cantidadCapturada, mat != null ? mat.UnidadMedidaID : null, 1m);
            }

            // Verificar que la conversión sigue activa en servidor (seguridad)
            var conv = db.ConversionesMaterial
                .FirstOrDefault(c => c.ConversionID == it.unidadId &&
                                     c.MaterialID == it.itemId &&
                                     c.Activo);
            if (conv == null)
                throw new InvalidOperationException(
                    $"Conversión ID={it.unidadId} ya no está activa para el material {it.itemId}.");

            return (it.cantidadCapturada * conv.Factor, conv.UnidadOrigenID, conv.Factor);
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
                // Revisar primero si ya hay un insert pendiente para el mismo (Base, Material)
                // en el mismo contexto — evita duplicado cuando el mismo material aparece
                // varias veces en el lote (distintas unidades de captura)
                var pending = db.GetChangeSet().Inserts
                    .OfType<StockMateriales>()
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.MaterialID == itemID);

                if (pending != null)
                {
                    pending.CantidadActual += delta;
                    pending.FechaUltimaModif = AppHelper.Ahora;
                    return;
                }

                var s = db.StockMateriales
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.MaterialID == itemID);

                if (s == null)
                {
                    db.StockMateriales.InsertOnSubmit(new StockMateriales
                    {
                        BaseID = baseID.Value,
                        MaterialID = itemID,
                        CantidadActual = delta,
                        FechaUltimaModif = AppHelper.Ahora
                    });
                }
                else
                {
                    s.CantidadActual += delta;
                    s.FechaUltimaModif = AppHelper.Ahora;
                }
            }
            else if (tipoItem == "Producto") // CantidadBuenas es int
            {
                // Misma protección para productos
                int deltaInt = (int)Math.Round(delta, MidpointRounding.AwayFromZero);

                var pendingP = db.GetChangeSet().Inserts
                    .OfType<StockProductos>()
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.ProductoID == itemID);

                if (pendingP != null)
                {
                    pendingP.CantidadBuenas += deltaInt;
                    pendingP.FechaUltimaModif = AppHelper.Ahora;
                    return;
                }

                var s = db.StockProductos
                    .FirstOrDefault(x => x.BaseID == baseID.Value && x.ProductoID == itemID);

                if (s == null)
                {
                    db.StockProductos.InsertOnSubmit(new StockProductos
                    {
                        BaseID = baseID.Value,
                        ProductoID = itemID,
                        CantidadBuenas = deltaInt > 0 ? deltaInt : 0,
                        CantidadRechazo = 0,
                        FechaUltimaModif = AppHelper.Ahora
                    });
                }
                else
                {
                    s.CantidadBuenas += deltaInt;
                    s.FechaUltimaModif = AppHelper.Ahora;
                }
            }
        }

        private void UpsertRechazoProducto(InventarioAnkhalDBDataContext db,
            int productoID, int baseID, int delta)
        {
            var s = db.StockProductos
                .FirstOrDefault(x => x.BaseID == baseID && x.ProductoID == productoID);

            if (s == null)
            {
                db.StockProductos.InsertOnSubmit(new StockProductos
                {
                    BaseID = baseID,
                    ProductoID = productoID,
                    CantidadBuenas = 0,
                    CantidadRechazo = delta > 0 ? delta : 0,
                    FechaUltimaModif = AppHelper.Ahora
                });
            }
            else
            {
                s.CantidadRechazo += delta;
                s.FechaUltimaModif = AppHelper.Ahora;
            }
        }

        private void UpsertStockMerma(System.Data.IDbConnection conn, System.Data.IDbTransaction tx,
            int materialID, int baseID, decimal delta)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE dbo.StockMerma
                       SET CantidadActual   = CantidadActual + @delta,
                           FechaUltimaModif = SYSUTCDATETIME()
                     WHERE BaseID = @baseID AND MaterialID = @materialID";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@delta"; p1.Value = delta; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@baseID"; p2.Value = baseID; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.ParameterName = "@materialID"; p3.Value = materialID; cmd.Parameters.Add(p3);
                int filas = cmd.ExecuteNonQuery();
                if (filas == 0 && delta > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                        INSERT INTO dbo.StockMerma (BaseID, MaterialID, CantidadActual, FechaUltimaModif)
                        VALUES (@baseID, @materialID, @delta, SYSUTCDATETIME())";
                    var q1 = cmd.CreateParameter(); q1.ParameterName = "@baseID"; q1.Value = baseID; cmd.Parameters.Add(q1);
                    var q2 = cmd.CreateParameter(); q2.ParameterName = "@materialID"; q2.Value = materialID; cmd.Parameters.Add(q2);
                    var q3 = cmd.CreateParameter(); q3.ParameterName = "@delta"; q3.Value = delta; cmd.Parameters.Add(q3);
                    cmd.ExecuteNonQuery();
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

            decimal actual = 0;
            string nombreItem = "";

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
            else if (tipoItem == "MermaMaterial")
            {
                actual = db.ExecuteQuery<decimal?>(
                    "SELECT CantidadActual FROM dbo.StockMerma WHERE BaseID={0} AND MaterialID={1}",
                    baseID.Value, itemID).FirstOrDefault() ?? 0m;

                nombreItem = db.Materiales
                    .Where(m => m.MaterialID == itemID)
                    .Select(m => m.Descripcion)
                    .FirstOrDefault() ?? "Material (Merma)";
            }
            else if (tipoItem == "MermaProducto")
            {
                actual = db.StockProductos
                    .Where(x => x.BaseID == baseID.Value && x.ProductoID == itemID)
                    .Select(x => (decimal)x.CantidadRechazo)
                    .FirstOrDefault();

                nombreItem = db.Productos
                    .Where(p => p.ProductoID == itemID)
                    .Select(p => p.Descripcion)
                    .FirstOrDefault() ?? "Producto (Rechazo)";
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

        // ══ Helper para la etiqueta de merma en la columna Item ═════════════
        /// <summary>
        /// Devuelve un badge HTML naranja "Merma" cuando el TipoItem es MermaMaterial
        /// o MermaProducto; cadena vacía en cualquier otro caso.
        /// </summary>
        public string GetEtiquetaMerma(object tipoItem)
        {
            string tipo = (tipoItem ?? "").ToString();
            if (tipo == "MermaMaterial" || tipo == "MermaProducto")
                return "<span class='badge' style='background:#e67e22;color:#fff;" +
                       "font-size:.70rem;vertical-align:middle;margin-left:4px;'>Merma</span>";
            return "";
        }

        // ══ Helper para la columna Cantidad/Unidad ═══════════════════════════
        public string FormatCantidadGrilla(object cantCapObj, object unidCapObj,
                                           object cantBaseObj, object unidBaseObj,
                                           object tuvoConvObj)
        {
            decimal cantCap = Convert.ToDecimal(cantCapObj ?? 0m);
            decimal cantBase = Convert.ToDecimal(cantBaseObj ?? 0m);
            string unidCap = (unidCapObj ?? "").ToString();
            string unidBase = (unidBaseObj ?? "").ToString();
            bool tuvoConv = Convert.ToBoolean(tuvoConvObj ?? false);

            string FmtDec(decimal d) => d.ToString("0.##");

            if (tuvoConv && !string.IsNullOrEmpty(unidCap))
                return string.Format(
                    "<strong>{0} {1}</strong>" +
                    "<small class='text-muted d-block'>= {2} {3}</small>",
                    FmtDec(cantCap),
                    System.Web.HttpUtility.HtmlEncode(unidCap),
                    FmtDec(cantBase),
                    System.Web.HttpUtility.HtmlEncode(unidBase));

            return string.Format(
                "<strong>{0}</strong> <span class='text-muted'>{1}</span>",
                FmtDec(cantBase),
                System.Web.HttpUtility.HtmlEncode(unidBase));
        }

        // ══ Helper para el badge de tipo en el GridView ═══════════════════════
        public string GetBadgeTipo(object clave)
        {
            switch ((clave ?? "").ToString().ToUpper())
            {
                case "ENTRADA": return "badge badge-entrada";
                case "SALIDA": return "badge badge-salida";
                case "TRANSFERENCIA": return "badge badge-transferencia";
                case "CONSUMO": return "badge badge-consumo";
                case "MERMA": return "badge badge-merma";
                case "AJUSTE_POS": return "badge badge-ajuste-pos";
                case "AJUSTE_NEG": return "badge badge-ajuste-neg";
                default: return "badge badge-secondary";
            }
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void LimpiarModal()
        {
            ddlTipoMovimiento.SelectedIndex = 0;
            hdnTipoItemSeleccionado.Value = "Material";
            hdnItemsJson.Value = "[]";
            ddlBaseOrigen.SelectedIndex = 0;
            ddlBaseDestino.SelectedIndex = 0;
            ddlProveedor.SelectedIndex = 0;
            txtNumeroNota.Text = "";
            txtCantidad.Text = "";
            txtCosto.Text = "";
            txtObservaciones.Text = "";
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        // ══ WebMethods ═══════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el precio vigente de un material para un proveedor dado.
        /// "Vigente" = registro con MAX(FechaInicio) donde FechaInicio &lt;= hoy.
        /// Retorna { precio: decimal?, encontrado: bool }.
        /// Si no existe vínculo activo o no hay precio, encontrado = false.
        /// </summary>
        [System.Web.Services.WebMethod(EnableSession = true)]
        public static object ObtenerPrecioProveedor(int materialID, int proveedorID)
        {
            if (materialID <= 0 || proveedorID <= 0)
                return new { precio = (decimal?)null, encontrado = false };

            string connStr = System.Configuration.ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                // Buscar vínculo activo entre este proveedor y este material
                var pm = db.ProveedorMateriales.FirstOrDefault(x =>
                    x.ProveedorID == proveedorID &&
                    x.MaterialID == materialID &&
                    x.Activo);

                if (pm == null)
                    return new { precio = (decimal?)null, encontrado = false };

                // Precio vigente: TOP 1 donde FechaInicio <= hoy.
                // Desempate por HistorialPrecioID DESC para que, cuando dos precios
                // comparten la misma FechaInicio, gane el registrado más recientemente.
                DateTime hoy = AppHelper.Hoy;
                decimal? precioV = db.HistorialPrecios
                    .Where(hp => hp.ProveedorMaterialID == pm.ProveedorMaterialID
                              && hp.FechaInicio <= hoy)
                    .OrderByDescending(hp => hp.FechaInicio)
                    .ThenByDescending(hp => hp.HistorialPrecioID)
                    .Select(hp => (decimal?)hp.Precio)
                    .FirstOrDefault();

                return new { precio = precioV, encontrado = precioV.HasValue };
            }
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static object ObtenerPrecioProveedorProducto(int productoID, int proveedorID)
        {
            if (productoID <= 0 || proveedorID <= 0)
                return new { precio = (decimal?)null, encontrado = false };

            string connStr = System.Configuration.ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                var pp = db.ProveedorProductos.FirstOrDefault(x =>
                    x.ProveedorID == proveedorID &&
                    x.ProductoID == productoID &&
                    x.Activo);

                if (pp == null)
                    return new { precio = (decimal?)null, encontrado = false };

                DateTime hoy = AppHelper.Hoy;
                decimal? precioV = db.HistorialPreciosProductos
                    .Where(h => h.ProveedorProductoID == pp.ProveedorProductoID
                             && h.FechaInicio <= hoy)
                    .OrderByDescending(h => h.FechaInicio)
                    .ThenByDescending(h => h.HistorialPrecioProductoID)
                    .Select(h => (decimal?)h.Precio)
                    .FirstOrDefault();

                return new { precio = precioV, encontrado = precioV.HasValue };
            }
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static object BuscarItems(string query, string tipo)
        {
            string connStr = System.Configuration.ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                if (tipo == "Producto" || tipo == "MermaProducto")
                {
                    return db.Productos
                        .Where(p => p.Activo &&
                            (p.Descripcion.Contains(query) || p.Codigo.Contains(query)))
                        .OrderBy(p => p.Codigo)
                        .Take(15)
                        .Select(p => new {
                            id = p.ProductoID,
                            nombre = "[" + p.Codigo + "] " + p.Descripcion,
                            precio = p.PrecioVenta
                        })
                        .ToList();
                }
                else // Material o MermaMaterial
                {
                    var mats = db.Materiales
                        .Where(m => m.Activo &&
                            (m.Descripcion.Contains(query) || m.Codigo.Contains(query)))
                        .OrderBy(m => m.Codigo)
                        .Take(15)
                        .ToList();

                    var matIDs = mats.Select(m => m.MaterialID).ToList();
                    var unidades = db.UnidadesMedida.ToDictionary(u => u.UnidadMedidaID);
                    var convs = (from c in db.ConversionesMaterial
                                 where c.Activo && matIDs.Contains(c.MaterialID)
                                 join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                                 select new { c.MaterialID, c.ConversionID, c.Factor, u.Clave, u.Nombre })
                                   .ToList();

                    var resultado = new List<object>();
                    foreach (var m in mats)
                    {
                        string baseNombre = m.Unidad ?? "";
                        string baseVal = "0";
                        if (m.UnidadMedidaID.HasValue && unidades.ContainsKey(m.UnidadMedidaID.Value))
                        {
                            var ub = unidades[m.UnidadMedidaID.Value];
                            baseNombre = ub.Nombre + " (" + ub.Clave + ")";
                            baseVal = m.UnidadMedidaID.Value.ToString();
                        }
                        var opciones = new List<object>();
                        opciones.Add(new { val = baseVal, txt = baseNombre + " — base", factor = 1.0 });

                        foreach (var c in convs.Where(x => x.MaterialID == m.MaterialID))
                        {
                            string txt = c.Nombre + " (" + c.Clave + ")  [×" + c.Factor.ToString("N6") + "]";
                            opciones.Add(new { val = c.ConversionID.ToString(), txt, factor = (double)c.Factor });
                        }

                        resultado.Add(new
                        {
                            id = m.MaterialID,
                            nombre = "[" + m.Codigo + "] " + m.Descripcion,
                            precio = m.PrecioUnitario,
                            conversiones = opciones
                        });
                    }
                    return resultado;
                }
            }
        }
    }
}
