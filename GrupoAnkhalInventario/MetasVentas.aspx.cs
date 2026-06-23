using System;
using System.Configuration;
using System.Linq;
using GrupoAnkhalInventario.Helpers;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario
{
    public partial class MetasVentas : System.Web.UI.Page
    {
        // ══ ViewModel ════════════════════════════════════════════════════════
        public class MetaBaseVM
        {
            public string  BaseNombre   { get; set; }
            public string  BaseTipo     { get; set; }
            public decimal MetaDiaria   { get; set; }   // Meta de ventas diaria fija de la base
            public decimal MetaPeriodo  { get; set; }   // MetaVentasDiaria × número de días
            public decimal ValorPeriodo { get; set; }   // Valor vendido en el período
            public int     CumplPct     { get; set; }
            public bool    Cumplio      { get; set; }
        }

        // ══ DB Factory ═══════════════════════════════════════════════════════
        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var cs = ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;
            var db = new InventarioAnkhalDBDataContext(cs);
            if (!tracking) db.ObjectTrackingEnabled = false;
            return db;
        }

        // ══ Page Load ════════════════════════════════════════════════════════
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }
            if (!IsPostBack)
            {
                using (var db = NuevoDb(false))
                {
                    var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
                    var basesQ = db.Bases.Where(b => b.Activo);
                    if (basesUsuario != null)
                        basesQ = basesQ.Where(b => basesUsuario.Contains(b.BaseID));

                    // Poblar checkboxes con los tipos distintos de bases activas
                    var tipos = basesQ
                        .Where(b => b.Tipo != null)
                        .Select(b => b.Tipo)
                        .Distinct()
                        .OrderBy(t => t)
                        .ToList();
                    foreach (var t in tipos)
                        cblFiltrTipo.Items.Add(new System.Web.UI.WebControls.ListItem(t, t));

                    // Poblar dropdown de base específica
                    var bases = basesQ.OrderBy(b => b.Nombre)
                        .Select(b => new { b.BaseID, b.Nombre })
                        .ToList();
                    foreach (var b in bases)
                        ddlFiltrBase.Items.Add(new System.Web.UI.WebControls.ListItem(b.Nombre, b.BaseID.ToString()));
                }

                string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
                txtDesde.Text = hoy;
                txtHasta.Text = hoy;
                Cargar();
            }
        }

        // ══ Botones filtro ═══════════════════════════════════════════════════
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            Cargar();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            foreach (System.Web.UI.WebControls.ListItem li in cblFiltrTipo.Items) li.Selected = false;
            ddlFiltrBase.SelectedIndex = 0;
            string hoy = AppHelper.Hoy.ToString("yyyy-MM-dd");
            txtDesde.Text = hoy;
            txtHasta.Text = hoy;
            Cargar();
        }

        // ══ Orquestador ══════════════════════════════════════════════════════
        private void Cargar()
        {
            DateTime desde, hasta;
            if (!DateTime.TryParse(txtDesde.Text, out desde)) desde = AppHelper.Hoy;
            if (!DateTime.TryParse(txtHasta.Text, out hasta)) hasta = AppHelper.Hoy;
            if (hasta < desde) hasta = desde;

            int numDias = (hasta - desde).Days + 1;

            var selTipos = cblFiltrTipo.Items.Cast<System.Web.UI.WebControls.ListItem>()
                .Where(li => li.Selected)
                .Select(li => li.Value)
                .ToList();

            int? selBase = null;
            if (!string.IsNullOrEmpty(ddlFiltrBase.SelectedValue))
                selBase = int.Parse(ddlFiltrBase.SelectedValue);

            // Etiquetas del período
            string periodoStr = desde == hasta
                ? desde.ToString("dd/MM/yyyy")
                : desde.ToString("dd/MM/yyyy") + " al " + hasta.ToString("dd/MM/yyyy");
            lblPeriodo.Text  = periodoStr;
            lblNumDias.Text  = numDias == 1 ? "1 d&iacute;a" : numDias + " d&iacute;as";
            lblCardMeta.Text = numDias == 1
                ? "META DIARIA &mdash; ANKHAL"
                : "META DEL PER&Iacute;ODO &mdash; ANKHAL (" + numDias + " d&iacute;as)";

            CargarDashboard(desde, hasta, numDias, selTipos, selBase);
            CargarMetaBases(desde, hasta, numDias, selTipos, selBase);
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard(DateTime desde, DateTime hasta, int numDias,
            System.Collections.Generic.List<string> selTipos, int? selBase)
        {
            using (var db = NuevoDb(false))
            {
                // Bases activas aplicando filtro de tipo, base específica y permisos del usuario
                var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
                var basesQ = db.Bases.Where(b => b.Activo);
                if (basesUsuario != null)
                    basesQ = basesQ.Where(b => basesUsuario.Contains(b.BaseID));
                if (selBase.HasValue)
                    basesQ = basesQ.Where(b => b.BaseID == selBase.Value);
                else if (selTipos.Any())
                    basesQ = basesQ.Where(b => selTipos.Contains(b.Tipo));

                var baseIds = basesQ.Select(b => b.BaseID).ToList();

                decimal metaDiaria = basesQ.Sum(b => (decimal?)b.MetaVentasDiaria) ?? 0m;
                decimal metaPeriodo = metaDiaria * numDias;

                // Valor vendido en el período (solo bases filtradas), vía join Entregas + DetalleEntregas
                var valorRaw = (from e in db.Entregas
                                join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                where e.FechaEntrega >= desde && e.FechaEntrega <= hasta
                                      && baseIds.Contains(e.BaseOrigenID)
                                      && e.Estado == "ENTREGADA"
                                select de.Cantidad * de.PrecioUnitario).ToList();
                decimal valorPeriodo = valorRaw.Any() ? valorRaw.Sum() : 0m;

                int cumplPct = metaPeriodo > 0
                    ? (int)Math.Round((double)valorPeriodo / (double)metaPeriodo * 100)
                    : 0;

                lblMetaTotal.Text    = metaPeriodo.ToString("$#,##0.00");
                lblValorVendido.Text = valorPeriodo.ToString("$#,##0.00");
                lblCumplimiento.Text = cumplPct.ToString() + "%";
            }
        }

        // ══ Tabla por base ═══════════════════════════════════════════════════
        private void CargarMetaBases(DateTime desde, DateTime hasta, int numDias,
            System.Collections.Generic.List<string> selTipos, int? selBase)
        {
            using (var db = NuevoDb(false))
            {
                // Bases activas aplicando filtro de tipo, base específica y permisos del usuario
                var basesUsuario = AppHelper.ObtenerBasesUsuario(Session);
                var basesQ = db.Bases.Where(b => b.Activo);
                if (basesUsuario != null)
                    basesQ = basesQ.Where(b => basesUsuario.Contains(b.BaseID));
                if (selBase.HasValue)
                    basesQ = basesQ.Where(b => b.BaseID == selBase.Value);
                else if (selTipos.Any())
                    basesQ = basesQ.Where(b => selTipos.Contains(b.Tipo));
                var bases = basesQ.OrderBy(b => b.Nombre).ToList();

                var baseIds = bases.Select(b => b.BaseID).ToList();

                // Valor vendido en el período agrupado por base (solo bases filtradas)
                var valorPorBase = (from e in db.Entregas
                                    join de in db.DetalleEntregas on e.EntregaID equals de.EntregaID
                                    where e.FechaEntrega >= desde && e.FechaEntrega <= hasta
                                          && baseIds.Contains(e.BaseOrigenID)
                                          && e.Estado == "ENTREGADA"
                                    group de.Cantidad * de.PrecioUnitario by e.BaseOrigenID into g
                                    select new { BaseID = g.Key, Valor = g.Sum() }).ToList();

                var lista = bases.Select(b =>
                {
                    decimal valor      = valorPorBase
                        .Where(v => v.BaseID == b.BaseID)
                        .Select(v => v.Valor)
                        .FirstOrDefault();
                    decimal metaPeriodo = b.MetaVentasDiaria * numDias;
                    int pct = metaPeriodo > 0
                        ? (int)Math.Round((double)valor / (double)metaPeriodo * 100)
                        : 0;
                    return new MetaBaseVM
                    {
                        BaseNombre   = b.Nombre,
                        BaseTipo     = b.Tipo ?? "",
                        MetaDiaria   = b.MetaVentasDiaria,
                        MetaPeriodo  = metaPeriodo,
                        ValorPeriodo = valor,
                        CumplPct     = pct,
                        Cumplio      = metaPeriodo > 0 && valor >= metaPeriodo
                    };
                }).ToList();

                rptMetaBases.DataSource = lista;
                rptMetaBases.DataBind();
            }
        }
    }
}
