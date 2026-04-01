using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GrupoAnkhalInventario
{
    public partial class Default : Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ── Estado del filtro (inicializado una vez por request) ──────────────
        private DateTime  _desde, _hastaExcl;
        private string    _periodoLabel;
        private int?      _baseID;
        private List<int> _basesUsuario;

        // ── DTOs internos ─────────────────────────────────────────────────────
        private class ProdResumenVM
        {
            public string  Base         { get; set; }
            public string  Producto     { get; set; }
            public int     Buenos       { get; set; }
            public int     Rechazo      { get; set; }
            public decimal ValorProducido { get; set; }
            public decimal CostoMat     { get; set; }
            public decimal Margen       { get { return ValorProducido - CostoMat; } }
        }

        private class EntregaDashVM
        {
            public string   Folio   { get; set; }
            public string   Cliente { get; set; }
            public string   Base    { get; set; }
            public decimal  Total   { get; set; }
            public string   Estado  { get; set; }
            public DateTime Fecha   { get; set; }
        }

        private class ValorBaseVM
        {
            public string  Base    { get; set; }
            public decimal ValMat  { get; set; }
            public decimal ValProd { get; set; }
        }

        // ══ Page_Load ═════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }

            if (!IsPostBack)
            {
                CargarHeader();
                CargarDropdownBase();
                InicializarFiltros();
                CargarDashboard();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            InicializarFiltros();
            CargarDashboard();
        }

        // ── Header bienvenida ─────────────────────────────────────────────────
        private void CargarHeader()
        {
            lblNombreUsuario.Text = Session["NombreCompleto"]?.ToString() ?? "Usuario";
            lblRol.Text           = Session["Rol"]?.ToString() ?? "";
            lblFechaHora.Text = AppHelper.Ahora.ToString("dddd dd/MM/yyyy  HH:mm",
                                    new CultureInfo("es-MX"));
        }

        // ── Dropdown bases ────────────────────────────────────────────────────
        private void CargarDropdownBase()
        {
            var bases = AppHelper.ObtenerBasesActivasParaUsuario(Session);
            ddlBase.Items.Clear();
            ddlBase.Items.Add(new ListItem("-- Todas --", ""));
            foreach (var b in bases)
                ddlBase.Items.Add(new ListItem(b.Nombre, b.BaseID.ToString()));
        }

        // ── Calcula rango de fechas según periodo seleccionado ────────────────
        private void InicializarFiltros()
        {
            _basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
            _baseID = string.IsNullOrEmpty(ddlBase.SelectedValue)
                ? (int?)null
                : int.Parse(ddlBase.SelectedValue);

            DateTime hoy = AppHelper.Hoy;

            // En la primera carga: pre-llenar los TextBox según el período del dropdown
            if (!IsPostBack)
            {
                switch (ddlPeriodo.SelectedValue)
                {
                    case "semana":
                        int dow = (int)hoy.DayOfWeek == 0 ? 7 : (int)hoy.DayOfWeek;
                        txtDesde.Text = hoy.AddDays(-(dow - 1)).ToString("yyyy-MM-dd");
                        txtHasta.Text = hoy.ToString("yyyy-MM-dd");
                        break;
                    case "mes":
                        txtDesde.Text = new DateTime(hoy.Year, hoy.Month, 1).ToString("yyyy-MM-dd");
                        txtHasta.Text = hoy.ToString("yyyy-MM-dd");
                        break;
                    default: // hoy
                        txtDesde.Text = hoy.ToString("yyyy-MM-dd");
                        txtHasta.Text = hoy.ToString("yyyy-MM-dd");
                        break;
                }
            }

            // Siempre: leer el rango real desde los TextBox
            DateTime desde, hasta;
            if (!DateTime.TryParse(txtDesde.Text, out desde)) desde = hoy;
            if (!DateTime.TryParse(txtHasta.Text,  out hasta)) hasta = hoy;
            if (hasta < desde) hasta = desde;

            _desde     = desde;
            _hastaExcl = hasta.AddDays(1);

            // Label del período para los KPI cards
            switch (ddlPeriodo.SelectedValue)
            {
                case "semana":      _periodoLabel = "Esta Semana"; break;
                case "mes":        _periodoLabel = "Este Mes";    break;
                case "personalizado":
                    _periodoLabel = desde == hasta
                        ? desde.ToString("dd/MM/yyyy")
                        : desde.ToString("dd/MM/yyyy") + " – " + hasta.ToString("dd/MM/yyyy");
                    break;
                default: _periodoLabel = "Hoy"; break;
            }

            // Etiquetas de período en los KPI
            lblPeriodoA.Text   = _periodoLabel;
            lblPeriodoB.Text   = _periodoLabel;
            lblPeriodoC.Text   = _periodoLabel;
            lblPeriodoD.Text   = _periodoLabel;
            lblTituloProd.Text = _periodoLabel;
        }

        // ══ CARGA PRINCIPAL ═══════════════════════════════════════════════════
        private void CargarDashboard()
        {
            using (var db = NuevoDb(tracking: false))
            {
                CargarKpiInventario(db);
                CargarKpiProduccion(db);
                CargarKpiEntregas(db);
                CargarKpiCostoMargen(db);
                CargarKpiCriticos(db);
                CargarTablaProduccion(db);
                CargarTablaCriticos(db);
                CargarTablaEntregas(db);
                CargarTablaValorPorBase(db);
            }
        }

        // ── KPI 1: Valor inventario ───────────────────────────────────────────
        private void CargarKpiInventario(InventarioAnkhalDBDataContext db)
        {
            // Stock materiales filtrado por bases del usuario
            var smQ = from sm in db.StockMateriales
                      join m  in db.Materiales on sm.MaterialID equals m.MaterialID
                      join b  in db.Bases      on sm.BaseID      equals b.BaseID
                      where b.Activo && m.Activo
                      select new { b.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario };
            if (_basesUsuario != null) smQ = smQ.Where(x => _basesUsuario.Contains(x.BaseID));
            if (_baseID.HasValue)      smQ = smQ.Where(x => x.BaseID == _baseID.Value);
            decimal valMat = smQ.Sum(x => (decimal?)x.Valor) ?? 0m;

            // Stock productos
            var spQ = from sp in db.StockProductos
                      join p in db.Productos on sp.ProductoID equals p.ProductoID
                      join b in db.Bases     on sp.BaseID      equals b.BaseID
                      where b.Activo && p.Activo
                      select new { b.BaseID,
                                   ValBuenos  = sp.CantidadBuenas  * p.PrecioVenta,
                                   ValRechazo = sp.CantidadRechazo * (p.PrecioVenta * 0.5m) };
            if (_basesUsuario != null) spQ = spQ.Where(x => _basesUsuario.Contains(x.BaseID));
            if (_baseID.HasValue)      spQ = spQ.Where(x => x.BaseID == _baseID.Value);
            decimal valProd = spQ.Sum(x => (decimal?)(x.ValBuenos + x.ValRechazo)) ?? 0m;

            // Conteos
            int nMat  = db.Materiales.Count(m => m.Activo);
            int nProd = db.Productos .Count(p => p.Activo);

            lblValorInventario.Text = (valMat + valProd).ToString("N2");
            lblInvSub.Text = string.Format("{0} materiales  |  {1} productos activos", nMat, nProd);
        }

        // ── KPI 2: Producción del periodo ─────────────────────────────────────
        private void CargarKpiProduccion(InventarioAnkhalDBDataContext db)
        {
            var q = from p  in db.Produccion
                    join pr in db.Productos on p.ProductoID equals pr.ProductoID
                    join b  in db.Bases     on p.BaseID      equals b.BaseID
                    where p.Fecha >= _desde && p.Fecha < _hastaExcl
                    select new { b.BaseID, p.CantidadBuena, p.CantidadRechazo,
                                 Valor = p.CantidadBuena * pr.PrecioVenta };
            if (_basesUsuario != null) q = q.Where(x => _basesUsuario.Contains(x.BaseID));
            if (_baseID.HasValue)      q = q.Where(x => x.BaseID == _baseID.Value);

            var datos = q.ToList();
            decimal valProd     = datos.Sum(x => x.Valor);
            int     uBuenas     = datos.Sum(x => x.CantidadBuena);
            int     uRechazo    = datos.Sum(x => x.CantidadRechazo);

            lblValorProducido.Text = valProd.ToString("N2");
            lblProdSub.Text = string.Format("{0} uds. buenas  |  {1} uds. rechazo", uBuenas, uRechazo);
        }

        // ── KPI 3: Entregas del periodo ───────────────────────────────────────
        private void CargarKpiEntregas(InventarioAnkhalDBDataContext db)
        {
            var q = from e  in db.Entregas
                    join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                    join b  in db.Bases           on e.BaseOrigenID equals b.BaseID into bj
                    from b in bj.DefaultIfEmpty()
                    where e.Estado == "ENTREGADA"
                          && e.FechaEntrega >= _desde && e.FechaEntrega < _hastaExcl
                    select new { BaseID = (int?)b.BaseID, Total = de.Cantidad * de.PrecioUnitario,
                                 e.EntregaID };
            if (_basesUsuario != null) q = q.Where(x => x.BaseID == null || _basesUsuario.Contains((int)x.BaseID));
            if (_baseID.HasValue)      q = q.Where(x => x.BaseID == _baseID.Value);

            var datos = q.ToList();
            decimal valEnt  = datos.Sum(x => x.Total);
            int     nEnt    = datos.Select(x => x.EntregaID).Distinct().Count();

            lblValorEntregado.Text = valEnt.ToString("N2");
            lblEntSub.Text = string.Format("{0} entrega{1} completada{1}", nEnt, nEnt == 1 ? "" : "s");
        }

        // ── KPI 4 y 5: Costo y Margen ─────────────────────────────────────────
        private void CargarKpiCostoMargen(InventarioAnkhalDBDataContext db)
        {
            var q = from cp in db.ConsumosProduccion
                    join pr in db.Produccion on cp.ProduccionID equals pr.ProduccionID
                    join m  in db.Materiales on cp.MaterialID   equals m.MaterialID
                    join b  in db.Bases      on pr.BaseID        equals b.BaseID
                    where pr.Fecha >= _desde && pr.Fecha < _hastaExcl
                    select new { b.BaseID, Costo = cp.CantidadReal * m.PrecioUnitario };
            if (_basesUsuario != null) q = q.Where(x => _basesUsuario.Contains(x.BaseID));
            if (_baseID.HasValue)      q = q.Where(x => x.BaseID == _baseID.Value);
            decimal costoMat = q.Sum(x => (decimal?)x.Costo) ?? 0m;

            // Leer el valor entregado que ya se calculó (lo recalculamos ligero)
            var eq = from e  in db.Entregas
                     join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                     join b  in db.Bases on e.BaseOrigenID equals b.BaseID into bj
                     from b in bj.DefaultIfEmpty()
                     where e.Estado == "ENTREGADA"
                           && e.FechaEntrega >= _desde && e.FechaEntrega < _hastaExcl
                     select new { BaseID = (int?)b.BaseID, Total = de.Cantidad * de.PrecioUnitario };
            if (_basesUsuario != null) eq = eq.Where(x => x.BaseID == null || _basesUsuario.Contains((int)x.BaseID));
            if (_baseID.HasValue)      eq = eq.Where(x => x.BaseID == _baseID.Value);
            decimal valEntregado = eq.Sum(x => (decimal?)x.Total) ?? 0m;

            decimal margen = valEntregado - costoMat;
            decimal margenPct = valEntregado > 0 ? Math.Round(margen / valEntregado * 100, 1) : 0m;

            lblCostoMaterial.Text = costoMat.ToString("N2");
            lblMargenDia.Text     = margen.ToString("N2");
            lblMargenPct.Text     = margenPct.ToString("N1") + "% margen";

            // Color del card de margen
            if (margen < 0)
                pnlMargenCard.CssClass = "kpi-card margen-neg h-100";
            else
                pnlMargenCard.CssClass = "kpi-card margen h-100";
        }

        // ── KPI 6: Críticos (count) ───────────────────────────────────────────
        private void CargarKpiCriticos(InventarioAnkhalDBDataContext db)
        {
            var stockPorMat = (from sm in db.StockMateriales
                               join b in db.Bases on sm.BaseID equals b.BaseID
                               where b.Activo
                               group sm by sm.MaterialID into g
                               select new { MatID = g.Key, Total = g.Sum(s => s.CantidadActual) }).ToList();

            var mats = db.Materiales.Where(m => m.Activo).Select(m => new
                { m.MaterialID, m.StockMinimo }).ToList();

            int count = (from m in mats
                         join s in stockPorMat on m.MaterialID equals s.MatID into sg
                         let stock = sg.Any() ? sg.First().Total : 0m
                         where stock < m.StockMinimo
                         select m).Count();

            lblCriticosCount.Text      = count.ToString();
            lblCountCriticosPanel.Text = count.ToString();

            if (count == 0)
            {
                pnlAlertasCard.CssClass = "kpi-card alertas-ok h-100";
                lblCriticosSub.Text     = "Todos los materiales en nivel aceptable";
            }
            else
            {
                pnlAlertasCard.CssClass = "kpi-card alertas h-100";
                lblCriticosSub.Text     = string.Format("{0} material{1} necesita{2} reabastecimiento",
                                              count, count == 1 ? "" : "es", count == 1 ? "" : "n");
            }
        }

        // ══ TABLA: Producción agrupada por Base+Producto ══════════════════════
        private void CargarTablaProduccion(InventarioAnkhalDBDataContext db)
        {
            // Producción del período
            var prodQ = from p  in db.Produccion
                        join pr in db.Productos on p.ProductoID equals pr.ProductoID
                        join b  in db.Bases     on p.BaseID     equals b.BaseID
                        where p.Fecha >= _desde && p.Fecha < _hastaExcl
                        select new
                        {
                            b.BaseID, BaseNombre = b.Nombre,
                            p.ProduccionID,
                            ProductoNombre = pr.Descripcion,
                            p.CantidadBuena, p.CantidadRechazo,
                            Valor = p.CantidadBuena * pr.PrecioVenta
                        };
            if (_basesUsuario != null) prodQ = prodQ.Where(x => _basesUsuario.Contains(x.BaseID));
            if (_baseID.HasValue)      prodQ = prodQ.Where(x => x.BaseID == _baseID.Value);
            var prodData = prodQ.ToList();

            // Costos por ProduccionID
            var prodIDs = prodData.Select(p => p.ProduccionID).Distinct().ToList();
            var costosQ = from cp in db.ConsumosProduccion
                          join m  in db.Materiales on cp.MaterialID equals m.MaterialID
                          where prodIDs.Contains(cp.ProduccionID)
                          select new { cp.ProduccionID, Costo = cp.CantidadReal * m.PrecioUnitario };
            var costosPorProd = costosQ.ToList()
                .GroupBy(x => x.ProduccionID)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Costo));

            // Agrupar por Base + Producto en memoria
            var resumen = prodData
                .GroupBy(p => new { p.BaseID, p.BaseNombre, p.ProductoNombre })
                .Select(g =>
                {
                    var prodIDsGrupo = prodData
                        .Where(p => p.BaseID == g.Key.BaseID && p.ProductoNombre == g.Key.ProductoNombre)
                        .Select(p => p.ProduccionID).ToList();
                    decimal costoGrupo = prodIDsGrupo
                        .Sum(id => costosPorProd.ContainsKey(id) ? costosPorProd[id] : 0m);
                    return new ProdResumenVM
                    {
                        Base          = g.Key.BaseNombre,
                        Producto      = g.Key.ProductoNombre,
                        Buenos        = g.Sum(x => x.CantidadBuena),
                        Rechazo       = g.Sum(x => x.CantidadRechazo),
                        ValorProducido = g.Sum(x => x.Valor),
                        CostoMat      = costoGrupo
                    };
                })
                .OrderBy(r => r.Base).ThenBy(r => r.Producto)
                .ToList();

            // Totales para footer
            ViewState["ProdTotales"] = new decimal[]
            {
                resumen.Sum(r => r.ValorProducido),
                resumen.Sum(r => r.CostoMat),
                resumen.Sum(r => r.Margen)
            };

            lblTotalProdRows.Text   = resumen.Count.ToString();
            gvProduccion.DataSource = resumen;
            gvProduccion.DataBind();
        }

        protected void gvProduccion_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.Footer) return;
            var t = ViewState["ProdTotales"] as decimal[];
            if (t == null || t.Length < 3) return;
            e.Row.Cells[0].Text = "<strong>TOTAL</strong>";
            e.Row.Cells[1].Text = "";
            e.Row.Cells[2].Text = "";
            e.Row.Cells[3].Text = "";
            e.Row.Cells[4].Text = "<strong>" + t[0].ToString("C2") + "</strong>";
            e.Row.Cells[5].Text = t[1].ToString("C2");
            string color = t[2] >= 0 ? "text-success" : "text-danger";
            e.Row.Cells[6].Text = "<strong class='" + color + "'>" + t[2].ToString("C2") + "</strong>";
        }

        // ══ PANEL: Materiales críticos ════════════════════════════════════════
        private void CargarTablaCriticos(InventarioAnkhalDBDataContext db)
        {
            var stockPorMat = (from sm in db.StockMateriales
                               join b in db.Bases on sm.BaseID equals b.BaseID
                               where b.Activo
                               group sm by sm.MaterialID into g
                               select new { MatID = g.Key, Total = g.Sum(s => s.CantidadActual) }).ToList();

            var mats = db.Materiales.Where(m => m.Activo)
                .Select(m => new { m.MaterialID, m.Codigo, m.Descripcion, m.Unidad, m.StockMinimo }).ToList();

            var criticos = (from m in mats
                            join s in stockPorMat on m.MaterialID equals s.MatID into sg
                            let stock = sg.Any() ? sg.First().Total : 0m
                            where stock < m.StockMinimo
                            orderby (m.StockMinimo - stock) descending
                            select new
                            {
                                m.Codigo, m.Descripcion, m.Unidad,
                                StockActual = stock,
                                m.StockMinimo,
                                Deficit = m.StockMinimo - stock
                            }).ToList();

            if (criticos.Count == 0)
            {
                litCriticos.Text = "<div class='critico-vacio'><i class='fas fa-check-circle'></i> Sin alertas — todos los materiales tienen stock suficiente.</div>";
                return;
            }

            var sb = new StringBuilder();
            foreach (var c in criticos)
            {
                sb.Append("<div class='critico-item'>");
                sb.Append("<div>");
                sb.Append("<div class='mat-name'>" + System.Web.HttpUtility.HtmlEncode(c.Descripcion) + "</div>");
                sb.Append("<div class='mat-stock text-muted'>");
                sb.Append("Stock: <strong>" + c.StockActual.ToString("N2") + "</strong> " + System.Web.HttpUtility.HtmlEncode(c.Unidad));
                sb.Append(" &nbsp;/&nbsp; Min: " + c.StockMinimo.ToString("N2"));
                sb.Append("</div></div>");
                sb.Append("<div class='mat-deficit'>-" + c.Deficit.ToString("N2") + " " + System.Web.HttpUtility.HtmlEncode(c.Unidad) + "</div>");
                sb.Append("</div>");
            }
            litCriticos.Text = sb.ToString();
        }

        // ══ TABLA: Últimas 8 entregas ════════════════════════════════════════
        private void CargarTablaEntregas(InventarioAnkhalDBDataContext db)
        {
            // Últimas 8 entregas (sin filtro de periodo — siempre las más recientes)
            var entQ = (from e in db.Entregas
                        join b in db.Bases on e.BaseOrigenID equals b.BaseID into bj
                        from b in bj.DefaultIfEmpty()
                        join c in db.Clientes on e.ClienteID equals c.ClienteID into cj
                        from c in cj.DefaultIfEmpty()
                        orderby e.FechaEntrega descending
                        select new
                        {
                            e.EntregaID, e.Folio, e.FechaEntrega, e.Estado,
                            BaseNombre    = b != null ? b.Nombre : "-",
                            ClienteNombre = c != null ? c.Nombre : e.Cliente,
                            BaseID        = (int?)b.BaseID
                        });
            if (_basesUsuario != null) entQ = entQ.Where(x => x.BaseID == null || _basesUsuario.Contains((int)x.BaseID));
            if (_baseID.HasValue)      entQ = entQ.Where(x => x.BaseID == _baseID.Value);

            var entregas = entQ.Take(8).ToList();
            var ids = entregas.Select(e => e.EntregaID).ToList();

            // Totales por entrega
            var totales = (from de in db.DetalleEntregas
                           where ids.Contains(de.EntregaID)
                           group de by de.EntregaID into g
                           select new { EntregaID = g.Key, Total = g.Sum(d => d.Cantidad * d.PrecioUnitario) })
                          .ToDictionary(x => x.EntregaID, x => x.Total);

            var vm = entregas.Select(e => new EntregaDashVM
            {
                Folio   = e.Folio ?? e.EntregaID.ToString(),
                Cliente = e.ClienteNombre ?? "Sin cliente",
                Base    = e.BaseNombre,
                Total   = totales.ContainsKey(e.EntregaID) ? totales[e.EntregaID] : 0m,
                Estado  = e.Estado ?? "",
                Fecha   = e.FechaEntrega
            }).ToList();

            gvUltimasEntregas.DataSource = vm;
            gvUltimasEntregas.DataBind();
        }

        protected void gvEntregas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var lbl = e.Row.FindControl("lblEstado") as Label;
            if (lbl == null) return;
            switch (lbl.Text)
            {
                case "ENTREGADA":       lbl.Text = "<span class='badge-entregada'>Entregada</span>";  break;
                case "PROGRAMADA":      lbl.Text = "<span class='badge-programada'>Programada</span>"; break;
                case "CANCELADA":       lbl.Text = "<span class='badge-cancelada'>Cancelada</span>";  break;
                case "PENDIENTE_STOCK": lbl.Text = "<span class='badge-pendiente'>Sin Stock</span>";   break;
            }
        }

        // ══ TABLA: Valor inventario por base ══════════════════════════════════
        private void CargarTablaValorPorBase(InventarioAnkhalDBDataContext db)
        {
            var basesQ = db.Bases.Where(b => b.Activo).AsQueryable();
            if (_basesUsuario != null) basesQ = basesQ.Where(b => _basesUsuario.Contains(b.BaseID));
            if (_baseID.HasValue)      basesQ = basesQ.Where(b => b.BaseID == _baseID.Value);
            var bases = basesQ.OrderBy(b => b.Nombre).ToList();

            var smTodos = (from sm in db.StockMateriales
                           join m in db.Materiales on sm.MaterialID equals m.MaterialID
                           where m.Activo
                           select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario }).ToList();

            var spTodos = (from sp in db.StockProductos
                           join p in db.Productos on sp.ProductoID equals p.ProductoID
                           where p.Activo
                           select new
                           {
                               sp.BaseID,
                               Valor = sp.CantidadBuenas * p.PrecioVenta
                                     + sp.CantidadRechazo * (p.PrecioVenta * 0.5m)
                           }).ToList();

            var vm = bases.Select(b => new ValorBaseVM
            {
                Base   = b.Nombre,
                ValMat = smTodos.Where(s => s.BaseID == b.BaseID).Sum(s => s.Valor),
                ValProd = spTodos.Where(s => s.BaseID == b.BaseID).Sum(s => s.Valor)
            }).ToList();

            ViewState["ValBaseTotales"] = new decimal[]
            {
                vm.Sum(r => r.ValMat),
                vm.Sum(r => r.ValProd)
            };

            gvValorPorBase.DataSource = vm;
            gvValorPorBase.DataBind();
        }

        protected void gvValorBase_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.Footer) return;
            var t = ViewState["ValBaseTotales"] as decimal[];
            if (t == null || t.Length < 2) return;
            e.Row.Cells[0].Text = "<strong>TOTAL</strong>";
            e.Row.Cells[1].Text = t[0].ToString("C2");
            e.Row.Cells[2].Text = t[1].ToString("C2");
            e.Row.Cells[3].Text = "<strong>" + (t[0] + t[1]).ToString("C2") + "</strong>";
        }
    }
}
