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
    public partial class RegistrarClientes : Page
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
                CargarClientes();
            else
                if (ViewState["TotalRegistros"] != null)
                    gvClientes.VirtualItemCount = (int)ViewState["TotalRegistros"];
        }

        // ══ CARGA ═════════════════════════════════════════════════════════════
        private void CargarClientes()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx   = gvClientes.PageIndex;
            int pageSz    = gvClientes.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var query = db.Clientes.AsQueryable();

                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(c =>
                        c.Nombre.ToLower().Contains(buscar) ||
                        (c.Contacto != null && c.Contacto.ToLower().Contains(buscar)));

                if (filEst == "1") query = query.Where(c => c.Activo == true);
                else if (filEst == "0") query = query.Where(c => c.Activo == false);

                query = query.OrderBy(c => c.Nombre);

                int total = query.Count();
                lblResultados.Text = total == 1 ? "1 registro encontrado." : total + " registros encontrados.";
                ViewState["TotalRegistros"] = total;

                gvClientes.VirtualItemCount = total;
                gvClientes.DataSource = query.Skip(pageIdx * pageSz).Take(pageSz).ToList();
                gvClientes.DataBind();
            }
        }

        protected void gvClientes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvClientes.PageIndex = e.NewPageIndex;
            CargarClientes();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvClientes.PageIndex = 0;
            CargarClientes();
        }

        protected void btnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrEstado.SelectedIndex = 0;
            gvClientes.PageIndex = 0;
            CargarClientes();
        }

        // ══ GUARDAR NUEVO ═════════════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            string nombreTrim = (txtNombre.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombreTrim))
            {
                SetMsg("warning", "Campo obligatorio", "El nombre del cliente es obligatorio.", "modalNuevo");
                return;
            }

            using (var db = NuevoDb())
            {
                if (db.Clientes.Any(c => c.Nombre.ToLower() == nombreTrim.ToLower()))
                {
                    SetMsg("error", "Nombre duplicado", "Ya existe un cliente con el nombre '" + nombreTrim + "'.", "modalNuevo");
                    return;
                }
                try
                {
                    db.Clientes.InsertOnSubmit(new Clientes
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
                        UsoCFDI           = NullIfEmpty(ddlUsoCFDI.SelectedValue),
                        CURP              = NullIfEmpty(txtCURP.Text)?.ToUpper(),
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
                    CargarClientes();
                    SetMsg("success", "¡Guardado!", "El cliente fue registrado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error guardar cliente: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo guardar el cliente. Contacte al administrador.", "modalNuevo");
                }
            }
        }

        // ══ GUARDAR EDICIÓN ═══════════════════════════════════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnClienteID.Value)) { SetMsg("error", "Error", "No se identificó el cliente a editar."); return; }

            string nombreTrim = (txtNombreEdit.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombreTrim))
            {
                SetMsg("warning", "Campo obligatorio", "El nombre del cliente es obligatorio.", "modalEditar");
                return;
            }

            int clienteID = Convert.ToInt32(hdnClienteID.Value);

            using (var db = NuevoDb())
            {
                if (db.Clientes.Any(c => c.Nombre.ToLower() == nombreTrim.ToLower() && c.ClienteID != clienteID))
                {
                    SetMsg("error", "Nombre duplicado", "Ya existe otro cliente con el nombre '" + nombreTrim + "'.", "modalEditar");
                    return;
                }
                try
                {
                    var c2 = db.Clientes.FirstOrDefault(c => c.ClienteID == clienteID);
                    if (c2 == null) { SetMsg("error", "Error", "No se encontró el cliente a editar."); return; }

                    c2.Nombre            = nombreTrim;
                    c2.Contacto          = NullIfEmpty(txtContactoEdit.Text);
                    c2.Telefono          = NullIfEmpty(txtTelefonoEdit.Text);
                    c2.Email             = NullIfEmpty(txtEmailEdit.Text);
                    c2.PaginaWeb         = NullIfEmpty(txtPaginaWebEdit.Text);
                    c2.TipoEmpresa       = NullIfEmpty(ddlTipoEmpresaEdit.SelectedValue);
                    c2.Nacionalidad      = NullIfEmpty(txtNacionalidadEdit.Text);
                    c2.TipoPersona       = NullIfEmpty(ddlTipoPersonaEdit.SelectedValue);
                    c2.RazonSocialFiscal = NullIfEmpty(txtRazonSocialFiscalEdit.Text);
                    c2.RFC               = NullIfEmpty(txtRFCEdit.Text)?.ToUpper();
                    c2.RegimenFiscal     = NullIfEmpty(ddlRegimenFiscalEdit.SelectedValue);
                    c2.UsoCFDI           = NullIfEmpty(ddlUsoCFDIEdit.SelectedValue);
                    c2.CURP              = NullIfEmpty(txtCURPEdit.Text)?.ToUpper();
                    c2.Pais              = NullIfEmpty(txtPaisEdit.Text);
                    c2.CodigoPostal      = NullIfEmpty(txtCodigoPostalEdit.Text);
                    c2.Estado            = NullIfEmpty(txtEstadoEdit.Text);
                    c2.Municipio         = NullIfEmpty(txtMunicipioEdit.Text);
                    c2.Colonia           = NullIfEmpty(txtColoniaEdit.Text);
                    c2.NumExt            = NullIfEmpty(txtNumExtEdit.Text);
                    c2.NumInt            = NullIfEmpty(txtNumIntEdit.Text);
                    c2.Referencia        = NullIfEmpty(txtReferenciaEdit.Text);

                    db.SubmitChanges();
                    CargarClientes();
                    SetMsg("success", "¡Actualizado!", "El cliente fue actualizado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error editar cliente: " + ex.Message);
                    SetMsg("error", "Error del sistema", "No se pudo actualizar el cliente. Contacte al administrador.", "modalEditar");
                }
            }
        }

        // ══ TOGGLE ════════════════════════════════════════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleClienteID.Value)) return;
            int id = Convert.ToInt32(hdnToggleClienteID.Value);
            using (var db = NuevoDb())
            {
                try
                {
                    var c2 = db.Clientes.FirstOrDefault(c => c.ClienteID == id);
                    if (c2 == null) return;
                    c2.Activo = !c2.Activo;
                    db.SubmitChanges();
                    CargarClientes();
                    SetMsg("success", "¡Listo!", "El cliente fue " + (c2.Activo ? "activado" : "desactivado") + " correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error toggle: " + ex.Message);
                    SetMsg("error", "Error", "No se pudo cambiar el estatus del cliente.");
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
            ddlUsoCFDI.SelectedIndex        = 0;
            txtCURP.Text                    = "";
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
    }
}
