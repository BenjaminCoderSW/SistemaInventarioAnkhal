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
    public partial class Paquetes : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ── DTOs ─────────────────────────────────────────────────────────────
        public class PaqueteVM
        {
            public int PaqueteID { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public bool Activo { get; set; }
            public int TotalComponentes { get; set; }
            public System.Data.Linq.Binary RowVersion { get; set; }
        }

        public class CompNuevoVM
        {
            public string tipo { get; set; }
            public string itemID { get; set; }
            public decimal cantidad { get; set; }
            public decimal precio { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarPaquetes();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvPaquetes.VirtualItemCount = (int)ViewState["TotalRegistros"];

                InjectJsData();
            }
        }

        // ══ CARGA PRINCIPAL ════════════════════════════════════════════════════
        private void CargarPaquetes()
        {
            string buscar = (txtBuscar.Text ?? "").Trim();
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvPaquetes.PageIndex;
            int pageSz = gvPaquetes.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var query = db.Paquetes.AsQueryable();

                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(p =>
                        p.Codigo.Contains(buscar) ||
                        p.Nombre.Contains(buscar));

                if (filEst == "1") query = query.Where(p => p.Activo == true);
                else if (filEst == "0") query = query.Where(p => p.Activo == false);

                query = query.OrderBy(p => p.Codigo);

                int totalRegistros = query.Count();

                lblResultados.Text = totalRegistros == 1
                    ? "1 registro encontrado."
                    : totalRegistros + " registros encontrados.";

                ViewState["TotalRegistros"] = totalRegistros;

                // Dashboard
                int totalTodos = db.Paquetes.Count();
                int totalActivos = db.Paquetes.Count(p => p.Activo);
                lblTotal.Text = totalTodos.ToString();
                lblActivos.Text = totalActivos.ToString();
                lblInactivos.Text = (totalTodos - totalActivos).ToString();

                var pagina = query
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                var idsPagina = pagina.Select(p => p.PaqueteID).ToList();

                var compCounts = db.PaqueteComponentes
                    .Where(pc => idsPagina.Contains(pc.PaqueteID))
                    .GroupBy(pc => pc.PaqueteID)
                    .Select(g => new { PaqueteID = g.Key, Total = g.Count() })
                    .ToList();

                var lista = pagina.Select(p => new PaqueteVM
                {
                    PaqueteID = p.PaqueteID,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Activo = p.Activo,
                    RowVersion = p.RowVersion,
                    TotalComponentes = compCounts
                        .FirstOrDefault(c => c.PaqueteID == p.PaqueteID)?.Total ?? 0
                }).ToList();

                gvPaquetes.VirtualItemCount = totalRegistros;
                gvPaquetes.DataSource = lista;
                gvPaquetes.DataBind();

                InjectJsData(db, lista);
            }
        }

        // ── Inyectar datos JS ─────────────────────────────────────────────────
        private void InjectJsData(InventarioAnkhalDBDataContext db, List<PaqueteVM> lista)
        {
            // Productos activos para los selects
            var productos = db.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Descripcion)
                .Select(p => new { id = p.ProductoID, nombre = p.Descripcion })
                .ToList();

            // Materiales activos para los selects
            var materiales = db.Materiales
                .Where(m => m.Activo)
                .OrderBy(m => m.Descripcion)
                .Select(m => new { id = m.MaterialID, nombre = m.Descripcion, unidad = m.Unidad })
                .ToList();

            // Componentes de los paquetes visibles en la página
            var idsPagina = lista.Select(p => p.PaqueteID).ToList();
            var pcs = db.PaqueteComponentes
                .Where(pc => idsPagina.Contains(pc.PaqueteID))
                .ToList();

            // Para resolver nombres necesitamos los IDs involucrados
            var prodIDs = pcs.Where(pc => pc.TipoComponente == "PRODUCTO" && pc.ProductoID.HasValue)
                             .Select(pc => pc.ProductoID.Value).Distinct().ToList();
            var matIDs = pcs.Where(pc => pc.TipoComponente == "MATERIAL" && pc.MaterialID.HasValue)
                             .Select(pc => pc.MaterialID.Value).Distinct().ToList();

            var prodMap = db.Productos.Where(p => prodIDs.Contains(p.ProductoID))
                            .ToDictionary(p => p.ProductoID, p => p.Descripcion);
            var matMap = db.Materiales.Where(m => matIDs.Contains(m.MaterialID))
                            .ToDictionary(m => m.MaterialID,
                                          m => new { nombre = m.Descripcion, unidad = m.Unidad });

            var dict = new Dictionary<string, object>();
            foreach (var pc in pcs)
            {
                string key = pc.PaqueteID.ToString();
                if (!dict.ContainsKey(key))
                    dict[key] = new List<object>();

                string nombre = "";
                string unidad = "";

                if (pc.TipoComponente == "PRODUCTO" && pc.ProductoID.HasValue)
                {
                    prodMap.TryGetValue(pc.ProductoID.Value, out nombre);
                }
                else if (pc.TipoComponente == "MATERIAL" && pc.MaterialID.HasValue)
                {
                    if (matMap.ContainsKey(pc.MaterialID.Value))
                    {
                        nombre = matMap[pc.MaterialID.Value].nombre;
                        unidad = matMap[pc.MaterialID.Value].unidad;
                    }
                }

                int itemID = pc.TipoComponente == "PRODUCTO"
                    ? (pc.ProductoID ?? 0)
                    : (pc.MaterialID ?? 0);

                ((List<object>)dict[key]).Add(new
                {
                    pcID = pc.PaqueteComponenteID,
                    tipo = pc.TipoComponente,
                    itemID = itemID,
                    nombre = nombre ?? "",
                    unidad = unidad,
                    cantidad = pc.Cantidad,
                    precio = pc.PrecioUnitario
                });
            }

            litJsData.Text = string.Format(
                "<script>window._productosData={0}; window._materialesData={1}; window._componentesData={2};</script>",
                _json.Serialize(productos),
                _json.Serialize(materiales),
                _json.Serialize(dict));
        }

        // Sobrecarga para postbacks sin lista disponible
        private void InjectJsData()
        {
            using (var db = NuevoDb(tracking: false))
            {
                var productos = db.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Descripcion)
                    .Select(p => new { id = p.ProductoID, nombre = p.Descripcion })
                    .ToList();

                var materiales = db.Materiales
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Descripcion)
                    .Select(m => new { id = m.MaterialID, nombre = m.Descripcion, unidad = m.Unidad })
                    .ToList();

                var pcs = db.PaqueteComponentes.ToList();
                var prodMap = db.Productos.ToDictionary(p => p.ProductoID, p => p.Descripcion);
                var matMap = db.Materiales.ToDictionary(m => m.MaterialID,
                                  m => new { nombre = m.Descripcion, unidad = m.Unidad });

                var dict = new Dictionary<string, object>();
                foreach (var pc in pcs)
                {
                    string key = pc.PaqueteID.ToString();
                    if (!dict.ContainsKey(key))
                        dict[key] = new List<object>();

                    string nombre = "", unidad = "";
                    if (pc.TipoComponente == "PRODUCTO" && pc.ProductoID.HasValue)
                        prodMap.TryGetValue(pc.ProductoID.Value, out nombre);
                    else if (pc.TipoComponente == "MATERIAL" && pc.MaterialID.HasValue)
                        if (matMap.ContainsKey(pc.MaterialID.Value))
                        { nombre = matMap[pc.MaterialID.Value].nombre; unidad = matMap[pc.MaterialID.Value].unidad; }

                    int itemID = pc.TipoComponente == "PRODUCTO"
                        ? (pc.ProductoID ?? 0) : (pc.MaterialID ?? 0);

                    ((List<object>)dict[key]).Add(new
                    {
                        pcID = pc.PaqueteComponenteID,
                        tipo = pc.TipoComponente,
                        itemID = itemID,
                        nombre = nombre ?? "",
                        unidad = unidad,
                        cantidad = pc.Cantidad,
                        precio = pc.PrecioUnitario
                    });
                }

                litJsData.Text = string.Format(
                    "<script>window._productosData={0}; window._materialesData={1}; window._componentesData={2};</script>",
                    _json.Serialize(productos),
                    _json.Serialize(materiales),
                    _json.Serialize(dict));
            }
        }

        // ══ PAGINACIÓN / BUSCAR / LIMPIAR ══════════════════════════════════════
        protected void gvPaquetes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvPaquetes.PageIndex = e.NewPageIndex;
            CargarPaquetes();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvPaquetes.PageIndex = 0;
            CargarPaquetes();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrEstado.SelectedIndex = 0;
            gvPaquetes.PageIndex = 0;
            CargarPaquetes();
        }

        // ══ GUARDAR NUEVO PAQUETE ══════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCampos(txtCodigo.Text, txtNombre.Text, "modalNuevo")) return;

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Paquetes.Any(p => p.Codigo == codigoUpper))
                { SetMsg("error", "Código duplicado", "Ya existe un paquete con el código '" + codigoUpper + "'.", "modalNuevo"); return; }

                if (db.Paquetes.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower()))
                { SetMsg("error", "Nombre duplicado", "Ya existe un paquete con el nombre '" + nombreTrim + "'.", "modalNuevo"); return; }

                try
                {
                    var nuevo = new GrupoAnkhalInventario.Modelo.Paquetes
                    {
                        Codigo = codigoUpper,
                        Nombre = nombreTrim,
                        Descripcion = txtDescripcion.Text.Trim(),
                        Activo = true,
                        FechaAlta = DateTime.Now,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    };
                    db.Paquetes.InsertOnSubmit(nuevo);
                    db.SubmitChanges();

                    // Guardar componentes si los hay
                    string jsonComp = hdnComponentesNuevo.Value;
                    if (!string.IsNullOrEmpty(jsonComp) && jsonComp != "[]")
                    {
                        var comps = _json.Deserialize<List<CompNuevoVM>>(jsonComp);
                        foreach (var c in comps)
                        {
                            if (string.IsNullOrEmpty(c.tipo) || string.IsNullOrEmpty(c.itemID)) continue;
                            if (c.cantidad <= 0) continue;

                            var pc = new GrupoAnkhalInventario.Modelo.PaqueteComponentes
                            {
                                PaqueteID = nuevo.PaqueteID,
                                TipoComponente = c.tipo,
                                Cantidad = c.cantidad,
                                PrecioUnitario = c.precio >= 0 ? c.precio : 0
                            };

                            if (c.tipo == "PRODUCTO")
                                pc.ProductoID = int.Parse(c.itemID);
                            else
                                pc.MaterialID = int.Parse(c.itemID);

                            db.PaqueteComponentes.InsertOnSubmit(pc);
                        }
                        db.SubmitChanges();
                    }

                    LimpiarNuevo();
                    CargarPaquetes();
                    SetMsg("success", "¡Guardado!", "El paquete fue creado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error guardar paquete: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo guardar el paquete.", "modalNuevo");
                }
            }
        }

        // ══ GUARDAR EDICIÓN CON CONTROL DE CONCURRENCIA ════════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnPaqueteID.Value)) return;
            if (!ValidarCampos(txtCodigoEdit.Text, txtNombreEdit.Text, "modalEditar")) return;

            int paqID = int.Parse(hdnPaqueteID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string nombreTrim = txtNombreEdit.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Paquetes.Any(p => p.Codigo == codigoUpper && p.PaqueteID != paqID))
                { SetMsg("error", "Código duplicado", "Ya existe otro paquete con ese código.", "modalEditar"); return; }

                if (db.Paquetes.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower() && p.PaqueteID != paqID))
                { SetMsg("error", "Nombre duplicado", "Ya existe otro paquete con ese nombre.", "modalEditar"); return; }

                try
                {
                    var paq = db.Paquetes.FirstOrDefault(p => p.PaqueteID == paqID);
                    if (paq == null) { SetMsg("error", "Error", "No se encontró el paquete."); return; }

                    // Control de concurrencia
                    byte[] rowVersionOriginal = null;
                    if (!string.IsNullOrEmpty(hdnRowVersion.Value))
                        rowVersionOriginal = Convert.FromBase64String(hdnRowVersion.Value);

                    if (rowVersionOriginal != null &&
                        paq.RowVersion != null &&
                        !rowVersionOriginal.SequenceEqual(paq.RowVersion.ToArray()))
                    {
                        SetMsg("warning",
                            "Registro modificado",
                            "Otro usuario acaba de modificar este paquete. " +
                            "Recarga la página para ver los datos actuales y vuelve a editar.",
                            "modalEditar");
                        return;
                    }

                    paq.Codigo = codigoUpper;
                    paq.Nombre = nombreTrim;
                    paq.Descripcion = txtDescripcionEdit.Text.Trim();
                    paq.FechaModif = DateTime.Now;
                    paq.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);

                    db.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);

                    CargarPaquetes();
                    SetMsg("success", "¡Actualizado!", "El paquete fue actualizado correctamente.");
                }
                catch (System.Data.Linq.ChangeConflictException)
                {
                    SetMsg("warning",
                        "Conflicto de edición",
                        "Otro usuario guardó cambios en este paquete al mismo tiempo. " +
                        "Recarga la página para ver los datos más recientes.",
                        "modalEditar");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error editar paquete: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo actualizar el paquete.", "modalEditar");
                }
            }
        }

        // ══ COMPONENTES: INSERT / UPDATE / DELETE ══════════════════════════════
        protected void btnGuardarComponentes_Click(object sender, EventArgs e)
        {
            string accion = hdnCompAccion.Value;
            int paqID = ParseInt(hdnCompPaqueteID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    switch (accion)
                    {
                        case "INSERT":
                            string tipo = hdnCompTipo.Value;
                            int itemID = ParseInt(hdnCompItemID.Value);
                            decimal cant = ParseDec(hdnCompCantidad.Value);
                            decimal pre = ParseDec(hdnCompPrecio.Value);

                            if (cant <= 0)
                            { SetMsg("error", "Cantidad inválida", "La cantidad debe ser mayor a 0.", null, true); break; }

                            // Verificar duplicado
                            bool duplic = tipo == "PRODUCTO"
                                ? db.PaqueteComponentes.Any(pc => pc.PaqueteID == paqID
                                    && pc.TipoComponente == "PRODUCTO" && pc.ProductoID == itemID)
                                : db.PaqueteComponentes.Any(pc => pc.PaqueteID == paqID
                                    && pc.TipoComponente == "MATERIAL" && pc.MaterialID == itemID);

                            if (duplic)
                            { SetMsg("error", "Duplicado", "Ese item ya es componente de este paquete.", null, true); break; }

                            var nuevo = new GrupoAnkhalInventario.Modelo.PaqueteComponentes
                            {
                                PaqueteID = paqID,
                                TipoComponente = tipo,
                                Cantidad = cant,
                                PrecioUnitario = pre
                            };
                            if (tipo == "PRODUCTO") nuevo.ProductoID = itemID;
                            else nuevo.MaterialID = itemID;

                            db.PaqueteComponentes.InsertOnSubmit(nuevo);
                            db.SubmitChanges();
                            SetMsg("success", "¡Agregado!", "Componente agregado correctamente.", null, true);
                            break;

                        case "UPDATE":
                            int pcID = ParseInt(hdnCompPCID.Value);
                            decimal c = ParseDec(hdnCompCantidad.Value);
                            decimal p = ParseDec(hdnCompPrecio.Value);

                            if (c <= 0)
                            { SetMsg("error", "Cantidad inválida", "La cantidad debe ser mayor a 0.", null, true); break; }

                            var pc2 = db.PaqueteComponentes.FirstOrDefault(x => x.PaqueteComponenteID == pcID);
                            if (pc2 == null) break;
                            pc2.Cantidad = c;
                            pc2.PrecioUnitario = p;
                            db.SubmitChanges();
                            SetMsg("success", "¡Actualizado!", "Componente actualizado.", null, true);
                            break;

                        case "DELETE":
                            int pcDel = ParseInt(hdnCompPCID.Value);
                            var pcDObj = db.PaqueteComponentes.FirstOrDefault(x => x.PaqueteComponenteID == pcDel);
                            if (pcDObj != null)
                            { db.PaqueteComponentes.DeleteOnSubmit(pcDObj); db.SubmitChanges(); }
                            SetMsg("success", "¡Eliminado!", "Componente eliminado.", null, true);
                            break;
                    }

                    CargarPaquetes();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error componente paquete: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo procesar la operación.", null, true);
                }
            }
        }

        // ══ TOGGLE ═════════════════════════════════════════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnTogglePaqueteID.Value)) return;
            int paqID = int.Parse(hdnTogglePaqueteID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    var p = db.Paquetes.FirstOrDefault(x => x.PaqueteID == paqID);
                    if (p == null) return;
                    p.Activo = !p.Activo;
                    p.FechaModif = DateTime.Now;
                    p.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);
                    db.SubmitChanges();

                    string estado = p.Activo ? "activado" : "desactivado";
                    CargarPaquetes();
                    SetMsg("success", "¡Listo!", "El paquete fue " + estado + " correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error toggle paquete: " + ex.Message);
                    SetMsg("error", "Error", "No se pudo cambiar el estatus.");
                }
            }
        }

        // ══ HELPERS ════════════════════════════════════════════════════════════
        public string RowVersionBase64(object rowVersion)
        {
            if (rowVersion == null) return "";
            if (rowVersion is System.Data.Linq.Binary)
                return Convert.ToBase64String(((System.Data.Linq.Binary)rowVersion).ToArray());
            if (rowVersion is byte[])
                return Convert.ToBase64String((byte[])rowVersion);
            return "";
        }

        private bool ValidarCampos(string cod, string nom, string modal)
        {
            if (string.IsNullOrWhiteSpace(cod) || cod.Trim().Length < 2)
            { SetMsg("warning", "Código inválido", "El código es obligatorio (mín. 2 caracteres).", modal); return false; }
            if (string.IsNullOrWhiteSpace(nom) || nom.Trim().Length < 3)
            { SetMsg("warning", "Nombre inválido", "El nombre es obligatorio (mín. 3 caracteres).", modal); return false; }
            return true;
        }

        private void SetMsg(string icon, string title, string text,
            string modal = null, bool reopenComp = false)
        {
            var obj = new { icon, title, text, modal = modal ?? "", reopenComp };
            hdnMensajePendiente.Value = _json.Serialize(obj);
        }

        private void LimpiarNuevo()
        {
            txtCodigo.Text = "";
            txtNombre.Text = "";
            txtDescripcion.Text = "";
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