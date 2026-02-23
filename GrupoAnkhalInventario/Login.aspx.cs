using GrupoAnkhalInventario.Modelo;
using System;
using System.Configuration;
using System.Linq;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Login : Page
    {
        // Conexión a la base de datos usando LINQ to SQL
        public InventarioAnkhalDBDataContext db = new InventarioAnkhalDBDataContext(
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Limpiar sesión al cargar el login
                Session.Clear();
            }
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar campos vacíos
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtClave.Text))
                {
                    MostrarError("Campos vacíos", "Por favor ingrese usuario y contraseña.");
                    return;
                }

                // Buscar usuario en la base de datos
                var usuarioDb = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario == txtUsuario.Text.Trim() &&
                    u.Clave == txtClave.Text &&
                    u.Activo == true);

                if (usuarioDb != null)
                {
                    // Obtener el rol activo del usuario (el más reciente sin revocar)
                    var rolActivo = (from ur in db.UsuarioRoles
                                     join r in db.Roles on ur.RolID equals r.RolID
                                     where ur.UsuarioID == usuarioDb.UsuarioID
                                           && ur.FechaRevocacion == null
                                           && r.Activo == true
                                     orderby ur.FechaAsignacion descending
                                     select r.Nombre).FirstOrDefault();

                    // Crear variables de sesión
                    Session["UsuarioID"] = usuarioDb.UsuarioID;
                    Session["NombreUsuario"] = usuarioDb.Nombre;
                    Session["NombreCompleto"] = $"{usuarioDb.Nombre} {usuarioDb.ApellidoPaterno ?? ""}".Trim();
                    Session["Rol"] = rolActivo ?? "Operador";
                    Session["Email"] = usuarioDb.Email;

                    // Actualizar último acceso
                    usuarioDb.UltimoAcceso = DateTime.Now;
                    db.SubmitChanges();

                    // Redirigir según el rol
                    string rol = Session["Rol"].ToString();

                    if (rol == "Administrador" || rol == "Supervisor")
                    {
                        Response.Redirect("~/Default.aspx", false);
                    }
                    else if (rol == "Operador")
                    {
                        Response.Redirect("~/Default.aspx", false);
                    }
                    else if (rol == "Consulta")
                    {
                        Response.Redirect("~/Default.aspx", false);
                    }
                    else
                    {
                        // Rol no reconocido, llevar al dashboard genérico
                        Response.Redirect("~/Default.aspx", false);
                    }

                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    // Usuario no encontrado o credenciales incorrectas
                    MostrarError("Acceso Denegado", "Usuario o contraseña incorrectos. Verifique sus credenciales.");
                }
            }
            catch (Exception ex)
            {
                // Error en el proceso de login
                MostrarError("Error del Sistema", $"Ocurrió un error al iniciar sesión. Por favor contacte al administrador del sistema.<br/><small>Detalles técnicos: {ex.Message}</small>");

                // Log del error para debugging
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error en Login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Muestra un mensaje de error usando SweetAlert2
        /// </summary>
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