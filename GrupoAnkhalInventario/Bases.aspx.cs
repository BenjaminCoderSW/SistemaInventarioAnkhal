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
    public partial class Bases : Page
    {
        public InventarioAnkhalDBDataContext db = new InventarioAnkhalDBDataContext(
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UsuarioID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarBases();
            }
        }

        // ── CARGA / FILTRADO ──────────────────────────────────
        private void CargarBases()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;

            var query = db.Bases.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(b =>
                    b.Codigo.ToLower().Contains(buscar) ||
                    b.Nombre.ToLower().Contains(buscar));

            if (!string.IsNullOrEmpty(filTipo))
                query = query.Where(b => b.Tipo == filTipo);

            if (filEst == "1")
                query = query.Where(b => b.Activo == true);
            else if (filEst == "0")
                query = query.Where(b => b.Activo == false);

            var lista = query.OrderBy(b => b.Codigo).ToList();

            lblResultados.Text = lista.Count == 1
                ? "1 registro encontrado."
                : lista.Count + " registros encontrados.";

            gvBases.DataSource = lista;
            gvBases.DataBind();
        }

        // ── PAGINACIÓN ────────────────────────────────────────
        protected void gvBases_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvBases.PageIndex = e.NewPageIndex;
            CargarBases();
        }

        // ── BUSCAR / LIMPIAR ──────────────────────────────────
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvBases.PageIndex = 0;
            CargarBases();
        }

        protected void btnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrTipo.SelectedIndex = 0;
            ddlFiltrEstado.SelectedIndex = 0;
            gvBases.PageIndex = 0;
            CargarBases();
        }

        // ── GUARDAR NUEVA ─────────────────────────────────────
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(ddlTipo.SelectedValue))
            {
                SetMensajePendiente("warning", "Campos obligatorios", "Código, nombre y tipo son obligatorios.", "modalNueva");
                return;
            }

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

            // Validar código duplicado
            if (db.Bases.Any(b => b.Codigo == codigoUpper))
            {
                SetMensajePendiente("error", "Código duplicado",
                    "Ya existe una base con el código '" + codigoUpper + "'.", "modalNueva");
                return;
            }

            // Validar nombre duplicado
            if (db.Bases.Any(b => b.Nombre.ToLower() == nombreTrim.ToLower()))
            {
                SetMensajePendiente("error", "Nombre duplicado",
                    "Ya existe una base con el nombre '" + nombreTrim + "'.", "modalNueva");
                return;
            }

            try
            {
                var nueva = new GrupoAnkhalInventario.Modelo.Bases
                {
                    Codigo = codigoUpper,
                    Nombre = nombreTrim,
                    Tipo = ddlTipo.SelectedValue,
                    Responsable = txtResponsable.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    MetaTarimas = ParseMeta(txtMetaTarimas.Text),
                    MetaCajas = ParseMeta(txtMetaCajas.Text),
                    MetaAccesorios = ParseMeta(txtMetaAccesorios.Text),
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    UsuarioAltaID = Convert.ToInt32(Session["UsuarioID"])
                };

                db.Bases.InsertOnSubmit(nueva);
                db.SubmitChanges();

                LimpiarNueva();
                CargarBases();
                SetMensajePendiente("success", "¡Guardado!", "La base fue creada correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar base: " + ex.Message);
                SetMensajePendiente("error", "Error del sistema",
                    "No se pudo guardar la base. Contacte al administrador.", "modalNueva");
            }
        }

        // ── GUARDAR EDICIÓN ───────────────────────────────────
        protected void btnGuardarEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnBaseID.Value) ||
                string.IsNullOrWhiteSpace(txtCodigoEdit.Text) ||
                string.IsNullOrWhiteSpace(txtNombreEdit.Text) ||
                string.IsNullOrWhiteSpace(ddlTipoEdit.SelectedValue))
            {
                SetMensajePendiente("warning", "Campos obligatorios",
                    "Código, nombre y tipo son obligatorios.", "modalEditar");
                return;
            }

            int baseID = Convert.ToInt32(hdnBaseID.Value);
            string codigoUpper = txtCodigoEdit.Text.Trim().ToUpper();
            string nombreTrim = txtNombreEdit.Text.Trim();

            // Validar código duplicado (excluir el registro actual)
            if (db.Bases.Any(b => b.Codigo == codigoUpper && b.BaseID != baseID))
            {
                SetMensajePendiente("error", "Código duplicado",
                    "Ya existe otra base con el código '" + codigoUpper + "'.", "modalEditar");
                return;
            }

            // Validar nombre duplicado (excluir el registro actual)
            if (db.Bases.Any(b => b.Nombre.ToLower() == nombreTrim.ToLower() && b.BaseID != baseID))
            {
                SetMensajePendiente("error", "Nombre duplicado",
                    "Ya existe otra base con el nombre '" + nombreTrim + "'.", "modalEditar");
                return;
            }

            try
            {
                var base_ = db.Bases.FirstOrDefault(b => b.BaseID == baseID);
                if (base_ == null)
                {
                    SetMensajePendiente("error", "Error", "No se encontró la base a editar.");
                    return;
                }

                base_.Codigo = codigoUpper;
                base_.Nombre = nombreTrim;
                base_.Tipo = ddlTipoEdit.SelectedValue;
                base_.Responsable = txtResponsableEdit.Text.Trim();
                base_.Telefono = txtTelefonoEdit.Text.Trim();
                base_.Direccion = txtDireccionEdit.Text.Trim();
                base_.MetaTarimas = ParseMeta(txtMetaTarimasEdit.Text);
                base_.MetaCajas = ParseMeta(txtMetaCajasEdit.Text);
                base_.MetaAccesorios = ParseMeta(txtMetaAccesoriosEdit.Text);
                base_.FechaModif = DateTime.Now;
                base_.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);

                db.SubmitChanges();
                CargarBases();
                SetMensajePendiente("success", "¡Actualizado!", "La base fue actualizada correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al editar base: " + ex.Message);
                SetMensajePendiente("error", "Error del sistema",
                    "No se pudo actualizar la base. Contacte al administrador.", "modalEditar");
            }
        }

        // ── TOGGLE desde botón del grid (compatibilidad) ──────
        protected void btnToggle_Click(object sender, EventArgs e) { }

        // ── TOGGLE desde botón oculto (SweetAlert) ────────────
        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleBaseID.Value)) return;

            int baseID = Convert.ToInt32(hdnToggleBaseID.Value);

            try
            {
                var b = db.Bases.FirstOrDefault(x => x.BaseID == baseID);
                if (b == null) return;

                b.Activo = !b.Activo;
                b.FechaModif = DateTime.Now;
                b.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);
                db.SubmitChanges();

                string estado = b.Activo ? "activada" : "desactivada";
                CargarBases();
                SetMensajePendiente("success", "¡Listo!", "La base fue " + estado + " correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cambiar estatus: " + ex.Message);
                SetMensajePendiente("error", "Error", "No se pudo cambiar el estatus de la base.");
            }
        }

        // ── HELPERS ───────────────────────────────────────────

        /// <summary>
        /// Serializa un mensaje en el HiddenField para que el JS lo dispare
        /// después del postback, evitando el conflicto con el DOM del modal.
        /// </summary>
        private void SetMensajePendiente(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            var ser = new JavaScriptSerializer();
            hdnMensajePendiente.Value = ser.Serialize(obj);
        }

        private int ParseMeta(string valor)
        {
            int resultado;
            return int.TryParse(valor, out resultado) && resultado >= 0 ? resultado : 0;
        }

        private void LimpiarNueva()
        {
            txtCodigo.Text = "";
            txtNombre.Text = "";
            ddlTipo.SelectedIndex = 0;
            txtResponsable.Text = "";
            txtTelefono.Text = "";
            txtDireccion.Text = "";
            txtMetaTarimas.Text = "0";
            txtMetaCajas.Text = "0";
            txtMetaAccesorios.Text = "0";
        }
    }
}