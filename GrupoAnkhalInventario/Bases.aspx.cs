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
        // ── Cadena de conexión centralizada ───────────────────────────────────
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ── Helper: crea un DataContext nuevo ─────────────────────────────────
        // tracking: false → solo lectura (menos memoria, sin caché de identidad)
        // tracking: true  → lectura + escritura (necesario para Insert/Update/Delete)
        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ─────────────────────────────────────────────────────────────────────
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
            else
            {
                // En cada postback (toggle, guardar, cambio de página, etc.)
                // el GridView pierde el VirtualItemCount porque el ciclo de vida
                // de ASP.NET no lo persiste automáticamente. Sin este valor el
                // paginador no puede calcular cuántos botones dibujar.
                // Lo restauramos desde ViewState ANTES de que se ejecute
                // cualquier evento, así el paginador siempre está sincronizado.
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
            int pageSz = gvBases.PageSize;   // 15

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
                // Una sola ida a la BD para saber el total real con los filtros
                // aplicados. Esto actualiza el paginador correctamente cada vez
                // que cambia la búsqueda.
                int totalRegistros = query.Count();

                lblResultados.Text = totalRegistros == 1
                    ? "1 registro encontrado."
                    : totalRegistros + " registros encontrados.";

                // ── Guardar total en ViewState ───────────────────────────────
                // Se guarda ANTES del DataBind para que el Page_Load de los
                // postbacks siguientes pueda restaurar VirtualItemCount a tiempo.
                ViewState["TotalRegistros"] = totalRegistros;

                // ── PAGINACIÓN EN SQL ────────────────────────────────────────
                // Skip/Take se traducen a OFFSET/FETCH NEXT en SQL Server.
                // Solo viajan los N registros de la página actual, nunca toda la tabla.
                var pagina = query
                    .Skip(pageIdx * pageSz)
                    .Take(pageSz)
                    .ToList();

                // VirtualItemCount le dice al GridView el total real para que
                // dibuje los botones de página correctamente con AllowCustomPaging.
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
        }

        // ══ GUARDAR EDICIÓN CON CONTROL DE CONCURRENCIA ══════════════════════
        //
        // ¿Cómo funciona el control de concurrencia?
        // ─────────────────────────────────────────────────────────────────────
        // 1. Al guardar, se lee el RowVersion actual de la BD y se compara con
        //    el que se guardó en ViewState cuando se abrió el modal.
        // 2. Si no coinciden: otro usuario editó el registro en el ínterin.
        //    Se muestra un aviso en lugar de pisar los cambios ajenos.
        // 3. LINQ to SQL también lanza ChangeConflictException como red de
        //    seguridad adicional al hacer SubmitChanges.
        //
        // REQUISITO: columna RowVersion en la tabla Bases + DataContext actualizado.
        //   ALTER TABLE [dbo].[Bases] ADD [RowVersion] rowversion NOT NULL;
        // ─────────────────────────────────────────────────────────────────────
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

                    // ── Control de concurrencia ──────────────────────────────────────
                    // El RowVersion que vio el usuario cuando abrió el modal viaja de
                    // vuelta en hdnRowVersion como Base64. Lo comparamos contra el que
                    // está en la BD ahora. Si no coinciden, alguien más guardó antes.
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
                    base_.MetaTarimas = ParseMeta(txtMetaTarimasEdit.Text);
                    base_.MetaCajas = ParseMeta(txtMetaCajasEdit.Text);
                    base_.MetaAccesorios = ParseMeta(txtMetaAccesoriosEdit.Text);
                    base_.FechaModif = DateTime.Now;
                    base_.UsuarioModifID = Convert.ToInt32(Session["UsuarioID"]);

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