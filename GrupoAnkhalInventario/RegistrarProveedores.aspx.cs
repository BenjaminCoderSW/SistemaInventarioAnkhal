using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using GrupoAnkhalInventario.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class RegistrarProveedores : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarProveedores();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvProveedores.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ══ CARGA ═════════════════════════════════════════════════════════════
        private void CargarProveedores()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvProveedores.PageIndex;
            int pageSz = gvProveedores.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var query = db.Proveedores.AsQueryable();

                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(p =>
                        p.Nombre.ToLower().Contains(buscar) ||
                        (p.Contacto != null && p.Contacto.ToLower().Contains(buscar)));

                if (filEst == "1") query = query.Where(p => p.Activo == true);
                else if (filEst == "0") query = query.Where(p => p.Activo == false);

                query = query.OrderBy(p => p.Nombre);

                int total = query.Count();
                lblResultados.Text = total == 1 ? "1 registro encontrado." : total + " registros encontrados.";
                ViewState["TotalRegistros"] = total;

                gvProveedores.VirtualItemCount = total;
                gvProveedores.DataSource = query.Skip(pageIdx * pageSz).Take(pageSz).ToList();
                gvProveedores.DataBind();
            }
        }

        protected void gvProveedores_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProveedores.PageIndex = e.NewPageIndex;
            CargarProveedores();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvProveedores.PageIndex = 0;
            CargarProveedores();
        }

        protected void btnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrEstado.SelectedIndex = 0;
            gvProveedores.PageIndex = 0;
            CargarProveedores();
        }

        // ══ GUARDAR NUEVO ═════════════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            string nombreTrim = (txtNombre.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombreTrim))
            {
                SetMsg("warning", "Campo obligatorio", "El nombre del proveedor es obligatorio.", "modalNuevo");
                return;
            }

            using (var db = NuevoDb())
            {
                if (db.Proveedores.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower()))
                {
                    SetMsg("error", "Nombre duplicado", "Ya existe un proveedor con el nombre '" + nombreTrim + "'.", "modalNuevo");
                    return;
                }
                try
                {
                    db.Proveedores.InsertOnSubmit(new Proveedores
                    {
                        Nombre = nombreTrim,
                        Contacto = NullIfEmpty(txtContacto.Text),
                        Telefono = NullIfEmpty(txtTelefono.Text),
                        Email = NullIfEmpty(txtEmail.Text),
                        PaginaWeb = NullIfEmpty(txtPaginaWeb.Text),
                        TipoEmpresa = NullIfEmpty(ddlTipoEmpresa.SelectedValue),
                        Nacionalidad = NullIfEmpty(txtNacionalidad.Text),
                        TipoPersona = NullIfEmpty(ddlTipoPersona.SelectedValue),
                        RazonSocialFiscal = NullIfEmpty(txtRazonSocialFiscal.Text),
                        RFC = NullIfEmpty(txtRFC.Text)?.ToUpper(),
                        RegimenFiscal = NullIfEmpty(ddlRegimenFiscal.SelectedValue),
                        CURP = NullIfEmpty(txtCURP.Text)?.ToUpper(),
                        Banco = NullIfEmpty(txtBanco.Text),
                        CLABE = NullIfEmpty(txtCLABE.Text),
                        CuentaBancaria = NullIfEmpty(txtCuentaBancaria.Text),
                        TitularCuenta = NullIfEmpty(txtTitularCuenta.Text),
                        DiasCredito = ParseNullableInt(txtDiasCredito.Text),
                        LimiteCredito = ParseNullableDecimal(txtLimiteCredito.Text),
                        Pais = NullIfEmpty(txtPais.Text),
                        CodigoPostal = NullIfEmpty(txtCodigoPostal.Text),
                        Estado = NullIfEmpty(txtEstado.Text),
                        Municipio = NullIfEmpty(txtMunicipio.Text),
                        Colonia = NullIfEmpty(txtColonia.Text),
                        NumExt = NullIfEmpty(txtNumExt.Text),
                        NumInt = NullIfEmpty(txtNumInt.Text),
                        Referencia = NullIfEmpty(txtReferencia.Text),
                        Activo = true,
                        FechaAlta = AppHelper.Ahora,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    });
                    db.SubmitChanges();
                    LimpiarNuevo();
                    CargarProveedores();
                    SetMsg("success", "¡Guardado!", "El proveedor fue registrado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error guardar proveedor: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo guardar el proveedor. Contacte al administrador.", "modalNuevo");
                }
            }
        }

        // ══ GUARDAR EDICIÓN ═══════════════════════════════════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProveedorID.Value)) { SetMsg("error", "Error", "No se identificó el proveedor a editar."); return; }

            string nombreTrim = (txtNombreEdit.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombreTrim))
            {
                SetMsg("warning", "Campo obligatorio", "El nombre del proveedor es obligatorio.", "modalEditar");
                return;
            }

            int proveedorID = Convert.ToInt32(hdnProveedorID.Value);

            using (var db = NuevoDb())
            {
                if (db.Proveedores.Any(p => p.Nombre.ToLower() == nombreTrim.ToLower() && p.ProveedorID != proveedorID))
                {
                    SetMsg("error", "Nombre duplicado", "Ya existe otro proveedor con el nombre '" + nombreTrim + "'.", "modalEditar");
                    return;
                }
                try
                {
                    var p2 = db.Proveedores.FirstOrDefault(p => p.ProveedorID == proveedorID);
                    if (p2 == null) { SetMsg("error", "Error", "No se encontró el proveedor a editar."); return; }

                    p2.Nombre = nombreTrim;
                    p2.Contacto = NullIfEmpty(txtContactoEdit.Text);
                    p2.Telefono = NullIfEmpty(txtTelefonoEdit.Text);
                    p2.Email = NullIfEmpty(txtEmailEdit.Text);
                    p2.PaginaWeb = NullIfEmpty(txtPaginaWebEdit.Text);
                    p2.TipoEmpresa = NullIfEmpty(ddlTipoEmpresaEdit.SelectedValue);
                    p2.Nacionalidad = NullIfEmpty(txtNacionalidadEdit.Text);
                    p2.TipoPersona = NullIfEmpty(ddlTipoPersonaEdit.SelectedValue);
                    p2.RazonSocialFiscal = NullIfEmpty(txtRazonSocialFiscalEdit.Text);
                    p2.RFC = NullIfEmpty(txtRFCEdit.Text)?.ToUpper();
                    p2.RegimenFiscal = NullIfEmpty(ddlRegimenFiscalEdit.SelectedValue);
                    p2.CURP = NullIfEmpty(txtCURPEdit.Text)?.ToUpper();
                    p2.Banco = NullIfEmpty(txtBancoEdit.Text);
                    p2.CLABE = NullIfEmpty(txtCLABEEdit.Text);
                    p2.CuentaBancaria = NullIfEmpty(txtCuentaBancariaEdit.Text);
                    p2.TitularCuenta = NullIfEmpty(txtTitularCuentaEdit.Text);
                    p2.DiasCredito = ParseNullableInt(txtDiasCreditoEdit.Text);
                    p2.LimiteCredito = ParseNullableDecimal(txtLimiteCreditoEdit.Text);
                    p2.Pais = NullIfEmpty(txtPaisEdit.Text);
                    p2.CodigoPostal = NullIfEmpty(txtCodigoPostalEdit.Text);
                    p2.Estado = NullIfEmpty(txtEstadoEdit.Text);
                    p2.Municipio = NullIfEmpty(txtMunicipioEdit.Text);
                    p2.Colonia = NullIfEmpty(txtColoniaEdit.Text);
                    p2.NumExt = NullIfEmpty(txtNumExtEdit.Text);
                    p2.NumInt = NullIfEmpty(txtNumIntEdit.Text);
                    p2.Referencia = NullIfEmpty(txtReferenciaEdit.Text);

                    db.SubmitChanges();
                    CargarProveedores();
                    SetMsg("success", "¡Actualizado!", "El proveedor fue actualizado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error editar proveedor: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo actualizar el proveedor. Contacte al administrador.", "modalEditar");
                }
            }
        }

        // ══ TOGGLE ════════════════════════════════════════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleProveedorID.Value)) return;
            int id = Convert.ToInt32(hdnToggleProveedorID.Value);
            using (var db = NuevoDb())
            {
                try
                {
                    var p2 = db.Proveedores.FirstOrDefault(p => p.ProveedorID == id);
                    if (p2 == null) return;
                    p2.Activo = !p2.Activo;
                    db.SubmitChanges();
                    CargarProveedores();
                    SetMsg("success", "¡Listo!", "El proveedor fue " + (p2.Activo ? "activado" : "desactivado") + " correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error toggle: " + ex.Message);
                    SetMsg("error", "Error", "No se pudo cambiar el estatus del proveedor.");
                }
            }
        }

        // ══ MÓDULO PRODUCTOS POR PROVEEDOR ════════════════════════════════════

        private void CargarProductosProveedor(int proveedorID)
        {
            using (var db = NuevoDb(false))
            {
                var prov = db.Proveedores
                    .Where(p => p.ProveedorID == proveedorID)
                    .Select(p => new { p.Nombre })
                    .FirstOrDefault();
                lblProveedorNombreProd.Text =
                    System.Web.HttpUtility.HtmlEncode(prov?.Nombre ?? "");

                var pps = db.ProveedorProductos
                    .Where(pp => pp.ProveedorID == proveedorID)
                    .ToList();

                if (!pps.Any())
                {
                    gvProductosProveedor.DataSource = new List<object>();
                    gvProductosProveedor.DataBind();
                    return;
                }

                var prodIds = pps.Select(pp => pp.ProductoID).Distinct().ToList();
                var prods = db.Productos
                    .Where(p => prodIds.Contains(p.ProductoID))
                    .Select(p => new { p.ProductoID, p.Codigo, p.Descripcion })
                    .ToList()
                    .ToDictionary(p => p.ProductoID);

                var ppIds = pps.Select(pp => pp.ProveedorProductoID).ToList();
                DateTime hoy = AppHelper.Hoy;

                var historialesBrutos = db.HistorialPreciosProductos
                    .Where(h => ppIds.Contains(h.ProveedorProductoID) && h.FechaInicio <= hoy)
                    .Select(h => new { h.HistorialPrecioProductoID, h.ProveedorProductoID, h.Precio, h.FechaInicio })
                    .ToList();

                var preciosVigentes = historialesBrutos
                    .GroupBy(h => h.ProveedorProductoID)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(h => h.FechaInicio)
                               .ThenByDescending(h => h.HistorialPrecioProductoID)
                               .First());

                var vm = pps.Select(pp => new
                {
                    pp.ProveedorProductoID,
                    Codigo = prods.ContainsKey(pp.ProductoID) ? prods[pp.ProductoID].Codigo : "",
                    Producto = prods.ContainsKey(pp.ProductoID) ? prods[pp.ProductoID].Descripcion : "",
                    PrecioVigente = preciosVigentes.ContainsKey(pp.ProveedorProductoID)
                                    ? preciosVigentes[pp.ProveedorProductoID].Precio : 0m,
                    FechaVigente = preciosVigentes.ContainsKey(pp.ProveedorProductoID)
                                    ? (DateTime?)preciosVigentes[pp.ProveedorProductoID].FechaInicio
                                    : (DateTime?)null,
                    pp.Activo
                }).OrderBy(x => x.Codigo).ToList();

                gvProductosProveedor.DataSource = vm;
                gvProductosProveedor.DataBind();
            }
        }

        private void CargarHistorialProducto(int proveedorProductoID)
        {
            using (var db = NuevoDb(false))
            {
                var hist = db.HistorialPreciosProductos
                    .Where(h => h.ProveedorProductoID == proveedorProductoID)
                    .OrderByDescending(h => h.FechaInicio)
                    .Select(h => new
                    {
                        h.FechaInicio,
                        h.Precio,
                        h.RegistradoPorID,
                        Notas = h.Notas ?? ""
                    })
                    .ToList();

                if (!hist.Any())
                {
                    gvHistorialProducto.DataSource = new List<object>();
                    gvHistorialProducto.DataBind();
                    return;
                }

                var claveIds = hist.Select(h => h.RegistradoPorID).Distinct().ToList();
                var claveToUsuario = db.DatosUsuario
                    .Where(du => claveIds.Contains(du.ClaveID))
                    .Select(du => new { du.ClaveID, du.UsuarioID })
                    .ToList();

                Dictionary<int, string> nombresDict;
                try
                {
                    var usuarioIds = claveToUsuario
                        .Where(x => x.UsuarioID.HasValue)
                        .Select(x => x.UsuarioID.Value)
                        .ToList();
                    var apiNombres = UsuarioService.ObtenerEmpleadosBulk(usuarioIds)
                        .ToDictionary(e => e.IdUsuario, e => e.NombreCompleto);

                    nombresDict = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => x.UsuarioID.HasValue && apiNombres.ContainsKey(x.UsuarioID.Value)
                             ? apiNombres[x.UsuarioID.Value]
                             : "Usuario " + x.ClaveID);
                }
                catch
                {
                    nombresDict = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => "Usuario " + x.ClaveID);
                }

                var vm = hist.Select(h => new
                {
                    h.FechaInicio,
                    h.Precio,
                    RegistradoPor = nombresDict.ContainsKey(h.RegistradoPorID)
                                    ? nombresDict[h.RegistradoPorID]
                                    : "Usuario " + h.RegistradoPorID,
                    h.Notas
                }).ToList();

                gvHistorialProducto.DataSource = vm;
                gvHistorialProducto.DataBind();
            }
        }

        protected void btnAgregarProducto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProductosProveedorID.Value)) return;
            int proveedorID = Convert.ToInt32(hdnProductosProveedorID.Value);
            int productoID;

            if (!int.TryParse(hdnProductoAgregarID.Value, out productoID) || productoID == 0)
            {
                SetMsg("warning", "Producto requerido", "Busque y seleccione un producto de la lista.", "modalProductos");
                return;
            }

            decimal precio;
            if (!decimal.TryParse(txtPrecioAgregarProducto.Text, out precio) || precio < 0)
            {
                SetMsg("warning", "Precio inválido", "Ingrese un precio inicial mayor o igual a cero.", "modalProductos");
                return;
            }

            int claveID = Convert.ToInt32(Session["ClaveID"]);

            using (var db = NuevoDb())
            {
                if (db.ProveedorProductos.Any(pp =>
                        pp.ProveedorID == proveedorID && pp.ProductoID == productoID))
                {
                    SetMsg("warning", "Ya registrado",
                        "Este producto ya está asociado a este proveedor. " +
                        "Si está inactivo puedes reactivarlo con el botón Activar.", "modalProductos");
                    CargarProductosProveedor(proveedorID);
                    return;
                }

                try
                {
                    var pp = new ProveedorProductos
                    {
                        ProveedorID = proveedorID,
                        ProductoID = productoID,
                        Activo = true,
                        FechaAlta = AppHelper.Ahora,
                        UsuarioAltaID = claveID
                    };
                    db.ProveedorProductos.InsertOnSubmit(pp);
                    db.SubmitChanges();

                    db.HistorialPreciosProductos.InsertOnSubmit(new HistorialPreciosProductos
                    {
                        ProveedorProductoID = pp.ProveedorProductoID,
                        Precio = precio,
                        FechaInicio = AppHelper.Hoy,
                        Notas = "Precio inicial",
                        RegistradoPorID = claveID,
                        FechaRegistro = AppHelper.Ahora
                    });
                    db.SubmitChanges();

                    hdnProductoAgregarID.Value = "";
                    hdnProductoAgregarNombre.Value = "";
                    txtPrecioAgregarProducto.Text = "";

                    CargarProductosProveedor(proveedorID);
                    SetMsg("success", "¡Producto agregado!",
                        "El producto fue vinculado al proveedor con el precio inicial registrado.",
                        "modalProductos");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error agregar ProveedorProducto: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo agregar el producto. Contacte al administrador.", "modalProductos");
                }
            }
        }

        protected void btnActualizarPrecioProducto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProveedorProductoID.Value)) return;
            int ppID = Convert.ToInt32(hdnProveedorProductoID.Value);
            int claveID = Convert.ToInt32(Session["ClaveID"]);

            decimal nuevoPrecio;
            if (!decimal.TryParse(hdnNuevoPrecioProducto.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out nuevoPrecio) || nuevoPrecio < 0)
            {
                SetMsg("warning", "Precio inválido", "El precio debe ser mayor o igual a cero.", "modalProductos");
                return;
            }

            var partes = (hdnNuevaNotaPrecioProducto.Value ?? "").Split(new[] { '|' }, 2);
            DateTime fechaVigencia;
            if (partes.Length < 1 || !DateTime.TryParse(partes[0].Trim(), out fechaVigencia))
                fechaVigencia = AppHelper.Hoy;
            string nota = partes.Length > 1 ? partes[1].Trim() : null;
            if (string.IsNullOrWhiteSpace(nota)) nota = null;

            using (var db = NuevoDb())
            {
                try
                {
                    db.HistorialPreciosProductos.InsertOnSubmit(new HistorialPreciosProductos
                    {
                        ProveedorProductoID = ppID,
                        Precio = nuevoPrecio,
                        FechaInicio = fechaVigencia.Date,
                        Notas = nota,
                        RegistradoPorID = claveID,
                        FechaRegistro = AppHelper.Ahora
                    });
                    db.SubmitChanges();

                    if (!string.IsNullOrWhiteSpace(hdnProductosProveedorID.Value))
                        CargarProductosProveedor(Convert.ToInt32(hdnProductosProveedorID.Value));

                    SetMsg("success", "¡Precio actualizado!",
                        "El nuevo precio fue registrado en el historial.", "modalProductos");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error actualizar precio producto: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo registrar el nuevo precio. Contacte al administrador.", "modalProductos");
                }
            }
        }

        protected void btnAccionProductos_Click(object sender, EventArgs e)
        {
            string accion = hdnAccionProductos.Value ?? "";
            int proveedorID = string.IsNullOrWhiteSpace(hdnProductosProveedorID.Value)
                                 ? 0 : Convert.ToInt32(hdnProductosProveedorID.Value);

            if (accion == "cargar")
            {
                if (proveedorID == 0) return;
                CargarProductosProveedor(proveedorID);
                hdnMensajePendiente.Value = new JavaScriptSerializer()
                    .Serialize(new { icon = "", title = "", text = "", modal = "modalProductos", showHistorial = false, showHistorialProducto = false });
                return;
            }

            if (accion == "historial")
            {
                int ppID = string.IsNullOrWhiteSpace(hdnProveedorProductoID.Value)
                           ? 0 : Convert.ToInt32(hdnProveedorProductoID.Value);
                if (ppID == 0) return;

                using (var db2 = NuevoDb(false))
                {
                    var info = (from pp2 in db2.ProveedorProductos
                                where pp2.ProveedorProductoID == ppID
                                join p in db2.Productos on pp2.ProductoID equals p.ProductoID
                                select new { p.Codigo, p.Descripcion })
                               .FirstOrDefault();
                    lblHistorialProducto.Text = info != null
                        ? System.Web.HttpUtility.HtmlEncode("[" + info.Codigo + "] " + info.Descripcion)
                        : "";
                }

                if (proveedorID > 0) CargarProductosProveedor(proveedorID);
                CargarHistorialProducto(ppID);
                hdnMensajePendiente.Value = new JavaScriptSerializer()
                    .Serialize(new { icon = "", title = "", text = "", modal = "modalProductos", showHistorial = false, showHistorialProducto = true });
                return;
            }

            if (accion == "toggle")
            {
                int ppID = string.IsNullOrWhiteSpace(hdnProveedorProductoID.Value)
                           ? 0 : Convert.ToInt32(hdnProveedorProductoID.Value);
                if (ppID == 0) return;

                using (var db = NuevoDb())
                {
                    try
                    {
                        var pp = db.ProveedorProductos
                            .FirstOrDefault(x => x.ProveedorProductoID == ppID);
                        if (pp == null) return;

                        pp.Activo = !pp.Activo;
                        db.SubmitChanges();

                        if (proveedorID > 0) CargarProductosProveedor(proveedorID);
                        SetMsg("success", "¡Listo!",
                            "El producto fue " + (pp.Activo ? "activado" : "desactivado") + " correctamente.",
                            "modalProductos");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error toggle producto: " + ex.Message);
                        SetMsg("error", "Error", "No se pudo cambiar el estatus del producto.", "modalProductos");
                    }
                }
            }
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            hdnMensajePendiente.Value = new JavaScriptSerializer()
                .Serialize(new { icon, title, text, modal = modal ?? "" });
        }

        private void LimpiarNuevo()
        {
            txtNombre.Text = "";
            txtContacto.Text = "";
            txtTelefono.Text = "";
            txtEmail.Text = "";
            txtPaginaWeb.Text = "";
            ddlTipoEmpresa.SelectedIndex = 0;
            txtNacionalidad.Text = "";
            ddlTipoPersona.SelectedIndex = 0;
            txtRazonSocialFiscal.Text = "";
            txtRFC.Text = "";
            ddlRegimenFiscal.SelectedIndex = 0;
            txtCURP.Text = "";
            txtBanco.Text = "";
            txtCLABE.Text = "";
            txtCuentaBancaria.Text = "";
            txtTitularCuenta.Text = "";
            txtDiasCredito.Text = "";
            txtLimiteCredito.Text = "";
            txtPais.Text = "";
            txtCodigoPostal.Text = "";
            txtEstado.Text = "";
            txtMunicipio.Text = "";
            txtColonia.Text = "";
            txtNumExt.Text = "";
            txtNumInt.Text = "";
            txtReferencia.Text = "";
        }

        private static string NullIfEmpty(string valor)
        {
            var v = (valor ?? "").Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private static int? ParseNullableInt(string valor)
        {
            var v = (valor ?? "").Trim();
            return string.IsNullOrEmpty(v) ? (int?)null : int.TryParse(v, out int r) ? r : (int?)null;
        }

        private static decimal? ParseNullableDecimal(string valor)
        {
            var v = (valor ?? "").Trim();
            return string.IsNullOrEmpty(v) ? (decimal?)null : decimal.TryParse(v, out decimal r) ? r : (decimal?)null;
        }

        // ══ MÓDULO MATERIALES POR PROVEEDOR ══════════════════════════════════

        /// <summary>
        /// Carga el GridView de materiales asociados a un proveedor,
        /// incluyendo el precio vigente de cada uno.
        /// </summary>
        private void CargarMaterialesProveedor(int proveedorID)
        {
            // Nombre del proveedor para el encabezado del modal
            using (var db = NuevoDb(false))
            {
                var prov = db.Proveedores
                    .Where(p => p.ProveedorID == proveedorID)
                    .Select(p => new { p.Nombre })
                    .FirstOrDefault();
                lblProveedorNombreMat.Text =
                    System.Web.HttpUtility.HtmlEncode(prov?.Nombre ?? "");

                // 1. ProveedorMateriales del proveedor
                var pms = db.ProveedorMateriales
                    .Where(pm => pm.ProveedorID == proveedorID)
                    .ToList();

                if (!pms.Any())
                {
                    gvMaterialesProveedor.DataSource = new List<object>();
                    gvMaterialesProveedor.DataBind();
                    return;
                }

                // 2. Datos de los materiales (Dictionary para lookup O(1))
                var matIds = pms.Select(pm => pm.MaterialID).Distinct().ToList();
                var mats = db.Materiales
                    .Where(m => matIds.Contains(m.MaterialID))
                    .Select(m => new { m.MaterialID, m.Codigo, m.Descripcion, m.Unidad })
                    .ToList()
                    .ToDictionary(m => m.MaterialID);

                // 3. Precio vigente por ProveedorMaterialID
                //    "Vigente" = MAX(FechaInicio) donde FechaInicio <= hoy
                var pmIds = pms.Select(pm => pm.ProveedorMaterialID).ToList();
                DateTime hoy = AppHelper.Hoy;

                // Traer todos los historiales relevantes y agrupar en memoria
                var historialesBrutos = db.HistorialPrecios
                    .Where(hp => pmIds.Contains(hp.ProveedorMaterialID) && hp.FechaInicio <= hoy)
                    .Select(hp => new { hp.HistorialPrecioID, hp.ProveedorMaterialID, hp.Precio, hp.FechaInicio })
                    .ToList();

                // Desempate por HistorialPrecioID DESC: si dos precios comparten la misma FechaInicio
                // (ej. dos actualizaciones el mismo día), gana el registrado más recientemente.
                var preciosVigentes = historialesBrutos
                    .GroupBy(hp => hp.ProveedorMaterialID)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(hp => hp.FechaInicio)
                               .ThenByDescending(hp => hp.HistorialPrecioID)
                               .First());

                // 4. Proyectar ViewModel
                var vm = pms.Select(pm => new
                {
                    pm.ProveedorMaterialID,
                    Codigo = mats.ContainsKey(pm.MaterialID) ? mats[pm.MaterialID].Codigo : "",
                    Material = mats.ContainsKey(pm.MaterialID) ? mats[pm.MaterialID].Descripcion : "",
                    Unidad = mats.ContainsKey(pm.MaterialID) ? mats[pm.MaterialID].Unidad : "",
                    PrecioVigente = preciosVigentes.ContainsKey(pm.ProveedorMaterialID)
                                   ? preciosVigentes[pm.ProveedorMaterialID].Precio : 0m,
                    FechaVigente = preciosVigentes.ContainsKey(pm.ProveedorMaterialID)
                                   ? (DateTime?)preciosVigentes[pm.ProveedorMaterialID].FechaInicio
                                   : (DateTime?)null,
                    pm.Activo
                }).OrderBy(x => x.Codigo).ToList();

                gvMaterialesProveedor.DataSource = vm;
                gvMaterialesProveedor.DataBind();
            }
        }

        /// <summary>
        /// Carga el GridView del historial de precios de un ProveedorMaterial específico.
        /// </summary>
        private void CargarHistorial(int proveedorMaterialID)
        {
            using (var db = NuevoDb(false))
            {
                var hist = db.HistorialPrecios
                    .Where(hp => hp.ProveedorMaterialID == proveedorMaterialID)
                    .OrderByDescending(hp => hp.FechaInicio)
                    .Select(hp => new
                    {
                        hp.FechaInicio,
                        hp.Precio,
                        hp.RegistradoPorID,
                        Notas = hp.Notas ?? ""
                    })
                    .ToList();

                if (!hist.Any())
                {
                    gvHistorial.DataSource = new List<object>();
                    gvHistorial.DataBind();
                    return;
                }

                // Resolver nombres de usuario via UsuarioService (con fallback)
                var claveIds = hist.Select(h => h.RegistradoPorID).Distinct().ToList();
                var claveToUsuario = db.DatosUsuario
                    .Where(du => claveIds.Contains(du.ClaveID))
                    .Select(du => new { du.ClaveID, du.UsuarioID })
                    .ToList();

                Dictionary<int, string> nombresDict;
                try
                {
                    var usuarioIds = claveToUsuario
                        .Where(x => x.UsuarioID.HasValue)
                        .Select(x => x.UsuarioID.Value)
                        .ToList();
                    var apiNombres = UsuarioService.ObtenerEmpleadosBulk(usuarioIds)
                        .ToDictionary(e => e.IdUsuario, e => e.NombreCompleto);

                    nombresDict = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => x.UsuarioID.HasValue && apiNombres.ContainsKey(x.UsuarioID.Value)
                             ? apiNombres[x.UsuarioID.Value]
                             : "Usuario " + x.ClaveID);
                }
                catch
                {
                    nombresDict = claveToUsuario.ToDictionary(
                        x => x.ClaveID,
                        x => "Usuario " + x.ClaveID);
                }

                var vm = hist.Select(h => new
                {
                    h.FechaInicio,
                    h.Precio,
                    RegistradoPor = nombresDict.ContainsKey(h.RegistradoPorID)
                                    ? nombresDict[h.RegistradoPorID]
                                    : "Usuario " + h.RegistradoPorID,
                    h.Notas
                }).ToList();

                gvHistorial.DataSource = vm;
                gvHistorial.DataBind();
            }
        }

        /// <summary>
        /// Agrega un nuevo vínculo Proveedor-Material con su precio inicial.
        /// Disparado desde JS via __doPostBack al btnAgregarMaterial invisible.
        /// </summary>
        protected void btnAgregarMaterial_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnMaterialesProveedorID.Value)) return;
            int proveedorID = Convert.ToInt32(hdnMaterialesProveedorID.Value);
            int materialID;

            if (!int.TryParse(hdnMaterialAgregarID.Value, out materialID) || materialID == 0)
            {
                SetMsg("warning", "Material requerido", "Busque y seleccione un material de la lista.", "modalMateriales");
                return;
            }

            decimal precio;
            if (!decimal.TryParse(txtPrecioAgregar.Text, out precio) || precio < 0)
            {
                SetMsg("warning", "Precio inválido", "Ingrese un precio inicial mayor o igual a cero.", "modalMateriales");
                return;
            }

            int claveID = Convert.ToInt32(Session["ClaveID"]);

            using (var db = NuevoDb())
            {
                // Verificar duplicado (mensaje amigable antes que el UNIQUE constraint dispare)
                if (db.ProveedorMateriales.Any(pm =>
                        pm.ProveedorID == proveedorID && pm.MaterialID == materialID))
                {
                    SetMsg("warning", "Ya registrado",
                        "Este material ya está asociado a este proveedor. " +
                        "Si está inactivo puedes reactivarlo con el botón Activar.", "modalMateriales");
                    CargarMaterialesProveedor(proveedorID);
                    return;
                }

                try
                {
                    // Insertar ProveedorMateriales
                    var pm = new ProveedorMateriales
                    {
                        ProveedorID = proveedorID,
                        MaterialID = materialID,
                        Activo = true,
                        FechaAlta = AppHelper.Ahora,
                        UsuarioAltaID = claveID
                    };
                    db.ProveedorMateriales.InsertOnSubmit(pm);
                    db.SubmitChanges(); // Necesario para obtener el ProveedorMaterialID generado

                    // Insertar precio inicial en historial
                    db.HistorialPrecios.InsertOnSubmit(new HistorialPrecios
                    {
                        ProveedorMaterialID = pm.ProveedorMaterialID,
                        Precio = precio,
                        FechaInicio = AppHelper.Hoy,
                        Notas = "Precio inicial",
                        RegistradoPorID = claveID,
                        FechaRegistro = AppHelper.Ahora
                    });
                    db.SubmitChanges();

                    // Limpiar el formulario de agregar
                    hdnMaterialAgregarID.Value = "";
                    hdnMaterialAgregarNombre.Value = "";
                    txtPrecioAgregar.Text = "";

                    CargarMaterialesProveedor(proveedorID);
                    SetMsg("success", "¡Material agregado!",
                        "El material fue vinculado al proveedor con el precio inicial registrado.",
                        "modalMateriales");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error agregar ProveedorMaterial: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo agregar el material. Contacte al administrador.", "modalMateriales");
                }
            }
        }

        /// <summary>
        /// Inserta un nuevo registro en HistorialPrecios (actualización de precio mensual).
        /// Disparado desde JS via __doPostBack al btnActualizarPrecio invisible.
        /// </summary>
        protected void btnActualizarPrecio_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnProveedorMaterialID.Value)) return;
            int pmID = Convert.ToInt32(hdnProveedorMaterialID.Value);
            int claveID = Convert.ToInt32(Session["ClaveID"]);

            decimal nuevoPrecio;
            if (!decimal.TryParse(hdnNuevoPrecio.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out nuevoPrecio) || nuevoPrecio < 0)
            {
                SetMsg("warning", "Precio inválido", "El precio debe ser mayor o igual a cero.", "modalMateriales");
                return;
            }

            // Desempacar "fecha|nota" del hidden field
            var partes = (hdnNuevaNotaPrecio.Value ?? "").Split(new[] { '|' }, 2);
            DateTime fechaVigencia;
            if (partes.Length < 1 || !DateTime.TryParse(partes[0].Trim(), out fechaVigencia))
                fechaVigencia = AppHelper.Hoy;
            string nota = partes.Length > 1 ? partes[1].Trim() : null;
            if (string.IsNullOrWhiteSpace(nota)) nota = null;

            using (var db = NuevoDb())
            {
                try
                {
                    db.HistorialPrecios.InsertOnSubmit(new HistorialPrecios
                    {
                        ProveedorMaterialID = pmID,
                        Precio = nuevoPrecio,
                        FechaInicio = fechaVigencia.Date,
                        Notas = nota,
                        RegistradoPorID = claveID,
                        FechaRegistro = AppHelper.Ahora
                    });
                    db.SubmitChanges();

                    // Rebind de la grilla del modal con datos actualizados
                    if (!string.IsNullOrWhiteSpace(hdnMaterialesProveedorID.Value))
                        CargarMaterialesProveedor(Convert.ToInt32(hdnMaterialesProveedorID.Value));

                    SetMsg("success", "¡Precio actualizado!",
                        "El nuevo precio fue registrado en el historial.", "modalMateriales");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error actualizar precio: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo registrar el nuevo precio. Contacte al administrador.", "modalMateriales");
                }
            }
        }

        /// <summary>
        /// Discrimina entre tres acciones del modal de materiales:
        /// "cargar" → carga y abre el modal,
        /// "historial" → carga historial y abre el modal,
        /// "toggle" → activa/desactiva el vínculo.
        /// </summary>
        protected void btnAccionMateriales_Click(object sender, EventArgs e)
        {
            string accion = hdnAccionMateriales.Value ?? "";
            int proveedorID = string.IsNullOrWhiteSpace(hdnMaterialesProveedorID.Value)
                                 ? 0 : Convert.ToInt32(hdnMaterialesProveedorID.Value);

            if (accion == "cargar")
            {
                if (proveedorID == 0) return;
                CargarMaterialesProveedor(proveedorID);
                // Apertura silenciosa: icon="" → el handler JS abre el modal sin SweetAlert
                hdnMensajePendiente.Value = new JavaScriptSerializer()
                    .Serialize(new { icon = "", title = "", text = "", modal = "modalMateriales", showHistorial = false });
                return;
            }

            if (accion == "historial")
            {
                int pmID = string.IsNullOrWhiteSpace(hdnProveedorMaterialID.Value)
                           ? 0 : Convert.ToInt32(hdnProveedorMaterialID.Value);
                if (pmID == 0) return;

                // Resolver nombre del material para el encabezado del historial
                using (var db2 = NuevoDb(false))
                {
                    var info = (from pm2 in db2.ProveedorMateriales
                                where pm2.ProveedorMaterialID == pmID
                                join m in db2.Materiales on pm2.MaterialID equals m.MaterialID
                                select new { m.Codigo, m.Descripcion })
                               .FirstOrDefault();
                    lblHistorialMaterial.Text = info != null
                        ? System.Web.HttpUtility.HtmlEncode("[" + info.Codigo + "] " + info.Descripcion)
                        : "";
                }

                if (proveedorID > 0) CargarMaterialesProveedor(proveedorID);
                CargarHistorial(pmID);
                hdnMensajePendiente.Value = new JavaScriptSerializer()
                    .Serialize(new { icon = "", title = "", text = "", modal = "modalMateriales", showHistorial = true });
                return;
            }

            if (accion == "toggle")
            {
                int pmID = string.IsNullOrWhiteSpace(hdnProveedorMaterialID.Value)
                           ? 0 : Convert.ToInt32(hdnProveedorMaterialID.Value);
                if (pmID == 0) return;

                using (var db = NuevoDb())
                {
                    try
                    {
                        var pm = db.ProveedorMateriales
                            .FirstOrDefault(x => x.ProveedorMaterialID == pmID);
                        if (pm == null) return;

                        pm.Activo = !pm.Activo;
                        db.SubmitChanges();

                        if (proveedorID > 0) CargarMaterialesProveedor(proveedorID);
                        SetMsg("success", "¡Listo!",
                            "El material fue " + (pm.Activo ? "activado" : "desactivado") + " correctamente.",
                            "modalMateriales");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error toggle material: " + ex.Message);
                        SetMsg("error", "Error", "No se pudo cambiar el estatus del material.", "modalMateriales");
                    }
                }
            }
        }

        // ══ WebMethods: autocomplete de materiales/productos para agregar a un proveedor ══

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static object BuscarMaterialesParaProveedor(string query)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado");

            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                return db.Materiales
                    .Where(m => m.Activo &&
                        (m.Descripcion.Contains(query) || m.Codigo.Contains(query)))
                    .OrderBy(m => m.Codigo)
                    .Take(15)
                    .Select(m => new { id = m.MaterialID, nombre = "[" + m.Codigo + "] " + m.Descripcion })
                    .ToList();
            }
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static object BuscarProductosParaProveedor(string query)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado");

            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            using (var db = new InventarioAnkhalDBDataContext(connStr))
            {
                return db.Productos
                    .Where(p => p.Activo &&
                        (p.Descripcion.Contains(query) || p.Codigo.Contains(query)))
                    .OrderBy(p => p.Codigo)
                    .Take(15)
                    .Select(p => new { id = p.ProductoID, nombre = "[" + p.Codigo + "] " + p.Descripcion })
                    .ToList();
            }
        }
    }
}
