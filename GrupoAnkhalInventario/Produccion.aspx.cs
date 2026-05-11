using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using GrupoAnkhalInventario.Services;

namespace GrupoAnkhalInventario
{
    public partial class ProduccionPage : System.Web.UI.Page
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

        // ══ ViewModels ═══════════════════════════════════════════════════════
        public class ProduccionVM
        {
            public int      ProduccionID    { get; set; }
            public DateTime Fecha           { get; set; }
            public string   BaseNombre      { get; set; }
            public string   Turno           { get; set; }
            public string   ProductoCodigo  { get; set; }
            public string   ProductoNombre  { get; set; }
            public int      CantidadBuena   { get; set; }
            public int      CantidadRechazo { get; set; }
            public int      Total           { get; set; }
            public decimal  MetaBase        { get; set; }   // MetaDiaria de la base ($)
            public int      CumplPct        { get; set; }
            public decimal  Valor           { get; set; }
            public string   RegistradoPor   { get; set; }
            public string   Observaciones   { get; set; }
            public List<ConsumoDetalleVM> Consumos { get; set; } = new List<ConsumoDetalleVM>();
        }

        public class ConsumoDetalleVM
        {
            public string  MaterialCodigo { get; set; }
            public string  MaterialNombre { get; set; }
            public string  Unidad         { get; set; }   // descripción larga (ej. "Litros")
            public string  UnidadClave    { get; set; }   // abreviatura unidad base (ej. "L")
            public decimal TeoMin         { get; set; }   // teórico mín en unidad base
            public decimal TeoMax         { get; set; }   // teórico máx en unidad base
            public decimal Real           { get; set; }   // real en unidad base
            public decimal Excedente      { get; set; }
            public decimal Deficit        { get; set; }
            public bool    EsMerma        { get; set; }
            // ── Valores capturados (unidad de captura del BOM) ─────────────────
            public decimal TeoMinCap      { get; set; }   // teórico mín en unidad captura
            public decimal TeoMaxCap      { get; set; }   // teórico máx en unidad captura
            public decimal RealCap        { get; set; }   // real capturado (no usado en grid por ahora)
            public string  UnidadCap      { get; set; }   // abreviatura unidad captura (ej. "ml")
            public bool    TieneCaptura   { get; set; }   // true si hay datos de unidad de captura
        }


        public class OpcionUnidadVM
        {
            public string  Valor  { get; set; }   // "base:{UnidadMedidaID}" o "conv:{ConversionID}"
            public string  Texto  { get; set; }
            public decimal Factor { get; set; }
        }

        public class ConsumoVM
        {
            public int     MaterialID           { get; set; }
            public int?    UnidadBaseID         { get; set; }   // UnidadMedidaID del material
            public string  MaterialCodigo       { get; set; }
            public string  MaterialNombre       { get; set; }
            public string  Unidad               { get; set; }
            public decimal CantidadMin          { get; set; }   // unidad base (para stock)
            public decimal CantidadMax          { get; set; }   // unidad base (para stock)
            public decimal TeoricoMin           { get; set; }   // unidad base
            public decimal TeoricoMax           { get; set; }   // unidad base
            // ── Campos de la unidad capturada en el BOM ──────────────────────
            public decimal CantMinCap           { get; set; }   // valor capturado al definir BOM
            public decimal CantMaxCap           { get; set; }   // valor capturado al definir BOM
            public decimal TeoricoMinCap        { get; set; }   // = CantMinCap × totalProd
            public decimal TeoricoMaxCap        { get; set; }   // = CantMaxCap × totalProd
            public string  UnidadCapTexto       { get; set; }   // abreviatura de la unidad BOM (ej. "cj")
            public string  ConvBOMValor         { get; set; }   // valor a pre-seleccionar en ddlUnidadConsumo
            // ─────────────────────────────────────────────────────────────────
            public string  UnidadBaseClave      { get; set; }   // abreviatura unidad base (ej. "L", "kg")
            public string  FactorBOMStr         { get; set; }   // factor unidad pre-sel., InvariantCulture (ej. "0.001")
            public decimal ConsumoReal          { get; set; }
            public decimal StockActual          { get; set; }
            public List<OpcionUnidadVM> UnidadesDisponibles { get; set; } = new List<OpcionUnidadVM>();
        }

