using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Services;
using System;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace GrupoAnkhalInventario
{
    /// <summary>
    /// Handler que genera una página HTML limpia para impresión del inventario por base.
    /// Recibe: ?base=ID&tipo=MAT|PROD|&buscarMat=X&buscarProd=X&soloExistencia=1
    /// </summary>
    public class ImprimirInventarioPorBase : IHttpHandler, IRequiresSessionState
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        public void ProcessRequest(HttpContext context)
        {
            // ── Auth ──────────────────────────────────────────────────────────
            if (context.Session["ClaveID"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.End();
                return;
            }

            // ── Parámetros ────────────────────────────────────────────────────
            int? baseFiltraID = null;
            if (!string.IsNullOrEmpty(context.Request.QueryString["base"]))
            {
                int bid;
                if (int.TryParse(context.Request.QueryString["base"], out bid) && bid > 0)
                    baseFiltraID = bid;
            }
            string tipoFiltro = (context.Request.QueryString["tipo"] ?? "").Trim().ToUpper();
            string buscarMat  = (context.Request.QueryString["buscarMat"]  ?? "").Trim();
            string buscarProd = (context.Request.QueryString["buscarProd"] ?? "").Trim();
            bool soloExistencia = context.Request.QueryString["soloExistencia"] == "1";

            var filtros = new FiltrosInventario
            {
                BasesUsuario    = AppHelper.ObtenerBasesUsuario(context.Session),
                BaseFiltraID    = baseFiltraID,
                TipoFiltro      = tipoFiltro,
                BuscarMat       = buscarMat,
                BuscarProd      = buscarProd,
                SoloConExistencia = soloExistencia
            };

            // ── Consultas vía servicio centralizado ───────────────────────────
            var svc = new InventarioService(_connStr);
            string baseNombre = svc.ObtenerNombreBase(baseFiltraID);

            var mats  = (tipoFiltro == "" || tipoFiltro == "MAT")  ? svc.ObtenerMaterialesPorBase(filtros)  : null;
            var prods = (tipoFiltro == "" || tipoFiltro == "PROD") ? svc.ObtenerProductosPorBase(filtros) : null;

            // ── Generar HTML ──────────────────────────────────────────────────
            string html = BuildHtml(baseNombre, tipoFiltro, mats, prods, buscarMat, buscarProd, soloExistencia);

            context.Response.ContentType = "text/html";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(html);
        }

        // ── Construcción del HTML de impresión ────────────────────────────────
        private string BuildHtml(
            string baseNombre,
            string tipoFiltro,
            System.Collections.Generic.List<DetalleBaseMatVM> mats,
            System.Collections.Generic.List<DetalleBaseProdVM> prods,
            string buscarMat,
            string buscarProd,
            bool soloExistencia)
        {
            var sb = new StringBuilder();
            string fecha = AppHelper.Ahora.ToString("dd/MM/yyyy HH:mm");

            sb.Append(@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='utf-8' />
<title>Inventario por Base - Grupo ANKHAL</title>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: Arial, sans-serif; font-size: 11px; color: #222; padding: 20px; }
  h1 { font-size: 16px; color: #003366; border-bottom: 2px solid #003366; padding-bottom: 4px; margin-bottom: 4px; }
  .meta { font-size: 10px; color: #666; margin-bottom: 14px; }
  h2 { font-size: 12px; background: #003366; color: #fff; padding: 4px 8px; margin: 14px 0 0 0; border-radius: 3px 3px 0 0; }
  table { width: 100%; border-collapse: collapse; margin-bottom: 2px; }
  th { background: #003366; color: #fff; padding: 4px 6px; text-align: left; font-size: 10px; }
  td { padding: 3px 6px; border-bottom: 1px solid #e0e0e0; font-size: 10px; }
  tr:nth-child(even) td { background: #f5f7fa; }
  tfoot td { background: #e8ecf4 !important; font-weight: 700; }
  .text-right { text-align: right; }
  .text-success { color: #1e8449; }
  .text-warning { color: #d35400; }
  .footer-print { margin-top: 20px; border-top: 1px solid #ccc; padding-top: 8px; font-size: 9px; color: #888; }
  @media print {
    body { padding: 10px; }
    .no-print { display: none !important; }
  }
</style>
</head>
<body>
");
            sb.Append("<h1>Inventario por Base / Planta &mdash; Grupo ANKHAL</h1>");

            // Línea de metadatos
            sb.Append("<div class='meta'>");
            sb.Append("Base: <strong>" + HttpUtility.HtmlEncode(baseNombre) + "</strong>");
            if (!string.IsNullOrEmpty(buscarMat))
                sb.Append(" &nbsp;|&nbsp; Filtro material: <strong>" + HttpUtility.HtmlEncode(buscarMat) + "</strong>");
            if (!string.IsNullOrEmpty(buscarProd))
                sb.Append(" &nbsp;|&nbsp; Filtro producto: <strong>" + HttpUtility.HtmlEncode(buscarProd) + "</strong>");
            if (soloExistencia)
                sb.Append(" &nbsp;|&nbsp; <strong>Solo con existencia</strong>");
            sb.Append(" &nbsp;|&nbsp; Generado: <strong>" + fecha + "</strong>");
            sb.Append("</div>");

            // Tabla Materiales
            if (mats != null && mats.Count > 0)
            {
                sb.Append("<h2>Materiales por Base (" + mats.Count + " filas)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Descripci&oacute;n</th><th>Tipo</th><th>Base</th>");
                sb.Append("<th class='text-right'>Cantidad</th><th>Unidad</th>");
                sb.Append("<th class='text-right'>Precio Unit.</th><th class='text-right'>Valor ($)</th>");
                sb.Append("</tr></thead><tbody>");
                decimal totalValMat = 0;
                foreach (var m in mats)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.BaseNombre) + "</td>");
                    sb.Append("<td class='text-right'>" + m.Cantidad.ToString("N2") + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Unidad) + "</td>");
                    sb.Append("<td class='text-right'>" + m.PrecioUnitario.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + m.ValorItem.ToString("C2") + "</strong></td>");
                    sb.Append("</tr>");
                    totalValMat += m.ValorItem;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='7' class='text-right'><strong>Total</strong></td>");
                sb.Append("<td class='text-right'><strong>" + totalValMat.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }
            else if (mats != null)
            {
                sb.Append("<h2>Materiales por Base</h2>");
                sb.Append("<p style='padding:8px;color:#666;'>Sin registros con los filtros seleccionados.</p>");
            }

            // Tabla Productos
            if (prods != null && prods.Count > 0)
            {
                sb.Append("<h2>Productos por Base (" + prods.Count + " filas)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Descripci&oacute;n</th><th>Tipo</th><th>Base</th>");
                sb.Append("<th class='text-right'>Buenos</th><th class='text-right'>Rechazo</th><th class='text-right'>Total</th>");
                sb.Append("<th class='text-right'>Precio Venta</th><th class='text-right'>Valor Buenos</th><th class='text-right'>Valor Rechazo</th>");
                sb.Append("</tr></thead><tbody>");
                decimal tVB = 0, tVR = 0;
                foreach (var p in prods)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.BaseNombre) + "</td>");
                    sb.Append("<td class='text-right'>" + p.Buenos + "</td>");
                    sb.Append("<td class='text-right'>" + p.Rechazo + "</td>");
                    sb.Append("<td class='text-right'><strong>" + (p.Buenos + p.Rechazo) + "</strong></td>");
                    sb.Append("<td class='text-right'>" + p.PrecioVenta.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right text-success'><strong>" + p.ValorBuenos.ToString("C2") + "</strong></td>");
                    sb.Append("<td class='text-right text-warning'>" + p.ValorRechazo.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                    tVB += p.ValorBuenos; tVR += p.ValorRechazo;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='8' class='text-right'><strong>Total</strong></td>");
                sb.Append("<td class='text-right text-success'><strong>" + tVB.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right text-warning'><strong>" + tVR.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }
            else if (prods != null)
            {
                sb.Append("<h2>Productos por Base</h2>");
                sb.Append("<p style='padding:8px;color:#666;'>Sin registros con los filtros seleccionados.</p>");
            }

            sb.Append("<div class='footer-print'>Grupo ANKHAL &mdash; Sistema de Inventario &mdash; Impreso el " + fecha + "</div>");
            sb.Append(@"
<script>
  window.onload = function() { window.print(); };
</script>
</body></html>");

            return sb.ToString();
        }

        public bool IsReusable { get { return false; } }
    }
}
