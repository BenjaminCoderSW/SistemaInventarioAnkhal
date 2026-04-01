using GrupoAnkhalInventario.Helpers;
﻿using GrupoAnkhalInventario.Services;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GrupoAnkhalInventario
{
    public partial class Login : Page
    {
        // ── Configuración de bloqueo ──────────────────────────────────────────
        private const int MAX_INTENTOS = 5;   // intentos antes de bloquear
        private const int MINUTOS_BLOQUEO = 5;   // minutos de bloqueo
        private const int VENTANA_INTENTOS = 15;  // minutos en que se acumulan intentos
        //      Si pasan VENTANA_INTENTOS minutos sin intentar, el contador se reinicia.

        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // ─────────────────────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Session.Clear();
                Session.Abandon();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(txtClave.Text))
                {
                    MostrarError("Campos vacíos", "Por favor ingrese usuario y contraseña.");
                    return;
                }

                // ── Paso 1: buscar el registro del usuario (solo BD local) ───────
                const string sqlSelect = @"
                    SELECT
                        u.ClaveID,
                        u.UsuarioID,
                        u.Usuario,
                        u.Clave,
                        u.Activo,
                        u.IntentosFallidos,
                        u.BloqueadoHasta,
                        u.UltimoIntento,
                        r.Nombre AS Rol
                    FROM dbo.Usuario u
                    LEFT JOIN dbo.UsuarioRoles ur
                        ON ur.UsuarioID = u.ClaveID AND ur.FechaRevocacion IS NULL
                    LEFT JOIN dbo.Roles r
                        ON r.RolID = ur.RolID AND r.Activo = 1
                    WHERE u.Usuario = @usuario";

                int claveID = 0;
                int usuarioID = 0;
                string usuario = "";
                string claveHash = "";
                bool activo = false;
                int intentosFallidos = 0;
                DateTime? bloqueadoHasta = null;
                DateTime? ultimoIntento = null;
                string nombre = "";
                string apellido = "";
                string email = "";
                string rol = "";
                bool encontrado = false;

                using (var cn = new SqlConnection(_connStr))
                {
                    cn.Open();

                    // ── Leer datos del usuario ────────────────────────────────
                    using (var cmd = new SqlCommand(sqlSelect, cn))
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
                                claveHash = rdr["Clave"].ToString();
                                activo = Convert.ToBoolean(rdr["Activo"]);
                                intentosFallidos = Convert.ToInt32(rdr["IntentosFallidos"]);
                                bloqueadoHasta = rdr["BloqueadoHasta"] != DBNull.Value
                                                    ? (DateTime?)Convert.ToDateTime(rdr["BloqueadoHasta"])
                                                    : null;
                                ultimoIntento = rdr["UltimoIntento"] != DBNull.Value
                                                    ? (DateTime?)Convert.ToDateTime(rdr["UltimoIntento"])
                                                    : null;
                                rol = rdr["Rol"] != DBNull.Value
                                                    ? rdr["Rol"].ToString() : "Reporte";
                            }
                        }
                    }

                    // ── Paso 2: usuario no existe → error genérico ────────────
                    // No revelar si el usuario existe o no (evita enumeración).
                    if (!encontrado)
                    {
                        System.Threading.Thread.Sleep(500);
                        MostrarError("Acceso Denegado",
                            "Usuario o contraseña incorrectos. Verifique sus credenciales.");
                        return;
                    }

                    // ── Paso 3: verificar si está bloqueado ───────────────────
                    if (bloqueadoHasta.HasValue && bloqueadoHasta.Value > AppHelper.Ahora)
                    {
                        TimeSpan restante = bloqueadoHasta.Value - AppHelper.Ahora;
                        string tiempoMsg = restante.TotalMinutes >= 1
                            ? $"{(int)Math.Ceiling(restante.TotalMinutes)} minuto(s)"
                            : $"{(int)Math.Ceiling(restante.TotalSeconds)} segundo(s)";

                        MostrarError("Cuenta bloqueada",
                            $"Su cuenta ha sido bloqueada por demasiados intentos fallidos. " +
                            $"Intente nuevamente en {tiempoMsg}.");
                        return;
                    }

                    // ── Paso 4: si el bloqueo ya expiró → limpiar contadores ──
                    if (bloqueadoHasta.HasValue && bloqueadoHasta.Value <= AppHelper.Ahora)
                    {
                        EjecutarNonQuery(cn,
                            @"UPDATE dbo.Usuario
                              SET IntentosFallidos = 0,
                                  BloqueadoHasta   = NULL,
                                  UltimoIntento    = NULL
                              WHERE ClaveID = @id",
                            ("@id", claveID));

                        intentosFallidos = 0;
                        bloqueadoHasta = null;
                        ultimoIntento = null;
                    }

                    // ── Paso 5: aplicar VENTANA_INTENTOS ─────────────────────
                    // Si el último intento fue hace más de VENTANA_INTENTOS minutos,
                    // el contador se reinicia: los fallos anteriores "caducan".
                    // Esto evita que alguien acumule 4 intentos, espere y vuelva
                    // a tener 5 intentos frescos indefinidamente sin ser bloqueado.
                    if (ultimoIntento.HasValue &&
                        (AppHelper.Ahora - ultimoIntento.Value).TotalMinutes > VENTANA_INTENTOS)
                    {
                        EjecutarNonQuery(cn,
                            @"UPDATE dbo.Usuario
                              SET IntentosFallidos = 0,
                                  UltimoIntento    = NULL
                              WHERE ClaveID = @id",
                            ("@id", claveID));

                        intentosFallidos = 0;
                        ultimoIntento = null;
                    }

                    // ── Paso 6: verificar contraseña ──────────────────────────
                    bool passwordCorrecto = BCrypt.Net.BCrypt.Verify(txtClave.Text, claveHash);

                    if (!passwordCorrecto)
                    {
                        int nuevosIntentos = intentosFallidos + 1;
                        bool debeBloquear = nuevosIntentos >= MAX_INTENTOS;
                        int intentosRestantes = MAX_INTENTOS - nuevosIntentos;

                        if (debeBloquear)
                        {
                            // Bloquear la cuenta
                            EjecutarNonQuery(cn,
                                @"UPDATE dbo.Usuario
                                  SET IntentosFallidos = @intentos,
                                      BloqueadoHasta   = DATEADD(MINUTE, @minutos, GETDATE()),
                                      UltimoIntento    = GETDATE()
                                  WHERE ClaveID = @id",
                                ("@intentos", nuevosIntentos),
                                ("@minutos", MINUTOS_BLOQUEO),
                                ("@id", claveID));

                            MostrarError("Cuenta bloqueada",
                                $"Ha alcanzado el máximo de {MAX_INTENTOS} intentos fallidos. " +
                                $"Su cuenta quedará bloqueada por {MINUTOS_BLOQUEO} minutos.");
                        }
                        else
                        {
                            // Solo incrementar el contador
                            EjecutarNonQuery(cn,
                                @"UPDATE dbo.Usuario
                                  SET IntentosFallidos = @intentos,
                                      UltimoIntento    = GETDATE()
                                  WHERE ClaveID = @id",
                                ("@intentos", nuevosIntentos),
                                ("@id", claveID));

                            string advertencia = intentosRestantes == 1
                                ? "Le queda <b>1 intento</b> antes de que su cuenta sea bloqueada."
                                : $"Le quedan <b>{intentosRestantes} intentos</b> antes de que su cuenta sea bloqueada.";

                            MostrarError("Acceso Denegado",
                                "Usuario o contraseña incorrectos. " + advertencia);
                        }

                        return;
                    }

                    // ── Paso 7: verificar que la cuenta esté activa ───────────
                    if (!activo)
                    {
                        MostrarError("Acceso Denegado",
                            "Su cuenta ha sido desactivada. Contacte al administrador.");
                        return;
                    }

                    // ── Paso 8: Login exitoso — limpiar contadores + sesión ───
                    // Obtener nombre y email desde la API de Asistencia
                    var empleado = UsuarioService.ObtenerEmpleado(usuarioID);
                    nombre  = empleado.Nombre;
                    apellido = empleado.ApellidoPaterno;
                    email   = empleado.Email;

                    Session.Clear();

                    Session["ClaveID"] = claveID;
                    Session["UsuarioID"] = usuarioID;
                    Session["NombreUsuario"] = usuario;
                    Session["NombreCompleto"] = (nombre + " " + apellido).Trim();
                    Session["Rol"] = rol;
                    Session["Email"] = email;

                    // Reiniciar contadores de brute force y actualizar UltimoAcceso
                    EjecutarNonQuery(cn,
                        @"UPDATE dbo.Usuario
                          SET IntentosFallidos = 0,
                              BloqueadoHasta   = NULL,
                              UltimoIntento    = NULL,
                              UltimoAcceso     = GETDATE()
                          WHERE ClaveID = @id",
                        ("@id", claveID));
                }

                Response.Redirect("~/Default.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] {ex.Message}");
                MostrarError("Error del Sistema",
                    "Ocurrió un error al iniciar sesión. Contacte al administrador.");
            }
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Ejecuta un UPDATE/INSERT sin resultado, con parámetros como tuplas (nombre, valor).
        /// Reutiliza la conexión abierta para evitar abrir una segunda conexión.
        /// </summary>
        private void EjecutarNonQuery(SqlConnection cn, string sql,
            params (string nombre, object valor)[] parametros)
        {
            using (var cmd = new SqlCommand(sql, cn))
            {
                foreach (var (nombre, valor) in parametros)
                    cmd.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Muestra un SweetAlert de error y limpia el campo de contraseña.
        /// Acepta HTML en el parámetro mensaje (usa html: en lugar de text:).
        /// </summary>
        private void MostrarError(string titulo, string mensaje)
        {
            titulo = titulo.Replace("'", "\\'");
            mensaje = mensaje.Replace("'", "\\'");

            string script = $@"
                Swal.fire({{
                    icon: 'error',
                    title: '{titulo}',
                    html:  '{mensaje}',
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