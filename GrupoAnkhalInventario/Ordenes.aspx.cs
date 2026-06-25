using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario
{
    public partial class Ordenes : System.Web.UI.Page
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
        public class OrdenVM
        {
            public int      OrdenID           { get; set; }
            public string   Folio             { get; set; }
            public DateTime FechaOrden        { get; set; }
            public string   BaseNombre        { get; set; }
            public string   ClienteNombre     { get; set; }
            public int      NumItems          { get; set; }
            public int      ItemsCompletados  { get; set; }
            public decimal  PorcentajeAvance  { get; set; }
            public decimal  TotalValor        { get; set; }
            public string   Estado            { get; set; }
        }

        private class UnidadOpcion
        {
            public string Valor  { get; set; }  // "base:{id}" o "conv:{id}"
            public string Texto  { get; set; }
            public double Factor { get; set; }
        }

        private class DetalleOrdenItemJson
        {
            public int     DetalleOrdenID    { get; set; }
            public string  TipoItem          { get; set; }
            public int     ItemID            { get; set; }
            public string  NombreItem        { get; set; }
            public decimal CantidadPedida    { get; set; }
            public decimal CantidadEntregada { get; set; }
            public decimal CantidadPendiente { get; set; }
            public decimal PrecioUnitario    { get; set; }
            public string  Estado            { get; set; }
            public string  Descripcion       { get; set; }   // cantidad original capturada (ej. "6 Kg/s")
            // Unidades disponibles para captura (solo materiales)
            public List<UnidadOpcion> Unidades      { get; set; }
            public string             UnidadBaseNombre { get; set; }
        }

        private class EntregaVinculadaJson
        {
            public int     EntregaID      { get; set; }
            public string  Folio          { get; set; }
            public string  Fecha          { get; set; }
            public string  NumeroFactura  { get; set; }
            public decimal Total          { get; set; }
            public string  Estado         { get; set; }
        }

        private class OrdenDetalleJson
        {
            public int    OrdenID   { get; set; }
            public string Folio     { get; set; }
            public string Fecha     { get; set; }
            public string Base      { get; set; }
            public string Cliente   { get; set; }
            public string Estado    { get; set; }
            public string Obs       { get; set; }
            public List<DetalleOrdenItemJson>  Items    { get; set; }
            public List<EntregaVinculadaJson>  Entregas { get; set; }
        }

        private class OrdenPartidaJson
        {
            public int    OrdenID      { get; set; }
            public string OrdenFolio   { get; set; }
            public string ClienteNombre { get; set; }
            public string BaseNombre   { get; set; }
            public string EstadoOrden  { get; set; }
            public int    BaseOrigenID { get; set; }
            public bool   EsCredito    { get; set; }
            public List<DetalleOrdenItemJson> Items { get; set; }
        }

        private class ItemPartidaModel
        {
            public int     DetalleOrdenID       { get; set; }
            public string  TipoItem             { get; set; }
            public int     ItemID               { get; set; }
            public decimal CantidadAEntregar    { get; set; }  // en unidad capturada
            public decimal PrecioUnitario       { get; set; }
            public string  UnidadVal            { get; set; }  // "base:{id}" o "conv:{id}"
            public decimal Factor               { get; set; } = 1m;
            public int?    UnidadCapturaIDDirecto { get; set; }
        }

        // ══ Page_Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarCatalogos();
                string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
                txtFiltrDesde.Text = hoy;
                txtFiltrHasta.Text = hoy;
                txtNuevoFecha.Text = hoy;
                hdnItemsJson.Value = "[]";
                CargarDashboard();
                CargarGrid();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvOrdenes.VirtualItemCount = (int)ViewState["TotalRegistros"];
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
                ddlNuevoBase.Items.Clear();
                ddlNuevoBase.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in bases)
                {
                    ddlFiltrBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));
                    ddlNuevoBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));
                }

                var clientes = db.Clientes
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Nombre)
                    .Select(c => new { c.ClienteID, c.Nombre, c.DiasCredito })
                    .ToList();

                ddlNuevoCliente.Items.Clear();
                ddlNuevoCliente.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var c in clientes)
                {
                    var item = new ListItem(c.Nombre, c.ClienteID.ToString());
                    item.Attributes["data-dias"] = (c.DiasCredito ?? 0).ToString();
                    ddlNuevoCliente.Items.Add(item);
                }
            }
        }

        // ══ Filtros ══════════════════════════════════════════════════════════
        private IQueryable<Modelo.Ordenes> AplicarFiltros(IQueryable<Modelo.Ordenes> q)
        {
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            if (basesUsuario != null)
                q = q.Where(o => basesUsuario.Contains(o.BaseOrigenID));

            if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
            {
                int id = int.Parse(ddlFiltrBase.SelectedValue);
                q = q.Where(o => o.BaseOrigenID == id);
            }
            if (!string.IsNullOrEmpty(ddlFiltrEstado.SelectedValue))
                q = q.Where(o => o.Estado == ddlFiltrEstado.SelectedValue);

            if (!string.IsNullOrEmpty(txtFiltrCliente.Text.Trim()))
            {
                string cli = txtFiltrCliente.Text.Trim().ToLower();
                q = q.Where(o => o.Clientes.Nombre != null && o.Clientes.Nombre.ToLower().Contains(cli));
            }
            if (!string.IsNullOrEmpty(txtFiltrFolio.Text.Trim()))
            {
                string folio = txtFiltrFolio.Text.Trim().ToUpper();
                q = q.Where(o => o.Folio.Contains(folio));
            }
            if (!string.IsNullOrEmpty(txtFiltrDesde.Text))
            {
                DateTime d = DateTime.Parse(txtFiltrDesde.Text);
                q = q.Where(o => o.FechaOrden >= d);
            }
            if (!string.IsNullOrEmpty(txtFiltrHasta.Text))
            {
                DateTime h = DateTime.Parse(txtFiltrHasta.Text);
                q = q.Where(o => o.FechaOrden <= h);
            }
            return q;
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                var q    = AplicarFiltros(db.Ordenes.AsQueryable());
                var data = q.Select(o => new { o.Estado, o.OrdenID }).ToList();

                lblTotal.Text      = data.Count.ToString("N0");
                lblAbiertas.Text   = data.Count(x => x.Estado == "ABIERTA").ToString("N0");
                lblParciales.Text  = data.Count(x => x.Estado == "PARCIAL").ToString("N0");
                lblCompletadas.Text= data.Count(x => x.Estado == "COMPLETADA").ToString("N0");
                lblCanceladas.Text = data.Count(x => x.Estado == "CANCELADA").ToString("N0");

                // Valor total: sum de DetalleEntregas de entregas ENTREGADA vinculadas a órdenes del período
                var ordenIDs = data.Select(x => x.OrdenID).ToList();
                decimal valorTotal = 0;
                if (ordenIDs.Any())
                {
                    valorTotal = (from e  in db.Entregas
                                  where e.OrdenID != null && ordenIDs.Contains(e.OrdenID.Value)
                                        && e.Estado == "ENTREGADA"
                                  join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                  select (decimal?)(de.Cantidad * de.PrecioUnitario))
                                 .Sum() ?? 0m;
                }
                lblValorTotal.Text = valorTotal.ToString("$#,##0.00");
            }
        }

        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                var q     = AplicarFiltros(db.Ordenes.AsQueryable());
                int total = q.Count();
                int pageIdx = gvOrdenes.PageIndex;
                int pageSz  = gvOrdenes.PageSize;
                gvOrdenes.VirtualItemCount = total;
                ViewState["TotalRegistros"] = total;

                lblResultados.Text = total == 0
                    ? "Sin órdenes para los filtros aplicados."
                    : string.Format("{0} orden(es) encontrada(s).", total);

                if (total == 0)
                {
                    gvOrdenes.DataSource = new List<OrdenVM>();
                    gvOrdenes.DataBind();
                    return;
                }

                var ids = q
                    .OrderByDescending(o => o.FechaOrden)
                    .ThenByDescending(o => o.OrdenID)
                    .Select(o => o.OrdenID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                var raw = (from o in db.Ordenes
                           where ids.Contains(o.OrdenID)
                           join b in db.Bases   on o.BaseOrigenID equals b.BaseID
                           join c in db.Clientes on o.ClienteID    equals c.ClienteID
                           select new
                           {
                               o.OrdenID, o.Folio, o.FechaOrden,
                               BaseNombre    = b.Nombre,
                               ClienteNombre = c.Nombre,
                               o.Estado
                           }).ToList();

                // Progreso: DetalleOrdenes por OrdenID
                var detalles = db.DetalleOrdenes
                    .Where(d => ids.Contains(d.OrdenID))
                    .Select(d => new { d.OrdenID, d.CantidadPedida, d.CantidadEntregada, d.Estado })
                    .ToList();

                // Valor entregado por Orden (entregas ENTREGADA)
                var entregasValor = (from e  in db.Entregas
                                     where e.OrdenID != null && ids.Contains(e.OrdenID.Value)
                                           && e.Estado == "ENTREGADA"
                                     join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                     select new { OrdenID = e.OrdenID.Value, Val = de.Cantidad * de.PrecioUnitario })
                                    .ToList();

                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.OrdenID == id))
                    .Where(r => r != null)
                    .Select(r =>
                    {
                        var dets = detalles.Where(d => d.OrdenID == r.OrdenID).ToList();
                        int numItems = dets.Count;
                        int completados = dets.Count(d => d.Estado == "COMPLETADO");
                        decimal pct = (numItems > 0 && dets.Sum(d => d.CantidadPedida) > 0)
                            ? Math.Min(100m, dets.Sum(d => d.CantidadEntregada) / dets.Sum(d => d.CantidadPedida) * 100m)
                            : 0m;
                        decimal val = entregasValor.Where(x => x.OrdenID == r.OrdenID).Sum(x => x.Val);

                        return new OrdenVM
                        {
                            OrdenID          = r.OrdenID,
                            Folio            = r.Folio,
                            FechaOrden       = r.FechaOrden,
                            BaseNombre       = r.BaseNombre,
                            ClienteNombre    = r.ClienteNombre,
                            NumItems         = numItems,
                            ItemsCompletados = completados,
                            PorcentajeAvance = Math.Round(pct, 1),
                            TotalValor       = val,
                            Estado           = r.Estado
                        };
                    }).ToList();

                gvOrdenes.DataSource = pagina;
                gvOrdenes.DataBind();
            }
        }

        // ══ Helpers de columnas del grid ══════════════════════════════════════
        public string GetBadgeEstadoOrden(object estado)
        {
            switch ((estado ?? "").ToString())
            {
                case "ABIERTA":    return "badge badge-abierta";
                case "PARCIAL":    return "badge badge-parcial";
                case "COMPLETADA": return "badge badge-completada";
                case "CANCELADA":  return "badge badge-cancelada";
                default:           return "badge badge-secondary";
            }
        }

        public string MostrarBtnRegistrarEntrega(string estado, string ordenID)
        {
            if (estado == "ABIERTA" || estado == "PARCIAL")
                return string.Format(
                    "<button type='button' class='btn btn-xs btn-warning' " +
                    "onclick=\"registrarEntrega({0})\"><i class='fas fa-truck'></i> Entregar</button>",
                    ordenID);
            return "";
        }

        public string MostrarBtnCancelarOrden(string estado, string ordenID)
        {
            if (estado == "CANCELADA" || estado == "COMPLETADA") return "";
            return string.Format(
                "<button type='button' class='btn btn-xs btn-danger' " +
                "onclick=\"cancelarOrden({0})\"><i class='fas fa-ban'></i></button>",
                ordenID);
        }

        // ══ Eventos filtros / paginación ══════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvOrdenes.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltrBase.SelectedIndex   = 0;
            ddlFiltrEstado.SelectedIndex = 0;
            txtFiltrCliente.Text         = "";
            txtFiltrFolio.Text           = "";
            string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
            txtFiltrDesde.Text = hoy;
            txtFiltrHasta.Text = hoy;
            gvOrdenes.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void gvOrdenes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvOrdenes.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // ══ Nueva Orden ═══════════════════════════════════════════════════════
        protected void btnNuevoOrden_Click(object sender, EventArgs e)
        {
            LimpiarModalNuevo();
            using (var db = NuevoDb(false))
            {
                DateTime hoy   = AppHelper.Hoy;
                int count      = db.Ordenes.Count(o => o.FechaOrden >= hoy && o.FechaOrden < hoy.AddDays(1));
                txtNuevoFolio.Text = string.Format("ORD-{0}-{1:D3}", hoy.ToString("yyyyMMdd"), count + 1);
            }
            ClientScript.RegisterStartupScript(GetType(), "abrirModalOrden",
                "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
        }

        protected void btnGuardarOrden_Click(object sender, EventArgs e)
        {
            var itemsRaw = LeerItemsModalNuevo();
            if (itemsRaw == null) return;

            int baseID, clienteID;
            DateTime fecha;
            if (!ValidarCamposModalNuevo(out baseID, out clienteID, out fecha)) return;

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
                            DateTime hoy = fecha.Date;
                            int count = db.ExecuteQuery<int>(
                                @"SELECT COUNT(*) FROM dbo.Ordenes WITH (UPDLOCK, HOLDLOCK)
                                  WHERE FechaOrden >= {0} AND FechaOrden < {1}",
                                hoy, hoy.AddDays(1)).First();
                            string folio = string.Format("ORD-{0}-{1:D3}", fecha.ToString("yyyyMMdd"), count + 1);

                            var orden = new Modelo.Ordenes
                            {
                                Folio           = folio,
                                FechaOrden      = fecha.Date,
                                ClienteID       = clienteID,
                                BaseOrigenID    = baseID,
                                Estado          = "ABIERTA",
                                Observaciones   = string.IsNullOrWhiteSpace(txtNuevoObservaciones.Text)
                                                    ? null : txtNuevoObservaciones.Text.Trim(),
                                EsCredito       = chkEsCreditoOrden.Checked,
                                RegistradoPorID = Convert.ToInt32(Session["ClaveID"]),
                                FechaRegistro   = AppHelper.Ahora
                            };
                            db.Ordenes.InsertOnSubmit(orden);
                            db.SubmitChanges(); // obtener OrdenID

                            foreach (var it in itemsRaw)
                            {
                                // Para materiales, almacenar en unidad base aplicando el factor
                                decimal cantBase = (it.TipoItem == "MATERIAL" && it.Factor > 0m)
                                    ? it.Cantidad * it.Factor
                                    : it.Cantidad;

                                // Descripción con la unidad original (para mostrar en historial)
                                string descripcion = null;
                                if (it.TipoItem == "MATERIAL" && it.Factor != 1m && !string.IsNullOrEmpty(it.UnidadTexto))
                                    descripcion = string.Format("{0:0.##} {1}", it.Cantidad, it.UnidadTexto);

                                var det = new Modelo.DetalleOrdenes
                                {
                                    OrdenID          = orden.OrdenID,
                                    TipoItem         = it.TipoItem,
                                    CantidadPedida   = cantBase,
                                    CantidadEntregada= 0m,
                                    PrecioUnitario   = it.PrecioUnitario,
                                    Descripcion      = descripcion,
                                    Estado           = "PENDIENTE",
                                    FechaRegistro    = AppHelper.Ahora
                                };
                                if (it.TipoItem == "PRODUCTO") det.ProductoID = it.ItemID;
                                else                           det.MaterialID  = it.ItemID;
                                db.DetalleOrdenes.InsertOnSubmit(det);
                            }
                            db.SubmitChanges();
                            tx.Commit();

                            LimpiarModalNuevo();
                            SetMsg("success", "Orden creada",
                                string.Format("Orden {0} creada correctamente con {1} ítem(s).", folio, itemsRaw.Count));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al guardar", "Ocurrió un error: " + ex.Message, "modalNuevo");
            }
        }

        // ══ Acciones del grid ══════════════════════════════════════════════════
        protected void btnProcesarAccion_Click(object sender, EventArgs e)
        {
            string accion  = hdnAccion.Value;
            hdnAccion.Value = "";

            switch (accion)
            {
                case "detalle":
                {
                    int id;
                    if (int.TryParse(hdnOrdenIDAccion.Value, out id)) AccionVerDetalle(id);
                    break;
                }
                case "registrar-entrega":
                {
                    int id;
                    if (int.TryParse(hdnOrdenIDAccion.Value, out id)) AccionCargarFormularioEntrega(id);
                    break;
                }
                case "confirmar-entrega":
                {
                    int id;
                    if (int.TryParse(hdnEntregaIDAccion.Value, out id)) AccionConfirmarEntrega(id);
                    break;
                }
                case "cancelar-entrega":
                {
                    int id;
                    if (int.TryParse(hdnEntregaIDAccion.Value, out id)) AccionCancelarEntrega(id);
                    break;
                }
                case "cancelar-orden":
                {
                    int id;
                    if (int.TryParse(hdnOrdenIDAccion.Value, out id)) AccionCancelarOrden(id);
                    break;
                }
                case "imprimir":
                {
                    int id;
                    if (int.TryParse(hdnOrdenIDAccion.Value, out id)) AccionImprimirOrden(id);
                    break;
                }
            }
        }

        // ── Ver historial de la orden
        private void AccionVerDetalle(int ordenID)
        {
            using (var db = NuevoDb(false))
            {
                var orden = (from o in db.Ordenes
                             where o.OrdenID == ordenID
                             join b in db.Bases    on o.BaseOrigenID equals b.BaseID
                             join c in db.Clientes on o.ClienteID    equals c.ClienteID
                             select new { o.OrdenID, o.Folio, o.FechaOrden, BaseNombre=b.Nombre,
                                          ClienteNombre=c.Nombre, o.Estado, o.Observaciones })
                            .FirstOrDefault();
                if (orden == null) return;

                // Ítems de la orden
                var detalles = db.DetalleOrdenes
                    .Where(d => d.OrdenID == ordenID)
                    .OrderBy(d => d.DetalleOrdenID)
                    .ToList();

                var prodIDs = detalles.Where(d => d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                                      .Select(d => d.ProductoID.Value).Distinct().ToList();
                var matIDs  = detalles.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                      .Select(d => d.MaterialID.Value).Distinct().ToList();

                var prodNombresD = prodIDs.Any()
                    ? db.Productos.Where(p => prodIDs.Contains(p.ProductoID))
                        .ToDictionary(p => p.ProductoID, p => "[" + p.Codigo + "] " + p.Descripcion)
                    : new Dictionary<int, string>();

                var matNombresD = matIDs.Any()
                    ? db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                        .ToDictionary(m => m.MaterialID, m => "[" + m.Codigo + "] " + m.Descripcion)
                    : new Dictionary<int, string>();

                // Unidad base de cada material para mostrar junto a las cantidades
                var matUnidBaseD = new Dictionary<int, string>();
                if (matIDs.Any())
                {
                    var matsInfoD = db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID }).ToList();
                    var ubIDsD = matsInfoD.Where(m => m.UnidadMedidaID.HasValue)
                                          .Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNomD = ubIDsD.Any()
                        ? db.UnidadesMedida.Where(u => ubIDsD.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID,
                                          u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                        : new Dictionary<int, string>();
                    foreach (var m in matsInfoD)
                        matUnidBaseD[m.MaterialID] = (m.UnidadMedidaID.HasValue && ubNomD.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNomD[m.UnidadMedidaID.Value] : (m.Unidad ?? "");
                }

                var itemsJson = detalles.Select(d =>
                {
                    string nombre   = d.TipoItem;
                    int    itemID   = 0;
                    string unidBase = "";
                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                    {
                        itemID = d.ProductoID.Value;
                        if (prodNombresD.ContainsKey(itemID)) nombre = prodNombresD[itemID];
                    }
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                    {
                        itemID = d.MaterialID.Value;
                        if (matNombresD.ContainsKey(itemID))   nombre   = matNombresD[itemID];
                        if (matUnidBaseD.ContainsKey(itemID))  unidBase = matUnidBaseD[itemID];
                    }
                    return new DetalleOrdenItemJson
                    {
                        DetalleOrdenID    = d.DetalleOrdenID,
                        TipoItem          = d.TipoItem,
                        ItemID            = itemID,
                        NombreItem        = nombre,
                        CantidadPedida    = d.CantidadPedida,
                        CantidadEntregada = d.CantidadEntregada,
                        CantidadPendiente = Math.Max(0m, d.CantidadPedida - d.CantidadEntregada),
                        PrecioUnitario    = d.PrecioUnitario,
                        Estado            = d.Estado,
                        Descripcion       = d.Descripcion,  // ej. "6 Kilogramo/s" si se capturó en una unidad diferente
                        UnidadBaseNombre  = unidBase
                    };
                }).ToList();

                // Entregas vinculadas
                var entregasVinc = db.Entregas
                    .Where(e => e.OrdenID == ordenID)
                    .OrderByDescending(e => e.FechaEntrega)
                    .ThenByDescending(e => e.EntregaID)
                    .ToList();

                var entIDs = entregasVinc.Select(e => e.EntregaID).ToList();
                var totalesPorEntrega = new Dictionary<int, decimal>();
                if (entIDs.Any())
                {
                    var tots = db.DetalleEntregas
                        .Where(d => entIDs.Contains(d.EntregaID))
                        .GroupBy(d => d.EntregaID)
                        .Select(g => new { EntregaID = g.Key, Total = g.Sum(d => d.Cantidad * d.PrecioUnitario) })
                        .ToList();
                    foreach (var t in tots) totalesPorEntrega[t.EntregaID] = t.Total;
                }

                var entregasJson = entregasVinc.Select(e => new EntregaVinculadaJson
                {
                    EntregaID     = e.EntregaID,
                    Folio         = e.Folio,
                    Fecha         = e.FechaEntrega.ToString("dd/MM/yyyy"),
                    NumeroFactura = e.NumeroFactura ?? "",
                    Total         = totalesPorEntrega.ContainsKey(e.EntregaID) ? totalesPorEntrega[e.EntregaID] : 0m,
                    Estado        = e.Estado
                }).ToList();

                var dto = new OrdenDetalleJson
                {
                    OrdenID  = orden.OrdenID,
                    Folio    = orden.Folio,
                    Fecha    = orden.FechaOrden.ToString("dd/MM/yyyy"),
                    Base     = orden.BaseNombre,
                    Cliente  = orden.ClienteNombre,
                    Estado   = orden.Estado,
                    Obs      = orden.Observaciones ?? "",
                    Items    = itemsJson,
                    Entregas = entregasJson
                };

                hdnDetalleJson.Value = _json.Serialize(dto);
            }
        }

        // ── Cargar datos para el modal Registrar Entrega
        private void AccionCargarFormularioEntrega(int ordenID)
        {
            using (var db = NuevoDb(false))
            {
                var orden = (from o in db.Ordenes
                             where o.OrdenID == ordenID
                             join b in db.Bases    on o.BaseOrigenID equals b.BaseID
                             join c in db.Clientes on o.ClienteID    equals c.ClienteID
                             select new { o.OrdenID, o.Folio, BaseNombre=b.Nombre,
                                          ClienteNombre=c.Nombre, o.Estado, o.BaseOrigenID, o.EsCredito })
                            .FirstOrDefault();
                if (orden == null) return;

                if (orden.Estado == "COMPLETADA" || orden.Estado == "CANCELADA")
                {
                    SetMsg("info", "Sin pendientes",
                        string.Format("La orden {0} ya está en estado {1}.", orden.Folio, orden.Estado));
                    CargarGrid();
                    return;
                }

                var detalles = db.DetalleOrdenes
                    .Where(d => d.OrdenID == ordenID && d.Estado != "COMPLETADO")
                    .OrderBy(d => d.DetalleOrdenID)
                    .ToList();

                if (!detalles.Any())
                {
                    SetMsg("info", "Sin pendientes",
                        "Todos los ítems de la orden ya fueron entregados.");
                    CargarGrid();
                    return;
                }

                // Calcular cantidades reservadas (en entregas PENDIENTE o PENDIENTE_STOCK no confirmadas)
                var detIDs = detalles.Select(d => d.DetalleOrdenID).ToList();
                // Sum de DetalleEntregas por ítem en entregas PENDIENTE/PENDIENTE_STOCK vinculadas
                var reservados = (from e  in db.Entregas
                                  where e.OrdenID == ordenID
                                        && (e.Estado == "PENDIENTE" || e.Estado == "PENDIENTE_STOCK")
                                  join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                  select new { de.ProductoID, de.MaterialID, de.TipoItem, de.Cantidad })
                                 .ToList();

                // Nombres de ítems
                var prodIDs = detalles.Where(d => d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                                      .Select(d => d.ProductoID.Value).Distinct().ToList();
                var matIDs  = detalles.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                      .Select(d => d.MaterialID.Value).Distinct().ToList();

                var prodNombres = prodIDs.Any()
                    ? db.Productos.Where(p => prodIDs.Contains(p.ProductoID))
                        .ToDictionary(p => p.ProductoID, p => "[" + p.Codigo + "] " + p.Descripcion)
                    : new Dictionary<int, string>();

                // Para materiales: traer datos de unidad base + conversiones disponibles
                var matInfos = matIDs.Any()
                    ? db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, Nombre = "[" + m.Codigo + "] " + m.Descripcion,
                                           m.UnidadMedidaID, m.Unidad })
                        .ToList()
                    : new List<object>().Select(x => new { MaterialID=0, Nombre="", UnidadMedidaID=(int?)null, Unidad="" }).ToList();

                var unidades = db.UnidadesMedida.ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre);
                var conversiones = matIDs.Any()
                    ? (from c in db.ConversionesMaterial
                       where c.Activo && matIDs.Contains(c.MaterialID)
                       join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                       select new { c.MaterialID, c.ConversionID, c.Factor,
                                    NombreUnidad = string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre })
                      .ToList()
                    : new List<object>().Select(x => new { MaterialID=0, ConversionID=0, Factor=0m, NombreUnidad="" }).ToList();

                var ci2 = System.Globalization.CultureInfo.InvariantCulture;

                var items = detalles.Select(d =>
                {
                    string nombre = d.TipoItem;
                    int    itemID = 0;
                    decimal reservado = 0m;
                    var opciones = new List<UnidadOpcion>();
                    string unidBaseNombre = "";

                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                    {
                        itemID    = d.ProductoID.Value;
                        nombre    = prodNombres.ContainsKey(itemID) ? prodNombres[itemID] : nombre;
                        reservado = reservados
                            .Where(r => r.TipoItem == "PRODUCTO" && r.ProductoID == itemID)
                            .Sum(r => r.Cantidad);
                    }
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                    {
                        itemID = d.MaterialID.Value;
                        var matInfo = matInfos.FirstOrDefault(m => m.MaterialID == itemID);
                        if (matInfo != null) nombre = matInfo.Nombre;
                        reservado = reservados
                            .Where(r => r.TipoItem == "MATERIAL" && r.MaterialID == itemID)
                            .Sum(r => r.Cantidad);

                        // Opciones de unidad para este material
                        if (matInfo != null && matInfo.UnidadMedidaID.HasValue)
                        {
                            unidBaseNombre = unidades.ContainsKey(matInfo.UnidadMedidaID.Value)
                                ? unidades[matInfo.UnidadMedidaID.Value] : (matInfo.Unidad ?? "");
                            opciones.Add(new UnidadOpcion
                            {
                                Valor  = "base:" + matInfo.UnidadMedidaID.Value,
                                Texto  = unidBaseNombre + " (base)",
                                Factor = 1.0
                            });
                        }
                        foreach (var conv in conversiones.Where(c => c.MaterialID == itemID))
                        {
                            string factorStr = (conv.Factor == Math.Floor(conv.Factor))
                                ? ((long)conv.Factor).ToString(ci2) : conv.Factor.ToString("G6", ci2);
                            opciones.Add(new UnidadOpcion
                            {
                                Valor  = "conv:" + conv.ConversionID,
                                Texto  = conv.NombreUnidad + " (= " + factorStr + " " + unidBaseNombre + ")",
                                Factor = (double)conv.Factor
                            });
                        }
                    }

                    decimal pendiente = Math.Max(0m, d.CantidadPedida - d.CantidadEntregada - reservado);

                    return new DetalleOrdenItemJson
                    {
                        DetalleOrdenID    = d.DetalleOrdenID,
                        TipoItem          = d.TipoItem,
                        ItemID            = itemID,
                        NombreItem        = nombre,
                        CantidadPedida    = d.CantidadPedida,
                        CantidadEntregada = d.CantidadEntregada,
                        CantidadPendiente = pendiente,
                        PrecioUnitario    = d.PrecioUnitario,
                        Estado            = d.Estado,
                        Unidades          = opciones,
                        UnidadBaseNombre  = unidBaseNombre
                    };
                }).ToList();

                var partida = new OrdenPartidaJson
                {
                    OrdenID       = orden.OrdenID,
                    OrdenFolio    = orden.Folio,
                    ClienteNombre = orden.ClienteNombre,
                    BaseNombre    = orden.BaseNombre,
                    EstadoOrden   = orden.Estado,
                    BaseOrigenID  = orden.BaseOrigenID,
                    EsCredito     = orden.EsCredito,
                    Items         = items
                };
                hdnPartidaJson.Value = _json.Serialize(partida);
            }
        }

        // ── Guardar entrega parcial (botón desde modal Registrar Entrega)
        protected void btnGuardarPartida_Click(object sender, EventArgs e)
        {
            int ordenID;
            if (!int.TryParse(hdnOrdenIDAccion.Value, out ordenID)) return;

            string modo = hdnModoGuardado.Value; // "PENDIENTE" o "CONFIRMAR"
            bool confirmarDirecto = modo == "CONFIRMAR";

            string jsonItems = hdnEntregaItemsJson.Value;
            if (string.IsNullOrEmpty(jsonItems) || jsonItems == "[]")
            {
                SetMsg("warning", "Sin cantidades", "No se ingresó ninguna cantidad a entregar.");
                CargarGrid(); return;
            }

            List<ItemPartidaModel> items;
            try { items = _json.Deserialize<List<ItemPartidaModel>>(jsonItems); }
            catch { SetMsg("error", "Error", "Error al leer las cantidades."); CargarGrid(); return; }

            items = items.Where(i => i.CantidadAEntregar > 0m).ToList();
            if (!items.Any())
            {
                SetMsg("warning", "Sin cantidades", "Ingrese al menos una cantidad mayor a 0.");
                CargarGrid(); return;
            }

            bool forzarCredito = hdnForzarLimiteCredito.Value == "true";
            hdnForzarLimiteCredito.Value = "";

            if (confirmarDirecto && chkEsCreditoPartida.Checked && !forzarCredito)
            {
                using (var dbValCred = NuevoDb(false))
                {
                    var ordenChk = dbValCred.Ordenes.FirstOrDefault(o => o.OrdenID == ordenID);
                    if (ordenChk != null)
                    {
                        decimal montoChk = items.Sum(i => (i.TipoItem == "MATERIAL" && i.Factor > 0m
                            ? i.CantidadAEntregar * i.Factor : i.CantidadAEntregar) * i.PrecioUnitario);
                        var infoChk = CuentasPorCobrarHelper.EvaluarLimiteCredito(dbValCred, ordenChk.ClienteID, montoChk);
                        if (infoChk != null)
                        {
                            SetMsg("warning", "Límite de crédito excedido", CuentasPorCobrarHelper.FormatearMensaje(infoChk),
                                null, new { accion = "reintentar-guardar-partida" });
                            CargarGrid();
                            return;
                        }
                    }
                }
            }

            string fechaStr = hdnFechaEntregaConf.Value;
            DateTime fecha;
            if (!DateTime.TryParse(fechaStr, out fecha)) fecha = AppHelper.Hoy;

            string numFactura = string.IsNullOrWhiteSpace(hdnNumeroFactura.Value)
                                ? null : hdnNumeroFactura.Value.Trim();

            // Limpiar hidden
            hdnEntregaItemsJson.Value = "[]";
            hdnModoGuardado.Value     = "";
            hdnNumeroFactura.Value    = "";
            hdnFechaEntregaConf.Value = "";

            using (var dbVal = NuevoDb(false))
            {
                // Validar que ningún ítem supere su pendiente real
                string errPend = ValidarCantidadesPendientes(dbVal, ordenID, items);
                if (errPend != null)
                {
                    SetMsg("warning", "Cantidad inválida", errPend);
                    CargarGrid(); return;
                }

                // Si se confirma directo, validar stock
                if (confirmarDirecto)
                {
                    var orden = dbVal.Ordenes.FirstOrDefault(o => o.OrdenID == ordenID);
                    if (orden == null) { CargarGrid(); return; }
                    var itemsStock = items.Select(i => new ItemEntregaModel
                    {
                        TipoItem       = i.TipoItem,
                        ItemID         = i.ItemID,
                        Cantidad       = (i.TipoItem == "MATERIAL" && i.Factor > 0m)
                                         ? i.CantidadAEntregar * i.Factor
                                         : i.CantidadAEntregar,
                        PrecioUnitario = i.PrecioUnitario,
                        Factor         = 1m
                    }).ToList();
                    string errStock = ValidarStockItems(dbVal, itemsStock, orden.BaseOrigenID);
                    if (errStock != null)
                    {
                        SetMsg("warning", "Stock insuficiente", errStock);
                        CargarGrid(); return;
                    }
                }
            }

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
                            var orden = db.Ordenes.First(o => o.OrdenID == ordenID);
                            string estadoEntrega = confirmarDirecto ? "ENTREGADA" : "PENDIENTE";

                            // Generar folio para la entrega.
                            // IMPORTANTE: contar por prefijo de folio, NO por FechaEntrega.
                            // Si se usa FechaEntrega y el usuario captura una fecha distinta
                            // a hoy, las entregas existentes de hoy no se contarían y se
                            // generaría un folio duplicado (viola UQ_Entregas_Folio).
                            DateTime hoyFolio    = AppHelper.Hoy;
                            string  folioPrefix  = "ENT-" + hoyFolio.ToString("yyyyMMdd") + "-%";
                            int     cntEnt       = db.ExecuteQuery<int>(
                                "SELECT COUNT(*) FROM dbo.Entregas WITH (UPDLOCK, HOLDLOCK) WHERE Folio LIKE {0}",
                                folioPrefix).First();
                            string folioEnt = string.Format("ENT-{0}-{1:D3}",
                                hoyFolio.ToString("yyyyMMdd"), cntEnt + 1);

                            // Nombre del cliente
                            string clienteNombre = "";
                            var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == orden.ClienteID);
                            if (cli != null) clienteNombre = cli.Nombre;

                            var entrega = new Modelo.Entregas
                            {
                                Folio           = folioEnt,
                                FechaEntrega    = fecha.Date,
                                BaseOrigenID    = orden.BaseOrigenID,
                                Cliente         = clienteNombre,
                                ClienteID       = orden.ClienteID,
                                Estado          = estadoEntrega,
                                OrdenID         = ordenID,
                                NumeroFactura   = confirmarDirecto ? numFactura : null,
                                EsCredito       = chkEsCreditoPartida.Checked,
                                Observaciones   = null,
                                RegistradoPorID = Convert.ToInt32(Session["ClaveID"]),
                                FechaRegistro   = AppHelper.Ahora
                            };
                            db.Entregas.InsertOnSubmit(entrega);
                            db.SubmitChanges(); // obtener EntregaID

                            // Insertar DetalleEntregas (aplicando conversión de unidad para materiales)
                            foreach (var it in items)
                            {
                                decimal cantBase = (it.TipoItem == "MATERIAL" && it.Factor > 0m)
                                    ? it.CantidadAEntregar * it.Factor
                                    : it.CantidadAEntregar;

                                var det = new DetalleEntregas
                                {
                                    EntregaID      = entrega.EntregaID,
                                    TipoItem       = it.TipoItem,
                                    Cantidad       = cantBase,
                                    PrecioUnitario = it.PrecioUnitario
                                };
                                if (it.TipoItem == "PRODUCTO")
                                {
                                    det.ProductoID = it.ItemID;
                                }
                                else
                                {
                                    det.MaterialID        = it.ItemID;
                                    // Guardar cantidad capturada y unidad para historial/impresión
                                    if (it.Factor != 1m)
                                    {
                                        det.CantidadCapturada = it.CantidadAEntregar;
                                        det.UnidadCapturaID   = ObtenerUnidadCapturaIDEntrega(it.ItemID, it.UnidadVal, db);
                                    }
                                }
                                db.DetalleEntregas.InsertOnSubmit(det);
                            }
                            db.SubmitChanges();

                            if (confirmarDirecto)
                            {
                                // Descontar stock usando cantidad en unidad BASE
                                var itemsStock = items.Select(i => new ItemEntregaModel
                                {
                                    TipoItem       = i.TipoItem,
                                    ItemID         = i.ItemID,
                                    Cantidad       = (i.TipoItem == "MATERIAL" && i.Factor > 0m)
                                                     ? i.CantidadAEntregar * i.Factor
                                                     : i.CantidadAEntregar,
                                    PrecioUnitario = i.PrecioUnitario,
                                    Factor         = 1m
                                }).ToList();
                                int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");
                                DescontarStockYRegistrarMovimientos(db, entrega.EntregaID,
                                    orden.BaseOrigenID, itemsStock, tipoSalidaID, null);
                                db.SubmitChanges();

                                ActualizarEstadoOrden(ordenID, db);
                                db.SubmitChanges();

                                if (forzarCredito && chkEsCreditoPartida.Checked)
                                {
                                    decimal montoNuevo = itemsStock.Sum(i => i.Cantidad * i.PrecioUnitario);
                                    var infoCredito = CuentasPorCobrarHelper.EvaluarLimiteCredito(db, orden.ClienteID, montoNuevo);
                                    if (infoCredito != null)
                                        CuentasPorCobrarHelper.RegistrarBitacora(db, infoCredito, entrega.EntregaID, Convert.ToInt32(Session["ClaveID"]));
                                }

                                CuentasPorCobrarHelper.GenerarSiAplica(db, entrega.EntregaID, Convert.ToInt32(Session["ClaveID"]));
                            }
                            else if (orden.Estado == "ABIERTA")
                            {
                                // Marcar como PARCIAL al crear la primera entrega pendiente
                                orden.Estado     = "PARCIAL";
                                orden.FechaModif = AppHelper.Ahora;
                                db.SubmitChanges();
                            }

                            tx.Commit();

                            string msg = confirmarDirecto
                                ? string.Format("Entrega {0} confirmada. Stock descontado.", folioEnt)
                                : string.Format("Entrega {0} guardada como PENDIENTE. Confírmela cuando esté lista.", folioEnt);
                            SetMsg("success", confirmarDirecto ? "Entrega confirmada" : "Entrega programada", msg);
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al guardar", "Ocurrió un error: " + ex.Message);
            }
        }

        // ── Confirmar entrega existente (PENDIENTE → ENTREGADA)
        private void AccionConfirmarEntrega(int entregaID)
        {
            string numFactura = string.IsNullOrWhiteSpace(hdnNumeroFactura.Value)
                                ? null : hdnNumeroFactura.Value.Trim();
            DateTime fecha;
            if (!DateTime.TryParse(hdnFechaEntregaConf.Value, out fecha)) fecha = AppHelper.Hoy;

            bool forzarCreditoEntrega = hdnForzarLimiteCredito.Value == "true";
            if (!forzarCreditoEntrega)
            {
                using (var dbValCred = NuevoDb(false))
                {
                    var entregaChk = dbValCred.Entregas.FirstOrDefault(e => e.EntregaID == entregaID);
                    if (entregaChk != null && entregaChk.EsCredito && entregaChk.ClienteID.HasValue)
                    {
                        var itemsChk = ObtenerItemsEntrega(dbValCred, entregaID);
                        decimal montoChk = itemsChk.Sum(i => i.Cantidad * i.PrecioUnitario);
                        var infoChk = CuentasPorCobrarHelper.EvaluarLimiteCredito(dbValCred, entregaChk.ClienteID.Value, montoChk);
                        if (infoChk != null)
                        {
                            SetMsg("warning", "Límite de crédito excedido", CuentasPorCobrarHelper.FormatearMensaje(infoChk),
                                null, new { accion = "reintentar-confirmar-entrega-orden", entregaId = entregaID });
                            return;
                        }
                    }
                }
            }
            hdnForzarLimiteCredito.Value = "";

            hdnNumeroFactura.Value    = "";
            hdnFechaEntregaConf.Value = "";

            using (var dbVal = NuevoDb(false))
            {
                var entrega = dbVal.Entregas.FirstOrDefault(e => e.EntregaID == entregaID);
                if (entrega == null) return;
                if (entrega.Estado == "ENTREGADA" || entrega.Estado == "CANCELADA")
                {
                    SetMsg("info", "Sin cambios",
                        string.Format("La entrega {0} ya está en estado {1}.", entrega.Folio, entrega.Estado));
                    CargarDashboard(); CargarGrid(); return;
                }

                var items = ObtenerItemsEntrega(dbVal, entregaID);
                string errStock = ValidarStockItems(dbVal, items, entrega.BaseOrigenID);
                if (errStock != null)
                {
                    using (var db2 = NuevoDb(true))
                    {
                        var ent2 = db2.Entregas.First(e => e.EntregaID == entregaID);
                        ent2.Estado     = "PENDIENTE_STOCK";
                        ent2.FechaModif = AppHelper.Ahora;
                        db2.SubmitChanges();
                    }
                    SetMsg("warning", "Stock insuficiente", errStock);
                    CargarDashboard(); CargarGrid(); return;
                }
            }

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
                            var entrega = db.Entregas.First(e => e.EntregaID == entregaID);
                            var items   = ObtenerItemsEntrega(db, entregaID);
                            int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");

                            DescontarStockYRegistrarMovimientos(db, entregaID,
                                entrega.BaseOrigenID, items, tipoSalidaID, null);

                            entrega.Estado         = "ENTREGADA";
                            entrega.FechaEntrega   = fecha.Date;
                            entrega.NumeroFactura  = numFactura;
                            entrega.FechaModif     = AppHelper.Ahora;
                            db.SubmitChanges();

                            if (entrega.OrdenID.HasValue)
                            {
                                ActualizarEstadoOrden(entrega.OrdenID.Value, db);
                                db.SubmitChanges();
                            }

                            if (forzarCreditoEntrega && entrega.EsCredito && entrega.ClienteID.HasValue)
                            {
                                decimal montoNuevo = items.Sum(i => i.Cantidad * i.PrecioUnitario);
                                var infoCredito = CuentasPorCobrarHelper.EvaluarLimiteCredito(db, entrega.ClienteID.Value, montoNuevo);
                                if (infoCredito != null)
                                    CuentasPorCobrarHelper.RegistrarBitacora(db, infoCredito, entregaID, Convert.ToInt32(Session["ClaveID"]));
                            }

                            CuentasPorCobrarHelper.GenerarSiAplica(db, entregaID, Convert.ToInt32(Session["ClaveID"]));

                            tx.Commit();

                            SetMsg("success", "Entrega confirmada",
                                string.Format("Entrega {0} confirmada. Stock descontado.", entrega.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al confirmar", "Ocurrió un error: " + ex.Message);
            }
        }

        // ── Cancelar entrega individual
        private void AccionCancelarEntrega(int entregaID)
        {
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
                            var entrega = db.Entregas.First(e => e.EntregaID == entregaID);

                            if (CuentasPorCobrarHelper.HayCobrosActivos(db, entregaID))
                                throw new CxCConCobrosActivosException(
                                    "Esta entrega tiene cobros (abonos) registrados en su Cuenta por Cobrar. " +
                                    "Cancele primero esos cobros desde el módulo de Cuentas por Cobrar antes de cancelar la entrega.");

                            if (entrega.Estado == "ENTREGADA")
                            {
                                var items = ObtenerItemsEntrega(db, entregaID);
                                int tipoAjusteID = ObtenerTipoMovimientoID(db, "AJUSTE_POS");
                                DevolverStockYRegistrarMovimientos(db, entregaID,
                                    entrega.BaseOrigenID, items, tipoAjusteID);

                                int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");
                                var movsOriginal = db.Movimientos
                                    .Where(m => m.EntregaID == entregaID && m.TipoMovimientoID == tipoSalidaID)
                                    .ToList();
                                foreach (var mv in movsOriginal)
                                    mv.Observaciones = (mv.Observaciones ?? "") + " [ANULADO - entrega cancelada]";
                            }

                            CuentasPorCobrarHelper.CancelarCxCSiExiste(db, entregaID);

                            int? ordenID = entrega.OrdenID;
                            entrega.Estado     = "CANCELADA";
                            entrega.FechaModif = AppHelper.Ahora;
                            db.SubmitChanges();

                            if (ordenID.HasValue)
                            {
                                ActualizarEstadoOrden(ordenID.Value, db);
                                db.SubmitChanges();
                            }

                            tx.Commit();
                            SetMsg("success", "Entrega cancelada",
                                string.Format("Entrega {0} cancelada.", entrega.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (CxCConCobrosActivosException cex)
            {
                SetMsg("warning", "Cobros pendientes", cex.Message);
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al cancelar", "Ocurrió un error: " + ex.Message);
            }
        }

        // ── Cancelar orden completa
        private void AccionCancelarOrden(int ordenID)
        {
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
                            var orden = db.Ordenes.First(o => o.OrdenID == ordenID);

                            var entregasVinc = db.Entregas
                                .Where(e => e.OrdenID == ordenID && e.Estado != "CANCELADA")
                                .ToList();

                            // Pre-chequeo (todo-o-nada): si alguna entrega ya tiene cobros
                            // activos en su CxC, no se cancela NADA de la orden.
                            var foliosBloqueados = entregasVinc
                                .Where(ent => CuentasPorCobrarHelper.HayCobrosActivos(db, ent.EntregaID))
                                .Select(ent => ent.Folio)
                                .ToList();
                            if (foliosBloqueados.Any())
                                throw new CxCConCobrosActivosException(
                                    "No se puede cancelar la orden: las siguientes entregas tienen cobros (abonos) " +
                                    "registrados y deben cancelarse primero desde el módulo de Cuentas por Cobrar: " +
                                    string.Join(", ", foliosBloqueados) + ".");

                            foreach (var ent in entregasVinc)
                            {
                                if (ent.Estado == "ENTREGADA")
                                {
                                    var items = ObtenerItemsEntrega(db, ent.EntregaID);
                                    int tipoAjusteID = ObtenerTipoMovimientoID(db, "AJUSTE_POS");
                                    DevolverStockYRegistrarMovimientos(db, ent.EntregaID,
                                        ent.BaseOrigenID, items, tipoAjusteID);

                                    int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");
                                    var movs = db.Movimientos
                                        .Where(m => m.EntregaID == ent.EntregaID && m.TipoMovimientoID == tipoSalidaID)
                                        .ToList();
                                    foreach (var mv in movs)
                                        mv.Observaciones = (mv.Observaciones ?? "") + " [ANULADO - orden cancelada]";
                                }

                                CuentasPorCobrarHelper.CancelarCxCSiExiste(db, ent.EntregaID);

                                ent.Estado     = "CANCELADA";
                                ent.FechaModif = AppHelper.Ahora;
                            }

                            orden.Estado     = "CANCELADA";
                            orden.FechaModif = AppHelper.Ahora;
                            db.SubmitChanges();
                            tx.Commit();

                            SetMsg("success", "Orden cancelada",
                                string.Format("Orden {0} cancelada. Stock restaurado donde aplica.", orden.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (CxCConCobrosActivosException cex)
            {
                SetMsg("warning", "Cobros pendientes", cex.Message);
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al cancelar", "Ocurrió un error: " + ex.Message);
            }
        }

        // ── Actualizar estado de la Orden y sus DetalleOrdenes
        private void ActualizarEstadoOrden(int ordenID, InventarioAnkhalDBDataContext db)
        {
            var detalles = db.DetalleOrdenes.Where(d => d.OrdenID == ordenID).ToList();
            if (!detalles.Any()) return;

            // Recalcular CantidadEntregada para cada detalle (solo entregas ENTREGADA)
            var totalesEntregados = (from e  in db.Entregas
                                     where e.OrdenID == ordenID && e.Estado == "ENTREGADA"
                                     join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                     select new { de.TipoItem, de.ProductoID, de.MaterialID, de.Cantidad })
                                    .ToList();

            foreach (var det in detalles)
            {
                decimal entregada = 0m;
                if (det.TipoItem == "PRODUCTO" && det.ProductoID.HasValue)
                    entregada = totalesEntregados
                        .Where(x => x.TipoItem == "PRODUCTO" && x.ProductoID == det.ProductoID)
                        .Sum(x => x.Cantidad);
                else if (det.TipoItem == "MATERIAL" && det.MaterialID.HasValue)
                    entregada = totalesEntregados
                        .Where(x => x.TipoItem == "MATERIAL" && x.MaterialID == det.MaterialID)
                        .Sum(x => x.Cantidad);

                det.CantidadEntregada = entregada;
                det.Estado = entregada <= 0m                    ? "PENDIENTE"
                           : entregada >= det.CantidadPedida    ? "COMPLETADO"
                           :                                       "PARCIAL";
            }

            var orden = db.Ordenes.First(o => o.OrdenID == ordenID);
            if (detalles.All(d => d.Estado == "COMPLETADO"))
                orden.Estado = "COMPLETADA";
            else if (detalles.Any(d => d.Estado != "PENDIENTE"))
                orden.Estado = "PARCIAL";
            else
                orden.Estado = "ABIERTA";
            orden.FechaModif = AppHelper.Ahora;
        }

        // ── Imprimir orden
        private void AccionImprimirOrden(int ordenID)
        {
            using (var db = NuevoDb(false))
            {
                var orden = (from o in db.Ordenes
                             where o.OrdenID == ordenID
                             join b in db.Bases    on o.BaseOrigenID equals b.BaseID
                             join c in db.Clientes on o.ClienteID    equals c.ClienteID
                             select new { o.OrdenID, o.Folio, o.FechaOrden, BaseNombre=b.Nombre,
                                          ClienteNombre=c.Nombre, o.Estado, o.Observaciones })
                            .FirstOrDefault();
                if (orden == null) return;

                var detalles = db.DetalleOrdenes.Where(d => d.OrdenID == ordenID)
                    .OrderBy(d => d.DetalleOrdenID).ToList();

                var prodIDs = detalles.Where(d => d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                                      .Select(d => d.ProductoID.Value).Distinct().ToList();
                var matIDs  = detalles.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                      .Select(d => d.MaterialID.Value).Distinct().ToList();
                var prodNombres = prodIDs.Any()
                    ? db.Productos.Where(p => prodIDs.Contains(p.ProductoID))
                        .ToDictionary(p => p.ProductoID, p => "[" + p.Codigo + "] " + p.Descripcion)
                    : new Dictionary<int, string>();
                var matNombres = matIDs.Any()
                    ? db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                        .ToDictionary(m => m.MaterialID, m => "[" + m.Codigo + "] " + m.Descripcion)
                    : new Dictionary<int, string>();

                // Unidades base de materiales para mostrar junto a las cantidades
                var matUnidBaseI = new Dictionary<int, string>();
                if (matIDs.Any())
                {
                    var matsI  = db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                                   .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID }).ToList();
                    var ubIDsI = matsI.Where(m => m.UnidadMedidaID.HasValue).Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNomI = ubIDsI.Any()
                        ? db.UnidadesMedida.Where(u => ubIDsI.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                        : new Dictionary<int, string>();
                    foreach (var m in matsI)
                        matUnidBaseI[m.MaterialID] = (m.UnidadMedidaID.HasValue && ubNomI.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNomI[m.UnidadMedidaID.Value] : (m.Unidad ?? "");
                }

                var entregasVinc = db.Entregas.Where(e => e.OrdenID == ordenID && e.Estado != "CANCELADA")
                    .OrderBy(e => e.FechaEntrega).ToList();

                var sb = new StringBuilder();
                sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'/>");
                sb.AppendFormat("<title>Orden {0}</title>", orden.Folio);
                sb.Append("<style>body{font-family:Arial,sans-serif;margin:30px;color:#1a1a1a}");
                sb.Append(".header{text-align:center;border-bottom:3px solid #003366;padding-bottom:12px;margin-bottom:20px}");
                sb.Append(".header h2{margin:0;color:#003366;font-size:1.3rem;letter-spacing:1px}");
                sb.Append(".meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:18px;font-size:.9rem;background:#f8f9fa;padding:12px;border-radius:6px}");
                sb.Append(".meta label{font-weight:bold;color:#003366}");
                sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:24px}");
                sb.Append("thead th{background:#003366;color:#fff;padding:8px 10px;font-size:.88rem;text-align:left}");
                sb.Append("thead th.r{text-align:right}");
                sb.Append("tbody td{padding:7px 10px;border-bottom:1px solid #ddd;font-size:.88rem}");
                sb.Append("tbody td.r{text-align:right}");
                sb.Append("tbody tr:nth-child(even){background:#f5f7fa}");
                sb.Append(".no-print{text-align:center;margin-top:24px}");
                sb.Append(".btn-print{padding:8px 24px;background:#003366;color:#fff;border:none;border-radius:4px;cursor:pointer;margin-right:8px}");
                sb.Append(".btn-close{padding:8px 24px;background:#6c757d;color:#fff;border:none;border-radius:4px;cursor:pointer}");
                sb.Append(".unid{font-size:.75rem;color:#888;display:block;font-weight:normal}");
                sb.Append("@media print{.no-print{display:none!important}}");
                sb.Append("</style></head><body>");
                sb.Append("<div class='header'>");
                sb.Append("<img src='img/ankhal.png' alt='ANKHAL' height='60' onerror=\"this.style.display='none'\"/>");
                sb.Append("<h2>ORDEN DE ENTREGA — GRUPO ANKHAL</h2></div>");
                sb.Append("<div class='meta'>");
                sb.AppendFormat("<div><label>Folio:</label> {0}</div>", orden.Folio);
                sb.AppendFormat("<div><label>Fecha:</label> {0:dd/MM/yyyy}</div>", orden.FechaOrden);
                sb.AppendFormat("<div><label>Base Origen:</label> {0}</div>", orden.BaseNombre);
                sb.AppendFormat("<div><label>Cliente:</label> {0}</div>", orden.ClienteNombre);
                sb.AppendFormat("<div><label>Estado:</label> {0}</div>", orden.Estado);
                sb.AppendFormat("<div><label>Observaciones:</label> {0}</div>", orden.Observaciones ?? "—");
                sb.Append("</div>");

                sb.Append("<h3 style='color:#003366;font-size:1rem;'>Ítems de la Orden</h3>");
                sb.Append("<table><thead><tr><th>Tipo</th><th>Descripción</th><th class='r'>Pedido</th><th class='r'>Entregado</th><th class='r'>Pendiente</th><th class='r'>P. Unit.</th></tr></thead><tbody>");
                foreach (var d in detalles)
                {
                    string nombre = d.TipoItem;
                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue && prodNombres.ContainsKey(d.ProductoID.Value))
                        nombre = prodNombres[d.ProductoID.Value];
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue && matNombres.ContainsKey(d.MaterialID.Value))
                        nombre = matNombres[d.MaterialID.Value];

                    decimal pend  = Math.Max(0m, d.CantidadPedida - d.CantidadEntregada);
                    string  unidN = (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue
                                     && matUnidBaseI.ContainsKey(d.MaterialID.Value))
                                    ? System.Web.HttpUtility.HtmlEncode(matUnidBaseI[d.MaterialID.Value]) : "";

                    // Columna Pedido: cantidad original capturada si la hubo (ej. "6 Kilogramo/s")
                    string pedidoStr;
                    if (d.TipoItem == "MATERIAL" && !string.IsNullOrEmpty(d.Descripcion))
                    {
                        pedidoStr = string.Format("<strong>{0}</strong>{1}",
                            System.Web.HttpUtility.HtmlEncode(d.Descripcion),
                            !string.IsNullOrEmpty(unidN)
                                ? string.Format("<span class='unid'>= {0:0.##} {1}</span>", d.CantidadPedida, unidN)
                                : "");
                    }
                    else
                    {
                        pedidoStr = d.CantidadPedida.ToString("0.##")
                            + (!string.IsNullOrEmpty(unidN)
                                ? string.Format(" <span class='unid'>{0}</span>", unidN) : "");
                    }

                    string entregadoStr = d.CantidadEntregada.ToString("0.##")
                        + (!string.IsNullOrEmpty(unidN)
                            ? string.Format(" <span class='unid'>{0}</span>", unidN) : "");
                    string pendienteStr = pend.ToString("0.##")
                        + (!string.IsNullOrEmpty(unidN)
                            ? string.Format(" <span class='unid'>{0}</span>", unidN) : "");

                    sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td class='r'>{2}</td><td class='r'>{3}</td><td class='r'>{4}</td><td class='r'>{5:C2}</td></tr>",
                        d.TipoItem, nombre, pedidoStr, entregadoStr, pendienteStr, d.PrecioUnitario);
                }
                sb.Append("</tbody></table>");

                if (entregasVinc.Any())
                {
                    sb.Append("<h3 style='color:#003366;font-size:1rem;'>Entregas Registradas</h3>");
                    sb.Append("<table><thead><tr><th>Folio Entrega</th><th>Fecha</th><th>Factura</th><th>Estado</th></tr></thead><tbody>");
                    foreach (var ent in entregasVinc)
                        sb.AppendFormat("<tr><td>{0}</td><td>{1:dd/MM/yyyy}</td><td>{2}</td><td>{3}</td></tr>",
                            ent.Folio, ent.FechaEntrega, ent.NumeroFactura ?? "—", ent.Estado);
                    sb.Append("</tbody></table>");
                }

                sb.Append("<div class='no-print'>");
                sb.Append("<button class='btn-print' onclick='window.print()'>Imprimir</button>");
                sb.Append("<button class='btn-close' onclick='window.close()'>Cerrar</button>");
                sb.Append("</div></body></html>");

                string htmlJson = _json.Serialize(sb.ToString());
                string script   = string.Format(
                    "(function(){{var w=window.open('','_blank','width=860,height=700,scrollbars=yes');" +
                    "w.document.write({0});w.document.close();w.focus();}})();", htmlJson);
                ClientScript.RegisterStartupScript(GetType(), "imprimirOrden", script, true);
            }
        }

        // ══ Helpers de lógica de negocio ══════════════════════════════════════

        // Valida que las cantidades a entregar no superen los pendientes reales
        private string ValidarCantidadesPendientes(InventarioAnkhalDBDataContext db,
            int ordenID, List<ItemPartidaModel> items)
        {
            var detalles = db.DetalleOrdenes.Where(d => d.OrdenID == ordenID).ToList();

            // Cantidades reservadas en entregas PENDIENTE/PENDIENTE_STOCK ya existentes
            var reservados = (from e  in db.Entregas
                              where e.OrdenID == ordenID
                                    && (e.Estado == "PENDIENTE" || e.Estado == "PENDIENTE_STOCK")
                              join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                              select new { de.TipoItem, de.ProductoID, de.MaterialID, de.Cantidad })
                             .ToList();

            foreach (var it in items)
            {
                var det = it.TipoItem == "PRODUCTO"
                    ? detalles.FirstOrDefault(d => d.TipoItem == "PRODUCTO" && d.ProductoID == it.ItemID)
                    : detalles.FirstOrDefault(d => d.TipoItem == "MATERIAL" && d.MaterialID == it.ItemID);

                if (det == null) continue;

                decimal reservado = it.TipoItem == "PRODUCTO"
                    ? reservados.Where(r => r.TipoItem == "PRODUCTO" && r.ProductoID == it.ItemID).Sum(r => r.Cantidad)
                    : reservados.Where(r => r.TipoItem == "MATERIAL" && r.MaterialID == it.ItemID).Sum(r => r.Cantidad);

                decimal pendiente = Math.Max(0m, det.CantidadPedida - det.CantidadEntregada - reservado);
                // Comparar en unidad base: CantidadAEntregar × Factor vs pendiente (en base)
                decimal cantBaseAEntregar = (it.TipoItem == "MATERIAL" && it.Factor > 0m)
                    ? it.CantidadAEntregar * it.Factor
                    : it.CantidadAEntregar;

                if (cantBaseAEntregar > pendiente)
                {
                    string nombre = it.TipoItem + " #" + it.ItemID;
                    using (var dbN = NuevoDb(false))
                    {
                        if (it.TipoItem == "PRODUCTO")
                            nombre = dbN.Productos.Where(p => p.ProductoID == it.ItemID)
                                         .Select(p => p.Descripcion).FirstOrDefault() ?? nombre;
                        else
                            nombre = dbN.Materiales.Where(m => m.MaterialID == it.ItemID)
                                         .Select(m => m.Descripcion).FirstOrDefault() ?? nombre;
                    }
                    return string.Format("La cantidad a entregar de '{0}' ({1:0.##} = {2:0.##} base) supera el pendiente disponible ({3:0.##} base).",
                        nombre, it.CantidadAEntregar, cantBaseAEntregar, pendiente);
                }
            }
            return null;
        }

        // Model auxiliar para ValidarStockItems y DescontarStock (reutiliza lógica de Entregas)
        private class ItemEntregaModel
        {
            public string  TipoItem       { get; set; }
            public int     ItemID         { get; set; }
            public decimal Cantidad       { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Factor         { get; set; } = 1m;
        }

        private List<ItemEntregaModel> ObtenerItemsEntrega(InventarioAnkhalDBDataContext db, int entregaID)
        {
            return db.DetalleEntregas
                .Where(d => d.EntregaID == entregaID)
                .ToList()
                .Select(d => new ItemEntregaModel
                {
                    TipoItem       = (d.TipoItem ?? "").ToUpper(),
                    ItemID         = d.TipoItem == "PRODUCTO" ? (d.ProductoID ?? 0) : (d.MaterialID ?? 0),
                    Cantidad       = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Factor         = 1m
                }).ToList();
        }

        private string ValidarStockItems(InventarioAnkhalDBDataContext db,
            List<ItemEntregaModel> items, int baseID)
        {
            var reqProductos = items
                .Where(it => it.TipoItem == "PRODUCTO")
                .GroupBy(it => it.ItemID)
                .Select(g => new { ItemID = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToList();

            foreach (var req in reqProductos)
            {
                decimal disponible = db.StockProductos
                    .Where(s => s.BaseID == baseID && s.ProductoID == req.ItemID)
                    .Select(s => (decimal)s.CantidadBuenas).FirstOrDefault();
                if (disponible < req.Total)
                {
                    string nombre = db.Productos.Where(p => p.ProductoID == req.ItemID)
                        .Select(p => p.Descripcion).FirstOrDefault() ?? "Producto";
                    return string.Format("Stock insuficiente para '{0}': disponible {1:N0}, requerido {2:N0}.",
                        nombre, disponible, req.Total);
                }
            }

            var reqMateriales = items
                .Where(it => it.TipoItem == "MATERIAL")
                .GroupBy(it => it.ItemID)
                .Select(g => new { ItemID = g.Key, TotalBase = g.Sum(x => x.Cantidad * x.Factor) })
                .ToList();

            foreach (var req in reqMateriales)
            {
                decimal disponible = db.StockMateriales
                    .Where(s => s.BaseID == baseID && s.MaterialID == req.ItemID)
                    .Select(s => s.CantidadActual).FirstOrDefault();
                if (disponible < req.TotalBase)
                {
                    string nombre = db.Materiales.Where(m => m.MaterialID == req.ItemID)
                        .Select(m => m.Descripcion).FirstOrDefault() ?? "Material";
                    return string.Format("Stock insuficiente para '{0}': disponible {1:N4}, requerido {2:N4}.",
                        nombre, disponible, req.TotalBase);
                }
            }
            return null;
        }

        private int ObtenerTipoMovimientoID(InventarioAnkhalDBDataContext db, string clave)
        {
            return db.TiposMovimiento.Where(t => t.Clave == clave)
                .Select(t => t.TipoMovimientoID).FirstOrDefault();
        }

        private void DescontarStockYRegistrarMovimientos(InventarioAnkhalDBDataContext db,
            int entregaID, int baseID, List<ItemEntregaModel> items, int tipoSalidaID,
            string obsEntrega)
        {
            int claveID = Convert.ToInt32(Session["ClaveID"]);

            var loteEnt = new Modelo.LotesMovimiento
            {
                Folio            = AppHelper.GenerarFolio(db, "ENT"),
                TipoMovimientoID = tipoSalidaID,
                BaseOrigenID     = baseID,
                BaseDestinoID    = null,
                Observaciones    = obsEntrega,
                RegistradoPorID  = claveID,
                FechaLote        = AppHelper.Ahora.Date,
                FechaRegistro    = AppHelper.Ahora
            };
            db.LotesMovimiento.InsertOnSubmit(loteEnt);
            db.SubmitChanges();
            int loteEntID = loteEnt.LoteID;

            foreach (var it in items)
            {
                if (it.TipoItem == "PRODUCTO")
                {
                    int actual = db.ExecuteQuery<int>(
                        "SELECT CantidadBuenas FROM dbo.StockProductos WITH (UPDLOCK, HOLDLOCK) WHERE BaseID={0} AND ProductoID={1}",
                        baseID, it.ItemID).FirstOrDefault();
                    if (actual < (int)it.Cantidad)
                        throw new InvalidOperationException(string.Format(
                            "Stock insuficiente para producto #{0}: disponible {1}, requerido {2}.",
                            it.ItemID, actual, (int)it.Cantidad));

                    var stock = db.StockProductos.FirstOrDefault(s => s.BaseID == baseID && s.ProductoID == it.ItemID);
                    if (stock != null) { stock.CantidadBuenas -= (int)it.Cantidad; stock.FechaUltimaModif = AppHelper.Ahora; }

                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoSalidaID, TipoItem = "Producto",
                        ProductoID = it.ItemID, MaterialID = null,
                        BaseOrigenID = baseID, BaseDestinoID = null,
                        Cantidad = it.Cantidad, Costo = it.PrecioUnitario,
                        EntregaID = entregaID, ProduccionID = null, LoteID = loteEntID,
                        Observaciones = obsEntrega, RegistradoPorID = claveID,
                        FechaMovimiento = AppHelper.Ahora
                    });
                }
                else if (it.TipoItem == "MATERIAL")
                {
                    decimal cantBase = it.Cantidad * it.Factor;
                    decimal actualMat = db.ExecuteQuery<decimal>(
                        "SELECT CantidadActual FROM dbo.StockMateriales WITH (UPDLOCK, HOLDLOCK) WHERE BaseID={0} AND MaterialID={1}",
                        baseID, it.ItemID).FirstOrDefault();
                    if (actualMat < cantBase)
                        throw new InvalidOperationException(string.Format(
                            "Stock insuficiente para material #{0}: disponible {1:N4}, requerido {2:N4}.",
                            it.ItemID, actualMat, cantBase));

                    var stock = db.StockMateriales.FirstOrDefault(s => s.BaseID == baseID && s.MaterialID == it.ItemID);
                    if (stock != null) { stock.CantidadActual -= cantBase; stock.FechaUltimaModif = AppHelper.Ahora; }

                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoSalidaID, TipoItem = "Material",
                        MaterialID = it.ItemID, ProductoID = null,
                        BaseOrigenID = baseID, BaseDestinoID = null,
                        Cantidad = cantBase, Costo = it.PrecioUnitario,
                        EntregaID = entregaID, ProduccionID = null, LoteID = loteEntID,
                        Observaciones = obsEntrega, RegistradoPorID = claveID,
                        FechaMovimiento = AppHelper.Ahora
                    });
                }
            }
        }

        private void DevolverStockYRegistrarMovimientos(InventarioAnkhalDBDataContext db,
            int entregaID, int baseID, List<ItemEntregaModel> items, int tipoAjusteID)
        {
            int claveID = Convert.ToInt32(Session["ClaveID"]);
            foreach (var it in items)
            {
                if (it.TipoItem == "PRODUCTO")
                {
                    var stock = db.StockProductos.FirstOrDefault(s => s.BaseID == baseID && s.ProductoID == it.ItemID);
                    if (stock != null) { stock.CantidadBuenas += (int)it.Cantidad; stock.FechaUltimaModif = AppHelper.Ahora; }
                    else db.StockProductos.InsertOnSubmit(new StockProductos
                    {
                        BaseID = baseID, ProductoID = it.ItemID,
                        CantidadBuenas = (int)it.Cantidad, CantidadRechazo = 0, FechaUltimaModif = AppHelper.Ahora
                    });
                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoAjusteID, TipoItem = "Producto",
                        ProductoID = it.ItemID, MaterialID = null,
                        BaseOrigenID = null, BaseDestinoID = baseID,
                        Cantidad = it.Cantidad, Costo = it.PrecioUnitario,
                        EntregaID = entregaID, ProduccionID = null,
                        Observaciones = string.Format("Devolución por cancelación entrega #{0}", entregaID),
                        RegistradoPorID = claveID, FechaMovimiento = AppHelper.Ahora
                    });
                }
                else if (it.TipoItem == "MATERIAL")
                {
                    decimal cantBase = it.Cantidad * it.Factor;
                    var stock = db.StockMateriales.FirstOrDefault(s => s.BaseID == baseID && s.MaterialID == it.ItemID);
                    if (stock != null) { stock.CantidadActual += cantBase; stock.FechaUltimaModif = AppHelper.Ahora; }
                    else db.StockMateriales.InsertOnSubmit(new StockMateriales
                    {
                        BaseID = baseID, MaterialID = it.ItemID, CantidadActual = cantBase, FechaUltimaModif = AppHelper.Ahora
                    });
                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoAjusteID, TipoItem = "Material",
                        MaterialID = it.ItemID, ProductoID = null,
                        BaseOrigenID = null, BaseDestinoID = baseID,
                        Cantidad = cantBase, Costo = it.PrecioUnitario,
                        EntregaID = entregaID, ProduccionID = null,
                        Observaciones = string.Format("Devolución por cancelación entrega #{0}", entregaID),
                        RegistradoPorID = claveID, FechaMovimiento = AppHelper.Ahora
                    });
                }
            }
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private class ItemModalNuevo
        {
            public string  TipoItem       { get; set; }
            public int     ItemID         { get; set; }
            public string  Nombre         { get; set; }
            public decimal Cantidad       { get; set; }   // cantidad capturada (antes del factor)
            public decimal PrecioUnitario { get; set; }
            public string  UnidadVal      { get; set; }   // "base:{id}" o "conv:{id}"
            public decimal Factor         { get; set; } = 1m;
            public string  UnidadTexto    { get; set; }
        }

        private List<ItemModalNuevo> LeerItemsModalNuevo()
        {
            string json = hdnItemsJson.Value;
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                SetMsg("warning", "Sin ítems", "Agregue al menos un ítem a la orden.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return null;
            }
            try { return _json.Deserialize<List<ItemModalNuevo>>(json); }
            catch
            {
                SetMsg("error", "Error", "Error al leer los ítems.", "modalNuevo");
                return null;
            }
        }

        private bool ValidarCamposModalNuevo(out int baseID, out int clienteID, out DateTime fecha)
        {
            baseID = 0; clienteID = 0; fecha = AppHelper.Hoy;
            if (!int.TryParse(ddlNuevoBase.SelectedValue, out baseID) || baseID == 0)
            {
                SetMsg("warning", "Campo requerido", "Seleccione la base origen.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            if (!int.TryParse(ddlNuevoCliente.SelectedValue, out clienteID) || clienteID == 0)
            {
                SetMsg("warning", "Campo requerido", "Seleccione el cliente.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            if (!DateTime.TryParse(txtNuevoFecha.Text, out fecha))
            {
                SetMsg("warning", "Campo requerido", "Ingrese una fecha válida.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            return true;
        }

        private void LimpiarModalNuevo()
        {
            txtNuevoFolio.Text            = "";
            txtNuevoFecha.Text            = AppHelper.Hoy.ToString("yyyy-MM-dd");
            ddlNuevoBase.SelectedIndex    = 0;
            ddlNuevoCliente.SelectedIndex = 0;
            txtNuevoObservaciones.Text    = "";
            chkEsCreditoOrden.Checked     = false;
            hdnItemsJson.Value            = "[]";
        }

        private void SetMsg(string icon, string title, string text, string modal = null, object confirmar = null)
        {
            hdnMensajePendiente.Value = _json.Serialize(new { icon, title, text, modal = modal ?? "", confirmar });
        }

        private int? ObtenerUnidadCapturaIDEntrega(int matID, string selectedVal,
            InventarioAnkhalDBDataContext db)
        {
            if (string.IsNullOrEmpty(selectedVal) || selectedVal.StartsWith("base:"))
                return null;
            if (selectedVal.StartsWith("conv:"))
            {
                int convID;
                if (!int.TryParse(selectedVal.Substring(5), out convID)) return null;
                var conv = db.ConversionesMaterial
                    .FirstOrDefault(c => c.ConversionID == convID && c.MaterialID == matID && c.Activo);
                return conv?.UnidadOrigenID;
            }
            return null;
        }

        // ══ WebMethod: búsqueda AJAX de productos / materiales ════════════════
        [WebMethod(EnableSession = true)]
        public static object BuscarItems(string query, string tipo)
        {
            if (System.Web.HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado.");

            query = (query ?? "").Trim();
            var ci = System.Globalization.CultureInfo.InvariantCulture;

            using (var db = new InventarioAnkhalDBDataContext(
                ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString))
            {
                db.ObjectTrackingEnabled = false;

                if (tipo == "PRODUCTO")
                {
                    return db.Productos
                        .Where(p => p.Activo && (p.Descripcion.Contains(query) || p.Codigo.Contains(query)))
                        .OrderBy(p => p.Codigo).Take(15)
                        .Select(p => new { id = p.ProductoID, nombre = "[" + p.Codigo + "] " + p.Descripcion, precio = p.PrecioVenta })
                        .ToList();
                }

                // MATERIAL: devuelve nombre + conversiones disponibles (igual que Entregas.aspx)
                var mats = db.Materiales
                    .Where(m => m.Activo && (m.Descripcion.Contains(query) || m.Codigo.Contains(query)))
                    .OrderBy(m => m.Codigo).Take(15).ToList();

                if (!mats.Any()) return new object[0];

                var matIDs   = mats.Select(m => m.MaterialID).ToList();
                var unidades = db.UnidadesMedida.ToDictionary(u => u.UnidadMedidaID,
                    u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre);
                var convs = (from c in db.ConversionesMaterial
                             where c.Activo && matIDs.Contains(c.MaterialID)
                             join u in db.UnidadesMedida on c.UnidadOrigenID equals u.UnidadMedidaID
                             select new { c.MaterialID, c.ConversionID, c.Factor,
                                          u.Clave, NombreUnidad = string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre })
                            .ToList();

                var resultado = new System.Collections.Generic.List<object>();
                foreach (var m in mats)
                {
                    string claveBase = (m.UnidadMedidaID.HasValue && unidades.ContainsKey(m.UnidadMedidaID.Value))
                        ? unidades[m.UnidadMedidaID.Value] : (m.Unidad ?? "");

                    var opciones = new System.Collections.Generic.List<object>();
                    if (m.UnidadMedidaID.HasValue)
                        opciones.Add(new { valor = "base:" + m.UnidadMedidaID.Value, texto = claveBase + " (base)", factor = 1.0 });

                    foreach (var c in convs.Where(x => x.MaterialID == m.MaterialID))
                    {
                        string factorStr = (c.Factor == Math.Floor(c.Factor))
                            ? ((long)c.Factor).ToString(ci) : c.Factor.ToString("G6", ci);
                        opciones.Add(new
                        {
                            valor  = "conv:" + c.ConversionID,
                            texto  = c.NombreUnidad + " (= " + factorStr + " " + claveBase + ")",
                            factor = (double)c.Factor
                        });
                    }

                    resultado.Add(new
                    {
                        id           = m.MaterialID,
                        nombre       = "[" + m.Codigo + "] " + m.Descripcion,
                        precio       = m.PrecioUnitario,
                        conversiones = opciones
                    });
                }
                return resultado;
            }
        }

        // ══ WebMethods: detalle e impresión de entrega vinculada ══════════

        [WebMethod(EnableSession = true)]
        public static object ObtenerDetalleEntrega(int entregaID)
        {
            if (System.Web.HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado.");

            var connStr = ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                db.ObjectTrackingEnabled = false;

                var entRaw = (from e in db.Entregas
                              where e.EntregaID == entregaID
                              join b in db.Bases on e.BaseOrigenID equals b.BaseID
                              select new {
                                  e.EntregaID, e.Folio, e.FechaEntrega,
                                  BaseNombre = b.Nombre, e.Cliente, e.ClienteID,
                                  e.Estado, e.Observaciones, e.NumeroFactura
                              }).FirstOrDefault();
                if (entRaw == null) return null;

                string clienteNombre = entRaw.Cliente ?? "";
                if (entRaw.ClienteID.HasValue)
                {
                    var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == entRaw.ClienteID.Value);
                    if (cli != null) clienteNombre = cli.Nombre;
                }

                var dets = db.DetalleEntregas
                    .Where(d => d.EntregaID == entregaID)
                    .OrderBy(d => d.DetalleEntregaID).ToList();

                // Unidades de captura
                var ucIDs = dets.Where(d => d.UnidadCapturaID.HasValue).Select(d => d.UnidadCapturaID.Value).Distinct().ToList();
                var ucNom = ucIDs.Any()
                    ? db.UnidadesMedida.Where(u => ucIDs.Contains(u.UnidadMedidaID))
                        .ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                    : new Dictionary<int, string>();

                // Unidades base de materiales
                var matIDs = dets.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                 .Select(d => d.MaterialID.Value).Distinct().ToList();
                var matUB  = new Dictionary<int, string>();
                if (matIDs.Any())
                {
                    var mInfo = db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID }).ToList();
                    var ubIDs = mInfo.Where(m => m.UnidadMedidaID.HasValue).Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNom = ubIDs.Any()
                        ? db.UnidadesMedida.Where(u => ubIDs.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                        : new Dictionary<int, string>();
                    foreach (var m in mInfo)
                        matUB[m.MaterialID] = (m.UnidadMedidaID.HasValue && ubNom.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNom[m.UnidadMedidaID.Value] : (m.Unidad ?? "");
                }

                var items = new List<object>();
                foreach (var d in dets)
                {
                    string nombre = d.TipoItem;
                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                    {
                        var p = db.Productos.FirstOrDefault(x => x.ProductoID == d.ProductoID.Value);
                        if (p != null) nombre = string.Format("[{0}] {1}", p.Codigo, p.Descripcion);
                    }
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                    {
                        var m = db.Materiales.FirstOrDefault(x => x.MaterialID == d.MaterialID.Value);
                        if (m != null) nombre = string.Format("[{0}] {1}", m.Codigo, m.Descripcion);
                    }
                    bool tuvoConv = d.TipoItem == "MATERIAL" && d.CantidadCapturada.HasValue
                        && d.UnidadCapturaID.HasValue && d.CantidadCapturada.Value != d.Cantidad;
                    decimal cantCap  = d.CantidadCapturada ?? d.Cantidad;
                    string  unidCap  = (tuvoConv && d.UnidadCapturaID.HasValue && ucNom.ContainsKey(d.UnidadCapturaID.Value))
                                       ? ucNom[d.UnidadCapturaID.Value] : "";
                    string  unidBase = (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue && matUB.ContainsKey(d.MaterialID.Value))
                                       ? matUB[d.MaterialID.Value] : "";
                    items.Add(new {
                        TipoItem = d.TipoItem, Nombre = nombre,
                        Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario,
                        CantidadCapturada = cantCap, UnidadCaptura = unidCap,
                        TuvoConversion = tuvoConv, UnidadBase = unidBase
                    });
                }

                return new {
                    Folio         = entRaw.Folio,
                    Fecha         = entRaw.FechaEntrega.ToString("dd/MM/yyyy"),
                    Base          = entRaw.BaseNombre,
                    Cliente       = clienteNombre,
                    Estado        = entRaw.Estado,
                    NumeroFactura = entRaw.NumeroFactura ?? "",
                    Obs           = entRaw.Observaciones ?? "",
                    Items         = items
                };
            }
        }

        [WebMethod(EnableSession = true)]
        public static string GenerarHtmlImpresionEntrega(int entregaID)
        {
            if (System.Web.HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado.");

            var connStr = ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                db.ObjectTrackingEnabled = false;

                var entRaw = (from e in db.Entregas
                              where e.EntregaID == entregaID
                              join b in db.Bases on e.BaseOrigenID equals b.BaseID
                              select new {
                                  e.Folio, e.FechaEntrega, BaseNombre = b.Nombre,
                                  e.Cliente, e.ClienteID, e.Estado,
                                  e.Observaciones, e.NumeroFactura
                              }).FirstOrDefault();
                if (entRaw == null) return "<html><body>Entrega no encontrada.</body></html>";

                string clienteNombre = entRaw.Cliente ?? "—";
                if (entRaw.ClienteID.HasValue)
                {
                    var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == entRaw.ClienteID.Value);
                    if (cli != null) clienteNombre = cli.Nombre;
                }

                var dets = db.DetalleEntregas.Where(d => d.EntregaID == entregaID)
                    .OrderBy(d => d.DetalleEntregaID).ToList();

                var ucIDsP = dets.Where(d => d.UnidadCapturaID.HasValue).Select(d => d.UnidadCapturaID.Value).Distinct().ToList();
                var ucNomP = ucIDsP.Any()
                    ? db.UnidadesMedida.Where(u => ucIDsP.Contains(u.UnidadMedidaID))
                        .ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                    : new Dictionary<int, string>();

                var matIDsP = dets.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                  .Select(d => d.MaterialID.Value).Distinct().ToList();
                var matUBP  = new Dictionary<int, string>();
                if (matIDsP.Any())
                {
                    var mP   = db.Materiales.Where(m => matIDsP.Contains(m.MaterialID))
                               .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID }).ToList();
                    var ubP  = mP.Where(m => m.UnidadMedidaID.HasValue).Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNP = ubP.Any()
                        ? db.UnidadesMedida.Where(u => ubP.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID, u => string.IsNullOrEmpty(u.Nombre) ? u.Clave : u.Nombre)
                        : new Dictionary<int, string>();
                    foreach (var m in mP)
                        matUBP[m.MaterialID] = (m.UnidadMedidaID.HasValue && ubNP.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNP[m.UnidadMedidaID.Value] : (m.Unidad ?? "");
                }

                var sb = new StringBuilder();
                sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'/>");
                sb.AppendFormat("<title>Entrega {0}</title>", entRaw.Folio);
                sb.Append("<style>body{font-family:Arial,sans-serif;margin:30px;color:#1a1a1a}");
                sb.Append(".header{text-align:center;border-bottom:3px solid #003366;padding-bottom:12px;margin-bottom:20px}");
                sb.Append(".header img{height:60px;margin-bottom:6px}.header h2{margin:0;color:#003366;font-size:1.3rem;letter-spacing:1px}");
                sb.Append(".meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:18px;font-size:.9rem;background:#f8f9fa;padding:12px;border-radius:6px}");
                sb.Append(".meta label{font-weight:bold;color:#003366}");
                sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:24px}");
                sb.Append("thead th{background:#003366;color:#fff;padding:8px 10px;font-size:.88rem;text-align:left}thead th.r{text-align:right}");
                sb.Append("tbody td{padding:7px 10px;border-bottom:1px solid #ddd;font-size:.88rem}tbody td.r{text-align:right}");
                sb.Append("tbody tr:nth-child(even){background:#f5f7fa}");
                sb.Append(".total-row{background:#003366!important;color:#fff;font-weight:bold}.total-row td{padding:8px 10px;text-align:right;border:none}");
                sb.Append(".firmas{display:flex;gap:40px;margin-top:50px}.firma{flex:1;border-top:2px solid #555;padding-top:6px;text-align:center;font-size:.85rem;color:#555}");
                sb.Append(".cap-info{display:block;font-size:.76rem;color:#666;font-weight:normal}");
                sb.Append(".no-print{text-align:center;margin-top:24px}");
                sb.Append(".btn-p{padding:8px 24px;background:#003366;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem;margin-right:8px}");
                sb.Append(".btn-c{padding:8px 24px;background:#6c757d;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem}");
                sb.Append("@media print{.no-print{display:none!important}}</style></head><body>");

                sb.Append("<div class='header'><img src='img/ankhal.png' alt='ANKHAL' onerror=\"this.style.display='none'\"/>");
                sb.Append("<h2>COMPROBANTE DE ENTREGA — GRUPO ANKHAL</h2></div>");

                sb.Append("<div class='meta'>");
                sb.AppendFormat("<div><label>Folio:</label> {0}</div>",         entRaw.Folio);
                sb.AppendFormat("<div><label>Fecha:</label> {0:dd/MM/yyyy}</div>", entRaw.FechaEntrega);
                sb.AppendFormat("<div><label>Base Origen:</label> {0}</div>",   entRaw.BaseNombre);
                sb.AppendFormat("<div><label>Cliente:</label> {0}</div>",       clienteNombre);
                sb.AppendFormat("<div><label>Estado:</label> {0}</div>",        entRaw.Estado);
                sb.AppendFormat("<div><label>Factura:</label> {0}</div>",       entRaw.NumeroFactura ?? "—");
                sb.AppendFormat("<div><label>Observaciones:</label> {0}</div>", entRaw.Observaciones ?? "—");
                sb.Append("</div>");

                sb.Append("<table><thead><tr><th>Tipo</th><th>Descripción</th><th class='r'>Cantidad</th><th class='r'>Precio Unit.</th><th class='r'>Subtotal</th></tr></thead><tbody>");

                decimal totalG = 0;
                foreach (var d in dets)
                {
                    string nom = d.TipoItem;
                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                    {
                        var p = db.Productos.FirstOrDefault(x => x.ProductoID == d.ProductoID.Value);
                        if (p != null) nom = string.Format("[{0}] {1}", p.Codigo, p.Descripcion);
                    }
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                    {
                        var m = db.Materiales.FirstOrDefault(x => x.MaterialID == d.MaterialID.Value);
                        if (m != null) nom = string.Format("[{0}] {1}", m.Codigo, m.Descripcion);
                    }

                    string cantHtml = d.Cantidad.ToString("0.##");
                    if (d.TipoItem == "MATERIAL")
                    {
                        string ubN = (d.MaterialID.HasValue && matUBP.ContainsKey(d.MaterialID.Value)) ? matUBP[d.MaterialID.Value] : "";
                        bool hasConv = d.CantidadCapturada.HasValue && d.UnidadCapturaID.HasValue && d.CantidadCapturada.Value != d.Cantidad;
                        if (hasConv)
                        {
                            string ucN = ucNomP.ContainsKey(d.UnidadCapturaID.Value) ? ucNomP[d.UnidadCapturaID.Value] : "";
                            cantHtml = string.Format("<strong>{0} {1}</strong><span class='cap-info'>= {2} {3}</span>",
                                d.CantidadCapturada.Value.ToString("0.##"), System.Web.HttpUtility.HtmlEncode(ucN),
                                d.Cantidad.ToString("0.##"), System.Web.HttpUtility.HtmlEncode(ubN));
                        }
                        else if (!string.IsNullOrEmpty(ubN))
                            cantHtml = string.Format("{0} {1}", d.Cantidad.ToString("0.##"), System.Web.HttpUtility.HtmlEncode(ubN));
                    }

                    decimal sub = d.Cantidad * d.PrecioUnitario;
                    totalG += sub;
                    sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td class='r'>{2}</td><td class='r'>{3:C2}</td><td class='r'>{4:C2}</td></tr>",
                        d.TipoItem, nom, cantHtml, d.PrecioUnitario, sub);
                }
                sb.Append("</tbody>");
                sb.AppendFormat("<tr class='total-row'><td colspan='4'>TOTAL</td><td>{0:C2}</td></tr>", totalG);
                sb.Append("</table>");
                sb.Append("<div class='firmas'><div class='firma'>Entregó</div><div class='firma'>Recibió</div></div>");
                sb.Append("<div class='no-print'><button class='btn-p' onclick='window.print()'>&#128424; Imprimir</button>");
                sb.Append("<button class='btn-c' onclick='window.close()'>Cerrar</button></div>");
                sb.Append("</body></html>");
                return sb.ToString();
            }
        }
    }
}
