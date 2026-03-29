using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;

namespace GrupoAnkhalInventario
{
    public partial class Usuarios : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarRoles();
                CargarBasesChecklist();
                CargarEmpleadosDisponibles();
                CargarGrid();
            }
            else
            {
                if (ViewState["TotalRegistros"] != null)
                    gvUsuarios.VirtualItemCount = (int)ViewState["TotalRegistros"];
            }
        }

        // ══ GRID ══════════════════════════════════════════════════════════════

        private void CargarGrid()
        {
            string buscar = (txtBuscar.Text ?? "").Trim();
            string filEst = ddlFiltrEstado.SelectedValue;
            int pageIdx = gvUsuarios.PageIndex;
            int pageSz = gvUsuarios.PageSize;

            var where = new System.Text.StringBuilder(" WHERE 1=1 ");
            if (!string.IsNullOrEmpty(buscar))
                where.Append(@" AND (
                    Nombre          LIKE @buscar OR
                    ApellidoPaterno LIKE @buscar OR
                    NumeroEmpleado  LIKE @buscar OR
                    Usuario         LIKE @buscar OR
                    (Nombre + ' ' + ApellidoPaterno) LIKE @buscar
                )");

            if (filEst == "1") where.Append(" AND Activo = 1 ");
            else if (filEst == "0") where.Append(" AND Activo = 0 ");

            string sqlCount = "SELECT COUNT(*) FROM DatosUsuario" + where;
            string sqlData = @"
                SELECT du.ClaveID, du.UsuarioID, du.Nombre, du.ApellidoPaterno, du.ApellidoMaterno,
                       du.NumeroEmpleado, du.Telefono, du.TelefonoFamiliar, du.Email,
                       du.Usuario, du.Rol, du.RolID, du.Activo, du.Foto,
                       (du.Nombre + ' ' + du.ApellidoPaterno +
                        ISNULL(' ' + NULLIF(du.ApellidoMaterno,''), '')) AS NombreCompleto,
                       ISNULL((SELECT STRING_AGG(CAST(ub.BaseID AS VARCHAR(10)), ',')
                               FROM dbo.UsuarioBases ub WHERE ub.ClaveID = du.ClaveID), '') AS BaseIDs
                FROM DatosUsuario du" + where +
                @" ORDER BY ApellidoPaterno, Nombre
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            try
            {
                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    int total = 0;
                    using (var cmd = new SqlCommand(sqlCount, cn))
                    {
                        if (!string.IsNullOrEmpty(buscar))
                            cmd.Parameters.AddWithValue("@buscar", "%" + buscar + "%");
                        total = (int)cmd.ExecuteScalar();
                    }

                    lblResultados.Text = total == 1 ? "1 registro encontrado." : total + " registros encontrados.";
                    ViewState["TotalRegistros"] = total;
                    gvUsuarios.VirtualItemCount = total;

                    using (var cmd = new SqlCommand(sqlData, cn))
                    {
                        if (!string.IsNullOrEmpty(buscar))
                            cmd.Parameters.AddWithValue("@buscar", "%" + buscar + "%");
                        cmd.Parameters.AddWithValue("@offset", pageIdx * pageSz);
                        cmd.Parameters.AddWithValue("@pageSize", pageSz);

                        var dt = new DataTable();
                        using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
                        gvUsuarios.DataSource = dt;
                        gvUsuarios.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Usuarios.CargarGrid] " + ex.Message);
                SetMsg("error", "Error", "No se pudo cargar la lista de usuarios.");
            }
        }

        // Foto viene de asistencia (campo Foto de la vista DatosUsuario)
        protected string ObtenerFotoBase64(object fotoObj)
        {
            if (fotoObj == null || fotoObj == DBNull.Value) return "dist/img/user2-160x160.jpg";
            var bytes = fotoObj as byte[];
            if (bytes == null || bytes.Length == 0) return "dist/img/user2-160x160.jpg";
            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }

        protected string ConfirmarToggleJS(bool activo, string nombre)
        {
            string accion = activo ? "desactivar" : "activar";
            return "return confirm('¿" + accion.Substring(0, 1).ToUpper() + accion.Substring(1)
                   + " al usuario " + nombre.Replace("'", "\\'") + "?');";
        }

        // ══ PAGINACIÓN / BÚSQUEDA ══════════════════════════════════════════════

        protected void gvUsuarios_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvUsuarios.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvUsuarios.PageIndex = 0;
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = "";
            ddlFiltrEstado.SelectedIndex = 0;
            gvUsuarios.PageIndex = 0;
            CargarGrid();
        }

        // ══ CATÁLOGOS ══════════════════════════════════════════════════════════

        private void CargarBasesChecklist()
        {
            const string sql = "SELECT BaseID, Codigo, Nombre FROM dbo.Bases WHERE Activo = 1 ORDER BY Nombre";
            var dt = EjecutarQuery(sql, null);

            foreach (var cbl in new CheckBoxList[] { cblBasesAgregar, cblBasesEditar })
            {
                cbl.Items.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    string etiqueta = row["Nombre"].ToString() + " (" + row["Codigo"].ToString() + ")";
                    cbl.Items.Add(new ListItem(etiqueta, row["BaseID"].ToString()));
                }
            }
        }

        private void GuardarAsignacionBases(SqlConnection cn, int claveID, CheckBoxList cbl, int rolID)
        {
            // Borrar asignaciones actuales
            using (var cmd = new SqlCommand("DELETE FROM dbo.UsuarioBases WHERE ClaveID = @cid", cn))
            {
                cmd.Parameters.AddWithValue("@cid", claveID);
                cmd.ExecuteNonQuery();
            }

            // Si es Administrador, no insertar registros (AppHelper retorna null y ve todo)
            string rolNombre = "";
            using (var cmd = new SqlCommand("SELECT Nombre FROM dbo.Roles WHERE RolID = @rid", cn))
            {
                cmd.Parameters.AddWithValue("@rid", rolID);
                var res = cmd.ExecuteScalar();
                if (res != null) rolNombre = res.ToString();
            }
            if (rolNombre == "Administrador") return;

            int asigPor = Convert.ToInt32(Session["ClaveID"]);
            const string sqlIns = @"
                INSERT INTO dbo.UsuarioBases (ClaveID, BaseID, FechaAsignacion, AsignadoPorID)
                VALUES (@cid, @bid, GETDATE(), @asigPor)";

            foreach (ListItem item in cbl.Items)
            {
                if (!item.Selected) continue;
                using (var cmd = new SqlCommand(sqlIns, cn))
                {
                    cmd.Parameters.AddWithValue("@cid", claveID);
                    cmd.Parameters.AddWithValue("@bid", int.Parse(item.Value));
                    cmd.Parameters.AddWithValue("@asigPor", asigPor);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CargarRoles()
        {
            const string sql = "SELECT RolID, Nombre FROM Roles WHERE Activo = 1 ORDER BY Nombre";
            var dt = EjecutarQuery(sql, null);

            foreach (var ddl in new[] { ddlRolAgregar, ddlRolEditar })
            {
                ddl.Items.Clear();
                ddl.Items.Add(new ListItem("-- Seleccione --", "0"));
                foreach (DataRow row in dt.Rows)
                    ddl.Items.Add(new ListItem(row["Nombre"].ToString(), row["RolID"].ToString()));
            }
        }

        private void CargarEmpleadosDisponibles()
        {
            const string sql = @"
                SELECT tu.IdUsuario,
                       tu.Nombre + ' ' + tu.ApellidoPaterno
                       + ISNULL(' ' + NULLIF(tu.ApellidoMaterno,''), '')
                       + ' — ' + ISNULL(tu.NumeroEmpleado, '') AS NombreCompleto
                FROM AsistenciaAnkhal.dbo.tUsuario tu
                WHERE tu.Estatus = 1
                  AND NOT EXISTS (SELECT 1 FROM dbo.Usuario u WHERE u.UsuarioID = tu.IdUsuario)
                ORDER BY tu.Nombre";

            var dt = EjecutarQuery(sql, null);
            ddlEmpleado.Items.Clear();
            ddlEmpleado.Items.Add(new ListItem("-- Seleccione un empleado --", "0"));
            foreach (DataRow row in dt.Rows)
                ddlEmpleado.Items.Add(new ListItem(row["NombreCompleto"].ToString(),
                                                   row["IdUsuario"].ToString()));
        }

        // ══ AUTOPOSTBACK: SELECCIÓN DE EMPLEADO ═══════════════════════════════

        protected void ddlEmpleado_SelectedIndexChanged(object sender, EventArgs e)
        {
            hdnReabrirModalAgregar.Value = "1";

            if (ddlEmpleado.SelectedValue == "0")
            {
                divInfoEmpleado.Visible = false;
                return;
            }

            int idUsuario = Convert.ToInt32(ddlEmpleado.SelectedValue);

            const string sql = @"
                SELECT Nombre, ApellidoPaterno, ApellidoMaterno,
                       NumeroEmpleado, Telefono, TelefonoFamiliar, Email, Foto
                FROM AsistenciaAnkhal.dbo.tUsuario
                WHERE IdUsuario = @id";

            var dt = EjecutarQuery(sql, new Dictionary<string, object> { { "@id", idUsuario } });
            if (dt.Rows.Count == 0) { divInfoEmpleado.Visible = false; return; }

            var row = dt.Rows[0];
            divInfoEmpleado.Visible = true;

            lblNombreEmp.Text = row["Nombre"] + " " + row["ApellidoPaterno"]
                                 + (row["ApellidoMaterno"] != DBNull.Value && row["ApellidoMaterno"].ToString() != ""
                                    ? " " + row["ApellidoMaterno"] : "");
            lblNumEmpEmp.Text = row["NumeroEmpleado"] != DBNull.Value ? row["NumeroEmpleado"].ToString() : "—";
            lblTelEmp.Text = row["Telefono"] != DBNull.Value ? row["Telefono"].ToString() : "—";
            lblTelFamEmp.Text = row["TelefonoFamiliar"] != DBNull.Value ? row["TelefonoFamiliar"].ToString() : "—";
            lblEmailEmp.Text = row["Email"] != DBNull.Value ? row["Email"].ToString() : "—";

            if (row["Foto"] != DBNull.Value)
            {
                byte[] fb = (byte[])row["Foto"];
                imgFotoEmpleado.ImageUrl = fb.Length > 0
                    ? "data:image/jpeg;base64," + Convert.ToBase64String(fb)
                    : "dist/img/user2-160x160.jpg";
            }
            else
            {
                imgFotoEmpleado.ImageUrl = "dist/img/user2-160x160.jpg";
            }
        }

        // ══ GUARDAR NUEVO USUARIO ══════════════════════════════════════════════

        protected void btnGuardarAgregar_Click(object sender, EventArgs e)
        {
            if (ddlEmpleado.SelectedValue == "0")
            { SetMsg("warning", "Empleado requerido", "Debe seleccionar un empleado."); ReabrirModal(); return; }

            if (ddlRolAgregar.SelectedValue == "0")
            { SetMsg("warning", "Rol requerido", "Debe seleccionar un rol."); ReabrirModal(); return; }

            if (string.IsNullOrWhiteSpace(txtUsuarioAgregar.Text))
            { SetMsg("warning", "Usuario requerido", "Debe ingresar el nombre de usuario de acceso."); ReabrirModal(); return; }

            string clave = txtClaveAgregar.Text;
            if (string.IsNullOrWhiteSpace(clave) || clave.Length < 6)
            { SetMsg("warning", "Contraseña inválida", "La contraseña debe tener al menos 6 caracteres."); ReabrirModal(); return; }

            if (clave != txtClaveConfirmarAgregar.Text)
            { SetMsg("warning", "Contraseñas distintas", "Las contraseñas no coinciden."); ReabrirModal(); return; }

            int idEmpleado = Convert.ToInt32(ddlEmpleado.SelectedValue);
            int idRol = Convert.ToInt32(ddlRolAgregar.SelectedValue);
            string usuario = txtUsuarioAgregar.Text.Trim();

            try
            {
                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    // Verificar que el empleado aún no tenga cuenta
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Usuario WHERE UsuarioID = @uid", cn))
                    {
                        cmd.Parameters.AddWithValue("@uid", idEmpleado);
                        if ((int)cmd.ExecuteScalar() > 0)
                        { SetMsg("error", "Ya registrado", "Este empleado ya tiene cuenta en el sistema de inventario."); ReabrirModal(); return; }
                    }

                    // Verificar login único
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Usuario WHERE Usuario = @usr", cn))
                    {
                        cmd.Parameters.AddWithValue("@usr", usuario);
                        if ((int)cmd.ExecuteScalar() > 0)
                        { SetMsg("error", "Usuario duplicado", "Ya existe otro usuario con ese nombre de acceso."); ReabrirModal(); return; }
                    }

                    string claveHash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor: 12);

                    // INSERT en tabla Usuario (sin Foto ni TelefonoFamiliar — vienen de asistencia)
                    int nuevoClaveID;
                    const string sqlInsert = @"
                        INSERT INTO dbo.Usuario (UsuarioID, Usuario, Clave, FechaAlta, UsuarioAltaID)
                        VALUES (@uid, @usr, @clave, GETDATE(), @altaID);
                        SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(sqlInsert, cn))
                    {
                        cmd.Parameters.AddWithValue("@uid", idEmpleado);
                        cmd.Parameters.AddWithValue("@usr", usuario);
                        cmd.Parameters.AddWithValue("@clave", claveHash);
                        cmd.Parameters.AddWithValue("@altaID", Convert.ToInt32(Session["ClaveID"]));
                        nuevoClaveID = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // INSERT en UsuarioRoles
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.UsuarioRoles (UsuarioID, RolID, FechaAsignacion, AsignadoPorID) VALUES (@uid,@rol,GETDATE(),@asigPor)",
                        cn))
                    {
                        cmd.Parameters.AddWithValue("@uid", nuevoClaveID);
                        cmd.Parameters.AddWithValue("@rol", idRol);
                        cmd.Parameters.AddWithValue("@asigPor", Convert.ToInt32(Session["ClaveID"]));
                        cmd.ExecuteNonQuery();
                    }

                    // Asignar bases seleccionadas
                    GuardarAsignacionBases(cn, nuevoClaveID, cblBasesAgregar, idRol);
                }

                LimpiarFormularioAgregar();
                CargarEmpleadosDisponibles();
                CargarGrid();
                SetMsg("success", "¡Usuario creado!", "El usuario fue registrado correctamente.");
                ScriptManager.RegisterStartupScript(this, GetType(),
                    "cerrarModalAgregar", "$('#modalAgregar').modal('hide');", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Usuarios.Agregar] " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo guardar el usuario. Contacte al administrador.");
                ReabrirModal();
            }
        }

        // ══ GUARDAR EDICIÓN ════════════════════════════════════════════════════

        protected void btnGuardarEditar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(hfClaveID.Value))
            { SetMsg("error", "Error", "No se encontró el ID del usuario."); return; }

            if (ddlRolEditar.SelectedValue == "0")
            { SetMsg("warning", "Rol requerido", "Debe seleccionar un rol."); ReabrirModalEditar(); return; }

            if (string.IsNullOrWhiteSpace(txtUsuarioEditar.Text))
            { SetMsg("warning", "Usuario requerido", "Debe ingresar el nombre de usuario."); ReabrirModalEditar(); return; }

            int claveID = Convert.ToInt32(hfClaveID.Value);
            int idRol = Convert.ToInt32(ddlRolEditar.SelectedValue);
            string usuario = txtUsuarioEditar.Text.Trim();

            string nuevaClave = txtNuevaClaveEditar.Text;
            bool cambiarClave = !string.IsNullOrWhiteSpace(nuevaClave);
            if (cambiarClave)
            {
                if (nuevaClave.Length < 6)
                { SetMsg("warning", "Contraseña corta", "La contraseña debe tener al menos 6 caracteres."); ReabrirModalEditar(); return; }
                if (nuevaClave != txtConfirmarClaveEditar.Text)
                { SetMsg("warning", "Contraseñas distintas", "Las contraseñas no coinciden."); ReabrirModalEditar(); return; }
            }

            try
            {
                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    // Verificar login único
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.Usuario WHERE Usuario = @usr AND ClaveID <> @id", cn))
                    {
                        cmd.Parameters.AddWithValue("@usr", usuario);
                        cmd.Parameters.AddWithValue("@id", claveID);
                        if ((int)cmd.ExecuteScalar() > 0)
                        { SetMsg("error", "Usuario duplicado", "Ya existe otro usuario con ese nombre de acceso."); ReabrirModalEditar(); return; }
                    }

                    // UPDATE: solo Usuario y Clave (sin TelefonoFamiliar ni Foto)
                    string sqlUpd = cambiarClave
                        ? "UPDATE dbo.Usuario SET Usuario=@usr, Clave=@clave, FechaModif=GETDATE() WHERE ClaveID=@id"
                        : "UPDATE dbo.Usuario SET Usuario=@usr,               FechaModif=GETDATE() WHERE ClaveID=@id";

                    using (var cmd = new SqlCommand(sqlUpd, cn))
                    {
                        cmd.Parameters.AddWithValue("@usr", usuario);
                        if (cambiarClave)
                            cmd.Parameters.AddWithValue("@clave",
                                BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor: 12));
                        cmd.Parameters.AddWithValue("@id", claveID);
                        cmd.ExecuteNonQuery();
                    }

                    // Actualizar rol si cambió
                    int rolActualID = 0, urID = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT UsuarioRolID, RolID FROM dbo.UsuarioRoles WHERE UsuarioID=@uid AND FechaRevocacion IS NULL", cn))
                    {
                        cmd.Parameters.AddWithValue("@uid", claveID);
                        using (var rdr = cmd.ExecuteReader())
                            if (rdr.Read()) { urID = (int)rdr["UsuarioRolID"]; rolActualID = (int)rdr["RolID"]; }
                    }

                    if (rolActualID != idRol)
                    {
                        if (urID > 0)
                        {
                            using (var cmd = new SqlCommand(
                                "UPDATE dbo.UsuarioRoles SET FechaRevocacion=GETDATE() WHERE UsuarioRolID=@id", cn))
                            { cmd.Parameters.AddWithValue("@id", urID); cmd.ExecuteNonQuery(); }
                        }
                        using (var cmd = new SqlCommand(
                            "INSERT INTO dbo.UsuarioRoles (UsuarioID, RolID, FechaAsignacion, AsignadoPorID) VALUES (@uid,@rol,GETDATE(),@asigPor)", cn))
                        {
                            cmd.Parameters.AddWithValue("@uid", claveID);
                            cmd.Parameters.AddWithValue("@rol", idRol);
                            cmd.Parameters.AddWithValue("@asigPor", Convert.ToInt32(Session["ClaveID"]));
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Actualizar bases asignadas
                    GuardarAsignacionBases(cn, claveID, cblBasesEditar, idRol);
                }

                CargarGrid();
                SetMsg("success", "¡Actualizado!", "El usuario fue actualizado correctamente.");
                ScriptManager.RegisterStartupScript(this, GetType(),
                    "cerrarModalEditar", "$('#modalEditar').modal('hide');", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Usuarios.Editar] " + ex.Message);
                SetMsg("error", "Error del sistema", "No se pudo actualizar el usuario. Contacte al administrador.");
            }
        }

        // ══ TOGGLE ACTIVO / INACTIVO ═══════════════════════════════════════════

        protected void btnToggleActivo_Click(object sender, EventArgs e)
        {
            try
            {
                int claveID = Convert.ToInt32(((Button)sender).CommandArgument);

                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    // Leer estado actual
                    bool activoActual = false;
                    using (var cmd = new SqlCommand(
                        "SELECT Activo FROM dbo.Usuario WHERE ClaveID = @id", cn))
                    {
                        cmd.Parameters.AddWithValue("@id", claveID);
                        var result = cmd.ExecuteScalar();
                        if (result != null) activoActual = Convert.ToBoolean(result);
                    }

                    // Invertir estado
                    using (var cmd = new SqlCommand(
                        "UPDATE dbo.Usuario SET Activo = @activo, FechaModif = GETDATE() WHERE ClaveID = @id", cn))
                    {
                        cmd.Parameters.AddWithValue("@activo", !activoActual);
                        cmd.Parameters.AddWithValue("@id", claveID);
                        cmd.ExecuteNonQuery();
                    }

                    string estado = !activoActual ? "activado" : "desactivado";
                    SetMsg("success", "¡Listo!", $"El usuario fue {estado} correctamente.");
                }

                CargarGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Usuarios.Toggle] " + ex.Message);
                SetMsg("error", "Error", "No se pudo cambiar el estado del usuario.");
            }
        }

        // ══ HELPERS ════════════════════════════════════════════════════════════

        private DataTable EjecutarQuery(string sql, Dictionary<string, object> parms)
        {
            using (var cn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                if (parms != null)
                    foreach (var p in parms)
                        cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
            }
        }

        private void SetMsg(string icon, string title, string text)
        {
            hdnMensajePendiente.Value = new JavaScriptSerializer()
                .Serialize(new { icon, title, text });
        }

        private void ReabrirModal()
        {
            hdnReabrirModalAgregar.Value = "1";
        }

        private void ReabrirModalEditar()
        {
            ScriptManager.RegisterStartupScript(this, GetType(),
                "reabrirEditar", "$('#modalEditar').modal('show');", true);
        }

        private void LimpiarFormularioAgregar()
        {
            ddlEmpleado.SelectedIndex = 0;
            divInfoEmpleado.Visible = false;
            ddlRolAgregar.SelectedIndex = 0;
            txtUsuarioAgregar.Text = "";
            txtClaveAgregar.Text = "";
            txtClaveConfirmarAgregar.Text = "";
            hdnReabrirModalAgregar.Value = "0";
        }
    }
}