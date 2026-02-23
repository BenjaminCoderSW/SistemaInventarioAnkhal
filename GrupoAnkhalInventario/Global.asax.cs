using System;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace GrupoAnkhalInventario
{
    public class Global : HttpApplication
    {
        /// <summary>
        /// Se ejecuta una vez al iniciar la aplicación
        /// </summary>
        protected void Application_Start(object sender, EventArgs e)
        {
            // Configuraciones globales de la aplicación
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Sistema de Inventario Ankhal iniciado");
        }

        /// <summary>
        /// Se ejecuta cada vez que se inicia una nueva sesión de usuario
        /// </summary>
        protected void Session_Start(object sender, EventArgs e)
        {
            // Configurar timeout de sesión (60 minutos)
            Session.Timeout = 60;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Nueva sesión iniciada: {Session.SessionID}");
        }

        /// <summary>
        /// Se ejecuta antes de cada request
        /// </summary>
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Aquí podrías agregar lógica para logging de requests si lo necesitas
        }

        /// <summary>
        /// Se ejecuta durante la autenticación del usuario
        /// </summary>
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            // Aquí podrías validar permisos adicionales si fuera necesario
        }

        /// <summary>
        /// Se ejecuta cuando ocurre un error no manejado en la aplicación
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (ex != null)
            {
                // Log del error para debugging
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] ERROR en aplicación: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // En producción, aquí podrías enviar el error a un sistema de logging
                // o guardar en base de datos
            }
        }

        /// <summary>
        /// Se ejecuta cuando finaliza una sesión (por timeout o logout)
        /// </summary>
        protected void Session_End(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Sesión finalizada: {Session.SessionID}");

            // Limpiar recursos de la sesión si es necesario
        }

        /// <summary>
        /// Se ejecuta cuando la aplicación se detiene
        /// </summary>
        protected void Application_End(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Sistema de Inventario Ankhal detenido");

            // Limpiar recursos globales aquí si fuera necesario
        }
    }
}