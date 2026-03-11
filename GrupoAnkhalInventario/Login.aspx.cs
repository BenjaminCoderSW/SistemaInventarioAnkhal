using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Login : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Limpiar sesion anterior al llegar al login
                Session.Clear();
                Session.Abandon();
            }
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(txtClave.Text))
                {
                    MostrarError("Campos vacios",
                        "Por favor ingrese usuario y contrasena.");
                    return;
                }

                const string sql = @"
                    SELECT ClaveID, UsuarioID, Usuario, Clave,
                           Nombre, ApellidoPaterno, Email,
                           Rol, Activo
                    FROM DatosUsuario
                    WHERE Usuario = @usuario";

                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    int claveID = 0;
                    int usuarioID = 0;
                    string usuario = "";
                    string clave = "";
                    string nombre = "";
                    string apellido = "";
                    string email = "";
                    string rol = "";
                    bool activo = false;
                    bool encontrado = false;

                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@usuario", txtUsuario.Text.Trim());

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                encontrado = true;
                                claveID = Convert.ToInt32(rdr["ClaveID"]);
                                usuarioID = Convert.ToInt32(rdr["UsuarioID"]);
                                usuario = rdr["Usuario"].ToString();
                                clave = rdr["Clave"].ToString();
                                nombre = rdr["Nombre"].ToString();
                                apellido = rdr["ApellidoPaterno"].ToString();
                                email = rdr["Email"] != DBNull.Value
                                             ? rdr["Email"].ToString() : "";
                                rol = rdr["Rol"] != DBNull.Value
                                             ? rdr["Rol"].ToString() : "Reporte";
                                activo = Convert.ToBoolean(rdr["Activo"]);
                            }
                        }
                    }

                    // Verificar contrasena
                    bool passwordCorrecto = encontrado &&
                        BCrypt.Net.BCrypt.Verify(txtClave.Text, clave);

                    if (!encontrado || !passwordCorrecto)
                    {
                        MostrarError("Acceso Denegado",
                            "Usuario o contrasena incorrectos. Verifique sus credenciales.");
                        return;
                    }

                    if (!activo)
                    {
                        MostrarError("Acceso Denegado",
                            "Su cuenta ha sido desactivada. Contacte al administrador.");
                        return;
                    }

                    // Session Fixation: limpiar sesion anonima anterior
                    // y escribir datos frescos en la misma sesion limpia.
                    // En WebForms no se puede regenerar el SessionID directamente,
                    // Clear() es suficiente para invalidar cualquier dato previo.
                    Session.Clear();

                    // ClaveID  = llave primaria de dbo.Usuario (uso interno inventario)
                    // UsuarioID = ID del empleado en AsistenciaAnkhal.dbo.tUsuario
                    Session["ClaveID"] = claveID;
                    Session["UsuarioID"] = usuarioID;
                    Session["NombreUsuario"] = usuario;
                    Session["NombreCompleto"] = (nombre + " " + apellido).Trim();
                    Session["Rol"] = rol;
                    Session["Email"] = email;

                    // Actualizar UltimoAcceso
                    using (var cmd = new SqlCommand(
                        "UPDATE dbo.Usuario SET UltimoAcceso = GETDATE() WHERE ClaveID = @id",
                        cn))
                    {
                        cmd.Parameters.AddWithValue("@id", claveID);
                        cmd.ExecuteNonQuery();
                    }

                    Response.Redirect("~/Default.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] {ex.Message}");
                MostrarError("Error del Sistema",
                    "Ocurrio un error al iniciar sesion. Contacte al administrador.");
            }
        }

        private void MostrarError(string titulo, string mensaje)
        {
            string script = $@"
                Swal.fire({{
                    icon: 'error',
                    title: '{titulo}',
                    html: '{mensaje}',
                    confirmButtonColor: '#ff6600'
                }}).then(() => {{
                    document.getElementById('{txtClave.ClientID}').value = '';
                    document.getElementById('{txtClave.ClientID}').focus();
                }});";
            ScriptManager.RegisterStartupScript(
                this, GetType(), "SweetAlert", script, true);
        }
    }
}