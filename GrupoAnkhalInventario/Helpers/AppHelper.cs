using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.SessionState;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario.Helpers
{
    public static class AppHelper
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        // UTC-6 fijo — México abolió el horario de verano en 2022, no usa DST
        private static readonly TimeZoneInfo _zonaMexico =
            TimeZoneInfo.CreateCustomTimeZone("MexicoCentro", TimeSpan.FromHours(-6),
                "México Centro", "México Centro");

        /// <summary>DateTime.Now en zona horaria de México (Centro, UTC-6). Usar en todo el sistema.</summary>
        public static DateTime Ahora =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaMexico);

        /// <summary>Fecha de hoy (sin hora) en zona horaria de México.</summary>
        public static DateTime Hoy => Ahora.Date;

        /// <summary>
        /// Retorna los BaseIDs a los que tiene acceso el usuario actual.
        /// Si es Administrador retorna null (sin filtro, ve todo).
        /// </summary>
        public static List<int> ObtenerBasesUsuario(HttpSessionState session)
        {
            string rol = session["Rol"]?.ToString() ?? "";
            if (rol == "Administrador") return null;

            int claveID = Convert.ToInt32(session["ClaveID"]);
            var lista = new List<int>();

            const string sql = "SELECT BaseID FROM dbo.UsuarioBases WHERE ClaveID = @claveID";
            using (var cn = new SqlConnection(_connStr))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@claveID", claveID);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            lista.Add(rdr.GetInt32(0));
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Retorna las bases activas filtradas por los permisos del usuario.
        /// Cada elemento: (BaseID, Codigo, Nombre).
        /// </summary>
        public static List<BaseLite> ObtenerBasesActivasParaUsuario(HttpSessionState session)
        {
            var basesUsuario = ObtenerBasesUsuario(session);
            var lista = new List<BaseLite>();

            const string sql = "SELECT BaseID, Codigo, Nombre FROM dbo.Bases WHERE Activo = 1 ORDER BY Nombre";
            using (var cn = new SqlConnection(_connStr))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int baseID = rdr.GetInt32(0);
                        if (basesUsuario != null && !basesUsuario.Contains(baseID))
                            continue;
                        lista.Add(new BaseLite
                        {
                            BaseID = baseID,
                            Codigo = rdr.GetString(1),
                            Nombre = rdr.GetString(2)
                        });
                    }
                }
            }
            return lista;
        }

        public class BaseLite
        {
            public int BaseID { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
        }

        /// <summary>
        /// Convierte una cantidad capturada a la unidad base del material.
        /// <para>
        /// Si unidadSeleccionadaID coincide con unidadBaseID → factor = 1, sin consulta a BD.
        /// Si difieren → busca la conversión activa por conversionID; lanza si no existe.
        /// </para>
        /// <para>
        /// Invariante de auditoría: CantidadCapturada × FactorAplicado = Cantidad (unidad base).
        /// </para>
        /// </summary>
        /// <param name="cantidadCapturada">Lo que el usuario capturó.</param>
        /// <param name="conversionID">ID en ConversionesMaterial (null si es la unidad base).</param>
        /// <param name="unidadBaseID">UnidadMedidaID del material.</param>
        /// <param name="unidadSeleccionadaID">ID de la unidad que escogió el usuario.</param>
        /// <param name="db">DataContext LINQ to SQL activo.</param>
        /// <returns>Cantidad equivalente en la unidad base.</returns>
        /// <exception cref="InvalidOperationException">
        /// Cuando no existe conversión activa para la combinación material/unidad.
        /// </exception>
        public static decimal AplicarConversion(
            decimal cantidadCapturada,
            int?    conversionID,
            int     unidadBaseID,
            int     unidadSeleccionadaID,
            InventarioAnkhalDBDataContext db)
        {
            // Caso 1: el usuario capturó en la unidad base → factor = 1
            if (unidadSeleccionadaID == unidadBaseID)
                return cantidadCapturada;

            // Caso 2: conversión requerida
            var conv = db.ConversionesMaterial
                .FirstOrDefault(c => c.ConversionID == conversionID && c.Activo);

            if (conv == null)
                throw new InvalidOperationException(
                    "No existe conversión activa para la unidad seleccionada. " +
                    "No se puede registrar el movimiento.");

            return cantidadCapturada * conv.Factor;
        }

        /// <summary>
        /// Retorna el factor de conversión dado el valor del dropdown de unidad.
        /// selectedVal: "base:{UnidadMedidaID}" | "conv:{ConversionID}" | null/vacío
        /// Throws InvalidOperationException si la conversión no existe o está inactiva.
        /// </summary>
        /// <summary>
        /// Genera un folio con formato {prefix}-yyyyMMdd-### con secuencia diaria independiente por prefijo.
        /// Debe llamarse dentro de una transacción activa en el DataContext para evitar duplicados.
        /// </summary>
        public static string GenerarFolio(InventarioAnkhalDBDataContext db, string prefix)
        {
            DateTime hoy = AppHelper.Hoy;
            int count = db.ExecuteQuery<int>(
                @"SELECT COUNT(*) FROM dbo.LotesMovimiento WITH (UPDLOCK, HOLDLOCK)
                  WHERE FechaLote >= {0} AND FechaLote < {1} AND Folio LIKE {2}",
                hoy, hoy.AddDays(1), prefix + "-%").First();
            return string.Format("{0}-{1}-{2:D3}", prefix, hoy.ToString("yyyyMMdd"), count + 1);
        }

        public static decimal ObtenerFactor(int matID, string selectedVal,
            InventarioAnkhalDBDataContext db)
        {
            if (string.IsNullOrEmpty(selectedVal) || selectedVal.StartsWith("base:"))
                return 1m;

            if (selectedVal.StartsWith("conv:"))
            {
                int convID;
                if (!int.TryParse(selectedVal.Substring(5), out convID))
                    return 1m;

                var conv = db.ConversionesMaterial
                    .FirstOrDefault(c => c.ConversionID == convID &&
                                         c.MaterialID   == matID  &&
                                         c.Activo);
                if (conv == null)
                    throw new InvalidOperationException(
                        string.Format(
                            "No existe conversión activa (ConversionID={0}) para el material ID={1}.",
                            convID, matID));

                return conv.Factor;
            }

            return 1m;
        }
    }
}
