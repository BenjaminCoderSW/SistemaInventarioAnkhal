using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace GrupoAnkhalInventario.Services
{
    // ── DTOs espejo de la API de Asistencia ──────────────────────────────────────

    public class EmpleadoDto
    {
        public int    IdUsuario        { get; set; }
        public string Nombre           { get; set; }
        public string ApellidoPaterno  { get; set; }
        public string ApellidoMaterno  { get; set; }
        public string Telefono         { get; set; }
        public string TelefonoFamiliar { get; set; }
        public string Email            { get; set; }
        public string NumeroEmpleado   { get; set; }
        public int    Estatus          { get; set; }

        public string NombreCompleto =>
            (Nombre + " " + ApellidoPaterno).Trim();
    }

    public class EmpleadoResumenDto
    {
        public int    IdUsuario      { get; set; }
        public string NombreCompleto { get; set; }
    }

    // ── Servicio ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Punto único de acceso a datos de empleados desde la API de AsistenciaAnkhal.
    /// Todos los métodos son síncronos (compatible con WebForms 4.7.2).
    /// Incluye cache en memoria para reducir llamadas HTTP.
    /// </summary>
    public static class UsuarioService
    {
        private static readonly HttpClient _client;
        private static readonly MemoryCache _cache = MemoryCache.Default;

        private const int CACHE_EMP_MIN  = 15;   // minutos cache de datos de empleado
        private const int CACHE_FOTO_MIN = 30;   // minutos cache de fotos

        static UsuarioService()
        {
            var baseUrl = ConfigurationManager.AppSettings["AsistenciaApiUrl"] ?? "";
            var apiKey  = ConfigurationManager.AppSettings["AsistenciaApiKey"] ?? "";

            _client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout     = TimeSpan.FromSeconds(10)
            };
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        // ── ObtenerEmpleado ──────────────────────────────────────────────────────
        /// <summary>
        /// Datos completos de un empleado por UsuarioID. Cache 15 min.
        /// Retorna objeto vacío (nunca null) si falla o no existe.
        /// </summary>
        public static EmpleadoDto ObtenerEmpleado(int usuarioID)
        {
            string key = $"emp_{usuarioID}";
            if (_cache[key] is EmpleadoDto cached) return cached;

            try
            {
                string json = GetSync(usuarioID.ToString());
                var emp = JsonConvert.DeserializeObject<EmpleadoDto>(json);
                if (emp != null)
                {
                    _cache.Set(key, emp, DateTimeOffset.Now.AddMinutes(CACHE_EMP_MIN));
                    return emp;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsuarioService.ObtenerEmpleado({usuarioID})] {ex.Message}");
            }

            return new EmpleadoDto { IdUsuario = usuarioID };
        }

        // ── ObtenerEmpleadosBulk ─────────────────────────────────────────────────
        /// <summary>
        /// Datos completos de varios empleados. Los que están en cache se resuelven
        /// localmente; solo los faltantes generan una llamada HTTP al endpoint bulk.
        /// </summary>
        public static List<EmpleadoDto> ObtenerEmpleadosBulk(IEnumerable<int> usuarioIDs)
        {
            var ids     = usuarioIDs?.Distinct().ToList() ?? new List<int>();
            var result  = new List<EmpleadoDto>();
            var faltantes = new List<int>();

            foreach (var id in ids)
            {
                string key = $"emp_{id}";
                if (_cache[key] is EmpleadoDto cached)
                    result.Add(cached);
                else
                    faltantes.Add(id);
            }

            if (faltantes.Count > 0)
            {
                try
                {
                    var body = JsonConvert.SerializeObject(new { ids = faltantes });
                    string json = PostSync("bulk", body);
                    var frescos = JsonConvert.DeserializeObject<List<EmpleadoDto>>(json)
                                  ?? new List<EmpleadoDto>();

                    foreach (var emp in frescos)
                    {
                        string key = $"emp_{emp.IdUsuario}";
                        _cache.Set(key, emp, DateTimeOffset.Now.AddMinutes(CACHE_EMP_MIN));
                        result.Add(emp);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UsuarioService.ObtenerEmpleadosBulk] {ex.Message}");
                    // Agrega entradas vacías para los IDs que fallaron
                    foreach (var id in faltantes)
                        result.Add(new EmpleadoDto { IdUsuario = id });
                }
            }

            return result;
        }

        // ── ObtenerDisponibles ───────────────────────────────────────────────────
        /// <summary>
        /// Empleados activos en Asistencia cuyos IDs NO están en excluirIds.
        /// Sin cache — siempre actualizado (para modal de nuevo usuario).
        /// </summary>
        public static List<EmpleadoResumenDto> ObtenerDisponibles(IEnumerable<int> excluirIds)
        {
            try
            {
                var ids = excluirIds?.Where(i => i > 0).ToList() ?? new List<int>();
                string qs = ids.Count > 0 ? "disponibles?excluirIds=" + string.Join(",", ids) : "disponibles";
                string json = GetSync(qs);
                return JsonConvert.DeserializeObject<List<EmpleadoResumenDto>>(json)
                       ?? new List<EmpleadoResumenDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsuarioService.ObtenerDisponibles] {ex.Message}");
                return new List<EmpleadoResumenDto>();
            }
        }

        // ── ObtenerFoto ──────────────────────────────────────────────────────────
        /// <summary>
        /// Foto (bytes) de un empleado. Cache 30 min. Retorna null si no tiene foto.
        /// </summary>
        public static byte[] ObtenerFoto(int usuarioID)
        {
            string key = $"foto_{usuarioID}";
            if (_cache[key] is byte[] cachedFoto) return cachedFoto;

            try
            {
                string json = GetSync($"foto/{usuarioID}");
                var dto = JsonConvert.DeserializeObject<FotoResponse>(json);

                if (!string.IsNullOrEmpty(dto?.Foto))
                {
                    byte[] bytes = Convert.FromBase64String(dto.Foto);
                    _cache.Set(key, bytes, DateTimeOffset.Now.AddMinutes(CACHE_FOTO_MIN));
                    return bytes;
                }

                // Sin foto: cachear array vacío para no repetir la llamada
                _cache.Set(key, new byte[0], DateTimeOffset.Now.AddMinutes(CACHE_FOTO_MIN));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsuarioService.ObtenerFoto({usuarioID})] {ex.Message}");
            }

            return null;
        }

        // ── Helpers HTTP síncronos ───────────────────────────────────────────────
        // Task.Run evita el deadlock del SynchronizationContext de ASP.NET 4.x
        // al correr la tarea en un hilo de ThreadPool sin contexto capturado.

        private static string GetSync(string relativeUrl)
        {
            return Task.Run(() => _client.GetStringAsync(relativeUrl))
                       .GetAwaiter().GetResult();
        }

        private static string PostSync(string relativeUrl, string jsonBody)
        {
            return Task.Run(async () =>
            {
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var resp    = await _client.PostAsync(relativeUrl, content);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }).GetAwaiter().GetResult();
        }

        // DTO interno para deserializar respuesta de /foto
        private class FotoResponse { public string Foto { get; set; } }
    }
}