        // ══ Page_Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarCatalogos();
                string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
                txtFechaDesde.Text      = hoy;
                txtFechaHasta.Text      = hoy;
                txtFecha.Text           = hoy;
                pnlConsumos.Visible     = false;
                lblSinConsumos.Visible  = true;
                CargarDashboard();
                CargarGrid();
            }
        }

        // ══ Catálogos ════════════════════════════════════════════════════════
        private void CargarCatalogos()
        {
            using (var db = NuevoDb(false))
            {
                var bases = AppHelper.ObtenerBasesActivasParaUsuario(Session);

                ddlFiltrBase.Items.Clear();
                ddlFiltrBase.Items.Add(new ListItem("-- Todas --", ""));
                foreach (var b in bases)
                    ddlFiltrBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                ddlBase.Items.Clear();
                ddlBase.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in bases)
                    ddlBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                var productos = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Codigo)
                    .Select(p => new { p.ProductoID, p.Descripcion, p.Codigo })
                    .ToList();

                ddlProducto.Items.Clear();
                ddlProducto.Items.Add(new ListItem("-- Seleccione --", ""));
                ddlProductoHoja.Items.Clear();
                ddlProductoHoja.Items.Add(new ListItem("-- Seleccione --", ""));
                ddlFiltrProducto.Items.Clear();
                ddlFiltrProducto.Items.Add(new ListItem("-- Todos --", ""));
                foreach (var p in productos)
                {
                    string textoProducto = string.Format("[{0}] {1}", p.Codigo, p.Descripcion);
                    ddlProducto.Items.Add(new ListItem(textoProducto, p.ProductoID.ToString()));
                    ddlProductoHoja.Items.Add(new ListItem(textoProducto, p.ProductoID.ToString()));
                    ddlFiltrProducto.Items.Add(new ListItem(textoProducto, p.ProductoID.ToString()));
                }
            }
        }

        // ══ Filtros ══════════════════════════════════════════════════════════
        private IQueryable<Produccion> AplicarFiltros(IQueryable<Produccion> q)
        {
            // Restringir por las bases del usuario (null = Administrador, ve todo)
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            if (basesUsuario != null)
                q = q.Where(p => basesUsuario.Contains(p.BaseID));

            if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
            {
                int id = int.Parse(ddlFiltrBase.SelectedValue);
                q = q.Where(p => p.BaseID == id);
            }
            if (!string.IsNullOrEmpty(txtFechaDesde.Text))
            {
                DateTime d = DateTime.Parse(txtFechaDesde.Text);
                q = q.Where(p => p.Fecha >= d);
            }
            if (!string.IsNullOrEmpty(txtFechaHasta.Text))
            {
                DateTime h = DateTime.Parse(txtFechaHasta.Text);
                q = q.Where(p => p.Fecha <= h);
            }
            if (!string.IsNullOrEmpty(ddlFiltrProducto.SelectedValue))
            {
                int pid = int.Parse(ddlFiltrProducto.SelectedValue);
                q = q.Where(p => p.ProductoID == pid);
            }
            return q;
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                var q = AplicarFiltros(db.Produccion.AsQueryable());

                var data = q.Select(p => new
                            {
                                p.CantidadBuena,
                                p.CantidadRechazo,
                                Valor = p.CantidadBuena * p.PrecioVenta
                            }).ToList();

                int     totalRegs   = data.Count;
                int     totalBuenos = data.Sum(x => x.CantidadBuena);
                int     totalRech   = data.Sum(x => x.CantidadRechazo);
                decimal totalValor  = data.Sum(x => x.Valor);

                lblTotalProd.Text = totalRegs.ToString("N0");
                lblBuenos.Text    = totalBuenos.ToString("N0");
                lblRechazo.Text   = totalRech.ToString("N0");
                lblValorProd.Text = totalValor.ToString("$#,##0.00");
            }
        }


        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                var q = AplicarFiltros(db.Produccion.AsQueryable());

                int total   = q.Count();
                int pageIdx = gvProduccion.PageIndex;
                int pageSz  = gvProduccion.PageSize;
                gvProduccion.VirtualItemCount = total;

                lblResultados.Text = total == 0
                    ? "Sin registros para los filtros aplicados."
                    : string.Format("{0} registro(s) encontrado(s).", total);

                if (total == 0)
                {
                    gvProduccion.DataSource = new List<ProduccionVM>();
                    gvProduccion.DataBind();
                    return;
                }

                // ── IDs de la página actual
                var ids = q
                    .OrderByDescending(p => p.Fecha)
                    .ThenByDescending(p => p.ProduccionID)
                    .Select(p => p.ProduccionID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                // ── Datos con JOINs solo para esos IDs
                var raw = (from p  in db.Produccion
                           where ids.Contains(p.ProduccionID)
                           join b  in db.Bases    on p.BaseID     equals b.BaseID
                           join pr in db.Productos on p.ProductoID equals pr.ProductoID
                           select new
                           {
                               p.ProduccionID,
                               p.Fecha,
                               BaseNombre      = b.Nombre,
                               MetaDiaria      = b.MetaDiaria,
                               p.Turno,
                               ProductoCodigo  = pr.Codigo,
                               ProductoNombre  = pr.Descripcion,
                               p.CantidadBuena,
                               p.CantidadRechazo,
                               PrecioVenta     = p.PrecioVenta,
                               p.RegistradoPorID,
                               p.Observaciones
                           }).ToList();

                // ── Nombres de usuario via API de Asistencia ─────────────────
                var nombresUsuario = new Dictionary<int, string>();
                try
                {
                    var claveIds = raw.Select(r => r.RegistradoPorID).Distinct().ToList();
                    var claveToUsuario = db.DatosUsuario
                        .Where(du => claveIds.Contains(du.ClaveID))
                        .Select(du => new { du.ClaveID, du.UsuarioID })
                        .ToList();

                    var usuarioIds = claveToUsuario
                        .Where(x => x.UsuarioID.HasValue)
                        .Select(x => x.UsuarioID.Value).ToList();
                    var apiNombres = UsuarioService.ObtenerEmpleadosBulk(usuarioIds)
                        .ToDictionary(e => e.IdUsuario, e => e.NombreCompleto);

                    nombresUsuario = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => x.UsuarioID.HasValue && apiNombres.ContainsKey(x.UsuarioID.Value)
                             ? apiNombres[x.UsuarioID.Value]
                             : $"Usuario {x.ClaveID}");
                }
                catch { /* si falla la API mostramos el ID */ }

                // ── Proyectar ViewModel respetando el orden de los IDs
                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.ProduccionID == id))
                    .Where(r => r != null)
                    .Select(r =>
                    {
                        int     tot2     = r.CantidadBuena + r.CantidadRechazo;
                        decimal valorReg = r.CantidadBuena * r.PrecioVenta;
                        int pct = r.MetaDiaria > 0
                            ? (int)Math.Round((double)valorReg / (double)r.MetaDiaria * 100)
                            : 0;
                        return new ProduccionVM
                        {
                            ProduccionID    = r.ProduccionID,
                            Fecha           = r.Fecha,
                            BaseNombre      = r.BaseNombre,
                            Turno           = r.Turno,
                            ProductoCodigo  = r.ProductoCodigo,
                            ProductoNombre  = r.ProductoNombre,
                            CantidadBuena   = r.CantidadBuena,
                            CantidadRechazo = r.CantidadRechazo,
                            Total           = tot2,
                            MetaBase        = r.MetaDiaria,
                            CumplPct        = pct,
                            Valor           = valorReg,
                            RegistradoPor   = nombresUsuario.ContainsKey(r.RegistradoPorID)
                                              ? nombresUsuario[r.RegistradoPorID]
                                              : r.RegistradoPorID.ToString(),
                            Observaciones   = r.Observaciones ?? ""
                        };
                    }).ToList();

                // ── Cargar detalle de consumos para los IDs de esta página
                var consumosRaw = (from cp in db.ConsumosProduccion
                                   where ids.Contains(cp.ProduccionID)
                                   join m in db.Materiales on cp.MaterialID equals m.MaterialID
                                   select new
                                   {
                                       cp.ProduccionID,
                                       m.MaterialID,
                                       m.Codigo,
                                       m.Descripcion,
                                       m.Unidad,
                                       m.UnidadMedidaID,
                                       cp.CantidadTeoricaMin,
                                       cp.CantidadTeoricaMax,
                                       cp.CantidadReal,
                                       cp.EsMerma,
                                       cp.CantidadTeoMinCap,
                                       cp.CantidadTeoMaxCap,
                                       cp.CantidadRealCap,
                                       cp.UnidadClaveCap
                                   }).ToList();

                // ── Cargar abreviaturas de unidades base (para columnas Real/Excedente)
                var unidMedidaIDs = consumosRaw
                    .Where(c => c.UnidadMedidaID.HasValue)
                    .Select(c => c.UnidadMedidaID.Value).Distinct().ToList();
                var unidClaveMap = unidMedidaIDs.Any()
                    ? db.UnidadesMedida
                        .Where(u => unidMedidaIDs.Contains(u.UnidadMedidaID))
                        .ToDictionary(u => u.UnidadMedidaID, u => u.Clave)
                    : new Dictionary<int, string>();

                foreach (var vm in pagina)
                {
                    vm.Consumos = consumosRaw
                        .Where(c => c.ProduccionID == vm.ProduccionID)
                        .Select(c => new ConsumoDetalleVM
                        {
                            MaterialCodigo = c.Codigo,
                            MaterialNombre = c.Descripcion,
                            Unidad         = c.Unidad ?? "",
                            UnidadClave    = c.UnidadMedidaID.HasValue &&
                                             unidClaveMap.ContainsKey(c.UnidadMedidaID.Value)
                                             ? unidClaveMap[c.UnidadMedidaID.Value] : "",
                            TeoMin         = c.CantidadTeoricaMin,
                            TeoMax         = c.CantidadTeoricaMax,
                            Real           = c.CantidadReal,
                            Excedente      = c.CantidadReal > c.CantidadTeoricaMax
                                             ? c.CantidadReal - c.CantidadTeoricaMax : 0m,
                            Deficit        = c.CantidadReal < c.CantidadTeoricaMin
                                             ? c.CantidadTeoricaMin - c.CantidadReal : 0m,
                            EsMerma        = c.EsMerma,
                            TeoMinCap      = c.CantidadTeoMinCap ?? 0m,
                            TeoMaxCap      = c.CantidadTeoMaxCap ?? 0m,
                            RealCap        = c.CantidadRealCap   ?? 0m,
                            UnidadCap      = c.UnidadClaveCap    ?? "",
                            TieneCaptura   = c.UnidadClaveCap    != null
                        }).ToList();
                }

                gvProduccion.DataSource = pagina;
                gvProduccion.DataBind();
            }
        }

        protected void gvProduccion_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var vm  = e.Row.DataItem as ProduccionVM;
            var rpt = e.Row.FindControl("rptDetalleConsumos") as Repeater;
            if (vm != null && rpt != null)
            {
                rpt.DataSource = vm.Consumos;
                rpt.DataBind();
            }
        }

        // ══ Eventos filtros / paginación ══════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvProduccion.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltrBase.SelectedIndex    = 0;
            ddlFiltrProducto.SelectedIndex = 0;
            string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
            txtFechaDesde.Text = hoy;
            txtFechaHasta.Text = hoy;
            gvProduccion.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void gvProduccion_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProduccion.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // ══ Abrir modal Nuevo ════════════════════════════════════════════════
        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarModal();
            ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                "window.addEventListener('load',function(){$('#modalRegistrar').modal('show');});", true);
        }

        // ══ Cargar consumos BOM al cambiar producto ═══════════════════════════
        protected void btnCargarConsumos_Click(object sender, EventArgs e)
        {
            string prodIdStr = hdnProductoSeleccionado.Value;

            if (string.IsNullOrEmpty(prodIdStr) || prodIdStr == "0")
            {
                pnlConsumos.Visible    = false;
                lblSinConsumos.Text    = "Seleccione un producto para cargar los consumos de materiales.";
                lblSinConsumos.Visible = true;
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalRegistrar').modal('show');});", true);
                return;
            }

            int productoID = int.Parse(prodIdStr);
            int baseID     = 0;
            int.TryParse(ddlBase.SelectedValue, out baseID);

            int cantBuena = 0, cantRechazo = 0;
            int.TryParse(txtCantBuena.Text,   out cantBuena);
            int.TryParse(txtCantRechazo.Text, out cantRechazo);
            int totalProd = cantBuena + cantRechazo;

            List<ConsumoVM> bom;
            using (var db = NuevoDb(false))
            {
                bom = (from pm in db.ProductoMateriales
                           where pm.ProductoID == productoID && pm.Activo
                           join m in db.Materiales on pm.MaterialID equals m.MaterialID
                           select new ConsumoVM
                           {
                               MaterialID     = m.MaterialID,
                               UnidadBaseID   = m.UnidadMedidaID,
                               MaterialCodigo = m.Codigo,
                               MaterialNombre = m.Descripcion,
                               Unidad         = m.Unidad,
                               CantidadMin    = pm.CantidadMin,
                               CantidadMax    = pm.CantidadMax,
                               TeoricoMin     = pm.CantidadMin * totalProd,
                               TeoricoMax     = pm.CantidadMax * totalProd,
                               // Capturadas: usar los valores originales del BOM si existen
                               CantMinCap     = pm.CantMinCapturada ?? pm.CantidadMin,
                               CantMaxCap     = pm.CantMaxCapturada ?? pm.CantidadMax,
                               TeoricoMinCap  = (pm.CantMinCapturada ?? pm.CantidadMin) * totalProd,
                               TeoricoMaxCap  = (pm.CantMaxCapturada ?? pm.CantidadMax) * totalProd,
                               ConvBOMValor   = pm.ConversionID.HasValue
                                                    ? "conv:" + pm.ConversionID.Value.ToString()
                                                    : (m.UnidadMedidaID.HasValue
                                                        ? "base:" + m.UnidadMedidaID.Value.ToString()
                                                        : ""),
                               ConsumoReal    = pm.CantidadMin * totalProd,
                               StockActual    = 0m
                           }).ToList();

                // Agregar stock actual por base seleccionada
                if (baseID > 0 && bom.Any())
                {
                    var matIDs = bom.Select(x => x.MaterialID).ToList();
                    var stocks = db.StockMateriales
                        .Where(s => s.BaseID == baseID && matIDs.Contains(s.MaterialID))
                        .Select(s => new { s.MaterialID, s.CantidadActual })
                        .ToDictionary(s => s.MaterialID, s => s.CantidadActual);

                    foreach (var item in bom)
                        item.StockActual = stocks.ContainsKey(item.MaterialID)
                                           ? stocks[item.MaterialID]
                                           : 0m;
                }

                // Poblar unidades de captura por material
                if (bom.Any())
                {
                    var matIDsList = bom.Select(x => x.MaterialID).ToList();
                    var conversiones = (from c in db.ConversionesMaterial
                                        where c.Activo && matIDsList.Contains(c.MaterialID)
                                        join u in db.UnidadesMedida
                                            on c.UnidadOrigenID equals u.UnidadMedidaID
                                        select new { c.MaterialID, c.ConversionID, c.Factor,
                                                     u.Nombre, u.Clave }).ToList();

                    var unidades = db.UnidadesMedida
                        .ToDictionary(u => u.UnidadMedidaID);

                    foreach (var item in bom)
                    {
                        item.UnidadesDisponibles = new List<OpcionUnidadVM>();

                        // Opción base
                        string baseTexto = item.Unidad ?? "";
                        if (item.UnidadBaseID.HasValue && unidades.ContainsKey(item.UnidadBaseID.Value))
                        {
                            var ub = unidades[item.UnidadBaseID.Value];
                            baseTexto = ub.Nombre + " (" + ub.Clave + ")";
                        }
                        item.UnidadesDisponibles.Add(new OpcionUnidadVM
                        {
                            Valor  = "base:" + (item.UnidadBaseID.HasValue
                                        ? item.UnidadBaseID.Value.ToString() : "0"),
                            Texto  = baseTexto + " — base",
                            Factor = 1m
                        });

                        // Opciones de conversión
                        foreach (var c in conversiones.Where(x => x.MaterialID == item.MaterialID))
                        {
                            item.UnidadesDisponibles.Add(new OpcionUnidadVM
                            {
                                Valor  = "conv:" + c.ConversionID,
                                Texto  = c.Nombre + " (" + c.Clave + ")  [×" + c.Factor.ToString("N6") + "]",
                                Factor = c.Factor
                            });
                        }

                        // Resolver texto abreviado de la unidad capturada en el BOM
                        if (!string.IsNullOrEmpty(item.ConvBOMValor) && item.ConvBOMValor.StartsWith("conv:"))
                        {
                            // La unidad BOM es una conversión — buscar su clave
                            int convIDVal;
                            if (int.TryParse(item.ConvBOMValor.Substring(5), out convIDVal))
                            {
                                var convItem = conversiones.FirstOrDefault(c =>
                                    c.MaterialID == item.MaterialID && c.ConversionID == convIDVal);
                                item.UnidadCapTexto = convItem != null
                                    ? convItem.Clave   // ej. "cj", "ml"
                                    : "";
                            }
                        }
                        else
                        {
                            // La unidad BOM es la base — usar la clave de la unidad base
                            if (item.UnidadBaseID.HasValue && unidades.ContainsKey(item.UnidadBaseID.Value))
                                item.UnidadCapTexto = unidades[item.UnidadBaseID.Value].Clave;
                        }

                        // Clave de unidad base (siempre, para la columna de stock)
                        if (item.UnidadBaseID.HasValue && unidades.ContainsKey(item.UnidadBaseID.Value))
                            item.UnidadBaseClave = unidades[item.UnidadBaseID.Value].Clave;

                        // Factor de la unidad pre-seleccionada en el BOM (InvariantCulture para JS)
                        // Se escribe en un <input hidden> de cada fila del repeater para que JS
                        // pueda leerlo con parseFloat() sin depender del texto de la opción ni
                        // de atributos data-* en <option> (que ASP.NET WebForms no siempre emite).
                        string valBOM = string.IsNullOrEmpty(item.ConvBOMValor)
                            ? (item.UnidadesDisponibles.Count > 0 ? item.UnidadesDisponibles[0].Valor : "")
                            : item.ConvBOMValor;
                        var opBOM = item.UnidadesDisponibles.FirstOrDefault(op => op.Valor == valBOM);
                        item.FactorBOMStr = (opBOM != null ? opBOM.Factor : 1m)
                            .ToString("0.##########", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                if (!bom.Any())
                {
                    pnlConsumos.Visible    = false;
                    lblSinConsumos.Text    = "Este producto no tiene materiales registrados en su BOM.";
                    lblSinConsumos.Visible = true;
                }
                else
                {
                    rptConsumos.DataSource = bom;
                    rptConsumos.DataBind();
                    pnlConsumos.Visible    = true;
                    lblSinConsumos.Visible = false;
                }
            }

            // ── Construir diccionario de factores para validación JS
            // (no depender de data-factor en <option> que ASP.NET no siempre renderiza)
            var factorDict = new Dictionary<string, string>();
            if (bom.Any())
            {
                foreach (var item in bom)
                {
                    foreach (var op in item.UnidadesDisponibles)
                    {
                        if (!factorDict.ContainsKey(op.Valor))
                            factorDict[op.Valor] = op.Factor.ToString("0.##########",
                                System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            var sbFactores = new System.Text.StringBuilder("window._factoresProd={");
            sbFactores.Append(string.Join(",", factorDict.Select(kv =>
                string.Format("\"{0}\":{1}", kv.Key, kv.Value))));
            sbFactores.Append("};");

            // Abrir modal e inicializar hints/validaciones de stock al cargar consumos.
            // La inicialización corre de forma inmediata (no en window.load) porque al llegar aquí
            // todos los elementos del DOM ya fueron parseados. Solo la apertura del modal espera
            // a window.load para que Bootstrap esté listo.
            ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                sbFactores.ToString() +
                // Inicializar hints e invocar validación de inmediato (DOM ya disponible)
                "(function(){" +
                "document.querySelectorAll('#tblConsumos tbody tr').forEach(function(row){" +
                "var hint=row.querySelector('.consumo-hint-unid');" +
                "var ui=row.querySelector('input[name=\"unidCapTxt\"]');" +
                "if(hint&&ui)hint.textContent=ui.value||'\u2014';" +
                "var inp=row.querySelector('.consumo-input');" +
                "if(inp)validarConsumoStock(inp);" +
                "});})();" +
                // Abrir modal cuando Bootstrap esté listo
                "window.addEventListener('load',function(){$('#modalRegistrar').modal('show');});", true);
        }

        // ══ ItemDataBound del repeater de consumos ═══════════════════════════
        protected void rptConsumos_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var vm  = e.Item.DataItem as ConsumoVM;
            var ddl = e.Item.FindControl("ddlUnidadConsumo") as System.Web.UI.WebControls.DropDownList;
            if (ddl == null || vm == null) return;

            ddl.Items.Clear();
            if (vm.UnidadesDisponibles != null)
            {
                foreach (var op in vm.UnidadesDisponibles)
                {
                    var li = new ListItem(op.Texto, op.Valor);
                    li.Attributes["data-factor"] = op.Factor.ToString("0.######",
                        System.Globalization.CultureInfo.InvariantCulture);
                    ddl.Items.Add(li);
                }
            }
            ddl.DataBind();

            // Pre-seleccionar la unidad con la que fue definido en el BOM
            if (!string.IsNullOrEmpty(vm.ConvBOMValor))
            {
                var opcion = ddl.Items.FindByValue(vm.ConvBOMValor);
                if (opcion != null)
                    ddl.SelectedValue = vm.ConvBOMValor;
            }

            // Embeber el factor de conversión de la opción pre-seleccionada directamente en el <select>
            // (data-convfactor en el WebControl <select> SÍ se renderiza en HTML; a diferencia de
            //  ListItem.Attributes en <option> que ASP.NET WebForms no siempre emite)
            string selectedValForFactor = string.IsNullOrEmpty(vm.ConvBOMValor)
                ? (ddl.Items.Count > 0 ? ddl.Items[0].Value : "")
                : vm.ConvBOMValor;
            var opForFactor = vm.UnidadesDisponibles != null
                ? vm.UnidadesDisponibles.FirstOrDefault(op => op.Valor == selectedValForFactor)
                : null;
            decimal factorParaDDL = opForFactor != null ? opForFactor.Factor : 1m;
            ddl.Attributes["data-convfactor"] = factorParaDDL.ToString("0.##########",
                System.Globalization.CultureInfo.InvariantCulture);

            // Al cambiar unidad: actualizar el hidden input con el factor correcto,
            // luego re-validar stock y actualizar el hint.
            // El hidden input .factor-bom-input es la fuente de verdad del factor en JS;
            // window._factoresProd es el diccionario inyectado por el servidor al cargar consumos.
            ddl.Attributes["onchange"] =
                "var r=this.closest('tr');" +
                "var sel=this.options[this.selectedIndex].value;" +
                "var fi=r.querySelector('input.factor-bom-input');" +
                "if(fi&&window._factoresProd&&window._factoresProd[sel]!=null)" +
                "  fi.value=window._factoresProd[sel];" +
                "validarConsumoStock(r.querySelector('.consumo-input'));" +
                "actualizarHint(this);";

            // Si solo hay una opción (unidad base, sin conversiones), mostrar como solo-lectura
            // IMPORTANTE: NO usar ddl.Enabled=false porque los campos disabled no se envían en el POST,
            // lo que desalinea el array de unidades con el array de materiales.
            if (ddl.Items.Count <= 1)
                ddl.CssClass = "form-control form-control-sm ddl-readonly";
        }

        // ══ Guardar producción ═══════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // ── Validaciones de campos requeridos
            if (string.IsNullOrEmpty(ddlBase.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione una base.", "modalRegistrar");
                return;
            }
            if (string.IsNullOrEmpty(txtFecha.Text))
            {
                SetMsg("warning", "Campo requerido", "Seleccione la fecha de producción.", "modalRegistrar");
                return;
            }
            if (string.IsNullOrEmpty(ddlTurno.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione el turno.", "modalRegistrar");
                return;
            }
            if (string.IsNullOrEmpty(ddlProducto.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione el producto.", "modalRegistrar");
                return;
            }

            int cantBuena = 0, cantRechazo = 0, metaDia = 0;
            if (!int.TryParse(txtCantBuena.Text, out cantBuena) || cantBuena < 0)
            {
                SetMsg("warning", "Campo inválido", "Ingrese una cantidad buena válida (≥ 0).", "modalRegistrar");
                return;
            }
            int.TryParse(txtCantRechazo.Text, out cantRechazo);
            int.TryParse(txtMetaDia.Text,     out metaDia);

            if (cantBuena + cantRechazo == 0)
            {
                SetMsg("warning", "Cantidades requeridas",
                    "Debe ingresar al menos una unidad buena o de rechazo.", "modalRegistrar");
                return;
            }

            int      baseID     = int.Parse(ddlBase.SelectedValue);
            DateTime fecha      = DateTime.Parse(txtFecha.Text);
            string   turno      = ddlTurno.SelectedValue;
            int      productoID = int.Parse(ddlProducto.SelectedValue);
            int      claveID    = Convert.ToInt32(Session["ClaveID"]);
            string   obs        = txtObservaciones.Text.Trim();
            int      totalProd  = cantBuena + cantRechazo;

            // ── Leer consumos del repeater (inputs HTML con name fijo)
            string[] arrMatIDs      = Request.Form.GetValues("matID")       ?? new string[0];
            string[] arrConsumos    = Request.Form.GetValues("consumoReal") ?? new string[0];
            string[] arrCantMins    = Request.Form.GetValues("cantMin")     ?? new string[0];
            string[] arrCantMaxs    = Request.Form.GetValues("cantMax")     ?? new string[0];
            string[] arrCantMinsCap = Request.Form.GetValues("cantMinCap")  ?? new string[0];
            string[] arrCantMaxsCap = Request.Form.GetValues("cantMaxCap")  ?? new string[0];
            string[] arrUnidCapTxt  = Request.Form.GetValues("unidCapTxt")  ?? new string[0];
            // Unidades de captura: server controls dentro del Repeater → leer via Request.Form.AllKeys
            string[] arrUnidades = Request.Form.AllKeys
                .Where(k => k != null && k.EndsWith("$ddlUnidadConsumo"))
                .OrderBy(k => k)
                .Select(k => Request.Form[k])
                .ToArray();

            var listaConsumos = new List<(int MatID, decimal CantCapturada, string UnidadVal,
                decimal CantMin, decimal CantMax, decimal CantMinCap, decimal CantMaxCap, string UnidCapTxt)>();
            for (int i = 0; i < arrMatIDs.Length; i++)
            {
                int matID;
                if (!int.TryParse(arrMatIDs[i], out matID)) continue;
                decimal cantCapturada = ParseDecimal(i < arrConsumos.Length    ? arrConsumos[i]    : "0");
                decimal cantMin       = ParseDecimal(i < arrCantMins.Length    ? arrCantMins[i]    : "0");
                decimal cantMax       = ParseDecimal(i < arrCantMaxs.Length    ? arrCantMaxs[i]    : "0");
                decimal cantMinCap    = ParseDecimal(i < arrCantMinsCap.Length ? arrCantMinsCap[i] : "0");
                decimal cantMaxCap    = ParseDecimal(i < arrCantMaxsCap.Length ? arrCantMaxsCap[i] : "0");
                string  unidCapTxt    = i < arrUnidCapTxt.Length  ? (arrUnidCapTxt[i]  ?? "") : "";
                string  unidadVal     = i < arrUnidades.Length     ? (arrUnidades[i]    ?? "") : "";
                listaConsumos.Add((matID, cantCapturada, unidadVal, cantMin, cantMax, cantMinCap, cantMaxCap, unidCapTxt));
            }

            // ── Pre-validar stock con la cantidad ya convertida a unidad base
            try
            {
                using (var dbVal = NuevoDb(false))
                {
                    // Cargar código y unidad base de cada material para el mensaje de error
                    var matIDsVal   = listaConsumos.Select(c => c.MatID).Distinct().ToList();
                    var matInfoVal  = dbVal.Materiales
                        .Where(m => matIDsVal.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, m.Codigo, m.UnidadMedidaID })
                        .ToList();
                    var unidClavesVal = dbVal.UnidadesMedida
                        .ToDictionary(u => u.UnidadMedidaID, u => u.Clave);

                    foreach (var c in listaConsumos.Where(c => c.CantCapturada > 0))
                    {
                        decimal factor   = AppHelper.ObtenerFactor(c.MatID, c.UnidadVal, dbVal);
                        decimal cantBase = c.CantCapturada * factor;

                        var infoMat  = matInfoVal.FirstOrDefault(x => x.MaterialID == c.MatID);
                        string uclave = infoMat?.UnidadMedidaID.HasValue == true &&
                                        unidClavesVal.ContainsKey(infoMat.UnidadMedidaID.Value)
                                        ? unidClavesVal[infoMat.UnidadMedidaID.Value] : "";

                        if (!ValidarStockSuficiente(dbVal, c.MatID, baseID, cantBase, uclave))
                            return;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                SetMsg("error", "Conversión inválida", ex.Message, "modalRegistrar");
                return;
            }

            // ── Guardar todo en una transacción explícita del DataContext
            try
            {
                using (var db = NuevoDb(true))
                {
                    db.Connection.Open();
                    using (var tx = db.Connection.BeginTransaction())
                    {
                        db.Transaction = tx;
                        try
                        {
                            // Lookup TipoMovimientoID para CONSUMO
                            int tipoConsumoID = db.TiposMovimiento
                                .Where(t => t.Clave == "CONSUMO")
                                .Select(t => t.TipoMovimientoID)
                                .First();

                            // Precios de materiales para el movimiento
                            var matIDsInt = listaConsumos.Select(c => c.MatID).ToList();
                            var precios   = db.Materiales
                                .Where(m => matIDsInt.Contains(m.MaterialID))
                                .Select(m => new { m.MaterialID, m.PrecioUnitario })
                                .ToDictionary(m => m.MaterialID, m => m.PrecioUnitario);

                            // 1. Insertar Produccion → necesitamos el ProduccionID
                            decimal precioVentaActual = db.Productos
                                .Where(p => p.ProductoID == productoID)
                                .Select(p => p.PrecioVenta)
                                .FirstOrDefault();

                            var prod = new Produccion
                            {
                                BaseID          = baseID,
                                Fecha           = fecha,
                                Turno           = turno,
                                ProductoID      = productoID,
                                CantidadBuena   = cantBuena,
                                CantidadRechazo = cantRechazo,
                                MetaDia         = metaDia,
                                Observaciones   = string.IsNullOrEmpty(obs) ? null : obs,
                                RegistradoPorID = claveID,
                                FechaRegistro   = AppHelper.Ahora,
                                PrecioVenta     = precioVentaActual
                            };
                            db.Produccion.InsertOnSubmit(prod);
                            db.SubmitChanges(); // ← primer commit para obtener ProduccionID

                            // 2. Consumos de materiales del BOM
                            foreach (var c in listaConsumos)
                            {
                                // Resolver conversión: cantBase = lo que afecta inventario
                                decimal factor        = AppHelper.ObtenerFactor(c.MatID, c.UnidadVal, db);
                                decimal cantBase      = c.CantCapturada * factor;
                                int?    unidCapturaID = ObtenerUnidadCapturaID(c.MatID, c.UnidadVal, db);

                                decimal tMin = c.CantMin * totalProd;
                                decimal tMax = c.CantMax * totalProd;
                                string notaConsumo = cantBase > tMax
                                    ? string.Format("Merma: real {0:N2} > m\u00e1x {1:N2}", cantBase, tMax)
                                    : cantBase < tMin
                                        ? string.Format("Bajo m\u00ednimo: real {0:N2} < m\u00edn {1:N2}", cantBase, tMin)
                                        : string.Format("Dentro de rango ({0:N2}\u2013{1:N2})", tMin, tMax);

                                // Teóricos en unidad capturada (para mostrar en grid con la unidad original)
                                decimal tMinCap    = c.CantMinCap * totalProd;
                                decimal tMaxCap    = c.CantMaxCap * totalProd;
                                string  unidCapStr = string.IsNullOrEmpty(c.UnidCapTxt) ? null : c.UnidCapTxt;

                                db.ConsumosProduccion.InsertOnSubmit(new ConsumosProduccion
                                {
                                    ProduccionID       = prod.ProduccionID,
                                    MaterialID         = c.MatID,
                                    CantidadReal       = cantBase,          // en unidad base
                                    CantidadTeoricaMin = tMin,
                                    CantidadTeoricaMax = tMax,
                                    EsMerma            = (cantBase > tMax),
                                    Notas              = notaConsumo,
                                    // Valores en la unidad de captura seleccionada en el BOM
                                    CantidadRealCap    = c.CantCapturada,
                                    CantidadTeoMinCap  = tMinCap,
                                    CantidadTeoMaxCap  = tMaxCap,
                                    UnidadClaveCap     = unidCapStr
                                });

                                if (cantBase > 0)
                                {
                                    decimal costoMat = precios.ContainsKey(c.MatID)
                                                       ? precios[c.MatID] : 0m;

                                    // Movimiento tipo CONSUMO vinculado a esta producción
                                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                                    {
                                        TipoMovimientoID  = tipoConsumoID,
                                        TipoItem          = "Material",
                                        MaterialID        = c.MatID,
                                        ProductoID        = null,
                                        BaseOrigenID      = baseID,
                                        BaseDestinoID     = null,
                                        Cantidad          = cantBase,            // en unidad base → afecta stock
                                        CantidadCapturada = c.CantCapturada,     // original capturado
                                        UnidadCapturaID   = unidCapturaID,       // null si se capturó en unidad base
                                        FactorAplicado    = factor,               // factor congelado (1 si no hubo conversión)
                                        Costo             = costoMat,
                                        ProduccionID      = prod.ProduccionID,
                                        EntregaID         = null,
                                        Observaciones     = string.Format("Producción #{0}", prod.ProduccionID),
                                        RegistradoPorID   = claveID,
                                        FechaMovimiento   = AppHelper.Ahora
                                    });

                                    // Descontar stock del material en la base (en unidad base)
                                    UpsertStockMaterial(db, c.MatID, baseID, -cantBase);
                                }
                            }

                            // 3. Acreditar el producto terminado en StockProductos
                            if (cantBuena > 0 || cantRechazo > 0)
                                UpsertStockProducto(db, productoID, baseID, cantBuena, cantRechazo);

                            db.SubmitChanges(); // ← commit de consumos, movimientos y stock
                            tx.Commit();        // ← confirma la transacción completa
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                LimpiarModal();
                SetMsg("success", "Producción registrada",
                    string.Format("Se registraron {0} unidades buenas y {1} de rechazo.",
                        cantBuena, cantRechazo));
                CargarDashboard();
                CargarGrid();
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al guardar",
                    "Ocurrió un error: " + ex.Message, "modalRegistrar");
            }
        }

        // ══ Hoja de fabricación — ventana imprimible ══════════════════════════
        protected void btnGenerarHoja_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlProductoHoja.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione un producto.");
                ClientScript.RegisterStartupScript(GetType(), "abrirHoja",
                    "window.addEventListener('load',function(){$('#modalHoja').modal('show');});", true);
                return;
            }

            int productoID = int.Parse(ddlProductoHoja.SelectedValue);
            int cantidad   = 0;
            if (!int.TryParse(txtCantidadHoja.Text, out cantidad) || cantidad <= 0)
                cantidad = 1;

            using (var db = NuevoDb(false))
            {
                var prod = db.Productos
                    .Where(p => p.ProductoID == productoID)
                    .Select(p => new { p.Descripcion, p.Codigo })
                    .FirstOrDefault();

                if (prod == null) return;

                var bom = (from pm in db.ProductoMateriales
                           where pm.ProductoID == productoID && pm.Activo
                           join m in db.Materiales on pm.MaterialID equals m.MaterialID
                           orderby m.Descripcion
                           select new
                           {
                               m.Codigo,
                               m.Descripcion,
                               m.Unidad,
                               UnitMin = pm.CantidadMin,
                               UnitMax = pm.CantidadMax,
                               NecMin  = pm.CantidadMin * cantidad,
                               NecMax  = pm.CantidadMax * cantidad
                           }).ToList();

                if (!bom.Any())
                {
                    SetMsg("info", "Sin materiales",
                        string.Format("'{0}' no tiene materiales registrados en su BOM.", prod.Descripcion));
                    ClientScript.RegisterStartupScript(GetType(), "abrirHoja",
                        "window.addEventListener('load',function(){$('#modalHoja').modal('show');});", true);
                    return;
                }

                // Construir HTML de la página imprimible
                var sb = new StringBuilder();
                sb.Append("<!DOCTYPE html><html lang='es'><head><meta charset='utf-8'/>");
                sb.Append("<title>Hoja de Fabricación — Grupo ANKHAL</title><style>");
                sb.Append("body{font-family:Arial,sans-serif;margin:30px;color:#1a1a1a}");
                sb.Append(".header{text-align:center;border-bottom:3px solid #003366;padding-bottom:12px;margin-bottom:20px}");
                sb.Append(".header img{height:60px;margin-bottom:6px}");
                sb.Append(".header h2{margin:0;color:#003366;font-size:1.3rem;letter-spacing:1px}");
                sb.Append(".header h3{margin:4px 0 0;font-size:1rem;color:#555}");
                sb.Append(".meta{display:flex;gap:30px;margin-bottom:18px;font-size:.9rem}");
                sb.Append(".meta label{font-weight:bold;color:#003366}");
                sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:30px}");
                sb.Append("thead th{background:#003366;color:#fff;padding:8px 10px;font-size:.88rem}");
                sb.Append("thead th.r{text-align:right}");
                sb.Append("tbody td{padding:7px 10px;border-bottom:1px solid #ddd;font-size:.88rem}");
                sb.Append("tbody td.r{text-align:right}");
                sb.Append("tbody tr:nth-child(even){background:#f5f7fa}");
                sb.Append(".firmas{display:flex;gap:40px;margin-top:50px}");
                sb.Append(".firma{flex:1;border-top:2px solid #555;padding-top:6px;text-align:center;font-size:.85rem;color:#555}");
                sb.Append(".btn-print{padding:8px 24px;background:#003366;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem;margin-right:8px}");
                sb.Append(".btn-close{padding:8px 24px;background:#6c757d;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem}");
                sb.Append("@media print{.no-print{display:none!important}}");
                sb.Append("</style></head><body>");

                sb.Append("<div class='header'>");
                sb.Append("<img src='img/ankhal.png' alt='ANKHAL' onerror=\"this.style.display='none'\"/>");
                sb.Append("<h2>HOJA DE FABRICACIÓN — GRUPO ANKHAL</h2>");
                sb.AppendFormat("<h3>{0} &nbsp;|&nbsp; Código: {1}</h3>", prod.Descripcion, prod.Codigo);
                sb.Append("</div>");

                sb.Append("<div class='meta'>");
                sb.AppendFormat("<div><label>Cantidad a fabricar:</label> <strong>{0:N0}</strong></div>", cantidad);
                sb.AppendFormat("<div><label>Fecha:</label> <strong>{0:dd/MM/yyyy}</strong></div>", AppHelper.Hoy);
                sb.Append("<div><label>Responsable:</label> ________________________</div>");
                sb.Append("</div>");

                sb.Append("<table><thead><tr>");
                sb.Append("<th>Código</th><th>Material</th><th>Unidad</th>");
                sb.Append("<th class='r'>Cant/Ud Mín</th><th class='r'>Cant/Ud Máx</th>");
                sb.Append("<th class='r'>Total Mín</th><th class='r'>Total Máx</th>");
                sb.Append("</tr></thead><tbody>");
                foreach (var item in bom)
                {
                    sb.AppendFormat(
                        "<tr><td>{0}</td><td>{1}</td><td>{2}</td>" +
                        "<td class='r'>{3:N4}</td><td class='r'>{4:N4}</td>" +
                        "<td class='r'><strong>{5:N4}</strong></td><td class='r'><strong>{6:N4}</strong></td></tr>",
                        item.Codigo, item.Descripcion, item.Unidad,
                        item.UnitMin, item.UnitMax, item.NecMin, item.NecMax);
                }
                sb.Append("</tbody></table>");

                sb.Append("<div class='firmas'>");
                sb.Append("<div class='firma'>Elaboró</div>");
                sb.Append("<div class='firma'>Supervisó</div>");
                sb.Append("<div class='firma'>Recibió materiales</div>");
                sb.Append("</div>");

                sb.Append("<div class='no-print' style='text-align:center;margin-top:24px'>");
                sb.Append("<button class='btn-print' onclick='window.print()'>🖨 Imprimir</button>");
                sb.Append("<button class='btn-close' onclick='window.close()'>Cerrar</button>");
                sb.Append("</div>");

                sb.Append("</body></html>");

                // Abrir en ventana nueva
                string htmlJson = _json.Serialize(sb.ToString());
                string script   = string.Format(
                    "(function(){{var w=window.open('','_blank','width=860,height=700,scrollbars=yes');" +
                    "w.document.write({0});w.document.close();w.focus();}})();", htmlJson);

                ClientScript.RegisterStartupScript(GetType(), "abrirHoja", script, true);
            }
        }

        // ══ Helpers de stock ═════════════════════════════════════════════════
        private void UpsertStockMaterial(InventarioAnkhalDBDataContext db,
            int materialID, int baseID, decimal delta)
        {
            var s = db.StockMateriales
                .FirstOrDefault(x => x.BaseID == baseID && x.MaterialID == materialID);

            if (s == null)
            {
                db.StockMateriales.InsertOnSubmit(new StockMateriales
                {
                    BaseID           = baseID,
                    MaterialID       = materialID,
                    CantidadActual   = delta,
                    FechaUltimaModif = AppHelper.Ahora
                });
            }
            else
            {
                // UPDLOCK bloquea la fila hasta que la transacción termine,
                // evitando que dos peticiones concurrentes lean el mismo valor y ambas descuenten.
                decimal actual = db.ExecuteQuery<decimal>(
                    "SELECT CantidadActual FROM dbo.StockMateriales WITH (UPDLOCK, HOLDLOCK) WHERE BaseID={0} AND MaterialID={1}",
                    baseID, materialID).FirstOrDefault();

                if (delta < 0 && actual + delta < 0)
                    throw new InvalidOperationException(
                        string.Format("Stock insuficiente para material #{0}: disponible {1:N4}, requerido {2:N4}.",
                            materialID, actual, -delta));

                s.CantidadActual   += delta;
                s.FechaUltimaModif  = AppHelper.Ahora;
            }
        }

        private void UpsertStockProducto(InventarioAnkhalDBDataContext db,
            int productoID, int baseID, int cantBuena, int cantRechazo)
        {
            var s = db.StockProductos
                .FirstOrDefault(x => x.BaseID == baseID && x.ProductoID == productoID);

            if (s == null)
            {
                db.StockProductos.InsertOnSubmit(new StockProductos
                {
                    BaseID           = baseID,
                    ProductoID       = productoID,
                    CantidadBuenas   = cantBuena   > 0 ? cantBuena   : 0,
                    CantidadRechazo  = cantRechazo > 0 ? cantRechazo : 0,
                    FechaUltimaModif = AppHelper.Ahora
                });
            }
            else
            {
                s.CantidadBuenas  += cantBuena;
                s.CantidadRechazo += cantRechazo;
                s.FechaUltimaModif = AppHelper.Ahora;
            }
        }

        private bool ValidarStockSuficiente(InventarioAnkhalDBDataContext db,
            int materialID, int baseID, decimal cantRequerida, string unidClave = "")
        {
            decimal actual = db.StockMateriales
                .Where(x => x.BaseID == baseID && x.MaterialID == materialID)
                .Select(x => x.CantidadActual)
                .FirstOrDefault();

            if (actual >= cantRequerida) return true;

            var mat = db.Materiales
                .Where(m => m.MaterialID == materialID)
                .Select(m => new { m.Codigo, m.Descripcion })
                .FirstOrDefault();

            string referencia = mat != null
                ? string.Format("[{0}] {1}", mat.Codigo, mat.Descripcion)
                : "Material #" + materialID;
            string u = string.IsNullOrEmpty(unidClave) ? "" : " " + unidClave;

            SetMsg("warning", "Stock insuficiente",
                string.Format("{0}: stock actual {1:N2}{2}, se requieren {3:N2}{2}. " +
                              "Registre una entrada primero.",
                    referencia, actual, u, cantRequerida),
                "modalRegistrar");
            return false;
        }

        // ══ Helper badge turno (usado desde el TemplateField del GridView) ════
        public string GetBadgeTurno(object turno)
        {
            switch ((turno ?? "").ToString().ToUpper())
            {
                case "MAÑANA": return "badge badge-manana";
                case "TARDE":  return "badge badge-tarde";
                case "NOCHE":  return "badge badge-noche";
                case "UNICO":  return "badge badge-unico";
                default:       return "badge badge-secondary";
            }
        }

        /// <summary>
        /// Retorna el UnidadMedidaID de la unidad en que se capturó.
        /// null si se capturó directamente en la unidad base del material.
        /// </summary>
        private int? ObtenerUnidadCapturaID(int matID, string selectedVal,
            InventarioAnkhalDBDataContext db)
        {
            if (string.IsNullOrEmpty(selectedVal) || selectedVal.StartsWith("base:"))
                return null;   // capturado en unidad base → no hay "unidad de captura alternativa"

            if (selectedVal.StartsWith("conv:"))
            {
                int convID;
                if (!int.TryParse(selectedVal.Substring(5), out convID)) return null;

                var conv = db.ConversionesMaterial
                    .FirstOrDefault(c => c.ConversionID == convID &&
                                         c.MaterialID   == matID  &&
                                         c.Activo);
                return conv?.UnidadOrigenID;
            }

            return null;
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private static decimal ParseDecimal(string s)
        {
            decimal result;
            return decimal.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out result) ? result : 0m;
        }

        private void LimpiarModal()
        {
            ddlBase.SelectedIndex         = 0;
            txtFecha.Text                 = AppHelper.Hoy.ToString("yyyy-MM-dd");
            ddlTurno.SelectedIndex        = 0;
            ddlProducto.SelectedIndex     = 0;
            hdnProductoSeleccionado.Value = "";
            txtMetaDia.Text               = "";
            txtCantBuena.Text             = "";
            txtCantRechazo.Text           = "";
            txtObservaciones.Text         = "";
            pnlConsumos.Visible           = false;
            lblSinConsumos.Text           = "Seleccione un producto para cargar los consumos de materiales.";
            lblSinConsumos.Visible        = true;
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }
    }
}
