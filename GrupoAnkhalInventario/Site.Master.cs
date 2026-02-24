using GrupoAnkhalInventario.Modelo;
using System;
using System.Configuration;
using System.Linq;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Site : MasterPage
    {
        // Conexión a la base de datos
        public InventarioAnkhalDBDataContext db = new InventarioAnkhalDBDataContext(
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Verificar si hay sesión activa
                if (Session["UsuarioID"] != null)
                {
                    CargarDatosUsuario();
                    ConfigurarMenuPorRol();
                }
                else
                {
                    // No hay sesión, redirigir al login
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        /// <summary>
        /// Carga los datos del usuario desde la sesión
        /// </summary>
        private void CargarDatosUsuario()
        {
            try
            {
                // Mostrar nombre completo del usuario
                lblUsuario.Text = Session["NombreCompleto"]?.ToString() ?? "Usuario";

                // Mostrar rol
                lblRol.Text = Session["Rol"]?.ToString() ?? "Sin rol";

                // Cargar foto del usuario
                int usuarioID = Convert.ToInt32(Session["UsuarioID"]);
                var usuario = db.Usuarios.FirstOrDefault(u => u.UsuarioID == usuarioID);

                if (usuario != null && usuario.Foto != null)
                {
                    byte[] fotoBytes = usuario.Foto.ToArray();
                    if (fotoBytes != null && fotoBytes.Length > 0)
                    {
                        string base64 = Convert.ToBase64String(fotoBytes);
                        imgUsuario.Src = "data:image/png;base64," + base64;
                    }
                    else
                    {
                        imgUsuario.Src = "dist/img/user2-160x160.jpg";
                    }
                }
                else
                {
                    imgUsuario.Src = "dist/img/user2-160x160.jpg";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar datos del usuario: {ex.Message}");
                lblUsuario.Text = "Usuario";
                lblRol.Text = "Sin rol";
                imgUsuario.Src = "dist/img/user2-160x160.jpg";
            }
        }

        /// <summary>
        /// Configura la visibilidad del menú según el rol del usuario
        /// </summary>
        private void ConfigurarMenuPorRol()
        {
            string rol = Session["Rol"]?.ToString() ?? "Consulta";

            switch (rol)
            {
                case "Administrador":
                    // Ve todo
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuMovimientos.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    headerAdministracion.Visible = true;
                    menuUsuarios.Visible = true;
                    lnkInicio.Visible = true;
                    break;

                case "Supervisor":
                    // Ve configuración, operaciones e inventario
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuMovimientos.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    headerAdministracion.Visible = false;
                    menuUsuarios.Visible = false;
                    lnkInicio.Visible = true;
                    break;

                case "Operador":
                    // Solo ve operaciones e inventario (consulta)
                    headerConfiguracion.Visible = false;
                    menuCatalogos.Visible = false;
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuMovimientos.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    headerAdministracion.Visible = false;
                    menuUsuarios.Visible = false;
                    lnkInicio.Visible = true;
                    break;

                case "Consulta":
                    // Solo ve inventario (lectura)
                    headerConfiguracion.Visible = false;
                    menuCatalogos.Visible = false;
                    headerOperaciones.Visible = false;
                    menuProduccion.Visible = false;
                    menuEntregas.Visible = false;
                    menuMovimientos.Visible = false;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    headerAdministracion.Visible = false;
                    menuUsuarios.Visible = false;
                    lnkInicio.Visible = true;
                    break;

                default:
                    // Rol desconocido - solo dashboard
                    headerConfiguracion.Visible = false;
                    menuCatalogos.Visible = false;
                    headerOperaciones.Visible = false;
                    menuProduccion.Visible = false;
                    menuEntregas.Visible = false;
                    menuMovimientos.Visible = false;
                    headerInventario.Visible = false;
                    menuInventario.Visible = false;
                    headerAdministracion.Visible = false;
                    menuUsuarios.Visible = false;
                    lnkInicio.Visible = true;
                    break;
            }
        }

        /// <summary>
        /// Redirige al dashboard al hacer click en el logo
        /// </summary>
        protected void btnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Default.aspx");
        }

        /// <summary>
        /// Cierra la sesión del usuario
        /// </summary>
        protected void CerrarSesion_Click(object sender, EventArgs e)
        {
            // Limpiar completamente la sesión
            Session.Clear();
            Session.Abandon();

            // Redirigir al login
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}