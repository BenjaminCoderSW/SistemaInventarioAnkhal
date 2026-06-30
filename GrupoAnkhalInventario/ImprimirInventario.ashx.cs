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
    /// Handler que genera una página HTML limpia para impresión del inventario.
    /// Recibe: ?base=ID&tipo=MAT|PROD|&buscarMat=X&buscarProd=X&soloExistencia=1
    /// </summary>
    public class ImprimirInventario : IHttpHandler, IRequiresSessionState
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

            var mats    = (tipoFiltro == "" || tipoFiltro == "MAT")  ? svc.ObtenerMateriales(filtros)      : null;
            var prods   = (tipoFiltro == "" || tipoFiltro == "PROD") ? svc.ObtenerProductos(filtros)       : null;
            var resumen = svc.ObtenerResumenPorBase(filtros);
            var kpis    = svc.ObtenerKpis(filtros);

            // ── Generar HTML ──────────────────────────────────────────────────
            string html = BuildHtml(baseNombre, tipoFiltro, mats, prods, resumen, kpis);

            context.Response.ContentType = "text/html";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(html);
        }

        // ── Construcción del HTML de impresión ────────────────────────────────
        private string BuildHtml(
            string baseNombre,
            string tipoFiltro,
            System.Collections.Generic.List<MaterialInvVM> mats,
            System.Collections.Generic.List<ProductoInvVM> prods,
            System.Collections.Generic.List<ResumenBaseVM> resumen,
            KpisVM kpis)
        {
            var sb = new StringBuilder();
            string fecha = AppHelper.Ahora.ToString("dd/MM/yyyy HH:mm");

            sb.Append(@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='utf-8' />
<title>Inventario General - Grupo ANKHAL</title>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: Arial, sans-serif; font-size: 11px; color: #222; padding: 20px; }
  h1 { font-size: 16px; color: #003366; border-bottom: 2px solid #003366; padding-bottom: 4px; margin-bottom: 4px; }
  .meta { font-size: 10px; color: #666; margin-bottom: 14px; }
  .cards { display: flex; gap: 12px; margin-bottom: 16px; flex-wrap: wrap; }
  .card { flex: 1; min-width: 130px; border: 1px solid #ccc; border-radius: 6px; padding: 8px 12px; }
  .card .lbl { font-size: 9px; text-transform: uppercase; color: #666; }
  .card .num { font-size: 15px; font-weight: 700; color: #003366; }
  h2 { font-size: 12px; background: #003366; color: #fff; padding: 4px 8px; margin: 14px 0 0 0; border-radius: 3px 3px 0 0; }
  table { width: 100%; border-collapse: collapse; margin-bottom: 2px; }
  th { background: #003366; color: #fff; padding: 4px 6px; text-align: left; font-size: 10px; }
  td { padding: 3px 6px; border-bottom: 1px solid #e0e0e0; font-size: 10px; }
  tr:nth-child(even) td { background: #f5f7fa; }
  tfoot td { background: #e8ecf4 !important; font-weight: 700; }
  .text-right { text-align: right; }
  .nivel-critico { color: #c0392b; font-weight: 600; }
  .nivel-bajo    { color: #d35400; font-weight: 600; }
  .nivel-optimo  { color: #1e8449; font-weight: 600; }
  .nivel-sin     { color: #7f8c8d; }
  .merma         { color: #922b21; font-weight: 600; }
  .footer-print { margin-top: 20px; border-top: 1px solid #ccc; padding-top: 8px; font-size: 9px; color: #888; }
  @media print {
    body { padding: 10px; }
    .no-print { display: none !important; }
  }
</style>
</head>
<body>
");
            sb.Append("<h1>Inventario General &mdash; Grupo ANKHAL</h1>");
            sb.Append("<div class='meta'>Base: <strong>" + HttpUtility.HtmlEncode(baseNombre) + "</strong> &nbsp;|&nbsp; Generado: <strong>" + fecha + "</strong></div>");

            sb.Append("<div class='cards'>");
            sb.Append("<div class='card'><div class='lbl'>Materiales</div><div class='num'>" + kpis.ValorMateriales.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Prod. Buenos</div><div class='num'>" + kpis.ValorBuenos.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Prod. Rechazo</div><div class='num'>" + kpis.ValorRechazo.ToString("C2") + "</div></div>");
            sb.Append("</div>");
            sb.Append("<div class='cards'>");
            sb.Append("<div class='card'><div class='lbl'>Merma MP</div><div class='num merma'>" + kpis.ValorMerma.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Valor Total</div><div class='num'>" + kpis.ValorTotal.ToString("C2") + "</div></div>");
            sb.Append("</div>");

            // Tabla Materiales
            if (mats != null && mats.Count > 0)
            {
                sb.Append("<h2>Materiales (" + mats.Count + " registros)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Tipo</th><th>Unidad</th>");
                sb.Append("<th class='text-right'>Stock Global</th><th>Nivel</th>");
                sb.Append("<th class='text-right'>Precio Unit.</th><th class='text-right'>Valor ($)</th>");
                sb.Append("<th class='text-right'>Merma</th><th class='text-right'>Valor Merma ($)</th>");
                sb.Append("</tr></thead><tbody>");
                decimal totalValMat = 0, totalValMerma = 0;
                foreach (var m in mats)
                {
                    string nivel = GetNivel(m.StockGlobal, m.StockMinimo, m.StockMaximo, m.StockOptimo);
                    decimal valor = m.StockGlobal * m.PrecioUnitario;
                    decimal valorMerma = m.MermaGlobal * m.PrecioUnitario;
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.TipoNombre) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Unidad) + "</td>");
                    sb.Append("<td class='text-right'>" + m.StockGlobal.ToString("N2") + "</td>");
                    sb.Append("<td class='nivel-" + nivel + "'>" + NivelTexto(nivel) + "</td>");
                    sb.Append("<td class='text-right'>" + m.PrecioUnitario.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + valor.ToString("C2") + "</strong></td>");
                    sb.Append("<td class='text-right merma'>" + m.MermaGlobal.ToString("N2") + "</td>");
                    sb.Append("<td class='text-right merma'>" + valorMerma.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                    totalValMat += valor;
                    totalValMerma += valorMerma;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='6' class='text-right'><strong>Total Materiales</strong></td>");
                sb.Append("<td class='text-right'><strong>" + totalValMat.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right merma'></td>");
                sb.Append("<td class='text-right merma'><strong>" + totalValMerma.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }

            // Tabla Productos
            if (prods != null && prods.Count > 0)
            {
                sb.Append("<h2>Productos (" + prods.Count + " registros)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Tipo</th>");
                sb.Append("<th class='text-right'>Buenos</th><th class='text-right'>Rechazo</th><th class='text-right'>Total</th>");
                sb.Append("<th class='text-right'>Precio Venta</th><th class='text-right'>Valor Buenos</th><th class='text-right'>Valor Rechazo</th>");
                sb.Append("</tr></thead><tbody>");
                decimal tVB = 0, tVR = 0;
                foreach (var p in prods)
                {
                    decimal vb = p.TotalBuenos * p.PrecioVenta;
                    decimal vr = p.TotalRechazo * (p.PrecioVenta * 0.5m);
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.TipoNombre) + "</td>");
                    sb.Append("<td class='text-right'>" + p.TotalBuenos + "</td>");
                    sb.Append("<td class='text-right'>" + p.TotalRechazo + "</td>");
                    sb.Append("<td class='text-right'><strong>" + (p.TotalBuenos + p.TotalRechazo) + "</strong></td>");
                    sb.Append("<td class='text-right'>" + p.PrecioVenta.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + vb.ToString("C2") + "</strong></td>");
                    sb.Append("<td class='text-right'>" + vr.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                    tVB += vb; tVR += vr;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='6' class='text-right'><strong>Total Productos</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tVB.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tVR.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }

            // Resumen por base
            if (resumen != null && resumen.Count > 0)
            {
                sb.Append("<h2>Resumen por Base / Planta</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>Base / Planta</th><th class='text-right'>Materiales ($)</th><th class='text-right'>Prod. Buenos ($)</th><th class='text-right'>Prod. Rechazo ($)</th><th class='text-right'>Merma MP ($)</th><th class='text-right'>TOTAL ($)</th>");
                sb.Append("</tr></thead><tbody>");
                decimal tMat = 0, tBuenos = 0, tRec = 0, tMerma = 0;
                foreach (var r in resumen)
                {
                    decimal tot = r.ValorMateriales + r.ValorBuenos + r.ValorRechazo + r.ValorMerma;
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(r.BaseNombre) + "</td>");
                    sb.Append("<td class='text-right'>" + r.ValorMateriales.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'>" + r.ValorBuenos.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'>" + r.ValorRechazo.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right merma'>" + r.ValorMerma.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + tot.ToString("C2") + "</strong></td>");
                    sb.Append("</tr>");
                    tMat += r.ValorMateriales; tBuenos += r.ValorBuenos; tRec += r.ValorRechazo; tMerma += r.ValorMerma;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td><strong>TOTAL</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tMat.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tBuenos.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tRec.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right merma'><strong>" + tMerma.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + (tMat + tBuenos + tRec + tMerma).ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }

            sb.Append("<div class='footer-print'>Grupo ANKHAL &mdash; Sistema de Inventario &mdash; Impreso el " + fecha + "</div>");
            sb.Append(@"
<script>
  window.onload = function() { window.print(); };
</script>
</body></html>");

            return sb.ToString();
        }

        // ── Helpers nivel (locales al handler de impresión) ───────────────────
        private string GetNivel(decimal stock, decimal minimo, decimal maximo, decimal optimo)
        {
            if (stock == 0)     return "sin";
            if (stock < minimo) return "critico";
            if (stock < maximo) return "bajo";
            return "optimo";
        }

        private string NivelTexto(string nivel)
        {
            switch (nivel)
            {
                case "critico": return "Critico";
                case "bajo":    return "Bajo";
                case "optimo":  return "Optimo";
                default:        return "Sin stock";
            }
        }

        public bool IsReusable { get { return false; } }
    }
}
