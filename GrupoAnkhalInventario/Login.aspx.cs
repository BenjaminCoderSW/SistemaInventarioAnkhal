using System;
using System.Configuration;
using System.Data;
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
                Session.Clear();
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtClave.Text))
                {
                    MostrarError("Campos vacíos", "Por favor ingrese usuario y contraseña.");
                    return;
                }

                const string sql = @"
                    SELECT ClaveID, UsuarioID, Usuario, Clave,
                           Nombre, ApellidoPaterno, Email,
                           Rol, Activo
                    FROM DatosUsuario
                    WHERE Usuario = @usuario AND Activo = 1";

                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    DataRow row = null;
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@usuario", txtUsuario.Text.Trim());
                        var dt = new DataTable();
                        using (var da = new SqlDataAdapter(cmd))
                            da.Fill(dt);
                        if (dt.Rows.Count > 0)
                            row = dt.Rows[0];
                    }

                    // Verificar contraseña con BCrypt
                    bool valido = row != null &&
                        BCrypt.Net.BCrypt.Verify(txtClave.Text, row["Clave"].ToString());

                    if (valido)
                    {
                        int claveID = Convert.ToInt32(row["ClaveID"]);

                        // Guardar sesión
                        Session["UsuarioID"] = claveID;
                        Session["NombreUsuario"] = row["Usuario"].ToString();
                        Session["NombreCompleto"] = (row["Nombre"] + " " + row["ApellidoPaterno"]).Trim();
                        Session["Rol"] = row["Rol"] != DBNull.Value ? row["Rol"].ToString() : "Reporte";
                        Session["Email"] = row["Email"] != DBNull.Value ? row["Email"].ToString() : "";

                        // Actualizar UltimoAcceso
                        using (var cmd = new SqlCommand(
                            "UPDATE dbo.Usuario SET UltimoAcceso = GETDATE() WHERE ClaveID = @id", cn))
                        {
                            cmd.Parameters.AddWithValue("@id", claveID);
                            cmd.ExecuteNonQuery();
                        }

                        Response.Redirect("~/Default.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                    }
                    else
                    {
                        MostrarError("Acceso Denegado",
                            "Usuario o contraseña incorrectos. Verifique sus credenciales.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] {ex.Message}");
                MostrarError("Error del Sistema",
                    "Ocurrió un error al iniciar sesión. Contacte al administrador.");
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
            ScriptManager.RegisterStartupScript(this, GetType(), "SweetAlert", script, true);
        }
    }
}