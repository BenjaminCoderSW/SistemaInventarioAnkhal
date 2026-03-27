using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.SessionState;

namespace GrupoAnkhalInventario.Helpers
{
    public static class AppHelper
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

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
    }
}
