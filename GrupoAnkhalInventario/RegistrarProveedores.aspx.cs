using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using System;
using System.Configuration;
using System.Linq;
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
                CargarProveedores();
            else
                if (ViewState["TotalRegistros"] != null)
                    gvProveedores.VirtualItemCount = (int)ViewState["TotalRegistros"];
        }

        // ══ CARGA ═════════════════════════════════════════════════════════════
        private void CargarProveedores()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx   = gvProveedores.PageIndex;
            int pageSz    = gvProveedores.PageSize;

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
                        Nombre            = nombreTrim,
                        Contacto          = NullIfEmpty(txtContacto.Text),
                        Telefono          = NullIfEmpty(txtTelefono.Text),
                        Email             = NullIfEmpty(txtEmail.Text),
                        PaginaWeb         = NullIfEmpty(txtPaginaWeb.Text),
                        TipoEmpresa       = NullIfEmpty(ddlTipoEmpresa.SelectedValue),
                        Nacionalidad      = NullIfEmpty(txtNacionalidad.Text),
                        TipoPersona       = NullIfEmpty(ddlTipoPersona.SelectedValue),
                        RazonSocialFiscal = NullIfEmpty(txtRazonSocialFiscal.Text),
                        RFC               = NullIfEmpty(txtRFC.Text)?.ToUpper(),
                        RegimenFiscal     = NullIfEmpty(ddlRegimenFiscal.SelectedValue),
                        CURP              = NullIfEmpty(txtCURP.Text)?.ToUpper(),
                        Banco             = NullIfEmpty(txtBanco.Text),
                        CLABE             = NullIfEmpty(txtCLABE.Text),
                        CuentaBancaria    = NullIfEmpty(txtCuentaBancaria.Text),
                        TitularCuenta     = NullIfEmpty(txtTitularCuenta.Text),
                        DiasCredito       = ParseNullableInt(txtDiasCredito.Text),
                        LimiteCredito     = ParseNullableDecimal(txtLimiteCredito.Text),
                        Pais              = NullIfEmpty(txtPais.Text),
                        CodigoPostal      = NullIfEmpty(txtCodigoPostal.Text),
                        Estado            = NullIfEmpty(txtEstado.Text),
                        Municipio         = NullIfEmpty(txtMunicipio.Text),
                        Colonia           = NullIfEmpty(txtColonia.Text),
                        NumExt            = NullIfEmpty(txtNumExt.Text),
                        NumInt            = NullIfEmpty(txtNumInt.Text),
                        Referencia        = NullIfEmpty(txtReferencia.Text),
                        Activo            = true,
                        FechaAlta         = AppHelper.Ahora,
                        UsuarioAltaID     = Convert.ToInt32(Session["ClaveID"])
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

                    p2.Nombre            = nombreTrim;
                    p2.Contacto          = NullIfEmpty(txtContactoEdit.Text);
                    p2.Telefono          = NullIfEmpty(txtTelefonoEdit.Text);
                    p2.Email             = NullIfEmpty(txtEmailEdit.Text);
                    p2.PaginaWeb         = NullIfEmpty(txtPaginaWebEdit.Text);
                    p2.TipoEmpresa       = NullIfEmpty(ddlTipoEmpresaEdit.SelectedValue);
                    p2.Nacionalidad      = NullIfEmpty(txtNacionalidadEdit.Text);
                    p2.TipoPersona       = NullIfEmpty(ddlTipoPersonaEdit.SelectedValue);
                    p2.RazonSocialFiscal = NullIfEmpty(txtRazonSocialFiscalEdit.Text);
                    p2.RFC               = NullIfEmpty(txtRFCEdit.Text)?.ToUpper();
                    p2.RegimenFiscal     = NullIfEmpty(ddlRegimenFiscalEdit.SelectedValue);
                    p2.CURP              = NullIfEmpty(txtCURPEdit.Text)?.ToUpper();
                    p2.Banco             = NullIfEmpty(txtBancoEdit.Text);
                    p2.CLABE             = NullIfEmpty(txtCLABEEdit.Text);
                    p2.CuentaBancaria    = NullIfEmpty(txtCuentaBancariaEdit.Text);
                    p2.TitularCuenta     = NullIfEmpty(txtTitularCuentaEdit.Text);
                    p2.DiasCredito       = ParseNullableInt(txtDiasCreditoEdit.Text);
                    p2.LimiteCredito     = ParseNullableDecimal(txtLimiteCreditoEdit.Text);
                    p2.Pais              = NullIfEmpty(txtPaisEdit.Text);
                    p2.CodigoPostal      = NullIfEmpty(txtCodigoPostalEdit.Text);
                    p2.Estado            = NullIfEmpty(txtEstadoEdit.Text);
                    p2.Municipio         = NullIfEmpty(txtMunicipioEdit.Text);
                    p2.Colonia           = NullIfEmpty(txtColoniaEdit.Text);
                    p2.NumExt            = NullIfEmpty(txtNumExtEdit.Text);
                    p2.NumInt            = NullIfEmpty(txtNumIntEdit.Text);
                    p2.Referencia        = NullIfEmpty(txtReferenciaEdit.Text);

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

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            hdnMensajePendiente.Value = new JavaScriptSerializer()
                .Serialize(new { icon, title, text, modal = modal ?? "" });
        }

        private void LimpiarNuevo()
        {
            txtNombre.Text                  = "";
            txtContacto.Text                = "";
            txtTelefono.Text                = "";
            txtEmail.Text                   = "";
            txtPaginaWeb.Text               = "";
            ddlTipoEmpresa.SelectedIndex    = 0;
            txtNacionalidad.Text            = "";
            ddlTipoPersona.SelectedIndex    = 0;
            txtRazonSocialFiscal.Text       = "";
            txtRFC.Text                     = "";
            ddlRegimenFiscal.SelectedIndex  = 0;
            txtCURP.Text                    = "";
            txtBanco.Text                   = "";
            txtCLABE.Text                   = "";
            txtCuentaBancaria.Text          = "";
            txtTitularCuenta.Text           = "";
            txtDiasCredito.Text             = "";
            txtLimiteCredito.Text           = "";
            txtPais.Text                    = "";
            txtCodigoPostal.Text            = "";
            txtEstado.Text                  = "";
            txtMunicipio.Text               = "";
            txtColonia.Text                 = "";
            txtNumExt.Text                  = "";
            txtNumInt.Text                  = "";
            txtReferencia.Text              = "";
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
    }
}
