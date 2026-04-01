using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace GrupoAnkhalInventario
{
    /// <summary>
    /// Handler que genera una página HTML limpia para impresión del inventario.
    /// Recibe: ?base=ID&tipo=MAT|PROD|  (tipo vacío = todos)
    /// </summary>
    public class ImprimirInventario : IHttpHandler, IRequiresSessionState
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private InventarioAnkhalDBDataContext NuevoDb()
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = false;
            return ctx;
        }

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
            var basesUsuario = AppHelper.ObtenerBasesUsuario(context.Session);

            // ── Consultas BD ──────────────────────────────────────────────────
            List<MaterialRow> materiales = new List<MaterialRow>();
            List<ProductoRow> productos = new List<ProductoRow>();
            List<ResumenRow> resumen = new List<ResumenRow>();
            decimal valMat = 0, valBuenos = 0, valRechazo = 0;
            string baseNombre = "Todas las bases";

            using (var db = NuevoDb())
            {
                if (baseFiltraID.HasValue)
                {
                    var b = db.Bases.FirstOrDefault(x => x.BaseID == baseFiltraID.Value);
                    if (b != null) baseNombre = b.Nombre;
                }

                // ── Materiales ────────────────────────────────────────────────
                if (tipoFiltro == "" || tipoFiltro == "MAT")
                {
                    var mats = (from m in db.Materiales
                                join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                                where m.Activo
                                orderby m.Codigo
                                select new { m.MaterialID, m.Codigo, m.Descripcion, TipoNombre = tp.Nombre, m.Unidad, m.PrecioUnitario, m.StockMinimo, m.StockMaximo, m.StockOptimo }).ToList();

                    var smQ = from sm in db.StockMateriales
                              join b in db.Bases on sm.BaseID equals b.BaseID
                              where b.Activo
                              select new { sm.MaterialID, b.BaseID, sm.CantidadActual };
                    if (basesUsuario != null) smQ = smQ.Where(s => basesUsuario.Contains(s.BaseID));
                    if (baseFiltraID.HasValue) smQ = smQ.Where(s => s.BaseID == baseFiltraID.Value);
                    var stockMat = smQ.ToList();

                    foreach (var m in mats)
                    {
                        decimal global = stockMat.Where(s => s.MaterialID == m.MaterialID).Sum(s => s.CantidadActual);
                        string nivel = GetNivel(global, m.StockMinimo, m.StockMaximo, m.StockOptimo);
                        materiales.Add(new MaterialRow
                        {
                            Codigo = m.Codigo,
                            Descripcion = m.Descripcion,
                            Tipo = m.TipoNombre,
                            Unidad = m.Unidad,
                            Stock = global,
                            Nivel = nivel,
                            Precio = m.PrecioUnitario,
                            Valor = global * m.PrecioUnitario
                        });
                        valMat += global * m.PrecioUnitario;
                    }
                }

                // ── Productos ─────────────────────────────────────────────────
                if (tipoFiltro == "" || tipoFiltro == "PROD")
                {
                    var prods = (from p in db.Productos
                                 join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                                 where p.Activo
                                 orderby p.Codigo
                                 select new { p.ProductoID, p.Codigo, p.Descripcion, TipoNombre = tp.Nombre, p.PrecioVenta }).ToList();

                    var spQ = from sp in db.StockProductos
                              join b in db.Bases on sp.BaseID equals b.BaseID
                              where b.Activo
                              select new { sp.ProductoID, b.BaseID, sp.CantidadBuenas, sp.CantidadRechazo };
                    if (basesUsuario != null) spQ = spQ.Where(s => basesUsuario.Contains(s.BaseID));
                    if (baseFiltraID.HasValue) spQ = spQ.Where(s => s.BaseID == baseFiltraID.Value);
                    var stockProd = spQ.ToList();

                    foreach (var p in prods)
                    {
                        int buenos = stockProd.Where(s => s.ProductoID == p.ProductoID).Sum(s => s.CantidadBuenas);
                        int rechazo = stockProd.Where(s => s.ProductoID == p.ProductoID).Sum(s => s.CantidadRechazo);
                        decimal vb = buenos * p.PrecioVenta;
                        decimal vr = rechazo * (p.PrecioVenta * 0.5m);
                        productos.Add(new ProductoRow
                        {
                            Codigo = p.Codigo,
                            Descripcion = p.Descripcion,
                            Tipo = p.TipoNombre,
                            Buenos = buenos,
                            Rechazo = rechazo,
                            Total = buenos + rechazo,
                            PrecioVenta = p.PrecioVenta,
                            ValorBuenos = vb,
                            ValorRechazo = vr
                        });
                        valBuenos += vb;
                        valRechazo += vr;
                    }
                }

                // ── Resumen por base ──────────────────────────────────────────
                var basesQ = db.Bases.Where(b => b.Activo).AsQueryable();
                if (basesUsuario != null) basesQ = basesQ.Where(b => basesUsuario.Contains(b.BaseID));
                if (baseFiltraID.HasValue) basesQ = basesQ.Where(b => b.BaseID == baseFiltraID.Value);
                var bases = basesQ.OrderBy(b => b.Nombre).ToList();

                var smTodos = (from sm in db.StockMateriales
                               join m in db.Materiales on sm.MaterialID equals m.MaterialID
                               select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario }).ToList();
                var spTodos = (from sp in db.StockProductos
                               join p in db.Productos on sp.ProductoID equals p.ProductoID
                               select new { sp.BaseID, VB = sp.CantidadBuenas * p.PrecioVenta, VR = sp.CantidadRechazo * (p.PrecioVenta * 0.5m) }).ToList();

                foreach (var b in bases)
                {
                    resumen.Add(new ResumenRow
                    {
                        Base = b.Nombre,
                        Materiales = smTodos.Where(s => s.BaseID == b.BaseID).Sum(s => s.Valor),
                        ProdBuenos = spTodos.Where(s => s.BaseID == b.BaseID).Sum(s => s.VB),
                        ProdRechazo = spTodos.Where(s => s.BaseID == b.BaseID).Sum(s => s.VR)
                    });
                }
            }

            // ── Generar HTML ──────────────────────────────────────────────────
            string html = BuildHtml(baseNombre, tipoFiltro, materiales, productos, resumen,
                                    valMat, valBuenos, valRechazo);

            context.Response.ContentType = "text/html";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(html);
        }

        // ── Construcción del HTML de impresión ────────────────────────────────
        private string BuildHtml(string baseNombre, string tipoFiltro,
            List<MaterialRow> mats, List<ProductoRow> prods, List<ResumenRow> resumen,
            decimal valMat, decimal valBuenos, decimal valRechazo)
        {
            var sb = new StringBuilder();
            string fecha = AppHelper.Ahora.ToString("dd/MM/yyyy HH:mm");
            decimal total = valMat + valBuenos + valRechazo;

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
  .footer-print { margin-top: 20px; border-top: 1px solid #ccc; padding-top: 8px; font-size: 9px; color: #888; }
  @media print {
    body { padding: 10px; }
    .no-print { display: none !important; }
  }
</style>
</head>
<body>
");
            // Encabezado
            sb.Append("<h1>Inventario General &mdash; Grupo ANKHAL</h1>");
            sb.Append("<div class='meta'>Base: <strong>" + HttpUtility.HtmlEncode(baseNombre) + "</strong> &nbsp;|&nbsp; Generado: <strong>" + fecha + "</strong></div>");

            // Cards resumen
            sb.Append("<div class='cards'>");
            sb.Append("<div class='card'><div class='lbl'>Valor Total</div><div class='num'>" + total.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Materiales</div><div class='num'>" + valMat.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Prod. Buenos</div><div class='num'>" + valBuenos.ToString("C2") + "</div></div>");
            sb.Append("<div class='card'><div class='lbl'>Prod. Rechazo</div><div class='num'>" + valRechazo.ToString("C2") + "</div></div>");
            sb.Append("</div>");

            // Tabla Materiales
            if ((tipoFiltro == "" || tipoFiltro == "MAT") && mats.Count > 0)
            {
                sb.Append("<h2>Materiales (" + mats.Count + " registros)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Descripci&oacute;n</th><th>Tipo</th><th>Unidad</th>");
                sb.Append("<th class='text-right'>Stock Global</th><th>Nivel</th>");
                sb.Append("<th class='text-right'>Precio Unit.</th><th class='text-right'>Valor ($)</th>");
                sb.Append("</tr></thead><tbody>");
                decimal totalValMat = 0;
                foreach (var m in mats)
                {
                    string nivelCls = "nivel-" + m.Nivel;
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Tipo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(m.Unidad) + "</td>");
                    sb.Append("<td class='text-right'>" + m.Stock.ToString("N2") + "</td>");
                    sb.Append("<td class='" + nivelCls + "'>" + NivelTexto(m.Nivel) + "</td>");
                    sb.Append("<td class='text-right'>" + m.Precio.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + m.Valor.ToString("C2") + "</strong></td>");
                    sb.Append("</tr>");
                    totalValMat += m.Valor;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='7' class='text-right'><strong>Total Materiales</strong></td>");
                sb.Append("<td class='text-right'><strong>" + totalValMat.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }

            // Tabla Productos
            if ((tipoFiltro == "" || tipoFiltro == "PROD") && prods.Count > 0)
            {
                sb.Append("<h2>Productos (" + prods.Count + " registros)</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>C&oacute;digo</th><th>Descripci&oacute;n</th><th>Tipo</th>");
                sb.Append("<th class='text-right'>Buenos</th><th class='text-right'>Rechazo</th><th class='text-right'>Total</th>");
                sb.Append("<th class='text-right'>Precio Venta</th><th class='text-right'>Valor Buenos</th><th class='text-right'>Valor Rechazo</th>");
                sb.Append("</tr></thead><tbody>");
                decimal tVB = 0, tVR = 0;
                foreach (var p in prods)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Codigo) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Descripcion) + "</td>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(p.Tipo) + "</td>");
                    sb.Append("<td class='text-right'>" + p.Buenos + "</td>");
                    sb.Append("<td class='text-right'>" + p.Rechazo + "</td>");
                    sb.Append("<td class='text-right'><strong>" + p.Total + "</strong></td>");
                    sb.Append("<td class='text-right'>" + p.PrecioVenta.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + p.ValorBuenos.ToString("C2") + "</strong></td>");
                    sb.Append("<td class='text-right'>" + p.ValorRechazo.ToString("C2") + "</td>");
                    sb.Append("</tr>");
                    tVB += p.ValorBuenos; tVR += p.ValorRechazo;
                }
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td colspan='7' class='text-right'><strong>Total Productos</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tVB.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tVR.ToString("C2") + "</strong></td>");
                sb.Append("</tr></tfoot></table>");
            }

            // Resumen por base
            if (resumen.Count > 0)
            {
                sb.Append("<h2>Resumen por Base / Planta</h2>");
                sb.Append("<table><thead><tr>");
                sb.Append("<th>Base / Planta</th><th class='text-right'>Materiales ($)</th><th class='text-right'>Prod. Buenos ($)</th><th class='text-right'>Prod. Rechazo ($)</th><th class='text-right'>TOTAL ($)</th>");
                sb.Append("</tr></thead><tbody>");
                decimal tMat = 0, tBuenos = 0, tRec = 0;
                foreach (var r in resumen)
                {
                    decimal tot = r.Materiales + r.ProdBuenos + r.ProdRechazo;
                    sb.Append("<tr>");
                    sb.Append("<td>" + HttpUtility.HtmlEncode(r.Base) + "</td>");
                    sb.Append("<td class='text-right'>" + r.Materiales.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'>" + r.ProdBuenos.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'>" + r.ProdRechazo.ToString("C2") + "</td>");
                    sb.Append("<td class='text-right'><strong>" + tot.ToString("C2") + "</strong></td>");
                    sb.Append("</tr>");
                    tMat += r.Materiales; tBuenos += r.ProdBuenos; tRec += r.ProdRechazo;
                }
                decimal grandTotal = tMat + tBuenos + tRec;
                sb.Append("</tbody><tfoot><tr>");
                sb.Append("<td><strong>TOTAL</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tMat.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tBuenos.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + tRec.ToString("C2") + "</strong></td>");
                sb.Append("<td class='text-right'><strong>" + grandTotal.ToString("C2") + "</strong></td>");
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

        // ── Helpers nivel ─────────────────────────────────────────────────────
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

        // ── DTOs internos ─────────────────────────────────────────────────────
        private class MaterialRow
        {
            public string Codigo, Descripcion, Tipo, Unidad, Nivel;
            public decimal Stock, Precio, Valor;
        }
        private class ProductoRow
        {
            public string Codigo, Descripcion, Tipo;
            public int Buenos, Rechazo, Total;
            public decimal PrecioVenta, ValorBuenos, ValorRechazo;
        }
        private class ResumenRow
        {
            public string Base;
            public decimal Materiales, ProdBuenos, ProdRechazo;
        }
    }
}
