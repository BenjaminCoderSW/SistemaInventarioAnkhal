using GrupoAnkhalInventario.Modelo;
using System;
using System.Globalization;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Verificar si hay sesión activa
                if (Session["UsuarioID"] == null)
                {
                    // No hay sesión, redirigir al login
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // Cargar información del usuario
                CargarInformacionUsuario();

                // Configurar accesos rápidos según el rol
                ConfigurarAccesosRapidos();
            }
        }

        /// <summary>
        /// Carga la información del usuario desde la sesión
        /// </summary>
        private void CargarInformacionUsuario()
        {
            try
            {
                // Obtener datos de la sesión
                string nombreCompleto = Session["NombreCompleto"]?.ToString() ?? "Usuario";
                string rol = Session["Rol"]?.ToString() ?? "Sin rol";

                // Mostrar en el welcome card
                lblNombreUsuario.Text = nombreCompleto;
                lblRol.Text = rol;

                // Mostrar fecha y hora actual
                CultureInfo culturaEspañol = new CultureInfo("es-MX");
                lblFechaHora.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy - hh:mm tt", culturaEspañol);

                // Mostrar en la sección de información del sistema
                lblUsuarioInfo.Text = nombreCompleto;
                lblRolInfo.Text = rol;
                lblUltimoAcceso.Text = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar información del usuario: {ex.Message}");
                lblNombreUsuario.Text = "Usuario";
                lblRol.Text = "Sin rol";
                lblFechaHora.Text = DateTime.Now.ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Configura la visibilidad de los accesos rápidos según el rol
        /// </summary>
        private void ConfigurarAccesosRapidos()
        {
            string rol = Session["Rol"]?.ToString() ?? "Consulta";

            switch (rol)
            {
                case "Administrador":
                    // Tiene acceso a todo
                    divInventario.Visible = true;
                    divProduccion.Visible = true;
                    divEntregas.Visible = true;
                    divCatalogos.Visible = true;
                    break;

                case "Supervisor":
                    // Tiene acceso a operaciones e inventario
                    divInventario.Visible = true;
                    divProduccion.Visible = true;
                    divEntregas.Visible = true;
                    divCatalogos.Visible = true;
                    break;

                case "Operador":
                    // Solo operaciones e inventario (sin catálogos)
                    divInventario.Visible = true;
                    divProduccion.Visible = true;
                    divEntregas.Visible = true;
                    divCatalogos.Visible = false;
                    break;

                case "Consulta":
                    // Solo inventario (lectura)
                    divInventario.Visible = true;
                    divProduccion.Visible = false;
                    divEntregas.Visible = false;
                    divCatalogos.Visible = false;
                    break;

                default:
                    // Sin accesos
                    divInventario.Visible = false;
                    divProduccion.Visible = false;
                    divEntregas.Visible = false;
                    divCatalogos.Visible = false;
                    break;
            }
        }
    }
}