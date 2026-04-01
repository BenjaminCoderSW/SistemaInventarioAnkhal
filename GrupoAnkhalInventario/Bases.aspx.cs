using GrupoAnkhalInventario.Helpers;
﻿using GrupoAnkhalInventario.Modelo;
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
                CargarBases();
            }
            else
            {
                // Restaurar VirtualItemCount antes de que se ejecuten los eventos
                if (ViewState["TotalRegistros"] != null)
                    gvBases.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ══ CARGA / FILTRADO CON PAGINACIÓN EN SQL ════════════════════════════
        private void CargarBases()
        {
            string buscar = (txtBuscar.Text ?? "").Trim().ToLower();
            string filTipo = ddlFiltrTipo.SelectedValue;
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvBases.PageIndex;
            int pageSz = gvBases.PageSize;

            using (var db = NuevoDb(tracking: false))
            {
                var query = db.Bases.AsQueryable();

                // ── Filtros ──────────────────────────────────────────────────
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(b =>
                        b.Codigo.ToLower().Contains(buscar) ||
                        b.Nombre.ToLower().Contains(buscar));

                if (!string.IsNullOrEmpty(filTipo))
                    query = query.Where(b => b.Tipo == filTipo);

                if (filEst == "1") query = query.Where(b => b.Activo == true);
                else if (filEst == "0") query = query.Where(b => b.Activo == false);

                query = query.OrderBy(b => b.Codigo);

                // ── COUNT en SQL ─────────────────────────────────────────────
                int totalRegistros = query.Count();

                lblResultados.Text = totalRegistros == 1
                    ? "1 registro encontrado."
                    : totalRegistros + " registros encontrados.";

                ViewState["TotalRegistros"] = totalRegistros;

                // ── PAGINACIÓN EN SQL ────────────────────────────────────────
                var pagina = query
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                gvBases.VirtualItemCount = totalRegistros;
                gvBases.DataSource = pagina;
                gvBases.DataBind();
            }
        }

        // ══ PAGINACIÓN ════════════════════════════════════════════════════════
        protected void gvBases_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvBases.PageIndex = e.NewPageIndex;
            CargarBases();
        }

        // ══ BUSCAR / LIMPIAR ══════════════════════════════════════════════════
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

        // ══ GUARDAR NUEVA BASE ════════════════════════════════════════════════
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(ddlTipo.SelectedValue))
            {
                SetMensajePendiente("warning", "Campos obligatorios",
                    "Código, nombre y tipo son obligatorios.", "modalNueva");
                return;
            }

            string codigoUpper = txtCodigo.Text.Trim().ToUpper();
            string nombreTrim = txtNombre.Text.Trim();

            using (var db = NuevoDb())
            {
                if (db.Bases.Any(b => b.Codigo == codigoUpper))
                {
                    SetMensajePendiente("error", "Código duplicado",
                        "Ya existe una base con el código '" + codigoUpper + "'.", "modalNueva");
                    return;
                }

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
                        MetaDiaria = ParseMeta(txtMetaDiaria.Text),
                        MetaSemanal = ParseMeta(txtMetaSemanal.Text),
                        MetaMensual = ParseMeta(txtMetaMensual.Text),
                        Activo = true,
                        FechaCreacion = AppHelper.Ahora,
                        UsuarioAltaID = Convert.ToInt32(Session["ClaveID"])
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
        }

        // ══ GUARDAR EDICIÓN CON CONTROL DE CONCURRENCIA ══════════════════════
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

            using (var db = NuevoDb())
            {
                if (db.Bases.Any(b => b.Codigo == codigoUpper && b.BaseID != baseID))
                {
                    SetMensajePendiente("error", "Código duplicado",
                        "Ya existe otra base con el código '" + codigoUpper + "'.", "modalEditar");
                    return;
                }

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

                    // ── Control de concurrencia ──────────────────────────────
                    byte[] rowVersionOriginal = null;
                    if (!string.IsNullOrEmpty(hdnRowVersion.Value))
                        rowVersionOriginal = Convert.FromBase64String(hdnRowVersion.Value);

                    if (rowVersionOriginal != null &&
                        base_.RowVersion != null &&
                        !rowVersionOriginal.SequenceEqual(base_.RowVersion.ToArray()))
                    {
                        SetMensajePendiente("warning",
                            "Registro modificado",
                            "Otro usuario acaba de modificar esta base justo ahora. " +
                            "Salte y vuelve a entrar a Bases para ver los datos actuales y poder editar.",
                            "modalEditar");
                        return;
                    }

                    base_.Codigo = codigoUpper;
                    base_.Nombre = nombreTrim;
                    base_.Tipo = ddlTipoEdit.SelectedValue;
                    base_.Responsable = txtResponsableEdit.Text.Trim();
                    base_.Telefono = txtTelefonoEdit.Text.Trim();
                    base_.Direccion = txtDireccionEdit.Text.Trim();
                    base_.MetaDiaria = ParseMeta(txtMetaDiariaEdit.Text);
                    base_.MetaSemanal = ParseMeta(txtMetaSemanalEdit.Text);
                    base_.MetaMensual = ParseMeta(txtMetaMensualEdit.Text);
                    base_.FechaModif = AppHelper.Ahora;
                    base_.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);

                    db.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);

                    CargarBases();
                    SetMensajePendiente("success", "¡Actualizado!", "La base fue actualizada correctamente.");
                }
                catch (System.Data.Linq.ChangeConflictException)
                {
                    SetMensajePendiente("warning",
                        "Conflicto de edición",
                        "Otro usuario guardó cambios en esta base al mismo tiempo. " +
                        "Recarga el registro para ver los datos más recientes.",
                        "modalEditar");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al editar base: " + ex.Message);
                    SetMensajePendiente("error", "Error del sistema",
                        "No se pudo actualizar la base. Contacte al administrador.", "modalEditar");
                }
            }
        }

        // ══ TOGGLE desde botón del grid (compatibilidad) ══════════════════════
        protected void btnToggle_Click(object sender, EventArgs e) { }

        // ══ TOGGLE desde botón oculto (SweetAlert) ════════════════════════════
        protected void btnToggleHidden_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hdnToggleBaseID.Value)) return;

            int baseID = Convert.ToInt32(hdnToggleBaseID.Value);

            using (var db = NuevoDb())
            {
                try
                {
                    var b = db.Bases.FirstOrDefault(x => x.BaseID == baseID);
                    if (b == null) return;

                    b.Activo = !b.Activo;
                    b.FechaModif = AppHelper.Ahora;
                    b.UsuarioModifID = Convert.ToInt32(Session["ClaveID"]);
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
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private void SetMensajePendiente(string icon, string title, string text, string modal = null)
        {
            var obj = new { icon, title, text, modal = modal ?? "" };
            hdnMensajePendiente.Value = new JavaScriptSerializer().Serialize(obj);
        }

        public string RowVersionBase64(object rowVersion)
        {
            if (rowVersion == null) return "";
            if (rowVersion is System.Data.Linq.Binary)
                return Convert.ToBase64String(((System.Data.Linq.Binary)rowVersion).ToArray());
            if (rowVersion is byte[])
                return Convert.ToBase64String((byte[])rowVersion);
            return "";
        }

        /// <summary>
        /// Parsea un decimal ≥ 0. Acepta tanto coma como punto como separador decimal.
        /// </summary>
        private decimal ParseMeta(string valor)
        {
            decimal resultado;
            if (decimal.TryParse(valor,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out resultado) && resultado >= 0)
                return resultado;

            // Segundo intento con cultura local (por si el navegador manda coma)
            if (decimal.TryParse(valor,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out resultado) && resultado >= 0)
                return resultado;

            return 0m;
        }

        private void LimpiarNueva()
        {
            txtCodigo.Text = "";
            txtNombre.Text = "";
            ddlTipo.SelectedIndex = 0;
            txtResponsable.Text = "";
            txtTelefono.Text = "";
            txtDireccion.Text = "";
            txtMetaDiaria.Text = "0";
            txtMetaSemanal.Text = "0";
            txtMetaMensual.Text = "0";
        }
    }
}