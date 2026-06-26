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
using GrupoAnkhalInventario.Services;

namespace GrupoAnkhalInventario
{
    public partial class Entregas : System.Web.UI.Page
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
        public class EntregaVM
        {
            public int      EntregaID     { get; set; }
            public string   Folio         { get; set; }
            public DateTime FechaEntrega  { get; set; }
            public string   BaseNombre    { get; set; }
            public string   ClienteNombre { get; set; }
            public int      NumItems      { get; set; }
            public decimal  TotalValor    { get; set; }
            public string   Estado        { get; set; }
        }

        // Modelo de item para serializar/deserializar JSON (modal ↔ servidor)
        private class ItemEntregaModel
        {
            public string  TipoItem                { get; set; }
            public int     ItemID                  { get; set; }
            public string  Nombre                  { get; set; }
            public decimal Cantidad                { get; set; }
            public decimal PrecioUnitario          { get; set; }
            public string  UnidadVal               { get; set; }   // "base:{id}" o "conv:{id}"
            public decimal Factor                  { get; set; } = 1m;
            public string  UnidadTexto             { get; set; }
            public int?    UnidadCapturaIDDirecto  { get; set; }   // poblado al leer de DetalleEntregas
        }

        // Modelo del detalle completo para el modal de ver detalle
        private class EntregaDetalleJson
        {
            public int    EntregaID  { get; set; }
            public string Folio      { get; set; }
            public string Fecha      { get; set; }
            public string Base       { get; set; }
            public string Cliente    { get; set; }
            public string Estado     { get; set; }
            public string Registrado { get; set; }
            public string Obs        { get; set; }
            public List<ItemDetalleJson> Items { get; set; }
        }

        private class ItemDetalleJson
        {
            public string  TipoItem          { get; set; }
            public string  Nombre            { get; set; }
            public decimal Cantidad          { get; set; }
            public decimal PrecioUnitario    { get; set; }
            public decimal CantidadCapturada { get; set; }
            public string  UnidadCaptura     { get; set; }   // nombre unidad capturada (ej. "Cubeta/s")
            public bool    TuvoConversion    { get; set; }
            public string  UnidadBase        { get; set; }   // nombre unidad base del material (ej. "Litro/s")
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
                // Restaurar paginación
                if (ViewState["TotalRegistros"] != null)
                    gvEntregas.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ══ Catálogos ════════════════════════════════════════════════════════
        private void CargarCatalogos()
        {
            using (var db = NuevoDb(false))
            {
                // Bases para filtro y modal (filtradas por permisos del usuario)
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

                // Clientes para modal
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
        private IQueryable<Modelo.Entregas> AplicarFiltros(IQueryable<Modelo.Entregas> q)
        {
            // Excluir entregas vinculadas a una Orden (se gestionan desde Ordenes.aspx)
            q = q.Where(e => e.OrdenID == null);

            // Restringir por las bases del usuario (null = Administrador, ve todo)
            var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            if (basesUsuario != null)
                q = q.Where(e => basesUsuario.Contains(e.BaseOrigenID));

            if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
            {
                int id = int.Parse(ddlFiltrBase.SelectedValue);
                q = q.Where(e => e.BaseOrigenID == id);
            }
            if (!string.IsNullOrEmpty(ddlFiltrEstado.SelectedValue))
                q = q.Where(e => e.Estado == ddlFiltrEstado.SelectedValue);

            if (!string.IsNullOrEmpty(txtFiltrCliente.Text.Trim()))
            {
                string cli = txtFiltrCliente.Text.Trim().ToLower();
                q = q.Where(e => e.Cliente != null && e.Cliente.ToLower().Contains(cli));
            }
            if (!string.IsNullOrEmpty(txtFiltrFolio.Text.Trim()))
            {
                string folio = txtFiltrFolio.Text.Trim().ToUpper();
                q = q.Where(e => e.Folio.Contains(folio));
            }
            if (!string.IsNullOrEmpty(txtFiltrDesde.Text))
            {
                DateTime d = DateTime.Parse(txtFiltrDesde.Text);
                q = q.Where(e => e.FechaEntrega >= d);
            }
            if (!string.IsNullOrEmpty(txtFiltrHasta.Text))
            {
                DateTime h = DateTime.Parse(txtFiltrHasta.Text);
                q = q.Where(e => e.FechaEntrega <= h);
            }
            return q;
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                var q = AplicarFiltros(db.Entregas.AsQueryable());

                var data = q.Select(e => new { e.Estado, e.EntregaID }).ToList();
                int total       = data.Count;
                int programadas = data.Count(x => x.Estado == "PROGRAMADA");
                int entregadas  = data.Count(x => x.Estado == "ENTREGADA");
                int canceladas  = data.Count(x => x.Estado == "CANCELADA");

                // Valor total de las entregadas en el período
                var entregadasIDs = data.Where(x => x.Estado == "ENTREGADA").Select(x => x.EntregaID).ToList();
                decimal valorTotal = 0;
                if (entregadasIDs.Any())
                {
                    valorTotal = db.DetalleEntregas
                        .Where(d => entregadasIDs.Contains(d.EntregaID))
                        .Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0m;
                }

                lblTotalHoy.Text    = total.ToString("N0");
                lblProgramadas.Text = programadas.ToString("N0");
                lblEntregadas.Text  = entregadas.ToString("N0");
                lblCanceladas.Text  = canceladas.ToString("N0");
                lblValorTotal.Text  = valorTotal.ToString("$#,##0.00");
            }
        }

        // ══ Grid ═════════════════════════════════════════════════════════════
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                var q = AplicarFiltros(db.Entregas.AsQueryable());

                int total   = q.Count();
                int pageIdx = gvEntregas.PageIndex;
                int pageSz  = gvEntregas.PageSize;
                gvEntregas.VirtualItemCount = total;
                ViewState["TotalRegistros"] = total;

                lblResultados.Text = total == 0
                    ? "Sin entregas para los filtros aplicados."
                    : string.Format("{0} entrega(s) encontrada(s).", total);

                if (total == 0)
                {
                    gvEntregas.DataSource = new List<EntregaVM>();
                    gvEntregas.DataBind();
                    return;
                }

                // IDs de la página actual
                var ids = q
                    .OrderByDescending(e => e.FechaEntrega)
                    .ThenByDescending(e => e.EntregaID)
                    .Select(e => e.EntregaID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                // Datos con JOINs
                var raw = (from e  in db.Entregas
                           where ids.Contains(e.EntregaID)
                           join b  in db.Bases    on e.BaseOrigenID equals b.BaseID
                           select new
                           {
                               e.EntregaID,
                               e.Folio,
                               e.FechaEntrega,
                               BaseNombre = b.Nombre,
                               e.Cliente,
                               e.ClienteID,
                               e.Estado
                           }).ToList();

                // Contar items y calcular total por entrega
                var rawIDs = raw.Select(r => r.EntregaID).ToList();
                var detalles = db.DetalleEntregas
                    .Where(d => rawIDs.Contains(d.EntregaID))
                    .Select(d => new { d.EntregaID, d.Cantidad, d.PrecioUnitario })
                    .ToList();

                // Nombres de clientes del catálogo
                var clienteIDs = raw.Where(r => r.ClienteID.HasValue)
                                    .Select(r => r.ClienteID.Value).Distinct().ToList();
                var nombresClientes = new Dictionary<int, string>();
                if (clienteIDs.Any())
                {
                    nombresClientes = db.Clientes
                        .Where(c => clienteIDs.Contains(c.ClienteID))
                        .ToDictionary(c => c.ClienteID, c => c.Nombre);
                }

                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.EntregaID == id))
                    .Where(r => r != null)
                    .Select(r =>
                    {
                        var dets    = detalles.Where(d => d.EntregaID == r.EntregaID).ToList();
                        int numItems = dets.Count;
                        decimal val = dets.Sum(d => d.Cantidad * d.PrecioUnitario);

                        // Nombre del cliente: del catálogo si hay ClienteID, sino texto libre
                        string clienteNombre = r.Cliente ?? "";
                        if (r.ClienteID.HasValue && nombresClientes.ContainsKey(r.ClienteID.Value))
                            clienteNombre = nombresClientes[r.ClienteID.Value];

                        return new EntregaVM
                        {
                            EntregaID     = r.EntregaID,
                            Folio         = r.Folio,
                            FechaEntrega  = r.FechaEntrega,
                            BaseNombre    = r.BaseNombre,
                            ClienteNombre = clienteNombre,
                            NumItems      = numItems,
                            TotalValor    = val,
                            Estado        = r.Estado
                        };
                    }).ToList();

