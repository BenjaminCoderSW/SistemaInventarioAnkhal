using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

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
            int pageSz  = gvUsuarios.PageSize;

            try
            {
                // ── Paso 1: obtener todos los UsuarioIDs con cuenta en Inventario ─────────
                const string sqlIds = "SELECT UsuarioID FROM dbo.Usuario WHERE UsuarioID IS NOT NULL";
                var dtIds = EjecutarQuery(sqlIds, null);
                var todosUsuarioIds = dtIds.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["UsuarioID"])).Distinct().ToList();

                // ── Paso 2: datos de empleados desde API (cacheados) ──────────────────────
                var empleados = UsuarioService.ObtenerEmpleadosBulk(todosUsuarioIds)
                    .ToDictionary(e => e.IdUsuario);

                // ── Paso 3: si hay búsqueda, filtrar UsuarioIDs vía API ───────────────────
                List<int> uidsNombreFiltro = null;
                if (!string.IsNullOrEmpty(buscar))
                {
                    uidsNombreFiltro = empleados.Values
                        .Where(e =>
                            (e.Nombre + " " + e.ApellidoPaterno)
                                .IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (e.NumeroEmpleado ?? "")
                                .IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(e => e.IdUsuario)
                        .ToList();
                }

                // ── Paso 4: WHERE dinámico ────────────────────────────────────────────────
                var where = new StringBuilder(" WHERE 1=1 ");

                if (!string.IsNullOrEmpty(buscar))
                {
                    if (uidsNombreFiltro != null && uidsNombreFiltro.Count > 0)
                    {
                        string ids = string.Join(",", uidsNombreFiltro);
                        where.Append($" AND (UsuarioID IN ({ids}) OR Usuario LIKE @buscar)");
                    }
                    else
                    {
                        // No hubo coincidencias de nombre → buscar solo por login
                        where.Append(" AND Usuario LIKE @buscar");
                    }
                }

                if (filEst == "1") where.Append(" AND Activo = 1 ");
                else if (filEst == "0") where.Append(" AND Activo = 0 ");

                // ── Paso 5: queries principales (count + paged data) ──────────────────────
                string sqlCount = "SELECT COUNT(*) FROM DatosUsuario" + where;
                string sqlData  = @"
                    SELECT du.ClaveID, du.UsuarioID, du.Nombre, du.ApellidoPaterno, du.ApellidoMaterno,
                           du.NumeroEmpleado, du.Telefono, du.TelefonoFamiliar, du.Email,
                           du.Usuario, du.Rol, du.RolID, du.Activo, du.Foto,
                           CAST(NULL AS varchar(1000)) AS NombreCompleto,
                           ISNULL((SELECT STRING_AGG(CAST(ub.BaseID AS VARCHAR(10)), ',')
                                   FROM dbo.UsuarioBases ub WHERE ub.ClaveID = du.ClaveID), '') AS BaseIDs
                    FROM DatosUsuario du" + where +
                    @" ORDER BY du.ClaveID
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

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

                    DataTable dt;
                    using (var cmd = new SqlCommand(sqlData, cn))
                    {
                        if (!string.IsNullOrEmpty(buscar))
                            cmd.Parameters.AddWithValue("@buscar", "%" + buscar + "%");
                        cmd.Parameters.AddWithValue("@offset", pageIdx * pageSz);
                        cmd.Parameters.AddWithValue("@pageSize", pageSz);

                        dt = new DataTable();
                        using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
                    }

                    // ── Paso 6: enriquecer DataTable con datos de la API ──────────────────
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["UsuarioID"] == DBNull.Value) continue;
                        int uid = Convert.ToInt32(row["UsuarioID"]);
                        if (!empleados.TryGetValue(uid, out var emp)) continue;

                        row["Nombre"]           = emp.Nombre          ?? "";
                        row["ApellidoPaterno"]  = emp.ApellidoPaterno ?? "";
                        row["ApellidoMaterno"]  = (object)emp.ApellidoMaterno  ?? DBNull.Value;
                        row["NumeroEmpleado"]   = (object)emp.NumeroEmpleado   ?? DBNull.Value;
                        row["Telefono"]         = (object)emp.Telefono         ?? DBNull.Value;
                        row["TelefonoFamiliar"] = (object)emp.TelefonoFamiliar ?? DBNull.Value;
                        row["Email"]            = (object)emp.Email            ?? DBNull.Value;
                        row["NombreCompleto"]   = emp.NombreCompleto;
                    }

                    gvUsuarios.DataSource = dt;
                    gvUsuarios.DataBind();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Usuarios.CargarGrid] " + ex.Message);
                SetMsg("error", "Error", "No se pudo cargar la lista de usuarios.");
            }
        }

        // Foto obtenida desde la API de Asistencia usando UsuarioID (cacheada 30 min)
        protected string ObtenerFotoBase64(object usuarioIdObj)
        {
            if (usuarioIdObj == null || usuarioIdObj == DBNull.Value)
                return "dist/img/user2-160x160.jpg";

            if (!int.TryParse(usuarioIdObj.ToString(), out int uid) || uid <= 0)
                return "dist/img/user2-160x160.jpg";

            byte[] bytes = UsuarioService.ObtenerFoto(uid);
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
            // Obtener UsuarioIDs que ya tienen cuenta en Inventario
            var dtExistentes = EjecutarQuery(
                "SELECT UsuarioID FROM dbo.Usuario WHERE UsuarioID IS NOT NULL", null);
            var idsExistentes = dtExistentes.AsEnumerable()
                .Select(r => Convert.ToInt32(r["UsuarioID"])).ToList();

            // Pedir a la API empleados activos que NO están en esa lista
            var disponibles = UsuarioService.ObtenerDisponibles(idsExistentes);

            ddlEmpleado.Items.Clear();
            ddlEmpleado.Items.Add(new ListItem("-- Seleccione un empleado --", "0"));
            foreach (var emp in disponibles)
                ddlEmpleado.Items.Add(new ListItem(emp.NombreCompleto,
                                                   emp.IdUsuario.ToString()));
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

            // Obtener datos del empleado desde la API de Asistencia
            var emp = UsuarioService.ObtenerEmpleado(idUsuario);
            if (emp == null || emp.IdUsuario == 0) { divInfoEmpleado.Visible = false; return; }

            divInfoEmpleado.Visible = true;

            lblNombreEmp.Text   = emp.Nombre + " " + emp.ApellidoPaterno +
                                  (!string.IsNullOrEmpty(emp.ApellidoMaterno) ? " " + emp.ApellidoMaterno : "");
            lblNumEmpEmp.Text   = !string.IsNullOrEmpty(emp.NumeroEmpleado)   ? emp.NumeroEmpleado   : "—";
            lblTelEmp.Text      = !string.IsNullOrEmpty(emp.Telefono)         ? emp.Telefono         : "—";
            lblTelFamEmp.Text   = !string.IsNullOrEmpty(emp.TelefonoFamiliar) ? emp.TelefonoFamiliar : "—";
            lblEmailEmp.Text    = !string.IsNullOrEmpty(emp.Email)            ? emp.Email            : "—";

            // Foto: llamada individual a la API (cacheada 30 min)
            byte[] foto = UsuarioService.ObtenerFoto(idUsuario);
            imgFotoEmpleado.ImageUrl = (foto != null && foto.Length > 0)
                ? "data:image/jpeg;base64," + Convert.ToBase64String(foto)
                : "dist/img/user2-160x160.jpg";
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