using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Site : MasterPage
    {
        // ── Mapa de permisos por página ───────────────────────────────────────
        // Clave: nombre del archivo .aspx (case-insensitive, sin ruta).
        // Valor: roles con acceso. Si la página no está en el diccionario,
        //        cualquier usuario autenticado puede acceder.
        // SINCRONIZAR con ConfigurarMenuPorRol() cuando cambien los roles.
        private static readonly Dictionary<string, List<string>> _permisosPagina =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // ── Catálogos (Administrador + Compras) ──
                { "bases.aspx",      new List<string> { "Administrador", "Compras" } },
                { "materiales.aspx", new List<string> { "Administrador", "Compras" } },
                { "productos.aspx",  new List<string> { "Administrador", "Compras", "Ventas" } },
                { "paquetes.aspx",   new List<string> { "Administrador", "Compras", "Ventas" } },

                // ── Operaciones ──
                { "produccion.aspx",  new List<string> { "Administrador", "Produccion", "Almacen" } },
                { "entregas.aspx",    new List<string> { "Administrador", "Ventas",     "Almacen" } },
                { "movimientos.aspx", new List<string> { "Administrador", "Almacen",    "Produccion" } },

                // ── Inventario (todos excepto roles no definidos) ──
                { "inventario.aspx", new List<string>
                    { "Administrador", "Ventas", "Compras", "Almacen", "Produccion", "Reporte" } },

                // ── Administración ──
                { "usuarios.aspx", new List<string> { "Administrador" } },
            };

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["UsuarioID"] != null)
                {
                    ValidarAccesoPagina();
                    CargarDatosUsuario();
                    ConfigurarMenuPorRol();
                }
                else
                {
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        // ── Validación centralizada de acceso ─────────────────────────────────
        private void ValidarAccesoPagina()
        {
            string pagina = System.IO.Path.GetFileName(Request.FilePath);
            if (!_permisosPagina.TryGetValue(pagina, out var rolesPermitidos))
                return; // No listada = accesible para cualquier autenticado

            string rol = Session["Rol"]?.ToString() ?? "";
            if (!rolesPermitidos.Contains(rol))
            {
                Response.Redirect("~/Default.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Para validación extra granular desde una página específica.
        /// Ejemplo: ((Site)Master).RequiereRol("Administrador")
        /// </summary>
        public void RequiereRol(params string[] roles)
        {
            string rol = Session["Rol"]?.ToString() ?? "";
            if (!((IList<string>)roles).Contains(rol))
            {
                Response.Redirect("~/Default.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        // ── Carga de datos del usuario en el sidebar ──────────────────────────
        private void CargarDatosUsuario()
        {
            try
            {
                if (Session["UsuarioID"] == null)
                {
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                lblUsuario.Text = Session["NombreCompleto"]?.ToString() ?? "Usuario";
                lblRol.Text = Session["Rol"]?.ToString() ?? "Sin rol";

                int usuarioID = Convert.ToInt32(Session["UsuarioID"]);

                using (var db = new InventarioAnkhalDBDataContext(
                    ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString))
                {
                    var usuario = db.Usuarios.FirstOrDefault(u => u.UsuarioID == usuarioID);
                    if (usuario?.Foto != null && usuario.Foto.Length > 0)
                    {
                        string base64 = Convert.ToBase64String(usuario.Foto.ToArray());
                        imgUsuario.Src = "data:image/jpeg;base64," + base64;
                    }
                    else
                    {
                        imgUsuario.Src = "dist/img/user2-160x160.jpg";
                    }
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

        // ── Configuración del menú por rol ────────────────────────────────────
        // Roles: Administrador | Ventas | Compras | Almacen | Produccion | Reporte
        private void ConfigurarMenuPorRol()
        {
            string rol = Session["Rol"]?.ToString() ?? "Reporte";

            // Por defecto: todo oculto
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

            switch (rol)
            {
                case "Administrador":
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
                    break;

                case "Ventas":
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;   // Productos y Paquetes
                    headerOperaciones.Visible = true;
                    menuEntregas.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Compras":
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;   // Bases, Materiales, Productos, Paquetes
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Almacen":
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuMovimientos.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Produccion":
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuMovimientos.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Reporte":
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                    // default: todo oculto (ya establecido arriba)
            }
        }

        // ── Navegación y sesión ───────────────────────────────────────────────
        protected void btnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Default.aspx");
        }

        protected void CerrarSesion_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}