                gvEntregas.DataSource = pagina;
                gvEntregas.DataBind();
            }
        }

        // ══ Helpers de columnas del grid (llamados desde TemplateField) ══════
        public string GetBadgeEstado(object estado)
        {
            switch ((estado ?? "").ToString())
            {
                case "PROGRAMADA":      return "badge badge-programada";
                case "ENTREGADA":       return "badge badge-entregada";
                case "CANCELADA":       return "badge badge-cancelada";
                case "PENDIENTE_STOCK": return "badge badge-pendiente-stock";
                default:                return "badge badge-secondary";
            }
        }

        public string MostrarBtnConfirmar(string estado, string entregaID)
        {
            if (estado == "PROGRAMADA" || estado == "PENDIENTE_STOCK")
            {
                return string.Format(
                    "<button type='button' class='btn btn-xs btn-success' " +
                    "onclick=\"confirmarEntrega({0}, '{1}')\"><i class='fas fa-check'></i> Confirmar</button>",
                    entregaID, "");
            }
            return "";
        }

        public string MostrarBtnCancelar(string estado, string entregaID)
        {
            if (estado == "CANCELADA") return "";
            return string.Format(
                "<button type='button' class='btn btn-xs btn-danger' " +
                "onclick=\"cancelarEntrega({0}, '{1}')\"><i class='fas fa-ban'></i></button>",
                entregaID, "");
        }

        // ══ Eventos filtros / paginación ══════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvEntregas.PageIndex = 0;
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
            gvEntregas.PageIndex = 0;
            CargarDashboard();
            CargarGrid();
        }

        protected void gvEntregas_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvEntregas.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // ══ Abrir modal Nuevo ════════════════════════════════════════════════
        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarModalNuevo();
            // Generar folio tentativo
            txtNuevoFolio.Text = GenerarFolioTentativo();
            ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
        }

        private string GenerarFolioTentativo()
        {
            using (var db = NuevoDb(false))
            {
                // Contar las entregas del día de hoy (sin importar estado)
                DateTime hoy = AppHelper.Hoy;
                int count = db.Entregas
                    .Count(e => e.FechaEntrega >= hoy && e.FechaEntrega < hoy.AddDays(1));
                return string.Format("ENT-{0}-{1:D3}", AppHelper.Hoy.ToString("yyyyMMdd"), count + 1);
            }
        }

        // ══ Guardar como PROGRAMADA ══════════════════════════════════════════
        protected void btnGuardarProgramada_Click(object sender, EventArgs e)
        {
            var items = LeerItemsDelJson();
            if (items == null) return;

            int baseID, clienteID;
            DateTime fecha;
            if (!ValidarCamposModal(out baseID, out clienteID, out fecha)) return;

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
                            var entrega = CrearEntregaEntity(db, baseID, clienteID, fecha, "PROGRAMADA", chkEsCredito.Checked);
                            db.Entregas.InsertOnSubmit(entrega);
                            db.SubmitChanges(); // Obtener EntregaID

                            InsertarDetalles(db, entrega.EntregaID, items);
                            db.SubmitChanges();
                            tx.Commit();

                            LimpiarModalNuevo();
                            SetMsg("success", "Entrega programada",
                                string.Format("Se guardó el folio {0} como PROGRAMADA. " +
                                    "Confírmela cuando esté lista para descontar el stock.",
                                    entrega.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al guardar", "Ocurrió un error: " + ex.Message, "modalNuevo");
            }
        }

        // ══ Confirmar y Entregar (desde modal: nueva entrega directa) ════════
        protected void btnConfirmarEntregar_Click(object sender, EventArgs e)
        {
            var items = LeerItemsDelJson();
            if (items == null) return;

            int baseID, clienteID;
            DateTime fecha;
            if (!ValidarCamposModal(out baseID, out clienteID, out fecha)) return;

            // Pre-validar stock antes de tocar la BD
            using (var dbVal = NuevoDb(false))
            {
                string errStock = ValidarStockItems(dbVal, items, baseID);
                if (errStock != null)
                {
                    SetMsg("warning", "Stock insuficiente", errStock, "modalNuevo");
                    return;
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
                            var entrega = CrearEntregaEntity(db, baseID, clienteID, fecha, "ENTREGADA", chkEsCredito.Checked);
                            db.Entregas.InsertOnSubmit(entrega);
                            db.SubmitChanges(); // Obtener EntregaID

                            InsertarDetalles(db, entrega.EntregaID, items);

                            // Descontar stock y registrar movimientos SALIDA
                            int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");
                            DescontarStockYRegistrarMovimientos(db, entrega.EntregaID, baseID, items, tipoSalidaID, entrega.Observaciones);

                            db.SubmitChanges();

                            if (chkEsCredito.Checked)
                            {
                                bool forzarCredito = hdnForzarLimiteCredito.Value == "true";
                                hdnForzarLimiteCredito.Value = "";
                                string motivoLimite = string.IsNullOrWhiteSpace(hdnMotivoLimiteCredito.Value) ? null : hdnMotivoLimiteCredito.Value.Trim();
                                hdnMotivoLimiteCredito.Value = "";
                                decimal montoNuevo = items.Sum(i => i.Cantidad * i.PrecioUnitario);
                                var infoCredito = CuentasPorCobrarHelper.EvaluarLimiteCredito(db, clienteID, montoNuevo);
                                if (infoCredito != null)
                                {
                                    if (!forzarCredito)
                                    {
                                        tx.Rollback();
                                        SetMsg("warning", "Límite de crédito excedido", CuentasPorCobrarHelper.FormatearMensaje(infoCredito),
                                            "modalNuevo", new { accion = "reintentar-confirmar-entregar" });
                                        return;
                                    }
                                    CuentasPorCobrarHelper.RegistrarBitacora(db, infoCredito, entrega.EntregaID, Convert.ToInt32(Session["ClaveID"]), motivoLimite);
                                }
                            }

                            CuentasPorCobrarHelper.GenerarSiAplica(db, entrega.EntregaID, Convert.ToInt32(Session["ClaveID"]));

                            tx.Commit();

                            LimpiarModalNuevo();
                            SetMsg("success", "Entrega confirmada",
                                string.Format("Folio {0} entregado correctamente. Stock descontado.", entrega.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al entregar", "Ocurrió un error: " + ex.Message, "modalNuevo");
            }
        }

        // ══ Acciones desde el grid (confirmar, cancelar, ver detalle, imprimir)
        protected void btnProcesarAccion_Click(object sender, EventArgs e)
        {
            string accion     = hdnAccion.Value;
            string idStr      = hdnEntregaIDAccion.Value;
            hdnAccion.Value   = "";

            int entregaID;
            if (!int.TryParse(idStr, out entregaID)) return;

            switch (accion)
            {
                case "confirmar": AccionConfirmar(entregaID); break;
                case "cancelar":  AccionCancelar(entregaID);  break;
                case "detalle":   AccionVerDetalle(entregaID); break;
                case "imprimir":  AccionImprimir(entregaID);   break;
            }
        }

        // ── Confirmar entrega existente (PROGRAMADA/PENDIENTE_STOCK → ENTREGADA)
        private void AccionConfirmar(int entregaID)
        {
            using (var dbVal = NuevoDb(false))
            {
                var entrega = dbVal.Entregas.FirstOrDefault(e => e.EntregaID == entregaID);
                if (entrega == null) return;
                if (entrega.Estado == "ENTREGADA" || entrega.Estado == "CANCELADA")
                {
                    SetMsg("info", "Sin cambios",
                        string.Format("El folio {0} ya tiene estado {1}.", entrega.Folio, entrega.Estado));
                    return;
                }

                var items = ObtenerItemsEntrega(dbVal, entregaID);
                string errStock = ValidarStockItems(dbVal, items, entrega.BaseOrigenID);
                if (errStock != null)
                {
                    // Marcar como PENDIENTE_STOCK
                    try
                    {
                        using (var db2 = NuevoDb(true))
                        {
                            var ent2 = db2.Entregas.First(e => e.EntregaID == entregaID);
                            ent2.Estado     = "PENDIENTE_STOCK";
                            ent2.FechaModif = AppHelper.Ahora;
                            db2.SubmitChanges();
                        }
                    }
                    catch { }
                    SetMsg("warning", "Stock insuficiente", errStock);
                    CargarDashboard();
                    CargarGrid();
                    return;
                }

                bool forzarCreditoChk = hdnForzarLimiteCredito.Value == "true";
                if (entrega.EsCredito && entrega.ClienteID.HasValue && !forzarCreditoChk)
                {
                    decimal montoChk = items.Sum(i => i.Cantidad * i.PrecioUnitario);
                    var infoChk = CuentasPorCobrarHelper.EvaluarLimiteCredito(dbVal, entrega.ClienteID.Value, montoChk);
                    if (infoChk != null)
                    {
                        SetMsg("warning", "Límite de crédito excedido", CuentasPorCobrarHelper.FormatearMensaje(infoChk),
                            null, new { accion = "reintentar-confirmar-grid", entregaId = entregaID });
                        CargarDashboard();
                        CargarGrid();
                        return;
                    }
                }
            }

            bool forzarCredito = hdnForzarLimiteCredito.Value == "true";
            hdnForzarLimiteCredito.Value = "";
            string motivoLimite = string.IsNullOrWhiteSpace(hdnMotivoLimiteCredito.Value) ? null : hdnMotivoLimiteCredito.Value.Trim();
            hdnMotivoLimiteCredito.Value = "";

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

                            DescontarStockYRegistrarMovimientos(db, entregaID, entrega.BaseOrigenID, items, tipoSalidaID, entrega.Observaciones);

                            entrega.Estado     = "ENTREGADA";
                            entrega.FechaModif = AppHelper.Ahora;

                            db.SubmitChanges();

                            if (forzarCredito && entrega.EsCredito && entrega.ClienteID.HasValue)
                            {
                                decimal montoNuevo = items.Sum(i => i.Cantidad * i.PrecioUnitario);
                                var infoCredito = CuentasPorCobrarHelper.EvaluarLimiteCredito(db, entrega.ClienteID.Value, montoNuevo);
                                if (infoCredito != null)
                                    CuentasPorCobrarHelper.RegistrarBitacora(db, infoCredito, entregaID, Convert.ToInt32(Session["ClaveID"]), motivoLimite);
                            }

                            CuentasPorCobrarHelper.GenerarSiAplica(db, entregaID, Convert.ToInt32(Session["ClaveID"]));

                            tx.Commit();

                            SetMsg("success", "Entrega confirmada",
                                string.Format("Folio {0} confirmado. Stock descontado correctamente.", entrega.Folio));
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                CargarDashboard();
                CargarGrid();
            }
            catch (Exception ex)
            {
                SetMsg("error", "Error al confirmar", "Ocurri\u00f3 un error: " + ex.Message);
            }
        }

        // ── Cancelar entrega
        private void AccionCancelar(int entregaID)
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

                            // Si estaba ENTREGADA, devolver stock y marcar movimientos SALIDA como anulados
                            if (entrega.Estado == "ENTREGADA")
                            {
                                var items = ObtenerItemsEntrega(db, entregaID);
                                int tipoAjusteID = ObtenerTipoMovimientoID(db, "AJUSTE_POS");
                                DevolverStockYRegistrarMovimientos(db, entregaID, entrega.BaseOrigenID, items, tipoAjusteID);

                                // Marcar los movimientos SALIDA originales como anulados
                                int tipoSalidaID = ObtenerTipoMovimientoID(db, "SALIDA");
                                var movsOriginal = db.Movimientos
                                    .Where(m => m.EntregaID == entregaID && m.TipoMovimientoID == tipoSalidaID)
                                    .ToList();
                                foreach (var mv in movsOriginal)
                                    mv.Observaciones = mv.Observaciones + " [ANULADO - entrega cancelada]";
                            }

                            CuentasPorCobrarHelper.CancelarCxCSiExiste(db, entregaID);

                            entrega.Estado     = "CANCELADA";
                            entrega.FechaModif = AppHelper.Ahora;
                            db.SubmitChanges();
                            tx.Commit();

                            SetMsg("success", "Entrega cancelada",
                                string.Format("Folio {0} cancelado.", entrega.Folio));
                            CargarDashboard();
                            CargarGrid();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
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

        // ── Ver detalle (carga JSON y JS abre el modal)
        private void AccionVerDetalle(int entregaID)
        {
            using (var db = NuevoDb(false))
            {
                var entrega = (from e  in db.Entregas
                               where e.EntregaID == entregaID
                               join b  in db.Bases on e.BaseOrigenID equals b.BaseID
                               select new
                               {
                                   e.EntregaID,
                                   e.Folio,
                                   e.FechaEntrega,
                                   BaseNombre = b.Nombre,
                                   e.Cliente,
                                   e.ClienteID,
                                   e.Estado,
                                   e.Observaciones,
                                   e.RegistradoPorID
                               }).FirstOrDefault();

                if (entrega == null) return;

                // Nombre del cliente
                string clienteNombre = entrega.Cliente ?? "";
                if (entrega.ClienteID.HasValue)
                {
                    var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == entrega.ClienteID.Value);
                    if (cli != null) clienteNombre = cli.Nombre;
                }

                // Nombre del usuario que registró (via API de Asistencia, cacheado)
                string registradoPor = entrega.RegistradoPorID.ToString();
                try
                {
                    var du = db.DatosUsuario.FirstOrDefault(u => u.ClaveID == entrega.RegistradoPorID);
                    if (du?.UsuarioID != null)
                    {
                        var emp = UsuarioService.ObtenerEmpleado(du.UsuarioID.Value);
                        registradoPor = emp.NombreCompleto;
                    }
                }
                catch { }

                // Items del detalle
                var detalles = (from d in db.DetalleEntregas
                                where d.EntregaID == entregaID
                                orderby d.DetalleEntregaID
                                select d).ToList();

                // Pre-cargar nombres de unidades de captura y base que aparecen en los detalles
                var ucIDs = detalles.Where(d => d.UnidadCapturaID.HasValue)
                                    .Select(d => d.UnidadCapturaID.Value).Distinct().ToList();

                var matIDs = detalles.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                     .Select(d => d.MaterialID.Value).Distinct().ToList();

                var ucNombres = ucIDs.Any()
                    ? db.UnidadesMedida
                        .Where(u => ucIDs.Contains(u.UnidadMedidaID))
                        .ToDictionary(u => u.UnidadMedidaID,
                                      u => !string.IsNullOrEmpty(u.Nombre) ? u.Nombre : u.Clave)
                    : new Dictionary<int, string>();

                // Unidad base por MaterialID (para mostrar "= N UnidadBase")
                var matUnidadBase = new Dictionary<int, string>();
                if (matIDs.Any())
                {
                    var matsInfo = db.Materiales
                        .Where(m => matIDs.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID })
                        .ToList();

                    var ubIDs = matsInfo.Where(m => m.UnidadMedidaID.HasValue)
                                        .Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNombres = ubIDs.Any()
                        ? db.UnidadesMedida
                            .Where(u => ubIDs.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID,
                                          u => !string.IsNullOrEmpty(u.Nombre) ? u.Nombre : u.Clave)
                        : new Dictionary<int, string>();

                    foreach (var m in matsInfo)
                    {
                        string nombre_ub = (m.UnidadMedidaID.HasValue && ubNombres.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNombres[m.UnidadMedidaID.Value]
                            : (m.Unidad ?? "");
                        matUnidadBase[m.MaterialID] = nombre_ub;
                    }
                }

                var itemsJson = new List<ItemDetalleJson>();
                foreach (var d in detalles)
                {
                    string nombre    = d.TipoItem;
                    string unidBase  = "";

                    if (d.TipoItem == "PRODUCTO" && d.ProductoID.HasValue)
                    {
                        var p = db.Productos.FirstOrDefault(x => x.ProductoID == d.ProductoID.Value);
                        if (p != null) nombre = string.Format("[{0}] {1}", p.Codigo, p.Descripcion);
                    }
                    else if (d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                    {
                        var m = db.Materiales.FirstOrDefault(x => x.MaterialID == d.MaterialID.Value);
                        if (m != null) nombre = string.Format("[{0}] {1}", m.Codigo, m.Descripcion);
                        if (matUnidadBase.ContainsKey(d.MaterialID.Value))
                            unidBase = matUnidadBase[d.MaterialID.Value];
                    }

                    // Leer directamente de los nuevos campos de DetalleEntregas
                    bool tuvoConv = d.TipoItem == "MATERIAL"
                                    && d.CantidadCapturada.HasValue
                                    && d.UnidadCapturaID.HasValue
                                    && d.CantidadCapturada.Value != d.Cantidad;

                    decimal cantCap = d.CantidadCapturada ?? d.Cantidad;
                    string  unidCap = (tuvoConv && ucNombres.ContainsKey(d.UnidadCapturaID.Value))
                                      ? ucNombres[d.UnidadCapturaID.Value] : "";

                    itemsJson.Add(new ItemDetalleJson
                    {
                        TipoItem          = d.TipoItem,
                        Nombre            = nombre,
                        Cantidad          = d.Cantidad,
                        PrecioUnitario    = d.PrecioUnitario,
                        CantidadCapturada = cantCap,
                        UnidadCaptura     = unidCap,
                        TuvoConversion    = tuvoConv,
                        UnidadBase        = unidBase
                    });
                }

                var dto = new EntregaDetalleJson
                {
                    EntregaID  = entrega.EntregaID,
                    Folio      = entrega.Folio,
                    Fecha      = entrega.FechaEntrega.ToString("dd/MM/yyyy"),
                    Base       = entrega.BaseNombre,
                    Cliente    = clienteNombre,
                    Estado     = entrega.Estado,
                    Registrado = registradoPor,
                    Obs        = entrega.Observaciones ?? "",
                    Items      = itemsJson
                };

                hdnDetalleJson.Value = _json.Serialize(dto);
                // El JavaScript en window.load lo leerá y abrirá el modal
            }
        }

        // ── Imprimir entrega (ventana nueva con HTML)
        private void AccionImprimir(int entregaID)
        {
            using (var db = NuevoDb(false))
            {
                var entrega = (from e  in db.Entregas
                               where e.EntregaID == entregaID
                               join b  in db.Bases on e.BaseOrigenID equals b.BaseID
                               select new
                               {
                                   e.EntregaID,
                                   e.Folio,
                                   e.FechaEntrega,
                                   BaseNombre = b.Nombre,
                                   e.Cliente,
                                   e.ClienteID,
                                   e.Estado,
                                   e.Observaciones
                               }).FirstOrDefault();

                if (entrega == null) return;

                string clienteNombre = entrega.Cliente ?? "—";
                if (entrega.ClienteID.HasValue)
                {
                    var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == entrega.ClienteID.Value);
                    if (cli != null) clienteNombre = cli.Nombre;
                }

                var detalles = (from d in db.DetalleEntregas
                                where d.EntregaID == entregaID
                                orderby d.DetalleEntregaID
                                select d).ToList();

                // Pre-cargar nombres de unidades de captura desde DetalleEntregas
                var ucIDsImp = detalles.Where(d => d.UnidadCapturaID.HasValue)
                                       .Select(d => d.UnidadCapturaID.Value).Distinct().ToList();
                var unidCapNombresImp = ucIDsImp.Any()
                    ? db.UnidadesMedida
                        .Where(u => ucIDsImp.Contains(u.UnidadMedidaID))
                        .ToDictionary(u => u.UnidadMedidaID,
                                      u => !string.IsNullOrEmpty(u.Nombre) ? u.Nombre : u.Clave)
                    : new Dictionary<int, string>();

                // Pre-cargar unidades base de los materiales del comprobante
                var matIDsImp = detalles.Where(d => d.TipoItem == "MATERIAL" && d.MaterialID.HasValue)
                                        .Select(d => d.MaterialID.Value).Distinct().ToList();
                var matUnidadBaseImp = new Dictionary<int, string>();
                if (matIDsImp.Any())
                {
                    var matsImp = db.Materiales
                        .Where(m => matIDsImp.Contains(m.MaterialID))
                        .Select(m => new { m.MaterialID, m.Unidad, m.UnidadMedidaID })
                        .ToList();
                    var ubIDsImp = matsImp.Where(m => m.UnidadMedidaID.HasValue)
                                          .Select(m => m.UnidadMedidaID.Value).Distinct().ToList();
                    var ubNombresImp = ubIDsImp.Any()
                        ? db.UnidadesMedida
                            .Where(u => ubIDsImp.Contains(u.UnidadMedidaID))
                            .ToDictionary(u => u.UnidadMedidaID,
                                          u => !string.IsNullOrEmpty(u.Nombre) ? u.Nombre : u.Clave)
                        : new Dictionary<int, string>();
                    foreach (var m in matsImp)
                    {
                        string ub = (m.UnidadMedidaID.HasValue && ubNombresImp.ContainsKey(m.UnidadMedidaID.Value))
                            ? ubNombresImp[m.UnidadMedidaID.Value] : (m.Unidad ?? "");
                        matUnidadBaseImp[m.MaterialID] = ub;
                    }
                }

                var sb = new StringBuilder();
                sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'/>");
                sb.AppendFormat("<title>Entrega {0}</title>", entrega.Folio);
                sb.Append("<style>");
                sb.Append("body{font-family:Arial,sans-serif;margin:30px;color:#1a1a1a}");
                sb.Append(".header{text-align:center;border-bottom:3px solid #003366;padding-bottom:12px;margin-bottom:20px}");
                sb.Append(".header img{height:60px;margin-bottom:6px}");
                sb.Append(".header h2{margin:0;color:#003366;font-size:1.3rem;letter-spacing:1px}");
                sb.Append(".meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:18px;font-size:.9rem;background:#f8f9fa;padding:12px;border-radius:6px}");
                sb.Append(".meta label{font-weight:bold;color:#003366}");
                sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:24px}");
                sb.Append("thead th{background:#003366;color:#fff;padding:8px 10px;font-size:.88rem;text-align:left}");
                sb.Append("thead th.r{text-align:right}");
                sb.Append("tbody td{padding:7px 10px;border-bottom:1px solid #ddd;font-size:.88rem}");
                sb.Append("tbody td.r{text-align:right}");
                sb.Append("tbody tr:nth-child(even){background:#f5f7fa}");
                sb.Append(".total-row{background:#003366!important;color:#fff;font-weight:bold}");
                sb.Append(".total-row td{padding:8px 10px;text-align:right;border:none}");
                sb.Append(".firmas{display:flex;gap:40px;margin-top:50px}");
                sb.Append(".firma{flex:1;border-top:2px solid #555;padding-top:6px;text-align:center;font-size:.85rem;color:#555}");
                sb.Append(".btn-print{padding:8px 24px;background:#003366;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem;margin-right:8px}");
                sb.Append(".btn-close{padding:8px 24px;background:#6c757d;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.9rem}");
                sb.Append(".cap-info{display:block;font-size:.76rem;color:#666;font-weight:normal}");
                sb.Append("@media print{.no-print{display:none!important}}");
                sb.Append("</style></head><body>");

                sb.Append("<div class='header'>");
                sb.Append("<img src='img/ankhal.png' alt='ANKHAL' onerror=\"this.style.display='none'\"/>");
                sb.Append("<h2>COMPROBANTE DE ENTREGA — GRUPO ANKHAL</h2>");
                sb.Append("</div>");

                sb.Append("<div class='meta'>");
                sb.AppendFormat("<div><label>Folio:</label> {0}</div>", entrega.Folio);
                sb.AppendFormat("<div><label>Fecha:</label> {0:dd/MM/yyyy}</div>", entrega.FechaEntrega);
                sb.AppendFormat("<div><label>Base Origen:</label> {0}</div>", entrega.BaseNombre);
                sb.AppendFormat("<div><label>Cliente:</label> {0}</div>", clienteNombre);
                sb.AppendFormat("<div><label>Estado:</label> {0}</div>", entrega.Estado);
                sb.AppendFormat("<div><label>Observaciones:</label> {0}</div>", entrega.Observaciones ?? "—");
                sb.Append("</div>");

                sb.Append("<table><thead><tr>");
                sb.Append("<th>Tipo</th><th>Descripción</th><th class='r'>Cantidad</th><th class='r'>Precio Unit.</th><th class='r'>Subtotal</th>");
                sb.Append("</tr></thead><tbody>");

                decimal totalGeneral = 0;
                foreach (var d in detalles)
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

                    // Datos de captura para materiales: usar campos de DetalleEntregas directamente
                    string cantidadHtml = d.Cantidad.ToString("0.##");
                    if (d.TipoItem == "MATERIAL")
                    {
                        string unidBaseN = (d.MaterialID.HasValue && matUnidadBaseImp.ContainsKey(d.MaterialID.Value))
                            ? matUnidadBaseImp[d.MaterialID.Value] : "";

                        bool tieneConvImp = d.CantidadCapturada.HasValue
                                            && d.UnidadCapturaID.HasValue
                                            && d.CantidadCapturada.Value != d.Cantidad;
                        if (tieneConvImp)
                        {
                            string unidCapN = unidCapNombresImp.ContainsKey(d.UnidadCapturaID.Value)
                                ? unidCapNombresImp[d.UnidadCapturaID.Value] : "";
                            string capStr = d.CantidadCapturada.Value.ToString("0.##");
                            cantidadHtml = string.Format(
                                "<strong>{0} {1}</strong><span class='cap-info'>= {2} {3}</span>",
                                capStr,
                                System.Web.HttpUtility.HtmlEncode(unidCapN),
                                d.Cantidad.ToString("0.##"),
                                System.Web.HttpUtility.HtmlEncode(unidBaseN));
                        }
                        else if (!string.IsNullOrEmpty(unidBaseN))
                        {
                            cantidadHtml = string.Format("{0} {1}",
                                d.Cantidad.ToString("0.##"),
                                System.Web.HttpUtility.HtmlEncode(unidBaseN));
                        }
                    }

                    decimal sub = d.Cantidad * d.PrecioUnitario;
                    totalGeneral += sub;
                    sb.AppendFormat(
                        "<tr><td>{0}</td><td>{1}</td><td class='r'>{2}</td><td class='r'>{3:C2}</td><td class='r'>{4:C2}</td></tr>",
                        d.TipoItem, nombre, cantidadHtml, d.PrecioUnitario, sub);
                }
                sb.Append("</tbody>");
                sb.AppendFormat(
                    "<tr class='total-row'><td colspan='4'>TOTAL</td><td>{0:C2}</td></tr>",
                    totalGeneral);
                sb.Append("</table>");

                sb.Append("<div class='firmas'>");
                sb.Append("<div class='firma'>Entregó</div>");
                sb.Append("<div class='firma'>Recibió</div>");
                sb.Append("</div>");

                sb.Append("<div class='no-print' style='text-align:center;margin-top:24px'>");
                sb.Append("<button class='btn-print' onclick='window.print()'>🖨 Imprimir</button>");
                sb.Append("<button class='btn-close' onclick='window.close()'>Cerrar</button>");
                sb.Append("</div></body></html>");

                string htmlJson = _json.Serialize(sb.ToString());
                string script   = string.Format(
                    "(function(){{var w=window.open('','_blank','width=860,height=700,scrollbars=yes');" +
                    "w.document.write({0});w.document.close();w.focus();}})();", htmlJson);
                ClientScript.RegisterStartupScript(GetType(), "imprimirEntrega", script, true);
            }
        }

        // ══ Helpers de lógica de negocio ══════════════════════════════════════

        private List<ItemEntregaModel> LeerItemsDelJson()
        {
            string json = hdnItemsJson.Value;
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                SetMsg("warning", "Sin items", "Agregue al menos un producto o material a la entrega.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return null;
            }
            try
            {
                var items = _json.Deserialize<List<ItemEntregaModel>>(json);
                if (items == null || items.Count == 0)
                {
                    SetMsg("warning", "Sin items", "Agregue al menos un item a la entrega.", "modalNuevo");
                    ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                        "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                    return null;
                }
                return items;
            }
            catch
            {
                SetMsg("error", "Error", "Error al leer los items de la entrega.", "modalNuevo");
                return null;
            }
        }

        private List<ItemEntregaModel> ObtenerItemsEntrega(InventarioAnkhalDBDataContext db, int entregaID)
        {
            var detalles = db.DetalleEntregas
                .Where(d => d.EntregaID == entregaID)
                .ToList();

            return detalles.Select(d =>
            {
                string tipo = (d.TipoItem ?? "").ToUpper();

                // Recuperar factor de conversión desde los campos guardados en DetalleEntregas
                bool tieneConv = tipo == "MATERIAL"
                                 && d.CantidadCapturada.HasValue
                                 && d.UnidadCapturaID.HasValue
                                 && d.CantidadCapturada.Value > 0
                                 && d.CantidadCapturada.Value != d.Cantidad;

                decimal cantCap = tieneConv ? d.CantidadCapturada.Value : d.Cantidad;
                decimal factor  = (tieneConv && cantCap != 0m) ? (d.Cantidad / cantCap) : 1m;

                return new ItemEntregaModel
                {
                    TipoItem               = tipo,
                    ItemID                 = tipo == "PRODUCTO" ? (d.ProductoID ?? 0) : (d.MaterialID ?? 0),
                    Cantidad               = cantCap,
                    PrecioUnitario         = d.PrecioUnitario,
                    Factor                 = factor,
                    UnidadCapturaIDDirecto = tieneConv ? d.UnidadCapturaID : null
                };
            }).ToList();
        }

        private bool ValidarCamposModal(out int baseID, out int clienteID, out DateTime fecha)
        {
            baseID    = 0;
            clienteID = 0;
            fecha     = AppHelper.Hoy;

            if (string.IsNullOrEmpty(ddlNuevoBase.SelectedValue) || !int.TryParse(ddlNuevoBase.SelectedValue, out baseID))
            {
                SetMsg("warning", "Campo requerido", "Seleccione la base origen.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            if (string.IsNullOrEmpty(ddlNuevoCliente.SelectedValue) || !int.TryParse(ddlNuevoCliente.SelectedValue, out clienteID))
            {
                SetMsg("warning", "Campo requerido", "Seleccione el cliente.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            if (string.IsNullOrEmpty(txtNuevoFecha.Text) || !DateTime.TryParse(txtNuevoFecha.Text, out fecha))
            {
                SetMsg("warning", "Campo requerido", "Ingrese una fecha válida.", "modalNuevo");
                ClientScript.RegisterStartupScript(GetType(), "abrirModal",
                    "window.addEventListener('load',function(){$('#modalNuevo').modal('show');});", true);
                return false;
            }
            return true;
        }

        private Modelo.Entregas CrearEntregaEntity(InventarioAnkhalDBDataContext db,
            int baseID, int clienteID, DateTime fecha, string estado, bool esCredito)
        {
            // Folio real: contar por PREFIJO de folio (no por FechaEntrega) para evitar
            // duplicados cuando otra página (Ordenes.aspx) genera entregas con la misma
            // fecha de folio pero distinta FechaEntrega. UPDLOCK+HOLDLOCK serializa
            // el acceso concurrente dentro de la transacción ya abierta.
            string prefixEnt = "ENT-" + fecha.ToString("yyyyMMdd") + "-%";
            int count = db.ExecuteQuery<int>(
                "SELECT COUNT(*) FROM dbo.Entregas WITH (UPDLOCK, HOLDLOCK) WHERE Folio LIKE {0}",
                prefixEnt).First();
            string folio = string.Format("ENT-{0}-{1:D3}", fecha.ToString("yyyyMMdd"), count + 1);

            // Nombre del cliente (texto libre del catálogo)
            string clienteNombre = "";
            var cli = db.Clientes.FirstOrDefault(c => c.ClienteID == clienteID);
            if (cli != null) clienteNombre = cli.Nombre;

            return new Modelo.Entregas
            {
                Folio           = folio,
                FechaEntrega    = fecha.Date,
                BaseOrigenID    = baseID,
                Cliente         = clienteNombre,
                ClienteID       = clienteID,
                Estado          = estado,
                EsCredito       = esCredito,
                Observaciones   = string.IsNullOrEmpty(txtNuevoObservaciones.Text.Trim())
                                    ? null
                                    : txtNuevoObservaciones.Text.Trim(),
                RegistradoPorID = Convert.ToInt32(Session["ClaveID"]),
                FechaRegistro   = AppHelper.Ahora
            };
        }

        private void InsertarDetalles(InventarioAnkhalDBDataContext db,
            int entregaID, List<ItemEntregaModel> items)
        {
            foreach (var it in items)
            {
                decimal cantAlmacenar = (it.TipoItem == "MATERIAL")
                    ? it.Cantidad * it.Factor
                    : it.Cantidad;

                if (cantAlmacenar <= 0m)
                    throw new InvalidOperationException(
                        string.Format("La cantidad del ítem {0} es inválida (≤ 0). " +
                            "Verifique la cantidad ingresada y la conversión de unidades.", it.ItemID));

                var det = new DetalleEntregas
                {
                    EntregaID      = entregaID,
                    TipoItem       = it.TipoItem,
                    Cantidad       = cantAlmacenar,
                    PrecioUnitario = it.PrecioUnitario,
                    Observaciones  = null
                };
                if (it.TipoItem == "PRODUCTO")
                {
                    det.ProductoID = it.ItemID;
                }
                else
                {
                    det.MaterialID        = it.ItemID;
                    det.CantidadCapturada = it.Cantidad;   // cantidad antes de aplicar el factor (ej. 1 cubeta)
                    det.UnidadCapturaID   = ObtenerUnidadCapturaIDEntrega(it.ItemID, it.UnidadVal, db);
                }

                db.DetalleEntregas.InsertOnSubmit(det);
            }
        }

        private string ValidarStockItems(InventarioAnkhalDBDataContext db,
            List<ItemEntregaModel> items, int baseID)
        {
            // Agrupar por producto/material y sumar el total requerido antes de comparar
            // con el stock. Sin esto, el mismo item en diferente unidad genera filas separadas
            // que pasan la validación individual pero juntas superan el stock disponible.

            var reqProductos = items
                .Where(it => it.TipoItem == "PRODUCTO")
                .GroupBy(it => it.ItemID)
                .Select(g => new { ItemID = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToList();

            foreach (var req in reqProductos)
            {
                decimal disponible = db.StockProductos
                    .Where(s => s.BaseID == baseID && s.ProductoID == req.ItemID)
                    .Select(s => (decimal)s.CantidadBuenas)
                    .FirstOrDefault();

                if (disponible < req.Total)
                {
                    string nombre = db.Productos
                        .Where(p => p.ProductoID == req.ItemID)
                        .Select(p => p.Descripcion)
                        .FirstOrDefault() ?? "Producto";
                    return string.Format(
                        "Stock insuficiente para '{0}': disponible {1:N0}, requerido {2:N0}.",
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
                    .Select(s => s.CantidadActual)
                    .FirstOrDefault();

                if (disponible < req.TotalBase)
                {
                    string nombre = db.Materiales
                        .Where(m => m.MaterialID == req.ItemID)
                        .Select(m => m.Descripcion)
                        .FirstOrDefault() ?? "Material";
                    return string.Format(
                        "Stock insuficiente para '{0}': disponible {1:N4}, requerido {2:N4}.",
                        nombre, disponible, req.TotalBase);
                }
            }

            return null; // null = todo bien
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
                    .FirstOrDefault(c => c.ConversionID == convID &&
                                         c.MaterialID   == matID  &&
                                         c.Activo);
                return conv?.UnidadOrigenID;
            }

            return null;
        }

        private int ObtenerTipoMovimientoID(InventarioAnkhalDBDataContext db, string clave)
        {
            return db.TiposMovimiento
                .Where(t => t.Clave == clave)
                .Select(t => t.TipoMovimientoID)
                .FirstOrDefault();
        }

        private void DescontarStockYRegistrarMovimientos(InventarioAnkhalDBDataContext db,
            int entregaID, int baseID, List<ItemEntregaModel> items, int tipoSalidaID,
            string obsEntrega = null)
        {
            int claveID = Convert.ToInt32(Session["ClaveID"]);

            // Crear lote que agrupa todos los movimientos de esta entrega
            var loteEnt = new Modelo.LotesMovimiento
            {
                Folio            = AppHelper.GenerarFolio(db, "ENT"),
                TipoMovimientoID = tipoSalidaID,
                BaseOrigenID     = baseID,
                BaseDestinoID    = null,
                Observaciones    = string.IsNullOrEmpty(obsEntrega) ? null : obsEntrega,
                RegistradoPorID  = claveID,
                FechaLote        = AppHelper.Ahora.Date,
                FechaRegistro    = AppHelper.Ahora
            };
            db.LotesMovimiento.InsertOnSubmit(loteEnt);
            db.SubmitChanges(); // obtener LoteID
            int loteEntID = loteEnt.LoteID;

            foreach (var it in items)
            {
                if (it.TipoItem == "PRODUCTO")
                {
                    // Descontar de StockProductos.CantidadBuenas
                    var stock = db.StockProductos
                        .FirstOrDefault(s => s.BaseID == baseID && s.ProductoID == it.ItemID);
                    if (stock != null)
                    {
                        // UPDLOCK bloquea la fila hasta que la transacción termine,
                        // evitando race condition entre dos entregas concurrentes del mismo producto.
                        int actualProd = db.ExecuteQuery<int>(
                            "SELECT CantidadBuenas FROM dbo.StockProductos WITH (UPDLOCK, HOLDLOCK) WHERE BaseID={0} AND ProductoID={1}",
                            baseID, it.ItemID).FirstOrDefault();

                        if (actualProd < (int)it.Cantidad)
                            throw new InvalidOperationException(
                                string.Format("Stock insuficiente para producto #{0} al momento de confirmar: disponible {1}, requerido {2}.",
                                    it.ItemID, actualProd, (int)it.Cantidad));

                        stock.CantidadBuenas  -= (int)it.Cantidad;
                        stock.FechaUltimaModif = AppHelper.Ahora;
                    }

                    // Movimiento SALIDA vinculado a la entrega
                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoSalidaID,
                        TipoItem         = "Producto",
                        ProductoID       = it.ItemID,
                        MaterialID       = null,
                        BaseOrigenID     = baseID,
                        BaseDestinoID    = null,
                        Cantidad         = it.Cantidad,
                        Costo            = it.PrecioUnitario,
                        EntregaID        = entregaID,
                        ProduccionID     = null,
                        LoteID           = loteEntID,
                        Observaciones    = string.IsNullOrEmpty(obsEntrega) ? null : obsEntrega,
                        RegistradoPorID  = claveID,
                        FechaMovimiento  = AppHelper.Ahora
                    });
                }
                else if (it.TipoItem == "MATERIAL")
                {
                    decimal cantBase = it.Cantidad * it.Factor;

                    // Descontar de StockMateriales.CantidadActual
                    var stock = db.StockMateriales
                        .FirstOrDefault(s => s.BaseID == baseID && s.MaterialID == it.ItemID);
                    if (stock != null)
                    {
                        // UPDLOCK bloquea la fila hasta que la transacción termine,
                        // evitando race condition entre dos entregas concurrentes del mismo material.
                        decimal actualMat = db.ExecuteQuery<decimal>(
                            "SELECT CantidadActual FROM dbo.StockMateriales WITH (UPDLOCK, HOLDLOCK) WHERE BaseID={0} AND MaterialID={1}",
                            baseID, it.ItemID).FirstOrDefault();

                        if (actualMat < cantBase)
                            throw new InvalidOperationException(
                                string.Format("Stock insuficiente para material #{0} al momento de confirmar: disponible {1:N4}, requerido {2:N4}.",
                                    it.ItemID, actualMat, cantBase));

                        stock.CantidadActual  -= cantBase;
                        stock.FechaUltimaModif = AppHelper.Ahora;
                    }

                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID  = tipoSalidaID,
                        TipoItem          = "Material",
                        MaterialID        = it.ItemID,
                        ProductoID        = null,
                        BaseOrigenID      = baseID,
                        BaseDestinoID     = null,
                        Cantidad          = cantBase,
                        Costo             = it.PrecioUnitario,
                        EntregaID         = entregaID,
                        ProduccionID      = null,
                        LoteID            = loteEntID,
                        Observaciones     = string.IsNullOrEmpty(obsEntrega) ? null : obsEntrega,
                        RegistradoPorID   = claveID,
                        FechaMovimiento   = AppHelper.Ahora,
                        CantidadCapturada = it.Cantidad,
                        UnidadCapturaID   = it.UnidadCapturaIDDirecto
                                            ?? ObtenerUnidadCapturaIDEntrega(it.ItemID, it.UnidadVal, db),
                        FactorAplicado    = it.Factor != 1m ? it.Factor : (decimal?)null
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
                    var stock = db.StockProductos
                        .FirstOrDefault(s => s.BaseID == baseID && s.ProductoID == it.ItemID);
                    if (stock != null)
                    {
                        stock.CantidadBuenas  += (int)it.Cantidad;
                        stock.FechaUltimaModif = AppHelper.Ahora;
                    }
                    else
                    {
                        db.StockProductos.InsertOnSubmit(new StockProductos
                        {
                            BaseID           = baseID,
                            ProductoID       = it.ItemID,
                            CantidadBuenas   = (int)it.Cantidad,
                            CantidadRechazo  = 0,
                            FechaUltimaModif = AppHelper.Ahora
                        });
                    }

                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoAjusteID,
                        TipoItem         = "Producto",
                        ProductoID       = it.ItemID,
                        MaterialID       = null,
                        BaseOrigenID     = null,
                        BaseDestinoID    = baseID,
                        Cantidad         = it.Cantidad,
                        Costo            = it.PrecioUnitario,
                        EntregaID        = entregaID,
                        ProduccionID     = null,
                        Observaciones    = string.Format("Devolución por cancelación entrega #{0}", entregaID),
                        RegistradoPorID  = claveID,
                        FechaMovimiento  = AppHelper.Ahora
                    });
                }
                else if (it.TipoItem == "MATERIAL")
                {
                    // La cantidad original deducida fue en unidad base (it.Cantidad × it.Factor).
                    // it.Cantidad es la cantidad capturada (ej. cubetas); hay que devolver en base.
                    decimal cantBaseDevolver = it.Cantidad * it.Factor;

                    var stock = db.StockMateriales
                        .FirstOrDefault(s => s.BaseID == baseID && s.MaterialID == it.ItemID);
                    if (stock != null)
                    {
                        stock.CantidadActual  += cantBaseDevolver;
                        stock.FechaUltimaModif = AppHelper.Ahora;
                    }
                    else
                    {
                        db.StockMateriales.InsertOnSubmit(new StockMateriales
                        {
                            BaseID           = baseID,
                            MaterialID       = it.ItemID,
                            CantidadActual   = cantBaseDevolver,
                            FechaUltimaModif = AppHelper.Ahora
                        });
                    }

                    db.Movimientos.InsertOnSubmit(new Modelo.Movimientos
                    {
                        TipoMovimientoID = tipoAjusteID,
                        TipoItem         = "Material",
                        MaterialID       = it.ItemID,
                        ProductoID       = null,
                        BaseOrigenID     = null,
                        BaseDestinoID    = baseID,
                        Cantidad         = cantBaseDevolver,
                        Costo            = it.PrecioUnitario,
                        EntregaID        = entregaID,
                        ProduccionID     = null,
                        Observaciones    = string.Format("Devolución por cancelación entrega #{0}", entregaID),
                        RegistradoPorID  = claveID,
                        FechaMovimiento  = AppHelper.Ahora
                    });
                }
            }
        }

        // ══ Utilidades ════════════════════════════════════════════════════════
        private void LimpiarModalNuevo()
        {
            txtNuevoFolio.Text           = "";
            txtNuevoFecha.Text           = AppHelper.Hoy.ToString("yyyy-MM-dd");
            ddlNuevoBase.SelectedIndex   = 0;
            ddlNuevoCliente.SelectedIndex= 0;
            txtNuevoObservaciones.Text   = "";
            chkEsCredito.Checked         = false;
            hdnItemsJson.Value           = "[]";
        }

        private void SetMsg(string icon, string title, string text, string modal = null, object confirmar = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "", confirmar };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        // ══ WebMethod: búsqueda AJAX de productos / materiales ════════════════
        [WebMethod(EnableSession = true)]
        public static object BuscarItems(string query, string tipo)
        {
            if (System.Web.HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado.");

            query = (query ?? "").Trim();
            var ci = System.Globalization.CultureInfo.InvariantCulture;

            using (var db = new InventarioAnkhalDBDataContext(_connStr))
            {
                db.ObjectTrackingEnabled = false;

                if (tipo == "PRODUCTO")
                {
                    return db.Productos
                        .Where(p => p.Activo &&
                            (p.Descripcion.Contains(query) || p.Codigo.Contains(query)))
                        .OrderBy(p => p.Codigo)
                        .Take(15)
                        .Select(p => new
                        {
                            id     = p.ProductoID,
                            nombre = "[" + p.Codigo + "] " + p.Descripcion,
                            precio = p.PrecioVenta
                        })
                        .ToList();
                }

                // MATERIAL: devuelve materiales + sus conversiones en una sola respuesta
                var mats = db.Materiales
                    .Where(m => m.Activo &&
                        (m.Descripcion.Contains(query) || m.Codigo.Contains(query)))
                    .OrderBy(m => m.Codigo)
                    .Take(15)
                    .ToList();

                if (!mats.Any()) return new object[0];

                var matIDs   = mats.Select(m => m.MaterialID).ToList();
                var unidades = db.UnidadesMedida.ToDictionary(u => u.UnidadMedidaID, u => u.Clave);
                var convs    = (from c in db.ConversionesMaterial
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
                            valor  = "conv:" + c.ConversionID,
                            texto  = textoConv + " (= " + factorStr + " " + claveBase + ")",
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
    }
}
