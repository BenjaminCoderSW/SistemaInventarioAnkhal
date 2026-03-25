using System;
using System.Configuration;
using System.Linq;
using GrupoAnkhalInventario.Modelo;

namespace GrupoAnkhalInventario
{
    public partial class MetasProduccion : System.Web.UI.Page
    {
        // ══ ViewModel ════════════════════════════════════════════════════════
        public class MetaBaseVM
        {
            public string  BaseNombre   { get; set; }
            public string  BaseTipo     { get; set; }
            public decimal MetaDiaria   { get; set; }   // Meta diaria fija de la base
            public decimal MetaPeriodo  { get; set; }   // MetaDiaria × número de días
            public decimal ValorPeriodo { get; set; }   // Valor producido en el período
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
                string hoy = DateTime.Today.ToString("yyyy-MM-dd");
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
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
            txtDesde.Text = hoy;
            txtHasta.Text = hoy;
            Cargar();
        }

        // ══ Orquestador ══════════════════════════════════════════════════════
        private void Cargar()
        {
            DateTime desde, hasta;
            if (!DateTime.TryParse(txtDesde.Text, out desde)) desde = DateTime.Today;
            if (!DateTime.TryParse(txtHasta.Text, out hasta)) hasta = DateTime.Today;
            if (hasta < desde) hasta = desde;

            int numDias = (hasta - desde).Days + 1;

            // Etiquetas del período
            string periodoStr = desde == hasta
                ? desde.ToString("dd/MM/yyyy")
                : desde.ToString("dd/MM/yyyy") + " al " + hasta.ToString("dd/MM/yyyy");
            lblPeriodo.Text  = periodoStr;
            lblNumDias.Text  = numDias == 1 ? "1 d&iacute;a" : numDias + " d&iacute;as";
            lblCardMeta.Text = numDias == 1
                ? "META DIARIA &mdash; ANKHAL"
                : "META DEL PER&Iacute;ODO &mdash; ANKHAL (" + numDias + " d&iacute;as)";

            CargarDashboard(desde, hasta, numDias);
            CargarMetaBases(desde, hasta, numDias);
        }

        // ══ Dashboard ════════════════════════════════════════════════════════
        private void CargarDashboard(DateTime desde, DateTime hasta, int numDias)
        {
            using (var db = NuevoDb(false))
            {
                // Meta total del período = MetaDiaria × días (suma de todas las bases activas)
                decimal metaDiaria = db.Bases
                    .Where(b => b.Activo)
                    .Sum(b => (decimal?)b.MetaDiaria) ?? 0m;
                decimal metaPeriodo = metaDiaria * numDias;

                // Valor producido en el período
                var valorRaw = (from p  in db.Produccion
                                join pr in db.Productos on p.ProductoID equals pr.ProductoID
                                where p.Fecha >= desde && p.Fecha <= hasta
                                select p.CantidadBuena * pr.PrecioVenta).ToList();
                decimal valorPeriodo = valorRaw.Any() ? valorRaw.Sum() : 0m;

                int cumplPct = metaPeriodo > 0
                    ? (int)Math.Round((double)valorPeriodo / (double)metaPeriodo * 100)
                    : 0;

                lblMetaTotal.Text    = metaPeriodo.ToString("$#,##0.00");
                lblCumplimiento.Text = cumplPct.ToString() + "%";
            }
        }

        // ══ Tabla por base ═══════════════════════════════════════════════════
        private void CargarMetaBases(DateTime desde, DateTime hasta, int numDias)
        {
            using (var db = NuevoDb(false))
            {
                // Valor producido en el período agrupado por base
                var valorPorBase = (from p  in db.Produccion
                                    join pr in db.Productos on p.ProductoID equals pr.ProductoID
                                    where p.Fecha >= desde && p.Fecha <= hasta
                                    group p.CantidadBuena * pr.PrecioVenta by p.BaseID into g
                                    select new { BaseID = g.Key, Valor = g.Sum() }).ToList();

                // Todas las bases activas
                var bases = db.Bases.Where(b => b.Activo).OrderBy(b => b.Nombre).ToList();

                var lista = bases.Select(b =>
                {
                    decimal valor      = valorPorBase
                        .Where(v => v.BaseID == b.BaseID)
                        .Select(v => v.Valor)
                        .FirstOrDefault();
                    decimal metaPeriodo = b.MetaDiaria * numDias;
                    int pct = metaPeriodo > 0
                        ? (int)Math.Round((double)valor / (double)metaPeriodo * 100)
                        : 0;
                    return new MetaBaseVM
                    {
                        BaseNombre   = b.Nombre,
                        BaseTipo     = b.Tipo ?? "",
                        MetaDiaria   = b.MetaDiaria,
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
