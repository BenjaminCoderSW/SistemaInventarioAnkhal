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
        // ── Cadena de conexión centralizada ───────────────────────────────────
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ── Helper: crea un DataContext nuevo ─────────────────────────────────
        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarClientes();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvClientes.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ══ CARGA / FILTRADO CON PAGINACIÓN ══════════════════════════════════
        private void CargarClientes()
        {
            string buscar  = (txtBuscar.Text ?? "").Trim().ToLower();
            string filEst  = ddlFiltrEstado.SelectedValue;
            int pageIdx    = gvClientes.PageIndex;
            int pageSz     = gvClientes.PageSize;

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

                int totalRegistros = query.Count();

                lblResultados.Text = totalRegistros == 1
                    ? "1 registro encontrado."
                    : totalRegistros + " registros encontrados.";

                ViewState["TotalRegistros"] = totalRegistros;

                var pagina = query
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                gvClientes.VirtualItemCount = totalRegistros;
                gvClientes.DataSource = pagina;
                gvClientes.DataBind();
            }
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════
        protected void gvClientes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvClientes.PageIndex = e.NewPageIndex;
            CargarClientes();
        }

        // ══ BUSCAR / LIMPIAR ══════════════════════════════════════════════════
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

        // ══ GUARDAR NUEVO CLIENTE ═════════════════════════════════════════════
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
                    SetMsg("error", "Nombre duplicado",
                        "Ya existe un cliente con el nombre '" + nombreTrim + "'.", "modalNuevo");
                    return;
                }

                try
                {
                    var nuevo = new Clientes
                    {
                        Nombre       = nombreTrim,
                        Contacto     = NullIfEmpty(txtContacto.Text),
                        Telefono     = NullIfEmpty(txtTelefono.Text),
                        Email        = NullIfEmpty(txtEmail.Text),
                        Direccion    = NullIfEmpty(txtDireccion.Text),
                        Activo       = true,
                        FechaAlta    = DateTime.Now,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
                    };

                    db.Clientes.InsertOnSubmit(nuevo);
                    db.SubmitChanges();

                    LimpiarNuevo();
                    CargarClientes();
                    SetMsg("success", "¡Guardado!", "El cliente fue registrado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al guardar cliente: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo guardar el cliente. Contacte al administrador.", "modalNuevo");
                }
            }
        }

        // ══ GUARDAR EDICIÓN ═══════════════════════════════════════════════════
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnClienteID.Value))
            {
                SetMsg("error", "Error", "No se identificó el cliente a editar.");
                return;
            }

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
                    SetMsg("error", "Nombre duplicado",
                        "Ya existe otro cliente con el nombre '" + nombreTrim + "'.", "modalEditar");
                    return;
                }

                try
                {
                    var cliente = db.Clientes.FirstOrDefault(c => c.ClienteID == clienteID);
                    if (cliente == null)
                    {
                        SetMsg("error", "Error", "No se encontró el cliente a editar.");
                        return;
                    }

                    cliente.Nombre    = nombreTrim;
                    cliente.Contacto  = NullIfEmpty(txtContactoEdit.Text);
                    cliente.Telefono  = NullIfEmpty(txtTelefonoEdit.Text);
                    cliente.Email     = NullIfEmpty(txtEmailEdit.Text);
                    cliente.Direccion = NullIfEmpty(txtDireccionEdit.Text);

                    db.SubmitChanges();

                    CargarClientes();
                    SetMsg("success", "¡Actualizado!", "El cliente fue actualizado correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al editar cliente: " + ex.Message);
                    SetMsg("error", "Error del sistema",
                        "No se pudo actualizar el cliente. Contacte al administrador.", "modalEditar");
                }
            }
        }

        // ══ TOGGLE (compatibilidad con botón del grid) ════════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        // ══ TOGGLE desde botón oculto (SweetAlert) ════════════════════════════
        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleClienteID.Value)) return;

            int clienteID = Convert.ToInt32(hdnToggleClienteID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    var cliente = db.Clientes.FirstOrDefault(c => c.ClienteID == clienteID);
                    if (cliente == null) return;

                    cliente.Activo = !cliente.Activo;
                    db.SubmitChanges();

                    string estado = cliente.Activo ? "activado" : "desactivado";
                    CargarClientes();
                    SetMsg("success", "¡Listo!", "El cliente fue " + estado + " correctamente.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al cambiar estatus: " + ex.Message);
                    SetMsg("error", "Error", "No se pudo cambiar el estatus del cliente.");
                }
            }
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private void SetMsg(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = new JavaScriptSerializer().Serialize(obj);
        }

        private void LimpiarNuevo()
        {
            txtNombre.Text    = "";
            txtContacto.Text  = "";
            txtTelefono.Text  = "";
            txtEmail.Text     = "";
            txtDireccion.Text = "";
        }

        private static string NullIfEmpty(string valor)
        {
            var v = (valor ?? "").Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }
    }
}
