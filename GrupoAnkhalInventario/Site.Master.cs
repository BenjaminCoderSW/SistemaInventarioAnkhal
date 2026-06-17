using GrupoAnkhalInventario.Modelo;
using GrupoAnkhalInventario.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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
                // ── Catálogos (Administrador + Compras + Jefe de planta) ──
                { "bases.aspx",               new List<string> { "Administrador", "Compras", "Jefe de planta" } },
                { "materiales.aspx",           new List<string> { "Administrador", "Compras", "Jefe de planta" } },
                { "productos.aspx",            new List<string> { "Administrador", "Compras", "Ventas", "Jefe de planta" } },
                { "registrarproveedores.aspx", new List<string> { "Administrador", "Compras", "Jefe de planta" } },

                // ── Clientes (solo Ventas, no Compras) ──
                { "registrarclientes.aspx", new List<string> { "Administrador", "Ventas", "Jefe de planta" } },

                // ── Operaciones ──
                { "produccion.aspx",  new List<string> { "Administrador", "Produccion", "Almacen" } },
                { "entregas.aspx",    new List<string> { "Administrador", "Ventas",     "Almacen" } },
                { "ordenes.aspx",     new List<string> { "Administrador", "Ventas",     "Almacen" } },
                { "movimientos.aspx", new List<string> { "Administrador", "Almacen",    "Produccion", "Compras", "Jefe de planta" } },
                { "mermas.aspx",          new List<string> { "Administrador", "Almacen",    "Produccion", "Jefe de planta" } },
                { "metasproduccion.aspx", new List<string> { "Administrador", "Produccion" } },
                { "cuentasporpagar.aspx", new List<string> { "Administrador", "Compras",   "Jefe de planta" } },

                // ── Inventario (todos excepto roles no definidos) ──
                { "inventario.aspx", new List<string>
                    { "Administrador", "Ventas", "Compras", "Almacen", "Produccion", "Reporte", "Jefe de planta" } },

                // ── Administración ──
                { "usuarios.aspx", new List<string> { "Administrador" } },
            };

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["ClaveID"] != null)
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
                if (Session["ClaveID"] == null)
                {
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                lblUsuario.Text = Session["NombreCompleto"]?.ToString() ?? "Usuario";
                lblRol.Text = Session["Rol"]?.ToString() ?? "Sin rol";

                // Obtener foto del empleado desde la API de Asistencia (cacheada 30 min)
                int usuarioID = Session["UsuarioID"] != null
                    ? Convert.ToInt32(Session["UsuarioID"]) : 0;

                if (usuarioID > 0)
                {
                    byte[] foto = UsuarioService.ObtenerFoto(usuarioID);
                    imgUsuario.Src = (foto != null && foto.Length > 0)
                        ? "data:image/jpeg;base64," + Convert.ToBase64String(foto)
                        : "dist/img/user2-160x160.jpg";
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

        // ── Configuración del menú por rol ────────────────────────────────────
        // Roles: Administrador | Ventas | Compras | Almacen | Produccion | Reporte
        private void ConfigurarMenuPorRol()
        {
            string rol = Session["Rol"]?.ToString() ?? "Reporte";

            // Por defecto: todo oculto
            headerConfiguracion.Visible = false;
            menuCatalogos.Visible = false;
            liMenuBases.Visible = false;
            liMenuMateriales.Visible = false;
            liMenuClientes.Visible = false;
            liMenuProveedores.Visible = false;
            liMenuMetas.Visible = false;
            headerOperaciones.Visible = false;
            menuProduccion.Visible = false;
            menuEntregas.Visible = false;
            menuOrdenes.Visible = false;
            menuMovimientos.Visible = false;
            menuCxP.Visible = false;
            menuMermas.Visible = false;
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
                    liMenuBases.Visible = true;
                    liMenuMateriales.Visible = true;
                    liMenuClientes.Visible = true;
                    liMenuProveedores.Visible = true;
                    liMenuMetas.Visible = true;
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuOrdenes.Visible = true;
                    menuMovimientos.Visible = true;
                    menuCxP.Visible = true;
                    menuMermas.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    headerAdministracion.Visible = true;
                    menuUsuarios.Visible = true;
                    break;

                case "Ventas":
                    // Catálogos: solo Productos (sin ID) y Clientes
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;
                    liMenuClientes.Visible = true;
                    headerOperaciones.Visible = true;
                    menuEntregas.Visible = true;
                    menuOrdenes.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Compras":
                    // Catálogos: Bases, Materiales, Productos (sin ID), Proveedores — sin Clientes
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;
                    liMenuBases.Visible = true;
                    liMenuMateriales.Visible = true;
                    liMenuProveedores.Visible = true;
                    headerOperaciones.Visible = true;
                    menuMovimientos.Visible = true;
                    menuCxP.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Almacen":
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    menuEntregas.Visible = true;
                    menuOrdenes.Visible = true;
                    menuMovimientos.Visible = true;
                    menuMermas.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Produccion":
                    // Sin acceso a Órdenes — ve solo su flujo de producción
                    headerOperaciones.Visible = true;
                    menuProduccion.Visible = true;
                    liMenuMetas.Visible = true;
                    menuMovimientos.Visible = true;
                    menuMermas.Visible = true;
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Reporte":
                    headerInventario.Visible = true;
                    menuInventario.Visible = true;
                    break;

                case "Jefe de planta":
                    headerConfiguracion.Visible = true;
                    menuCatalogos.Visible = true;
                    liMenuBases.Visible = true;
                    liMenuMateriales.Visible = true;
                    liMenuClientes.Visible = true;
                    liMenuProveedores.Visible = true;
                    headerOperaciones.Visible = true;
                    menuMovimientos.Visible = true;
                    menuCxP.Visible = true;
                    menuMermas.Visible = true;
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