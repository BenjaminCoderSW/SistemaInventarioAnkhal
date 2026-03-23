using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario
{
    public partial class Produccion : System.Web.UI.Page
    {
        // == Infraestructura ==
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // == ViewModels ==
        public class ProduccionVM
        {
            public int      ProduccionID    { get; set; }
            public DateTime Fecha           { get; set; }
            public string   BaseNombre      { get; set; }
            public string   ProductoNombre  { get; set; }
            public string   Turno           { get; set; }
            public int      CantidadBuena   { get; set; }
            public int      CantidadRechazo { get; set; }
            public int      Total           { get; set; }
            public int      MetaDia         { get; set; }
            public string   Observaciones   { get; set; }
            public string   RegistradoPor   { get; set; }
        }

        public class ConsumoInputVM
        {
            public int     materialID        { get; set; }
            public decimal cantidadReal      { get; set; }
            public decimal cantidadTeoricaMin { get; set; }
            public decimal cantidadTeoricaMax { get; set; }
            public bool    esMerma           { get; set; }
            public string  notas             { get; set; }
        }

        // == Page_Load ==
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

        // == Catalogos ==
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

                // Modal: Base
                ddlBase.Items.Clear();
                ddlBase.Items.Add(new ListItem("-- Seleccione --", ""));
                foreach (var b in bases)
                    ddlBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));

                // Productos
                var productos = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Descripcion)
                    .Select(p => new { p.ProductoID, p.Descripcion })
                    .ToList();

                // Filtro producto
                ddlFiltrProducto.Items.Clear();
                ddlFiltrProducto.Items.Add(new ListItem("-- Todos --", ""));
                foreach (var p in productos)
                    ddlFiltrProducto.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));

                // Modal: Producto
                ddlProducto.Items.Clear();
                ddlProducto.Items.Add(new ListItem("-- Seleccione un producto --", ""));
                foreach (var p in productos)
                    ddlProducto.Items.Add(new ListItem(p.Descripcion, p.ProductoID.ToString()));
            }
        }

        // == Inyeccion JS -- BOM data ==
        private void InjectJsData()
        {
            using (var db = NuevoDb(false))
            {
                // BOM: ProductoMateriales agrupados por ProductoID
                var bomRaw = (from pm in db.ProductoMateriales
                              join m in db.Materiales on pm.MaterialID equals m.MaterialID
                              select new
                              {
                                  pm.ProductoID,
                                  pm.MaterialID,
                                  nombre = m.Descripcion,
                                  unidad = m.Unidad,
                                  cantidadMin = pm.CantidadMin,
                                  cantidadMax = pm.CantidadMax
                              }).ToList();

                var bomDict = bomRaw
                    .GroupBy(x => x.ProductoID)
                    .ToDictionary(
                        g => g.Key.ToString(),
                        g => g.Select(x => new
                        {
                            x.MaterialID,
                            materialID = x.MaterialID,
                            x.nombre,
                            x.unidad,
                            x.cantidadMin,
                            x.cantidadMax
                        }).ToList());

                litJsData.Text = string.Format(
                    "<script>window._bomData={0};</script>",
                    _json.Serialize(bomDict));
            }
        }

        // == Dashboard ==
        private void CargarDashboard()
        {
            using (var db = NuevoDb(false))
            {
                DateTime hoy = DateTime.Today;

                var regHoy = db.Produccion
                    .Where(p => p.Fecha == hoy)
                    .Select(p => new { p.CantidadBuena, p.CantidadRechazo })
                    .ToList();

                lblRegistrosHoy.Text   = regHoy.Count.ToString();
                lblUnidadesBuenas.Text = regHoy.Sum(r => r.CantidadBuena).ToString();
                lblUnidadesRechazo.Text = regHoy.Sum(r => r.CantidadRechazo).ToString();
            }
        }

        // == Grid ==
        private void CargarGrid()
        {
            using (var db = NuevoDb(false))
            {
                // Paso 1: Filtros
                IQueryable<Modelo.Produccion> q = db.Produccion;

                if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
                {
                    int id = int.Parse(ddlFiltrBase.SelectedValue);
                    q = q.Where(p => p.BaseID == id);
                }
                if (!string.IsNullOrEmpty(ddlFiltrProducto.SelectedValue))
                {
                    int id = int.Parse(ddlFiltrProducto.SelectedValue);
                    q = q.Where(p => p.ProductoID == id);
                }
                if (!string.IsNullOrEmpty(ddlFiltrTurno.SelectedValue))
                {
                    string turno = ddlFiltrTurno.SelectedValue;
                    q = q.Where(p => p.Turno == turno);
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

                // Paso 2: IDs de la pagina
                var ids = q
                    .OrderByDescending(p => p.Fecha)
                    .ThenByDescending(p => p.FechaRegistro)
                    .Select(p => p.ProduccionID)
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                // Paso 3: Traer datos con JOINs
                var raw = (from p in db.Produccion
                           where ids.Contains(p.ProduccionID)
                           join b in db.Bases on p.BaseID equals b.BaseID
                           join pr in db.Productos on p.ProductoID equals pr.ProductoID
                           select new
                           {
                               p.ProduccionID,
                               p.Fecha,
                               BaseNombre = b.Nombre,
                               ProductoNombre = pr.Descripcion,
                               p.Turno,
                               p.CantidadBuena,
                               p.CantidadRechazo,
                               p.MetaDia,
                               p.Observaciones,
                               p.RegistradoPorID
                           }).ToList();

                // Paso 4: Nombres de usuarios
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
                catch { }

                // Paso 5: Proyectar a ViewModel
                var pagina = ids
                    .Select(id => raw.FirstOrDefault(r => r.ProduccionID == id))
                    .Where(r => r != null)
                    .Select(r => new ProduccionVM
                    {
                        ProduccionID    = r.ProduccionID,
                        Fecha           = r.Fecha,
                        BaseNombre      = r.BaseNombre ?? "",
                        ProductoNombre  = r.ProductoNombre ?? "",
                        Turno           = r.Turno ?? "",
                        CantidadBuena   = r.CantidadBuena,
                        CantidadRechazo = r.CantidadRechazo,
                        Total           = r.CantidadBuena + r.CantidadRechazo,
                        MetaDia         = r.MetaDia,
                        Observaciones   = r.Observaciones ?? "",
                        RegistradoPor   = nombresUsuario.ContainsKey(r.RegistradoPorID)
                                          ? nombresUsuario[r.RegistradoPorID]
                                          : r.RegistradoPorID.ToString()
                    }).ToList();

                gvProduccion.DataSource = pagina;
                gvProduccion.DataBind();
            }
        }

        // == Eventos de filtros y grid ==
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvProduccion.PageIndex = 0;
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltrBase.SelectedIndex     = 0;
            ddlFiltrProducto.SelectedIndex = 0;
            ddlFiltrTurno.SelectedIndex    = 0;
            txtFechaDesde.Text             = "";
            txtFechaHasta.Text             = "";
            gvProduccion.PageIndex         = 0;
            CargarGrid();
        }

        protected void gvProduccion_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProduccion.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        // == Guardar registro de produccion ==
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // -- Validar base --
            if (string.IsNullOrEmpty(ddlBase.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione una base.", "modalNuevo");
                return;
            }
            int baseID = int.Parse(ddlBase.SelectedValue);

            // -- Validar fecha --
            DateTime fecha;
            if (!DateTime.TryParse(txtFecha.Text, out fecha))
            {
                SetMsg("warning", "Campo requerido", "Seleccione una fecha valida.", "modalNuevo");
                return;
            }

            // -- Validar turno --
            if (string.IsNullOrEmpty(ddlTurno.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione un turno.", "modalNuevo");
                return;
            }
            string turno = ddlTurno.SelectedValue;

            // -- Validar producto --
            if (string.IsNullOrEmpty(ddlProducto.SelectedValue))
            {
                SetMsg("warning", "Campo requerido", "Seleccione un producto.", "modalNuevo");
                return;
            }
            int productoID = int.Parse(ddlProducto.SelectedValue);

            // -- Validar cantidades --
            int cantBuena;
            if (!int.TryParse(txtCantidadBuena.Text, out cantBuena) || cantBuena < 0)
            {
                SetMsg("warning", "Campo invalido", "La cantidad buena debe ser mayor o igual a cero.", "modalNuevo");
                return;
            }
            int cantRechazo = 0;
            if (!string.IsNullOrEmpty(txtCantidadRechazo.Text))
            {
                if (!int.TryParse(txtCantidadRechazo.Text, out cantRechazo) || cantRechazo < 0)
                {
                    SetMsg("warning", "Campo invalido", "La cantidad de rechazo no puede ser negativa.", "modalNuevo");
                    return;
                }
            }

            // -- Parsear consumos JSON --
            List<ConsumoInputVM> consumos = new List<ConsumoInputVM>();
            if (!string.IsNullOrEmpty(hdnConsumosJson.Value))
            {
                try
                {
                    consumos = _json.Deserialize<List<ConsumoInputVM>>(hdnConsumosJson.Value);
                }
                catch
                {
                    SetMsg("error", "Error", "No se pudieron procesar los consumos de materiales.", "modalNuevo");
                    return;
                }
            }

            string obs     = txtObservaciones.Text.Trim();
            int    claveID = Convert.ToInt32(Session["ClaveID"]);

            using (var db = NuevoDb(true))
            {
                // -- Validar stock de cada material --
                foreach (var c in consumos)
                {
                    if (c.cantidadReal <= 0) continue;

                    var stockActual = db.StockMateriales
                        .Where(s => s.BaseID == baseID && s.MaterialID == c.materialID)
                        .Select(s => s.CantidadActual)
                        .FirstOrDefault();

                    if (stockActual < c.cantidadReal)
                    {
                        string nombreMat = db.Materiales
                            .Where(m => m.MaterialID == c.materialID)
                            .Select(m => m.Descripcion)
                            .FirstOrDefault() ?? "Material";

                        SetMsg("warning", "Stock insuficiente",
                            string.Format("{0}: stock actual {1:N4}. Se requieren {2:N4}.",
                                nombreMat, stockActual, c.cantidadReal),
                            "modalNuevo");
                        return;
                    }
                }

                // -- Obtener MetaDiaria de la base --
                int metaDia = 0;
                var baseObj = db.Bases.FirstOrDefault(b => b.BaseID == baseID);
                if (baseObj != null)
                    metaDia = (int)Math.Round(baseObj.MetaDiaria, MidpointRounding.AwayFromZero);

                // -- Insertar registro Produccion --
                var reg = new Modelo.Produccion
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
                    FechaRegistro   = DateTime.Now
                };
                db.Produccion.InsertOnSubmit(reg);

                // -- Insertar consumos y restar stock de materiales --
                foreach (var c in consumos)
                {
                    if (c.cantidadReal <= 0) continue;

                    var consumo = new ConsumosProduccion
                    {
                        ProduccionID     = 0, // Se asigna con InsertOnSubmit via FK
                        MaterialID       = c.materialID,
                        CantidadReal     = c.cantidadReal,
                        CantidadTeoricaMin = c.cantidadTeoricaMin,
                        CantidadTeoricaMax = c.cantidadTeoricaMax,
                        EsMerma          = c.esMerma,
                        Notas            = string.IsNullOrEmpty(c.notas) ? null : c.notas
                    };
                    reg.ConsumosProduccion.Add(consumo);

                    // Restar del stock de materiales
                    UpsertStockMaterial(db, c.materialID, baseID, -c.cantidadReal);
                }

                // -- Sumar al stock de productos --
                UpsertStockProducto(db, productoID, baseID, cantBuena, cantRechazo);

                db.SubmitChanges();
            }

            LimpiarModal();
            SetMsg("success", "Registro guardado", "El registro de produccion se guardo correctamente.");
            CargarDashboard();
            CargarGrid();
        }

        // == Helpers de stock ==

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
                    CantidadActual   = delta > 0 ? delta : 0,
                    FechaUltimaModif = DateTime.Now
                });
            }
            else
            {
                s.CantidadActual   += delta;
                s.FechaUltimaModif  = DateTime.Now;
            }
        }

        private void UpsertStockProducto(InventarioAnkhalDBDataContext db,
            int productoID, int baseID, int cantBuenas, int cantRechazo)
        {
            var s = db.StockProductos
                .FirstOrDefault(x => x.BaseID == baseID && x.ProductoID == productoID);

            if (s == null)
            {
                db.StockProductos.InsertOnSubmit(new StockProductos
                {
                    BaseID           = baseID,
                    ProductoID       = productoID,
                    CantidadBuenas   = cantBuenas,
                    CantidadRechazo  = cantRechazo,
                    FechaUltimaModif = DateTime.Now
                });
            }
            else
            {
                s.CantidadBuenas   += cantBuenas;
                s.CantidadRechazo  += cantRechazo;
                s.FechaUltimaModif  = DateTime.Now;
            }
        }

        // == Utilidades ==
        private void LimpiarModal()
        {
            ddlBase.SelectedIndex     = 0;
            txtFecha.Text             = "";
            ddlTurno.SelectedIndex    = 0;
            ddlProducto.SelectedIndex = 0;
            txtCantidadBuena.Text     = "";
            txtCantidadRechazo.Text   = "";
            txtObservaciones.Text     = "";
            hdnConsumosJson.Value     = "";
        }

        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }
    }
}
