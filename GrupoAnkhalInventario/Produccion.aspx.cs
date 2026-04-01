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
            public string   ProductoNombre  { get; set; }
            public int      CantidadBuena   { get; set; }
            public int      CantidadRechazo { get; set; }
            public int      Total           { get; set; }
            public decimal  MetaBase        { get; set; }   // MetaDiaria de la base ($)
            public int      CumplPct        { get; set; }
            public decimal  Valor           { get; set; }
            public string   RegistradoPor   { get; set; }
            public List<ConsumoDetalleVM> Consumos { get; set; } = new List<ConsumoDetalleVM>();
        }

        public class ConsumoDetalleVM
        {
            public string  MaterialCodigo { get; set; }
            public string  MaterialNombre { get; set; }
            public string  Unidad         { get; set; }
            public decimal TeoMin         { get; set; }
            public decimal TeoMax         { get; set; }
            public decimal Real           { get; set; }
            public decimal Excedente      { get; set; }
            public bool    EsMerma        { get; set; }
        }


        public class ConsumoVM
        {
            public int     MaterialID     { get; set; }
            public string  MaterialCodigo { get; set; }
            public string  MaterialNombre { get; set; }
            public string  Unidad         { get; set; }
            public decimal CantidadMin    { get; set; }
            public decimal CantidadMax    { get; set; }
            public decimal TeoricoMin     { get; set; }
            public decimal TeoricoMax     { get; set; }
            public decimal ConsumoReal    { get; set; }
            public decimal StockActual    { get; set; }
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
                    .OrderBy(p => p.Descripcion)
                    .Select(p => new { p.ProductoID, p.Descripcion })
                    .ToList();

                ddlProducto.Items.Clear();
                ddlProducto.Items.Add(new ListItem("-- Seleccione --", ""));
                ddlProductoHoja.Items.Clear();
                ddlProductoHoja.Items.Add(new ListItem("-- Seleccione --", ""));
                ddlFiltrProducto.Items.Clear();
                ddlFiltrProducto.Items.Add(new ListItem("-- Todos --", ""));
                foreach (var p in productos)
                {
                    ddlProducto.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));
                    ddlProductoHoja.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));
                    ddlFiltrProducto.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));
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

                var data = (from p  in q
                            join pr in db.Productos on p.ProductoID equals pr.ProductoID
                            select new
                            {
                                p.CantidadBuena,
                                p.CantidadRechazo,
                                Valor = p.CantidadBuena * pr.PrecioVenta
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
                               ProductoNombre  = pr.Descripcion,
                               p.CantidadBuena,
                               p.CantidadRechazo,
                               PrecioVenta     = pr.PrecioVenta,
                               p.RegistradoPorID
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
                            ProductoNombre  = r.ProductoNombre,
                            CantidadBuena   = r.CantidadBuena,
                            CantidadRechazo = r.CantidadRechazo,
                            Total           = tot2,
                            MetaBase        = r.MetaDiaria,
                            CumplPct        = pct,
                            Valor           = valorReg,
                            RegistradoPor   = nombresUsuario.ContainsKey(r.RegistradoPorID)
                                              ? nombresUsuario[r.RegistradoPorID]
                                              : r.RegistradoPorID.ToString()
                        };
                    }).ToList();

                // ── Cargar detalle de consumos para los IDs de esta página
                var consumosRaw = (from cp in db.ConsumosProduccion
                                   where ids.Contains(cp.ProduccionID)
                                   join m in db.Materiales on cp.MaterialID equals m.MaterialID
                                   select new
                                   {
                                       cp.ProduccionID,
                                       m.Codigo,
                                       m.Descripcion,
                                       m.Unidad,
                                       cp.CantidadTeoricaMin,
                                       cp.CantidadTeoricaMax,
                                       cp.CantidadReal,
                                       cp.EsMerma
                                   }).ToList();

                foreach (var vm in pagina)
                {
                    vm.Consumos = consumosRaw
                        .Where(c => c.ProduccionID == vm.ProduccionID)
                        .Select(c => new ConsumoDetalleVM
                        {
                            MaterialCodigo = c.Codigo,
                            MaterialNombre = c.Descripcion,
                            Unidad         = c.Unidad ?? "",
                            TeoMin         = c.CantidadTeoricaMin,
                            TeoMax         = c.CantidadTeoricaMax,
                            Real           = c.CantidadReal,
                            Excedente      = c.CantidadReal > c.CantidadTeoricaMax
                                             ? c.CantidadReal - c.CantidadTeoricaMax : 0m,
                            EsMerma        = c.EsMerma
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

            using (var db = NuevoDb(false))
            {
                var bom = (from pm in db.ProductoMateriales
                           where pm.ProductoID == productoID
                           join m in db.Materiales on pm.MaterialID equals m.MaterialID
                           select new ConsumoVM
                           {
                               MaterialID     = m.MaterialID,
                               MaterialCodigo = m.Codigo,
                               MaterialNombre = m.Descripcion,
                               Unidad         = m.Unidad,
                               CantidadMin    = pm.CantidadMin,
                               CantidadMax    = pm.CantidadMax,
                               TeoricoMin     = pm.CantidadMin * totalProd,
                               TeoricoMax     = pm.CantidadMax * totalProd,
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

            ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                "window.addEventListener('load',function(){$('#modalRegistrar').modal('show');});", true);
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
            string[] arrMatIDs   = Request.Form.GetValues("matID")       ?? new string[0];
            string[] arrConsumos = Request.Form.GetValues("consumoReal") ?? new string[0];
            string[] arrCantMins = Request.Form.GetValues("cantMin")     ?? new string[0];
            string[] arrCantMaxs = Request.Form.GetValues("cantMax")     ?? new string[0];

            var listaConsumos = new List<(int MatID, decimal CantReal, decimal CantMin, decimal CantMax)>();
            for (int i = 0; i < arrMatIDs.Length; i++)
            {
                int matID;
                if (!int.TryParse(arrMatIDs[i], out matID)) continue;
                decimal cantReal = ParseDecimal(i < arrConsumos.Length ? arrConsumos[i] : "0");
                decimal cantMin  = ParseDecimal(i < arrCantMins.Length ? arrCantMins[i]  : "0");
                decimal cantMax  = ParseDecimal(i < arrCantMaxs.Length ? arrCantMaxs[i]  : "0");
                listaConsumos.Add((matID, cantReal, cantMin, cantMax));
            }

            // ── Pre-validar stock de materiales antes de tocar la BD
            using (var dbVal = NuevoDb(false))
            {
                foreach (var c in listaConsumos.Where(c => c.CantReal > 0))
                {
                    if (!ValidarStockSuficiente(dbVal, c.MatID, baseID, c.CantReal))
                        return;
                }
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
                                FechaRegistro   = AppHelper.Ahora
                            };
                            db.Produccion.InsertOnSubmit(prod);
                            db.SubmitChanges(); // ← primer commit para obtener ProduccionID

                            // 2. Consumos de materiales del BOM
                            foreach (var c in listaConsumos)
                            {
                                decimal tMin = c.CantMin * totalProd;
                                decimal tMax = c.CantMax * totalProd;
                                string notaConsumo = c.CantReal > tMax
                                    ? string.Format("Merma: real {0:N2} > m\u00e1x {1:N2}", c.CantReal, tMax)
                                    : c.CantReal < tMin
                                        ? string.Format("Bajo m\u00ednimo: real {0:N2} < m\u00edn {1:N2}", c.CantReal, tMin)
                                        : string.Format("Dentro de rango ({0:N2}\u2013{1:N2})", tMin, tMax);

                                db.ConsumosProduccion.InsertOnSubmit(new ConsumosProduccion
                                {
                                    ProduccionID       = prod.ProduccionID,
                                    MaterialID         = c.MatID,
                                    CantidadReal       = c.CantReal,
                                    CantidadTeoricaMin = tMin,
                                    CantidadTeoricaMax = tMax,
                                    EsMerma            = (c.CantReal > tMax),
                                    Notas              = notaConsumo
                                });

                                if (c.CantReal > 0)
                                {
                                    decimal costoMat = precios.ContainsKey(c.MatID)
                                                       ? precios[c.MatID] : 0m;

                                    // Movimiento tipo CONSUMO vinculado a esta producción
                                    // Nota: usamos Modelo.Movimientos para evitar ambigüedad
                                    // con la clase de página GrupoAnkhalInventario.Movimientos
                                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                                    {
                                        TipoMovimientoID = tipoConsumoID,
                                        TipoItem         = "Material",
                                        MaterialID       = c.MatID,
                                        ProductoID       = null,
                                        BaseOrigenID     = baseID,
                                        BaseDestinoID    = null,
                                        Cantidad         = c.CantReal,
                                        Costo            = costoMat,
                                        ProduccionID     = prod.ProduccionID,
                                        EntregaID        = null,
                                        Observaciones    = string.Format("Producción #{0}", prod.ProduccionID),
                                        RegistradoPorID  = claveID,
                                        FechaMovimiento  = AppHelper.Ahora
                                    });

                                    // Descontar stock del material en la base
                                    UpsertStockMaterial(db, c.MatID, baseID, -c.CantReal);
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
                           where pm.ProductoID == productoID
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
            int materialID, int baseID, decimal cantRequerida)
        {
            decimal actual = db.StockMateriales
                .Where(x => x.BaseID == baseID && x.MaterialID == materialID)
                .Select(x => x.CantidadActual)
                .FirstOrDefault();

            if (actual >= cantRequerida) return true;

            string nombre = db.Materiales
                .Where(m => m.MaterialID == materialID)
                .Select(m => m.Descripcion)
                .FirstOrDefault() ?? "Material";

            SetMsg("warning", "Stock insuficiente",
                string.Format("{0}: stock actual {1:N2}, se requieren {2:N2}. " +
                              "Registre una entrada primero.",
                    nombre, actual, cantRequerida),
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
